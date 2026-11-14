---
name: dotnet-backend
description: ASP.NET Core Web API conventions — thin controllers, DTO validation, idempotent create, ProblemDetails, async with CancellationToken, configuration.
---

# .NET Backend

## Use When

Working on `ServiceRequests.Api` or the use-case services in `ServiceRequests.Core`.

## Responsibilities

- Endpoints: `POST /api/requests`, `GET /api/requests`, `GET /api/requests?status={status}`.
- Request/response DTOs separate from the entity.
- Idempotent create semantics (201 / 200 / 409).
- Centralized error handling producing ProblemDetails.

## Required Rules

- Controllers only map HTTP ↔ use case; logic lives in Core services.
- `[ApiController]` + data annotations for validation (required, max lengths matching DB columns). Trim inputs.
- Invalid `status` filter → 400 ProblemDetails, not a silent empty list.
- Create flow: look up by `externalReference` → equivalent payload returns existing (200), different → 409;
  insert otherwise; on unique-index violation (concurrent insert) re-read and apply the same comparison.
- `AddProblemDetails()` + exception handler; never leak stack traces or SQL details to clients.
- `async`/`await` end to end; pass `CancellationToken` to every EF Core / provider call.
- Configuration via `appsettings.json` + environment variables; no secrets in source. CORS limited to the frontend dev origin.
- Response enum values serialized as strings.
- Configuration validated at startup (fail fast on missing connection string / invalid options).
- Structured `ILogger` logging for create outcomes (created / replayed / conflict) with `{ExternalReference}`;
  unexpected exceptions logged server-side once, returned as generic 500 ProblemDetails.

## Prohibited

DELETE/PUT/PATCH or generic status update endpoints, authentication, pagination, advanced search,
returning EF entities directly, `.Result`/`.Wait()`, catch-all exception swallowing.

## Completion Checklist

- [ ] 201 / 200 / 409 / 400 paths covered by tests
- [ ] Errors are ProblemDetails
- [ ] CancellationToken flows to I/O
- [ ] No secrets in committed config
