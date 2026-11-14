using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ServiceRequests.Core;
using ServiceRequests.Worker;

namespace ServiceRequests.Tests.Worker;

/// <summary>
/// Processor behavior with the repository and provider mocked and the real retry pipeline (2 retries, no delay).
/// </summary>
public class ServiceRequestProcessorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 15, 0, 0, TimeSpan.Zero);

    private readonly Mock<IServiceRequestRepository> _repository = new();
    private readonly Mock<IExternalProvider> _provider = new();
    private readonly List<ServiceRequestStatus> _persistedStatuses = [];
    private readonly ServiceRequestProcessor _processor;

    public ServiceRequestProcessorTests()
    {
        _repository.Setup(r => r.UpdateAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceRequest, CancellationToken>((request, _) => _persistedStatuses.Add(request.Status))
            .Returns(Task.CompletedTask);

        _processor = new ServiceRequestProcessor(
            _repository.Object,
            _provider.Object,
            ServiceRequestProcessor.CreateProviderPipeline(maxRetryAttempts: 2, TimeSpan.Zero),
            new FixedTimeProvider(Now),
            NullLogger<ServiceRequestProcessor>.Instance);
    }

    [Fact]
    public async Task NextPending_ProviderSucceeds_PersistsProcessingBeforeCallThenCompleted()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _provider.Setup(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()))
            .Callback(() => Assert.Equal([ServiceRequestStatus.Processing], _persistedStatuses))
            .Returns(Task.CompletedTask);

        var processed = await _processor.ProcessNextPendingAsync(CancellationToken.None);

        Assert.True(processed);
        Assert.Equal([ServiceRequestStatus.Processing, ServiceRequestStatus.Completed], _persistedStatuses);
        Assert.Equal(Now, request.UpdatedAt);
        Assert.Equal(Now.AddMinutes(-1), request.CreatedAt);
    }

    [Fact]
    public async Task NextPending_NoneAvailable_ReturnsFalseWithoutCallingProvider()
    {
        _repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ServiceRequest?)null);

        Assert.False(await _processor.ProcessNextPendingAsync(CancellationToken.None));
        _provider.Verify(p => p.ProcessAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransientFailureTwiceThenSuccess_RetriesAndCompletesAfterThreeCalls()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _provider.SetupSequence(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientProviderException("busy"))
            .ThrowsAsync(new TransientProviderException("busy"))
            .Returns(Task.CompletedTask);

        await _processor.ProcessNextPendingAsync(CancellationToken.None);

        _provider.Verify(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(3));
        Assert.Equal(ServiceRequestStatus.Completed, request.Status);
        Assert.Equal(Now.AddMinutes(-1), request.CreatedAt);
        Assert.Equal(Now, request.UpdatedAt);
        Assert.Equal([ServiceRequestStatus.Processing, ServiceRequestStatus.Completed], _persistedStatuses);
    }

    [Fact]
    public async Task ClaimCannotBePersisted_ProviderIsNeverCalled()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _repository.Setup(r => r.UpdateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _processor.ProcessNextPendingAsync(CancellationToken.None));

        _provider.Verify(p => p.ProcessAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancellationDuringProviderCall_IsNotRetriedAndDoesNotFailTheRequest()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _provider.Setup(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _processor.ProcessNextPendingAsync(CancellationToken.None));

        _provider.Verify(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(ServiceRequestStatus.Processing, request.Status);
        Assert.Equal([ServiceRequestStatus.Processing], _persistedStatuses);
    }

    [Fact]
    public async Task TransientFailureAlways_FailsAfterThreeCallsWithReason()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _provider.Setup(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientProviderException("busy"));

        await _processor.ProcessNextPendingAsync(CancellationToken.None);

        _provider.Verify(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(3));
        Assert.Equal(ServiceRequestStatus.Failed, request.Status);
        Assert.Contains("3 attempts", request.FailureReason);
        Assert.Equal([ServiceRequestStatus.Processing, ServiceRequestStatus.Failed], _persistedStatuses);
    }

    [Fact]
    public async Task PermanentFailure_FailsAfterSingleCall()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _provider.Setup(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PermanentProviderException("unsupported service"));

        await _processor.ProcessNextPendingAsync(CancellationToken.None);

        _provider.Verify(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(ServiceRequestStatus.Failed, request.Status);
        Assert.Contains("unsupported service", request.FailureReason);
    }

    [Fact]
    public async Task UnexpectedException_IsNotRetriedPropagatesAndLeavesRequestProcessing()
    {
        var request = PendingRequest();
        _repository.Setup(r => r.FindNextPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(request);
        _provider.Setup(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bug"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _processor.ProcessNextPendingAsync(CancellationToken.None));

        _provider.Verify(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(ServiceRequestStatus.Processing, request.Status);
        Assert.Equal([ServiceRequestStatus.Processing], _persistedStatuses);
    }

    [Fact]
    public async Task Resume_ProcessingRequest_CallsProviderWithoutClaimingAgain()
    {
        var request = PendingRequest();
        request.MarkProcessing(Now.AddSeconds(-30));
        _repository.Setup(r => r.FindByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        var resumed = await _processor.ResumeProcessingAsync(request.Id, CancellationToken.None);

        Assert.True(resumed);
        Assert.Equal(ServiceRequestStatus.Completed, request.Status);
        Assert.Equal([ServiceRequestStatus.Completed], _persistedStatuses);
        _provider.Verify(p => p.ProcessAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ServiceRequestStatus.Pending)]
    [InlineData(ServiceRequestStatus.Completed)]
    public async Task Resume_RequestNotProcessing_DoesNothing(ServiceRequestStatus status)
    {
        var request = PendingRequest();
        if (status == ServiceRequestStatus.Completed)
        {
            request.MarkProcessing(Now);
            request.Complete(Now);
        }

        _repository.Setup(r => r.FindByIdAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(request);

        Assert.False(await _processor.ResumeProcessingAsync(request.Id, CancellationToken.None));
        _provider.Verify(p => p.ProcessAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_persistedStatuses);
    }

    private static ServiceRequest PendingRequest() =>
        new($"REQ-{Guid.NewGuid():N}", "CUST-01", "INST", "Install service", Now.AddMinutes(-1));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
