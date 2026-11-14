namespace ServiceRequests.Worker;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>Wait between polls when no Pending request is found.</summary>
    public int PollingIntervalMilliseconds { get; init; } = 1000;

    /// <summary>Retries after the first provider call for modeled transient failures (2 → at most 3 calls).</summary>
    public int MaxRetryAttempts { get; init; } = 2;

    /// <summary>Constant delay between provider retries.</summary>
    public int RetryDelayMilliseconds { get; init; } = 200;
}
