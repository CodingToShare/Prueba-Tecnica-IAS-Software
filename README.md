# Service Requests Portal

Internal portal for a field-services company: requests are registered through an API and later processed in the
background against an external provider. Users retry when the portal feels slow and the provider fails transiently,
so the same business operation (`externalReference`) must never create duplicate effects, and a `Completed`
request never moves back.

> **Status:** Phases 00–02 complete — domain, SQL Server persistence and the idempotent HTTP API are implemented and
> tested. Background Worker (Phase 03) and React portal (Phase 04) are **not implemented yet**.
> Progress and evidence: [`docs/phases/roadmap.md`](docs/phases/roadmap.md).

## Architecture

```text
[React + TS (Vite) — Phase 04, pending] --HTTP--> ASP.NET Core Web API --> ServiceRequests.Core
                                                                       |--> EF Core / SQL Server
                                     [.NET Worker + Polly — Phase 03, pending] --> simulated provider
```

| Project | Responsibility |
| --- | --- |
| `src/ServiceRequests.Core` | `ServiceRequest` entity and state transitions, replay rule, `ServiceRequestService`, repository interface. No framework packages. |
| `src/ServiceRequests.Infrastructure` | EF Core `DbContext`, mapping, migrations, repository (SQL Server duplicate-key translation), DI registration. |
| `src/ServiceRequests.Api` | Controller, DTOs, ProblemDetails, CORS. |
| `src/ServiceRequests.Worker` | Host only (no processing yet — Phase 03). |
| `tests/ServiceRequests.Tests` | xUnit + Moq unit tests, LocalDB integration tests, HTTP tests (`WebApplicationFactory`). |

Dependencies point inward: Api/Worker → Infrastructure → Core. Decisions: [`docs/technical-decisions.md`](docs/technical-decisions.md).

## Prerequisites (validated environment)

- .NET 10 SDK (validated with 10.0.404) — the solution is driven from the .NET CLI.
- SQL Server LocalDB (`MSSQLLocalDB`, validated with SQL Server 2019 LocalDB) or another SQL Server reachable with a
  connection string.
- Git. No Docker, cloud services or Node.js are needed for the current backend.

## Database initialization

Explicit EF Core migrations (TD-16); the API never migrates the database on startup. From the repository root:

```bash
dotnet tool restore
dotnet ef database update --project src/ServiceRequests.Infrastructure --startup-project src/ServiceRequests.Api
```

This creates `ServiceRequestsDb` on `(localdb)\MSSQLLocalDB` with the migrations `InitialCreate` and
`AddRequestTimestamps` (UNIQUE index on `ExternalReference`, index on `Status`).

## Build, run and test

```bash
dotnet restore
dotnet build ServiceRequests.sln
dotnet run --project src/ServiceRequests.Api        # http://localhost:5164 (launch profile "http", Development)
dotnet test ServiceRequests.sln
```

| Suite | Needs | Covers |
| --- | --- | --- |
| Unit (`Domain/`, `Application/`) | nothing | state transitions (Completed terminal, Failed final), invariants, timestamps, replay rule, idempotent create incl. simulated UNIQUE race (Moq) |
| Integration (`Persistence/`) | LocalDB | migrations, UNIQUE and case-insensitive identity, timestamps round trip, SQL filter and ordering |
| Functional HTTP (`Api/`) | LocalDB | 201 / 200 replay / case-variant replay / 409 / 400 / status filter through the real pipeline |

Persistence and HTTP tests create a throwaway database `ServiceRequestsDb_Tests_<guid>` from the migrations and drop
it afterwards. To use another SQL Server, set `SERVICEREQUESTS_TEST_SQLSERVER` to a connection string without a
database name. Last validated run: 71 passed, 0 failed, 0 skipped.

## API

| Method | Route | Result |
| --- | --- | --- |
| `POST` | `/api/requests` | `201` new · `200` equivalent replay (existing request) · `409` conflicting replay · `400` validation |
| `GET` | `/api/requests` | all requests, newest first (`createdAt` desc, then `id` desc) |
| `GET` | `/api/requests?status=Pending` | only that status (`Pending`, `Processing`, `Completed`, `Failed`; case-insensitive); unknown value → `400` |

```bash
curl -i -H "Content-Type: application/json" http://localhost:5164/api/requests \
  -d '{"externalReference":"REQ-1","customerId":"CUST-01","serviceCode":"INST","description":"Install service"}'
```

Response body (201 and 200): `id`, `externalReference`, `customerId`, `serviceCode`, `description`, `status`
(string), `failureReason`, `createdAt`, `updatedAt`. Errors are RFC 7807 `application/problem+json`; unexpected
failures return a generic 500 without stack traces or SQL details. Clients cannot set id, status or timestamps.

## Idempotency semantics

- **Identity:** `externalReference` is trimmed and stored with the submitted casing. Lookup uses plain equality, so the
  database collation (`SQL_Latin1_General_CP1_CI_AS`, case-insensitive) decides identity: `REQ-1`, `req-1` and
  `Req-1` are the same request. No `ToLower`/`ToUpper` in code (TD-19).
- **Equivalent replay:** `customerId`, `serviceCode` and `description` equal after trimming, case-sensitive (OD-02 →
  TD-19). Example: existing `REQ-1 / CUST-01 / INST / Install service`; `req-1` with the same values → `200`;
  `req-1` with `cust-01` → `409`.
- **Concurrency:** the UNIQUE index is the final guard. If a concurrent request wins between lookup and insert, SQL
  Server rejects the insert (2601/2627), the repository reports it, and the service re-reads the winner and applies
  the same rule (TD-05, TD-06).

## Configuration

| Key | Default (`appsettings.json`) |
| --- | --- |
| `ConnectionStrings:ServiceRequests` | `Server=(localdb)\MSSQLLocalDB;Database=ServiceRequestsDb;Trusted_Connection=True` — override with the environment variable `ConnectionStrings__ServiceRequests`; startup fails if missing |
| `Cors:AllowedOrigins` | `["http://localhost:5173"]` (planned Vite dev server; GET/POST, `Content-Type`) |

No secrets are committed; LocalDB uses Windows authentication.

## Current status and deferred work

| Area | State |
| --- | --- |
| Domain, persistence, migrations | Done and tested (Phase 01) |
| Idempotent API | Done and tested (Phase 02) |
| Worker, simulated provider, Polly retries, Windows Service | Pending — Phase 03 |
| React portal | Pending — Phase 04 (design contract: [`docs/design/design-system.md`](docs/design/design-system.md)) |
| NoSQL alternative note, legacy code review, final README | Pending — Phase 06 |

Known items: a handled concurrent-insert race is logged by EF Core at Error level (Phase 05); rows created before
`AddRequestTimestamps` would get `0001-01-01` timestamps (none exist). Traceability:
[`docs/requirements-traceability.md`](docs/requirements-traceability.md).

## AI usage

AI assistants (ChatGPT for planning, Claude Code for implementation) were used as accelerators; their output was
accepted only after builds, tests, database checks and diff review. Per-phase evidence, including corrections:
[`docs/ai/ai-usage.md`](docs/ai/ai-usage.md). The final disclosure is completed in Phase 06.
