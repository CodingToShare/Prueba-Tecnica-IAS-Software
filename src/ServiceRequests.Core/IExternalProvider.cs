namespace ServiceRequests.Core;

/// <summary>
/// External provider boundary. <see cref="ServiceRequest.ExternalReference"/> is the provider's idempotency key:
/// processing an already processed reference again must not cause a second business effect.
/// </summary>
public interface IExternalProvider
{
    /// <summary>Processes the request. Returns on success.</summary>
    /// <exception cref="TransientProviderException">Temporary failure; the call may be retried.</exception>
    /// <exception cref="PermanentProviderException">The provider rejected the request; retrying will not help.</exception>
    Task ProcessAsync(ServiceRequest request, CancellationToken cancellationToken);
}

/// <summary>Modeled temporary provider failure (eligible for retry).</summary>
public sealed class TransientProviderException(string message) : Exception(message);

/// <summary>Modeled definitive provider rejection (never retried).</summary>
public sealed class PermanentProviderException(string message) : Exception(message);
