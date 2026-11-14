using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceRequests.Core;
using ServiceRequests.Infrastructure.Provider;
using ServiceRequests.Tests.Api;
using ServiceRequests.Tests.Persistence;
using ServiceRequests.Tests.TestSupport;
using ServiceRequests.Worker;

namespace ServiceRequests.Tests.Worker;

/// <summary>
/// SQL Server integration (processor + real repository) and functional processing (API → Worker host → GET)
/// using the production composition of the Worker and the deterministic simulated provider.
/// </summary>
[Collection(WorkerDbCollection.Name)]
public sealed class WorkerProcessingTests(LocalDbFixture database)
{
    private static readonly DateTimeOffset CreatedLongAgo = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Processor_OnLocalDb_CompletesPendingRequestAndPersistsTimestamps()
    {
        var request = await InsertPendingAsync("INST");
        await using var services = WorkerServices.Build(database.ConnectionString, transientFailuresBeforeSuccess: 1);

        await DrainPendingAsync(services);

        var stored = await LoadAsync(request.Id);
        Assert.Equal(ServiceRequestStatus.Completed, stored.Status);
        Assert.Equal(CreatedLongAgo, stored.CreatedAt);
        Assert.True(stored.UpdatedAt > request.UpdatedAt);
        Assert.Null(stored.FailureReason);
    }

    [Fact]
    public async Task Processor_OnLocalDb_ExhaustedTransientFailuresPersistFailedWithReason()
    {
        var request = await InsertPendingAsync("INST");
        await using var services = WorkerServices.Build(database.ConnectionString, transientFailuresBeforeSuccess: 5);

        await DrainPendingAsync(services);

        var stored = await LoadAsync(request.Id);
        Assert.Equal(ServiceRequestStatus.Failed, stored.Status);
        Assert.Contains("after 3 attempts", stored.FailureReason);
        Assert.True(stored.UpdatedAt > request.UpdatedAt);
        Assert.Equal(0, ((SimulatedExternalProvider)services.GetRequiredService<IExternalProvider>())
            .GetEffectCount(request.ExternalReference));
    }

    [Fact]
    public async Task Processor_OnLocalDb_ResumesProcessingRowWithoutReclaiming()
    {
        var request = new ServiceRequest($"WRK-{Guid.NewGuid():N}", "CUST-01", "INST", "claimed before crash", CreatedLongAgo);
        request.MarkProcessing(CreatedLongAgo.AddMinutes(1));
        await using (var context = database.CreateContext())
        {
            context.ServiceRequests.Add(request);
            await context.SaveChangesAsync();
        }

        await using var services = WorkerServices.Build(database.ConnectionString, transientFailuresBeforeSuccess: 0);
        await using (var scope = services.CreateAsyncScope())
        {
            Assert.True(await scope.ServiceProvider.GetRequiredService<ServiceRequestProcessor>()
                .ResumeProcessingAsync(request.Id, CancellationToken.None));
        }

        Assert.Equal(ServiceRequestStatus.Completed, (await LoadAsync(request.Id)).Status);
    }

    [Fact]
    public async Task ApiCreate_WorkerHostRetriesTransientFailures_GetObservesCompleted()
    {
        var logs = new CapturingLoggerProvider();
        await using var api = new ApiFactory(database.ConnectionString);
        using var client = api.CreateClient();
        var reference = $"E2E-{Guid.NewGuid():N}";

        var created = await client.PostAsJsonAsync("/api/requests",
            new { externalReference = reference, customerId = "CUST-01", serviceCode = "INST", description = "Install" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("Pending", (await created.Content.ReadFromJsonAsync<RequestBody>(Json))!.Status);

        await using var services = WorkerServices.Build(database.ConnectionString, transientFailuresBeforeSuccess: 2, logs);
        var worker = services.GetServices<IHostedService>().Single();
        await worker.StartAsync(CancellationToken.None);
        var observed = await WaitForStatusAsync(client, reference, "Completed");
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal("Completed", observed.Status);
        Assert.True(observed.UpdatedAt > observed.CreatedAt);
        var retries = logs.Entries.Count(e =>
            e.Level == LogLevel.Warning && e.Message.Contains($"Transient provider failure for {reference}"));
        Assert.Equal(2, retries);
        Assert.Contains(logs.Entries, e => e.Message.Contains($"Completed {reference} after 3 attempt(s)"));
        Assert.Equal(1, ((SimulatedExternalProvider)services.GetRequiredService<IExternalProvider>()).GetEffectCount(reference));
    }

    [Fact]
    public async Task ApiCreate_UnsupportedService_WorkerHostFailsOnce_GetObservesFailed()
    {
        await using var api = new ApiFactory(database.ConnectionString);
        using var client = api.CreateClient();
        var reference = $"E2E-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync("/api/requests",
            new { externalReference = reference, customerId = "CUST-01", serviceCode = "UNSUPPORTED", description = "Install" });

        await using var services = WorkerServices.Build(database.ConnectionString, transientFailuresBeforeSuccess: 0);
        var worker = services.GetServices<IHostedService>().Single();
        await worker.StartAsync(CancellationToken.None);
        var observed = await WaitForStatusAsync(client, reference, "Failed");
        await worker.StopAsync(CancellationToken.None);

        Assert.Contains("not supported", observed.FailureReason);
    }

    private async Task<ServiceRequest> InsertPendingAsync(string serviceCode)
    {
        var request = new ServiceRequest($"WRK-{Guid.NewGuid():N}", "CUST-01", serviceCode, "Install", CreatedLongAgo);
        await using var context = database.CreateContext();
        context.ServiceRequests.Add(request);
        await context.SaveChangesAsync();
        return request;
    }

    private async Task<ServiceRequest> LoadAsync(Guid id)
    {
        await using var context = database.CreateContext();
        return await context.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == id);
    }

    private static async Task DrainPendingAsync(IServiceProvider services)
    {
        for (var i = 0; i < 50; i++)
        {
            await using var scope = services.CreateAsyncScope();
            if (!await scope.ServiceProvider.GetRequiredService<ServiceRequestProcessor>()
                    .ProcessNextPendingAsync(CancellationToken.None))
            {
                return;
            }
        }

        throw new InvalidOperationException("Pending requests were not drained.");
    }

    // Waits on an observable condition (bounded), not a fixed sleep.
    private static async Task<RequestBody> WaitForStatusAsync(HttpClient client, string reference, string status)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (true)
        {
            var list = await client.GetFromJsonAsync<List<RequestBody>>($"/api/requests?status={status}", Json, timeout.Token);
            var match = list!.SingleOrDefault(r => r.ExternalReference == reference);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
        }
    }

    private sealed record RequestBody(
        string ExternalReference, string Status, string? FailureReason, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
}
