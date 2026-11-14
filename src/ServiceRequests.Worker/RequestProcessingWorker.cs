using Microsoft.Extensions.Options;
using ServiceRequests.Core;

namespace ServiceRequests.Worker;

/// <summary>
/// Hosted loop (single instance by assumption A-04): resumes requests left in Processing, then processes Pending
/// requests one at a time, waiting <see cref="WorkerOptions.PollingIntervalMilliseconds"/> when there is nothing to do.
/// Each unit of work runs in its own DI scope (fresh DbContext); a failure is logged and the loop continues.
/// </summary>
public sealed class RequestProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<RequestProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RecoverProcessingAsync(stoppingToken);

            var pollingInterval = TimeSpan.FromMilliseconds(options.Value.PollingIntervalMilliseconds);
            while (!stoppingToken.IsCancellationRequested)
            {
                var processed = false;
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    processed = await scope.ServiceProvider.GetRequiredService<ServiceRequestProcessor>()
                        .ProcessNextPendingAsync(stoppingToken);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception,
                        "Unexpected error while processing a request; a claimed request stays Processing until the Worker restarts");
                }

                if (!processed)
                {
                    await Task.Delay(pollingInterval, timeProvider, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task RecoverProcessingAsync(CancellationToken stoppingToken)
    {
        IReadOnlyList<Guid> orphaned;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IServiceRequestRepository>();
            orphaned = (await repository.ListAsync(ServiceRequestStatus.Processing, stoppingToken))
                .Select(r => r.Id)
                .ToList();
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            // Do not stop the host (e.g. database unavailable at startup); polling continues and retries each interval.
            logger.LogError(exception, "Startup recovery could not list Processing requests; continuing with polling");
            return;
        }

        if (orphaned.Count == 0)
        {
            return;
        }

        logger.LogInformation("Startup recovery: {Count} request(s) found in Processing", orphaned.Count);
        foreach (var id in orphaned)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ServiceRequestProcessor>()
                    .ResumeProcessingAsync(id, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Startup recovery failed for request {RequestId}; it stays Processing", id);
            }
        }
    }
}
