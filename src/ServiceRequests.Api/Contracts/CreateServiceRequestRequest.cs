using System.ComponentModel.DataAnnotations;
using ServiceRequests.Core;

namespace ServiceRequests.Api.Contracts;

/// <summary>
/// Body of <c>POST /api/requests</c>. Only business fields are accepted; id, status and timestamps are server-owned.
/// Values are trimmed on binding, so length limits (the database columns, <see cref="ServiceRequest"/> constants)
/// apply to the value that is actually stored.
/// </summary>
public sealed class CreateServiceRequestRequest
{
    [Required]
    [StringLength(ServiceRequest.ExternalReferenceMaxLength)]
    public string ExternalReference { get; init => field = value?.Trim() ?? string.Empty; } = string.Empty;

    [Required]
    [StringLength(ServiceRequest.CustomerIdMaxLength)]
    public string CustomerId { get; init => field = value?.Trim() ?? string.Empty; } = string.Empty;

    [Required]
    [StringLength(ServiceRequest.ServiceCodeMaxLength)]
    public string ServiceCode { get; init => field = value?.Trim() ?? string.Empty; } = string.Empty;

    [Required]
    [StringLength(ServiceRequest.DescriptionMaxLength)]
    public string Description { get; init => field = value?.Trim() ?? string.Empty; } = string.Empty;
}
