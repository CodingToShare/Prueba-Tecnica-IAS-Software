namespace ServiceRequests.Core;

/// <summary>
/// A service request to be processed against the external provider.
/// State changes only through <see cref="MarkProcessing"/>, <see cref="Complete"/> and <see cref="Fail"/>:
/// Pending → Processing → Completed | Failed. Completed and Failed are final (assumption A-03).
/// Timestamps are server-owned UTC values supplied by the caller's clock (<see cref="TimeProvider"/>).
/// </summary>
public sealed class ServiceRequest
{
    public const int ExternalReferenceMaxLength = 100;
    public const int CustomerIdMaxLength = 50;
    public const int ServiceCodeMaxLength = 50;
    public const int DescriptionMaxLength = 1000;
    public const int FailureReasonMaxLength = 500;

    public ServiceRequest(
        string externalReference, string customerId, string serviceCode, string description, DateTimeOffset now)
    {
        Id = Guid.CreateVersion7();
        ExternalReference = RequireText(externalReference, nameof(externalReference));
        CustomerId = RequireText(customerId, nameof(customerId));
        ServiceCode = RequireText(serviceCode, nameof(serviceCode));
        Description = RequireText(description, nameof(description));
        Status = ServiceRequestStatus.Pending;
        CreatedAt = UpdatedAt = now.ToUniversalTime();
    }

    // Used by EF Core when materializing from the database.
    private ServiceRequest()
    {
        ExternalReference = CustomerId = ServiceCode = Description = string.Empty;
    }

    public Guid Id { get; private set; }

    public string ExternalReference { get; private set; }

    public string CustomerId { get; private set; }

    public string ServiceCode { get; private set; }

    public string Description { get; private set; }

    public ServiceRequestStatus Status { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Replay rule (OD-02): the business payload is equivalent when CustomerId, ServiceCode and Description are
    /// ordinally equal after trimming. Casing is significant for these values.
    /// </summary>
    public bool HasEquivalentPayload(string customerId, string serviceCode, string description) =>
        string.Equals(CustomerId, customerId?.Trim(), StringComparison.Ordinal)
        && string.Equals(ServiceCode, serviceCode?.Trim(), StringComparison.Ordinal)
        && string.Equals(Description, description?.Trim(), StringComparison.Ordinal);

    public void MarkProcessing(DateTimeOffset now)
    {
        EnsureStatus(ServiceRequestStatus.Pending, ServiceRequestStatus.Processing);
        Status = ServiceRequestStatus.Processing;
        UpdatedAt = now.ToUniversalTime();
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureStatus(ServiceRequestStatus.Processing, ServiceRequestStatus.Completed);
        Status = ServiceRequestStatus.Completed;
        UpdatedAt = now.ToUniversalTime();
    }

    /// <summary>Marks the request as failed. The reason is trimmed and truncated to <see cref="FailureReasonMaxLength"/>.</summary>
    public void Fail(string reason, DateTimeOffset now)
    {
        var trimmed = RequireText(reason, nameof(reason));
        EnsureStatus(ServiceRequestStatus.Processing, ServiceRequestStatus.Failed);
        Status = ServiceRequestStatus.Failed;
        FailureReason = trimmed.Length <= FailureReasonMaxLength ? trimmed : trimmed[..FailureReasonMaxLength];
        UpdatedAt = now.ToUniversalTime();
    }

    private void EnsureStatus(ServiceRequestStatus expected, ServiceRequestStatus target)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Cannot change service request status from {Status} to {target}.");
        }
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", parameterName);
        }

        return value.Trim();
    }
}
