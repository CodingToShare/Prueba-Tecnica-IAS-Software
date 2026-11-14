using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using ServiceRequests.Core;

namespace ServiceRequests.Infrastructure.Provider;

public sealed class ExternalProviderOptions
{
    public const string SectionName = "ExternalProvider";

    /// <summary>Transient failures returned for each reference before it succeeds (0 = immediate success).</summary>
    public int TransientFailuresBeforeSuccess { get; init; }

    /// <summary>Service codes the provider rejects permanently (case-insensitive).</summary>
    public string[] PermanentFailureServiceCodes { get; init; } = [];
}

/// <summary>
/// Deterministic in-memory stand-in for the external provider. Idempotent by external reference for the lifetime of
/// the process: a reference that already succeeded returns success again without a second effect. Attempt counts and
/// processed references are kept in memory only and are lost when the process restarts.
/// </summary>
public sealed class SimulatedExternalProvider(IOptions<ExternalProviderOptions> options) : IExternalProvider
{
    // Same identity rule as the database collation: references differing only in case are the same operation.
    private readonly ConcurrentDictionary<string, int> _attempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _effects = new(StringComparer.OrdinalIgnoreCase);

    public Task ProcessAsync(ServiceRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = request.ExternalReference;

        if (_effects.ContainsKey(reference))
        {
            return Task.CompletedTask; // replay of an already processed operation: no second effect
        }

        var settings = options.Value;
        if (settings.PermanentFailureServiceCodes.Contains(request.ServiceCode, StringComparer.OrdinalIgnoreCase))
        {
            throw new PermanentProviderException($"Service code '{request.ServiceCode}' is not supported by the provider.");
        }

        var attempt = _attempts.AddOrUpdate(reference, 1, (_, previous) => previous + 1);
        if (attempt <= settings.TransientFailuresBeforeSuccess)
        {
            throw new TransientProviderException(
                $"Provider temporarily unavailable (simulated failure {attempt} of {settings.TransientFailuresBeforeSuccess}).");
        }

        _effects.TryAdd(reference, 1);
        return Task.CompletedTask;
    }

    /// <summary>Number of business effects applied for a reference (0 or 1). Exposed for verification.</summary>
    public int GetEffectCount(string externalReference) =>
        _effects.TryGetValue(externalReference, out var count) ? count : 0;
}
