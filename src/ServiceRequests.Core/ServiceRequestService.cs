namespace ServiceRequests.Core;

public enum CreateServiceRequestOutcome
{
    /// <summary>New external reference: the request was persisted.</summary>
    Created,

    /// <summary>Existing reference with an equivalent payload: the existing request is returned, nothing is written.</summary>
    Replayed,

    /// <summary>Existing reference with a different payload: nothing is written.</summary>
    Conflict
}

/// <param name="Outcome">What happened.</param>
/// <param name="Request">The created request, or the existing one for replay and conflict.</param>
public sealed record CreateServiceRequestResult(CreateServiceRequestOutcome Outcome, ServiceRequest Request);

/// <summary>Create (idempotent by external reference) and list service requests.</summary>
public sealed class ServiceRequestService(IServiceRequestRepository repository, TimeProvider timeProvider)
{
    public async Task<CreateServiceRequestResult> CreateAsync(
        string externalReference,
        string customerId,
        string serviceCode,
        string description,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalReference);
        var reference = externalReference.Trim();

        var existing = await repository.FindByExternalReferenceAsync(reference, cancellationToken);
        if (existing is not null)
        {
            return CompareWithExisting(existing, customerId, serviceCode, description);
        }

        var request = new ServiceRequest(reference, customerId, serviceCode, description, timeProvider.GetUtcNow());
        if (await repository.TryAddAsync(request, cancellationToken))
        {
            return new CreateServiceRequestResult(CreateServiceRequestOutcome.Created, request);
        }

        // A concurrent submission inserted the same reference between the lookup and the insert:
        // the UNIQUE index rejected ours, so apply the same replay rules to the winner.
        existing = await repository.FindByExternalReferenceAsync(reference, cancellationToken)
            ?? throw new InvalidOperationException(
                "The external reference was rejected as a duplicate but the existing request was not found.");

        return CompareWithExisting(existing, customerId, serviceCode, description);
    }

    public Task<IReadOnlyList<ServiceRequest>> ListAsync(
        ServiceRequestStatus? status, CancellationToken cancellationToken) =>
        repository.ListAsync(status, cancellationToken);

    private static CreateServiceRequestResult CompareWithExisting(
        ServiceRequest existing, string customerId, string serviceCode, string description) =>
        new(existing.HasEquivalentPayload(customerId, serviceCode, description)
                ? CreateServiceRequestOutcome.Replayed
                : CreateServiceRequestOutcome.Conflict,
            existing);
}
