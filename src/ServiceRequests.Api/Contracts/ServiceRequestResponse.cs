using ServiceRequests.Core;

namespace ServiceRequests.Api.Contracts;

public sealed record ServiceRequestResponse(
    Guid Id,
    string ExternalReference,
    string CustomerId,
    string ServiceCode,
    string Description,
    ServiceRequestStatus Status,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ServiceRequestResponse From(ServiceRequest request) => new(
        request.Id,
        request.ExternalReference,
        request.CustomerId,
        request.ServiceCode,
        request.Description,
        request.Status,
        request.FailureReason,
        request.CreatedAt,
        request.UpdatedAt);
}
