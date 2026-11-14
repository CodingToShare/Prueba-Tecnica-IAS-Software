using Microsoft.AspNetCore.Mvc;
using ServiceRequests.Api.Contracts;
using ServiceRequests.Core;

namespace ServiceRequests.Api.Controllers;

[ApiController]
[Route("api/requests")]
[Produces("application/json")]
public sealed class ServiceRequestsController(
    ServiceRequestService service,
    ILogger<ServiceRequestsController> logger) : ControllerBase
{
    /// <summary>Registers a request; idempotent by external reference (201 created, 200 replay, 409 conflict).</summary>
    [HttpPost]
    [ProducesResponseType<ServiceRequestResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ServiceRequestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateServiceRequestRequest body, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            body.ExternalReference, body.CustomerId, body.ServiceCode, body.Description, cancellationToken);

        var reference = result.Request.ExternalReference;
        switch (result.Outcome)
        {
            case CreateServiceRequestOutcome.Created:
                logger.LogInformation("Service request {ExternalReference} created", reference);
                return StatusCode(StatusCodes.Status201Created, ServiceRequestResponse.From(result.Request));

            case CreateServiceRequestOutcome.Replayed:
                logger.LogInformation("Service request {ExternalReference} replayed; existing request returned", reference);
                return Ok(ServiceRequestResponse.From(result.Request));

            case CreateServiceRequestOutcome.Conflict:
                logger.LogWarning("Service request {ExternalReference} rejected: payload differs from the existing request", reference);
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "External reference already registered with a different payload.",
                    detail: $"A request with externalReference '{reference}' already exists with different "
                        + "customerId, serviceCode or description. Use a new externalReference for a different operation.");

            default:
                throw new InvalidOperationException($"Unexpected create outcome {result.Outcome}.");
        }
    }

    /// <summary>Lists requests newest first, optionally filtered by status (Pending, Processing, Completed, Failed).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ServiceRequestResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        ServiceRequestStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseStatus(status, out var parsed))
            {
                ModelState.AddModelError(nameof(status),
                    $"Unknown status '{status}'. Allowed values: {string.Join(", ", Enum.GetNames<ServiceRequestStatus>())}.");
                return ValidationProblem(ModelState);
            }

            filter = parsed;
        }

        var requests = await service.ListAsync(filter, cancellationToken);
        return Ok(requests.Select(ServiceRequestResponse.From).ToList());
    }

    // Accepts status names only (case-insensitive); numeric values such as "1" are rejected.
    private static bool TryParseStatus(string value, out ServiceRequestStatus status)
    {
        var name = Enum.GetNames<ServiceRequestStatus>()
            .FirstOrDefault(n => string.Equals(n, value.Trim(), StringComparison.OrdinalIgnoreCase));
        status = name is null ? default : Enum.Parse<ServiceRequestStatus>(name);
        return name is not null;
    }
}
