using System.ComponentModel.DataAnnotations;
using ServiceRequests.Core;

namespace ServiceRequests.Api.Contracts;

/// <summary>
/// Body of <c>POST /api/requests</c>. Only business fields are accepted; id, status and timestamps are server-owned.
/// Lengths match the database columns (<see cref="ServiceRequest"/> constants).
/// </summary>
public sealed class CreateServiceRequestRequest
{
    [Required]
    [StringLength(ServiceRequest.ExternalReferenceMaxLength)]
    public string ExternalReference { get; init; } = string.Empty;

    [Required]
    [StringLength(ServiceRequest.CustomerIdMaxLength)]
    public string CustomerId { get; init; } = string.Empty;

    [Required]
    [StringLength(ServiceRequest.ServiceCodeMaxLength)]
    public string ServiceCode { get; init; } = string.Empty;

    [Required]
    [StringLength(ServiceRequest.DescriptionMaxLength)]
    public string Description { get; init; } = string.Empty;
}
