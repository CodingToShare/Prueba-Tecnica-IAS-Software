# Project Context — Bootstrap

## Business Problem

Field-services company: an internal portal registers service requests that must be processed against an external provider.
Users retry when the portal feels slow; the provider fails transiently; the same business operation (`externalReference`)
must never cause duplicate provider effects. Completed requests never move back.

## Challenge Success Criteria (reviewable minimum)

| Component | Minimum |
| --- | --- |
| API | Register and query requests; understandable contract and errors |
| Persistence | Data survives the process; reproducible storage initialization |
| Worker | Processes at least one Pending request via a simulated provider |
| Frontend | Create and list/filter requests (one allowed technology) |
| Quality | Automated tests for a relevant risk + instructions to run them |
| Decisions | Assumptions, trade-offs, AI usage documented briefly and verifiably |
| Legacy | Concrete risk + minimal fix for the WebForms and MVC snippets |

Also required: 3–5 lines on what would change with NoSQL; how the Worker becomes a Windows Service.

## Approved Stack

- Backend: .NET 10, ASP.NET Core Web API (controllers), EF Core 10, SQL Server.
- Worker: .NET Worker Service (`BackgroundService`), Polly.
- Frontend: React + TypeScript + Vite, native `fetch`, hooks.
- Tests: xUnit, Moq, `TheoryData`, `MemberData`.

## Visual Language

Contract: `docs/design/design-system.md` (skill `ui-design-system`); canonical visual reference:
`docs/design/reference/design-system-v3.html` (Portal de Solicitudes edition of design system v3: shows this portal's
screen, request lifecycle, processing with retries, alerts and states; sample data illustrative). Navy `#1E3A5F` primary, Plus Jakarta Sans, light surfaces, restrained
shadows, navy gradient only on the brand bar. Status tones: Pending → neutral, Processing → informational,
Completed → normal, Failed → critical; badges carry color + text + shape. Validated at ~1440/768/390 px.

## AI Assistance

Permitted and disclosed. Claude Code is the repository-aware implementation agent. AI does not override requirements
or accepted decisions and does not count as verification. Incremental evidence lives in `docs/ai/ai-usage.md`; the
final challenge disclosure is produced from it in Phase 06.

## Repository Target

```text
ServiceRequests.sln
src/ServiceRequests.Core | .Infrastructure | .Api | .Worker
tests/ServiceRequests.Tests
frontend/service-requests-web
docs/design/design-system.md     visual contract
docs/design/reference/           canonical visual reference (HTML)
docs/technical-decisions.md      decisions, assumptions, open decisions
docs/requirements-traceability.md  challenge requirement → phase → evidence
docs/ai/ai-usage.md              incremental AI usage evidence (tracked)
docs/phases/roadmap.md           phases, status, evidence
```

Dependencies: Api/Worker → Infrastructure → Core. Core stays free of EF Core/ASP.NET/Polly.

## Request Model

`externalReference` (idempotency key, UNIQUE), `customerId`, `serviceCode`, `description`, `status`
(+ technical fields as needed). Implemented in 01.1: Guid v7 `Id`, nullable `FailureReason`; lengths
100/50/50/1000/500; status stored as text. No timestamps yet (open finding for Phase 02/04).

## Idempotency Semantics

| Case | Result |
| --- | --- |
| New `externalReference` | persist → **201 Created** |
| Same reference, equivalent payload | return existing → **200 OK** |
| Same reference, conflicting payload | **409 Conflict** (ProblemDetails) |

The DB UNIQUE index is the final concurrency guard (an `Exists()` check alone is racy).
Provider side: the simulated provider treats `externalReference` as an idempotency key; a real provider must offer
equivalent semantics or reconciliation (crash after provider success but before persisting Completed).

## State Model

```text
Pending → Processing → Completed (terminal)
                    ↘ Failed
```

Transitions only through `MarkProcessing()`, `Complete()`, `Fail(reason)`. Invalid transitions are rejected.
No public status mutation endpoint.

## Worker Flow

```text
poll → find Pending → MarkProcessing + persist (claim)
     → call provider (Polly: bounded retry, short backoff, transient failures only)
     → success: Complete + persist | retries exhausted: Fail + persist
```

Single Worker instance assumed; multiple workers would need an atomic claim (e.g. conditional UPDATE / rowversion).
Planned: on startup the Worker resumes requests left in `Processing` by a crash (safe because the provider is idempotent
by `externalReference`); if cut for time, it is documented as a limitation.
Hosted so it can run via `UseWindowsService()`; manual `dotnet run` demonstrates the flow.

## Deliberate Non-Goals

YARP, cache, ETL/SSIS, Docker, CI/CD, cloud, Redis, brokers, outbox, distributed locks/transactions, microservices,
MediatR, AutoMapper, FluentValidation, generic repositories, CQRS, event sourcing, Kubernetes, advanced observability,
authentication, pagination, DELETE/generic update.

## Environment Findings (Phase 00)

- Git 2.54, Node 24.21, npm 11.19 available.
- .NET SDK 10.0.404 installed by the user (OD-01 resolved); 8.0.403 also present. Work is CLI-driven
  (Visual Studio 17.11 cannot open `net10.0` projects).
- SQL Server LocalDB present (`MSSQLLocalDB`, automatic instance not yet created — first start creates it).
  Connection string must stay overridable via configuration/environment variables.

## Current Phase

Phases 00, 00.1 and 01 (01.1 build, 01.2 review, 01.3 validation) complete; Phase 02 awaits authorization.
Tests: `dotnet test ServiceRequests.sln` (33; persistence tests use a throwaway LocalDB database, TD-17; override
server with `SERVICEREQUESTS_TEST_SQLSERVER`). Database collation is case-insensitive (`SQL_Latin1_General_CP1_CI_AS`)
— decide the externalReference comparison rule with OD-02. Database init (TD-16): `dotnet tool restore`, then
`dotnet ef database update --project src/ServiceRequests.Infrastructure --startup-project src/ServiceRequests.Api`.
See `docs/phases/roadmap.md` for status.

## Roadmap Summary (115 min planned, 120 min hard stop)

00 Governance 8 · 00.1 AI governance 5 · 01 Foundation + persistence 15 · 02 Idempotent API 22 · 03 Worker + resilience 20 ·
04 React portal 18 · 05 Test hardening 12 · 06 Docs + legacy review 10 · 07 Final validation 5 · Reserve 5.
