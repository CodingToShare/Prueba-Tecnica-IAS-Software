using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ServiceRequests.Core;
using ServiceRequests.Worker;

namespace ServiceRequests.Tests.Worker;

/// <summary>Worker loop behavior with mocked repository/provider and a 1 ms polling interval.</summary>
public class RequestProcessingWorkerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StartupRecoveryListFails_IsRetriedAndSucceedsBeforeAnyPendingIsPolled()
    {
        var events = new List<string>();
        var orphan = new ServiceRequest("ORPHAN-1", "CUST-01", "INST", "claimed before crash", Now);
        orphan.MarkProcessing(Now);
        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var repository = new Mock<IServiceRequestRepository>();
        repository.SetupSequence(r => r.ListAsync(ServiceRequestStatus.Processing, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"))
            .ThrowsAsync(new InvalidOperationException("database unavailable"))
            .ReturnsAsync([orphan]);
        repository.Setup(r => r.FindByIdAsync(orphan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(orphan);
        repository.Setup(r => r.UpdateAsync(orphan, It.IsAny<CancellationToken>()))
            .Callback(() => { events.Add($"saved {orphan.Status}"); recovered.TrySetResult(); })
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>()))
            .Callback(() => events.Add("poll pending"))
            .ReturnsAsync((ServiceRequest?)null);

        var provider = new Mock<IExternalProvider>();
        using var services = BuildServices(repository.Object, provider.Object);
        var worker = services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().Single();

        await worker.StartAsync(CancellationToken.None);
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        repository.Verify(r => r.ListAsync(ServiceRequestStatus.Processing, It.IsAny<CancellationToken>()), Times.Exactly(3));
        Assert.Equal("saved Completed", events[0]);
        Assert.Equal(ServiceRequestStatus.Completed, orphan.Status);
        provider.Verify(p => p.ProcessAsync(orphan, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnexpectedErrorResumingOneRequest_OtherRequestsAreStillRecovered()
    {
        var broken = new ServiceRequest("ORPHAN-A", "CUST-01", "INST", "a", Now);
        var healthy = new ServiceRequest("ORPHAN-B", "CUST-01", "INST", "b", Now.AddSeconds(1));
        broken.MarkProcessing(Now);
        healthy.MarkProcessing(Now);
        var healthySaved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var repository = new Mock<IServiceRequestRepository>();
        repository.Setup(r => r.ListAsync(ServiceRequestStatus.Processing, It.IsAny<CancellationToken>()))
            .ReturnsAsync([healthy, broken]);
        repository.Setup(r => r.FindByIdAsync(broken.Id, It.IsAny<CancellationToken>())).ReturnsAsync(broken);
        repository.Setup(r => r.FindByIdAsync(healthy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(healthy);
        repository.Setup(r => r.UpdateAsync(healthy, It.IsAny<CancellationToken>()))
            .Callback(() => healthySaved.TrySetResult())
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ServiceRequest?)null);

        var provider = new Mock<IExternalProvider>();
        provider.Setup(p => p.ProcessAsync(broken, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bug"));
        using var services = BuildServices(repository.Object, provider.Object);
        var worker = services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().Single();

        await worker.StartAsync(CancellationToken.None);
        await healthySaved.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(ServiceRequestStatus.Processing, broken.Status);
        Assert.Equal(ServiceRequestStatus.Completed, healthy.Status);
    }

    private static ServiceProvider BuildServices(IServiceRequestRepository repository, IExternalProvider provider) =>
        new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(TimeProvider.System)
            .AddSingleton(Options.Create(new WorkerOptions { PollingIntervalMilliseconds = 1 }))
            .AddSingleton(provider)
            .AddSingleton(ServiceRequestProcessor.CreateProviderPipeline(2, TimeSpan.Zero))
            .AddScoped(_ => repository)
            .AddScoped<ServiceRequestProcessor>()
            .AddHostedService<RequestProcessingWorker>()
            .BuildServiceProvider();
}
