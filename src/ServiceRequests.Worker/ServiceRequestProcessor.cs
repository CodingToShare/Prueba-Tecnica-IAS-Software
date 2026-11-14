using Polly;
using Polly.Retry;
using ServiceRequests.Core;

namespace ServiceRequests.Worker;

/// <summary>
/// Processes one service request against the external provider: claim (Pending → Processing, persisted before the
/// provider call), provider call through the retry pipeline, final state (Completed / Failed) persisted.
/// Only modeled provider failures become <c>Failed</c>; unexpected exceptions propagate and leave the request in
/// <c>Processing</c> so it is resumed when the Worker starts again.
/// </summary>
public sealed class ServiceRequestProcessor(
    IServiceRequestRepository repository,
    IExternalProvider provider,
    ResiliencePipeline providerPipeline,
    TimeProvider timeProvider,
    ILogger<ServiceRequestProcessor> logger)
{
    /// <summary>Retry only <see cref="TransientProviderException"/>, a bounded number of times with a constant delay.</summary>
    public static ResiliencePipeline CreateProviderPipeline(int maxRetryAttempts, TimeSpan delay) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TransientProviderException>(),
                MaxRetryAttempts = maxRetryAttempts,
                Delay = delay,
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false
            })
            .Build();

    /// <summary>Claims and processes the oldest Pending request. Returns <c>false</c> when there is none.</summary>
    public async Task<bool> ProcessNextPendingAsync(CancellationToken cancellationToken)
    {
        var request = await repository.FindNextPendingAsync(cancellationToken);
        if (request is null)
        {
            return false;
        }

        request.MarkProcessing(timeProvider.GetUtcNow());
        await repository.UpdateAsync(request, cancellationToken);
        logger.LogInformation("Claimed {ExternalReference}; status {Status}", request.ExternalReference, request.Status);

        await CallProviderAndCompleteAsync(request, cancellationToken);
        return true;
    }

    /// <summary>
    /// Resumes a request already in Processing (orphaned by an interrupted run) from the provider call onward,
    /// without claiming it again. Returns <c>false</c> if it no longer exists or is not Processing.
    /// </summary>
    public async Task<bool> ResumeProcessingAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await repository.FindByIdAsync(id, cancellationToken);
        if (request is not { Status: ServiceRequestStatus.Processing })
        {
            return false;
        }

        logger.LogInformation("Resuming {ExternalReference} left in {Status}", request.ExternalReference, request.Status);
        await CallProviderAndCompleteAsync(request, cancellationToken);
        return true;
    }

    private async Task CallProviderAndCompleteAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        var attempt = 0;
        try
        {
            await providerPipeline.ExecuteAsync(async token =>
            {
                attempt++;
                try
                {
                    await provider.ProcessAsync(request, token);
                }
                catch (TransientProviderException exception)
                {
                    logger.LogWarning("Transient provider failure for {ExternalReference} on attempt {Attempt}: {Reason}",
                        request.ExternalReference, attempt, exception.Message);
                    throw;
                }
            }, cancellationToken);

            request.Complete(timeProvider.GetUtcNow());
            logger.LogInformation("Completed {ExternalReference} after {Attempt} attempt(s); status {Status}",
                request.ExternalReference, attempt, request.Status);
        }
        catch (TransientProviderException exception)
        {
            request.Fail($"Transient provider failures exhausted after {attempt} attempts: {exception.Message}",
                timeProvider.GetUtcNow());
            logger.LogWarning("Failed {ExternalReference} after {Attempt} attempts; status {Status}",
                request.ExternalReference, attempt, request.Status);
        }
        catch (PermanentProviderException exception)
        {
            request.Fail($"Provider rejected the request: {exception.Message}", timeProvider.GetUtcNow());
            logger.LogWarning("Failed {ExternalReference}: permanent provider failure; status {Status}",
                request.ExternalReference, request.Status);
        }

        // The provider outcome already exists, so record it even if shutdown was requested meanwhile; cancelling this
        // write would leave the request Processing and cause a second provider call after restart.
        await repository.UpdateAsync(request, CancellationToken.None);
    }
}
