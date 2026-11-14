namespace ServiceRequests.Core;

/// <summary>Persistence operations required by the service request use cases.</summary>
public interface IServiceRequestRepository
{
    /// <summary>
    /// Finds a request by external reference using the database equality semantics (case-insensitive collation),
    /// so <c>REQ-1</c> and <c>req-1</c> resolve to the same request.
    /// </summary>
    Task<ServiceRequest?> FindByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the request. Returns <c>false</c> when the database rejects it because another request with the same
    /// external reference already exists (UNIQUE index race); any other failure propagates.
    /// </summary>
    Task<bool> TryAddAsync(ServiceRequest request, CancellationToken cancellationToken);

    /// <summary>Lists requests, optionally filtered by status, newest first (CreatedAt desc, Id desc).</summary>
    Task<IReadOnlyList<ServiceRequest>> ListAsync(ServiceRequestStatus? status, CancellationToken cancellationToken);
}
