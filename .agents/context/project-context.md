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

## Repository Target

```text
ServiceRequests.sln
src/ServiceRequests.Core | .Infrastructure | .Api | .Worker
tests/ServiceRequests.Tests
frontend/service-requests-web
```

Dependencies: Api/Worker → Infrastructure → Core. Core stays free of EF Core/ASP.NET/Polly.

## Request Model

`externalReference` (idempotency key, UNIQUE), `customerId`, `serviceCode`, `description`, `status`
(+ technical fields such as id, timestamps, attempts/last error as needed by the Worker).

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
Hosted so it can run via `UseWindowsService()`; manual `dotnet run` demonstrates the flow.

## Deliberate Non-Goals

YARP, cache, ETL/SSIS, Docker, CI/CD, cloud, Redis, brokers, outbox, distributed locks/transactions, microservices,
MediatR, AutoMapper, FluentValidation, generic repositories, CQRS, event sourcing, Kubernetes, advanced observability,
authentication, pagination, DELETE/generic update.

## Environment Findings (Phase 00)

- Git 2.54, Node 24.21, npm 11.19 available.
- **.NET SDK installed: 8.0.403 only (runtimes 6/7/8). The approved .NET 10 SDK is NOT installed.**
  Phase 01 entry requires a user decision: install the .NET 10 SDK, or record a decision to target .NET 8 LTS (EF Core 8).
- SQL Server LocalDB present (`MSSQLLocalDB`, automatic instance not yet created — first start creates it).
  Connection string must stay overridable via configuration/environment variables.

## Current Phase

Phase 00 — Agent governance and technical plan. See `docs/phases/roadmap.md` for status.

## Roadmap Summary (110 min target, 120 min hard stop)

00 Governance 8 · 01 Foundation + persistence 15 · 02 Idempotent API 22 · 03 Worker + resilience 20 ·
04 React portal 18 · 05 Test hardening 12 · 06 Docs + legacy review 10 · 07 Final validation 5 · Reserve 10.
