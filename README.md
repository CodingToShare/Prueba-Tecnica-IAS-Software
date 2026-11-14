# Service Requests Portal

Technical challenge: an internal portal registers field-service requests; a background Worker sends them to an external
provider. Users retry when the portal feels slow and the provider fails transiently, so the same business operation
(`externalReference`) must never create duplicate effects, and a `Completed` request never moves back.

**Implemented:** idempotent REST API, SQL Server persistence with migrations, a resilient background Worker (transient
retries with Polly, startup recovery, Windows Service ready) and a React portal to create, list and filter requests.
102 automated tests; the portal was accepted in a real browser against the real stack. Phase evidence:
[`docs/phases/roadmap.md`](docs/phases/roadmap.md).

## Architecture

```text
React + TypeScript portal (Vite, :5173)
        │ HTTP  POST /api/requests · GET /api/requests[?status=]
        ▼
ASP.NET Core Web API (:5164) ──► ServiceRequests.Core ──► EF Core ──► SQL Server (LocalDB)
                                                                        ▲
.NET Worker (BackgroundService) ── claim / complete / fail ─────────────┘
        │
        ▼
Polly retry (transient failures only) ──► simulated external provider
```

A pragmatic modular solution (one API, one Worker, one database — not microservices). Code dependencies point inward:
`Api` / `Worker` → `Infrastructure` → `Core`; `Core` (entity, state transitions, replay rule, use-case service,
repository and provider contracts) references no EF Core, ASP.NET or Polly.

| Project | Responsibility |
| --- | --- |
| `src/ServiceRequests.Core` | `ServiceRequest` entity and transitions, replay rule, `ServiceRequestService`, contracts |
| `src/ServiceRequests.Infrastructure` | EF Core `DbContext`, migrations, repository (duplicate-key translation), simulated provider |
| `src/ServiceRequests.Api` | Controller, DTOs, ProblemDetails, CORS |
| `src/ServiceRequests.Worker` | Hosted loop, `ServiceRequestProcessor`, Polly pipeline, Windows Service hosting |
| `tests/ServiceRequests.Tests` | Unit, SQL Server integration, functional HTTP and Worker tests |
| `frontend/service-requests-web` | React portal (native `fetch`, hooks, plain CSS from the design tokens) |

**Stack:** .NET 10 · ASP.NET Core controllers · EF Core 10.0.15 · SQL Server (validated on LocalDB 2019) · Polly.Core
8.9.1 · xUnit 2.9.3 · Moq 4.20.72 · React 19.3 · TypeScript 7.0 · Vite 8.3.

## Prerequisites

- .NET 10 SDK (validated with 10.0.404).
- SQL Server LocalDB (`MSSQLLocalDB`) or another SQL Server reachable with a connection string.
- Node.js and npm (validated with Node 24.21 and npm 11.19).
- Git. No Docker or cloud services.

## Quick start

From the repository root (Windows), one terminal per process:

```bash
# 1. .NET tools and packages
dotnet tool restore
dotnet restore

# 2. Database (explicit EF Core migrations; neither host migrates on startup — TD-16)
dotnet ef database update --project src/ServiceRequests.Infrastructure --startup-project src/ServiceRequests.Api

# 3. API — terminal 1 → http://localhost:5164
dotnet run --project src/ServiceRequests.Api

# 4. Worker — terminal 2
dotnet run --project src/ServiceRequests.Worker

# 5. Portal — terminal 3 → open http://localhost:5173
cd frontend/service-requests-web
npm ci
cp .env.example .env.local        # VITE_API_BASE_URL=http://localhost:5164
npm run dev
```

Step 2 creates `ServiceRequestsDb` on `(localdb)\MSSQLLocalDB` with the migrations `InitialCreate` and
`AddRequestTimestamps` (UNIQUE index on `ExternalReference`, index on `Status`). Both hosts read
`ConnectionStrings:ServiceRequests` (override with the environment variable `ConnectionStrings__ServiceRequests`;
startup fails if it is missing). `VITE_API_BASE_URL` is public browser configuration — never a secret; `.env.local` is
ignored by Git. The API allows the portal origin `http://localhost:5173` (CORS).

## Reviewer demo (in the portal)

1. **Success** — register `REQ-1` / `CUS-1042` / `MANT-PREV` / any description → green "Solicitud registrada · 201
   Created" with status Pending; within a few seconds the row turns Completed by itself (the Worker hits one simulated
   transient failure and succeeds on the retry; the list refreshes every 3 s).
2. **Idempotent replay** — submit the same data again (also `req-1` in lower case) → blue "Ya existía una solicitud… no
   se creó un duplicado · 200 OK"; still one row, same request.
3. **Conflict** — same reference with another customer → amber "ya existe con datos diferentes · 409 Conflict"; the form
   keeps the input and the stored request is unchanged.
4. **Permanent provider failure** — service code `UNSUPPORTED` → **Failed** after one call (no retry).
5. **Filter** — Todas / Pending / Processing / Completed / Failed; each option queries the API with `?status=`.

With default settings a request completes in about a second, so Processing is rarely visible. To watch Pending and
Processing, start the Worker slower:

```powershell
$env:Worker__PollingIntervalMilliseconds = "5000"; $env:Worker__RetryDelayMilliseconds = "5000"
dotnet run --project src/ServiceRequests.Worker
```

API-only equivalent:

```bash
curl -s -H "Content-Type: application/json" http://localhost:5164/api/requests \
  -d '{"externalReference":"REQ-1","customerId":"CUST-01","serviceCode":"INST","description":"Install service"}'
curl -s "http://localhost:5164/api/requests?status=Completed"
```

## API contract

| Method | Route | Result |
| --- | --- | --- |
| `POST` | `/api/requests` | `201` new · `200` equivalent replay (existing request) · `409` conflicting replay · `400` validation |
| `GET` | `/api/requests` | all requests, newest first (`createdAt` desc, then `id` desc) |
| `GET` | `/api/requests?status=Pending` | only that status (`Pending`, `Processing`, `Completed`, `Failed`; case-insensitive); unknown → `400` |

Request body: `externalReference`, `customerId`, `serviceCode`, `description` (required; trimmed; max 100/50/50/1000).
Response body: `id`, `externalReference`, `customerId`, `serviceCode`, `description`, `status` (text), `failureReason`,
`createdAt`, `updatedAt` (server-owned UTC). Errors are RFC 7807 `application/problem+json`; an unexpected failure
returns a generic 500 ProblemDetails without stack traces, SQL or connection details.

## Idempotency

**Local operation uniqueness.** `externalReference` is the idempotency key, backed by a SQL Server **UNIQUE index**.
Lookup uses plain equality, so the database collation (`SQL_Latin1_General_CP1_CI_AS`, case-insensitive) decides
identity: `REQ-1` and `req-1` are the same request (stored with the casing first submitted; no case folding in code).
A replay is *equivalent* when `customerId`, `serviceCode` and `description` match after trimming (case-sensitive) →
`200`; otherwise `409`. The application lookup is only a fast path: if two submissions race, SQL Server rejects the
second insert (error 2601/2627), the repository reports it and the service re-reads the winner and applies the same
rule. Verified with five truly concurrent POSTs on LocalDB: one row, one id, no 500 (TD-05, TD-06, TD-19).

**External-provider effects.** The local UNIQUE index does not by itself guarantee exactly-once effects at the provider
(for example, provider success followed by a crash before `Completed` is saved). The simulated provider models
idempotency by `externalReference` within its process; a real integration needs provider-side durable idempotency or
a reconciliation process. No exactly-once claim is made (TD-10).

## Background processing

```text
Pending ──claim (persisted)──► Processing ──provider ok──► Completed (terminal)
                                   ├─ transient failure → Polly retry → Completed, or Failed when retries run out
                                   ├─ permanent failure → Failed (no retry)
                                   └─ unexpected error  → logged; stays Processing; resumed on next Worker start
```

- The request is saved as `Processing` **before** the provider is called; if that save fails, the provider is not called.
- Only the modeled `TransientProviderException` is retried: `Worker:MaxRetryAttempts` = **2 retries = at most 3 provider
  calls**, constant `Worker:RetryDelayMilliseconds` (200 ms). Permanent failures, other exceptions and host
  cancellation are never retried and never turned into `Failed`.
- Transitions only through the entity (`MarkProcessing`, `Complete`, `Fail`); `Completed` and `Failed` are final;
  `CreatedAt` never changes, `UpdatedAt` changes on every transition.
- **Startup recovery:** before processing Pending requests, the Worker resumes every request left in `Processing` from
  the provider call (no second claim), retrying the lookup each interval if the database is unavailable.
- One request at a time, a fresh DI scope per unit of work, errors logged and the loop continues; once the provider has
  answered, the final state is saved even during shutdown.
- **Simulated provider:** deterministic — each reference fails transiently `ExternalProvider:TransientFailuresBeforeSuccess`
  times (default 1) and then succeeds; codes in `ExternalProvider:PermanentFailureServiceCodes` (default `UNSUPPORTED`)
  fail permanently.

## Windows Service

The Worker calls `AddWindowsService` (service name `ServiceRequestsWorker`): it runs as a console app with
`dotnet run --project src/ServiceRequests.Worker` and as a Windows Service when started by the Service Control Manager.

- **Validated:** the console host runs and processes requests; `dotnet publish` produces `ServiceRequests.Worker.exe`.
- **Documented deployment path (not executed here; needs an elevated prompt):**

```powershell
dotnet publish src/ServiceRequests.Worker -c Release -o .\publish\worker
sc.exe create ServiceRequestsWorker binPath= "C:\path\to\publish\worker\ServiceRequests.Worker.exe" start= auto
sc.exe start ServiceRequestsWorker
# or: New-Service -Name ServiceRequestsWorker -BinaryPathName "C:\path\to\publish\worker\ServiceRequests.Worker.exe" -StartupType Automatic
```

The service account needs database access; LocalDB is per-user, so a service would normally point
`ConnectionStrings__ServiceRequests` to a SQL Server instance. Remove with `sc.exe stop` / `sc.exe delete ServiceRequestsWorker`.

## Tests

```bash
dotnet build ServiceRequests.sln                                    # 0 warnings, 0 errors
dotnet test ServiceRequests.sln                                     # 102 tests (last run: 102 passed, 0 failed, 0 skipped)
cd frontend/service-requests-web && npm run build && npm run lint   # strict TypeScript + Vite build, oxlint
```

| Category | Needs | Covers |
| --- | --- | --- |
| Unit | — | state transitions (`TheoryData` matrix), invariants, timestamps, replay rule; idempotent create incl. a simulated UNIQUE race (Moq); Worker retry 3/3/1/1 calls, cancellation, claim before call, startup recovery |
| SQL Server integration | LocalDB | migrations, case-insensitive UNIQUE identity, timestamps, SQL filter/ordering, Worker processing persisted |
| Functional HTTP | LocalDB | 201/200/409/400 (`MemberData` invalid bodies)/filter, 500 ProblemDetails without internals, 5 concurrent POSTs (same payload → one row; different payloads → one winner + 409s) |
| Functional processing | LocalDB | API POST → real Worker host → GET observes Completed (with retries) or Failed |

Tools: **xUnit** for all suites; **Moq** for repository/provider boundaries (service, processor, Worker loop, 500 fault
injection); **TheoryData** for typed scenario matrices (transitions, replay payloads, conflicts); **MemberData** for
reusable invalid HTTP bodies and required values. Database tests create throwaway `ServiceRequestsDb_Tests_<guid>`
databases from the migrations and drop them; set `SERVICEREQUESTS_TEST_SQLSERVER` (connection string without a
database) to use another server. Test strength was checked with temporary mutations (removing the UNIQUE index, the race
re-read, the transition guards, the exception handler…), each of which made the targeted tests fail. No coverage
percentage was measured.

The portal has no automated UI suite by deliberate choice for this timed challenge (TD-12): it was validated by the
strict production build, oxlint and real-browser acceptance against the real API, SQL Server and Worker (create,
replay, 409, validation, filter, Failed, API outage and recovery, automatic refresh), plus review at 1440/768/390 px
against the design reference and a keyboard pass.

## Key technical decisions

Full records (context, alternatives, trade-offs, risks, validation) in
[`docs/technical-decisions.md`](docs/technical-decisions.md). Highlights:

- .NET 10, four projects with inward dependencies, Polly kept out of Core (TD-01, TD-04, TD-20).
- `externalReference` idempotency with the UNIQUE index as final guard and SQL-collation identity (TD-05, TD-06, TD-19).
- Explicit migrations; server-owned UTC `CreatedAt`/`UpdatedAt` added by a new migration (TD-16, TD-18).
- Separate Worker, claim before call, transient-only bounded retry, single instance, startup recovery (TD-07–TD-09, TD-20).
- React with native `fetch`, local state, plain CSS, server-side filtering, 3 s refresh (TD-02, TD-21).
- EF Core Error logs on a handled UNIQUE race are accepted rather than suppressed (TD-22).

## SQL Server vs NoSQL

SQL Server was chosen because the model is a single relational table whose correctness depends on a strong uniqueness
constraint (`externalReference`) and an indexed status query. With a document store, each request would map naturally
to one document, but `externalReference` would become the document key (or need a unique-key/conditional-write
feature) to keep the race barrier, state changes and the Worker claim would use conditional writes/ETags, and status
would need a secondary index. The Core contracts would stay the same; mappings, migrations (schema in code) and
concurrency handling would change.

## Portal

One screen following [`docs/design/design-system.md`](docs/design/design-system.md): navy brand bar, "Nueva solicitud"
form, "Solicitudes" table with a status filter; two columns from 980 px, stacked rows at ≤ 640 px. Feedback per API
result (201 normal, 200 informational, 409 warning, 400 field errors, network/5xx generic error with "Reintentar");
status badges carry color + text + shape (Failed uses the business `critical` tone, not the system-error red);
"Actualizada" is the server `updatedAt`. Accessibility: skip link, labels, `aria-invalid`/`aria-describedby`,
`aria-pressed`, visible focus, `prefers-reduced-motion`.

## Configuration

| Key | Used by | Default |
| --- | --- | --- |
| `ConnectionStrings:ServiceRequests` | API, Worker | `Server=(localdb)\MSSQLLocalDB;Database=ServiceRequestsDb;Trusted_Connection=True` (env `ConnectionStrings__ServiceRequests`) |
| `Cors:AllowedOrigins` | API | `["http://localhost:5173"]` |
| `Worker:PollingIntervalMilliseconds` / `MaxRetryAttempts` / `RetryDelayMilliseconds` | Worker | `1000` / `2` / `200` (validated on startup) |
| `ExternalProvider:TransientFailuresBeforeSuccess` / `PermanentFailureServiceCodes` | Worker | `1` / `["UNSUPPORTED"]` |
| `VITE_API_BASE_URL` | Portal (`.env.local`) | `http://localhost:5164` (page origin if unset) |

No secrets are committed; LocalDB uses Windows authentication. EF Core SQL command logs are kept at `Warning`.

## Legacy code review

The two legacy fragments are reviewed in [`docs/legacy-review.md`](docs/legacy-review.md):
**WebForms** — SQL injection through string concatenation → parameterized `SqlCommand` (`@customer`), not escaping.
**MVC** — `Find(id)` may be null (→ 404), `status` is unrestricted input, transitions can be bypassed and `Completed`
can regress (→ validate the value and enforce allowed transitions), and the EF entity is returned (→ small DTO).

## Assumptions, limitations and trade-offs

Assumptions (A-01…A-06 in the decision log): Spanish UI with the challenge status names; free-text, length-limited
customer and service codes; `Failed` is final; one Worker instance; no authentication (trusted internal network); only
transient and permanent provider failures are modeled.

| Type | Item | Scope / impact | Future production evolution |
| --- | --- | --- | --- |
| Migration limitation | `AddRequestTimestamps` would give rows that existed before it `0001-01-01` timestamps | None here: no database had rows before that migration; empty-database path validated | Explicit data backfill before enforcing the timestamp columns |
| Operational limitation | A request hit by an unexpected (non-provider) Worker error stays `Processing` until the next Worker start | That request waits; others continue. System errors are not mislabeled as provider failures (TD-20) | Periodic recovery of stale `Processing` rows using `UpdatedAt` as a lease |
| Simulation limitation | The simulated provider's idempotency memory is per process | After a restart a resumed request may call the simulator again | Real provider with durable idempotency keys, or reconciliation (TD-10) |
| Accepted operational trade-off | A handled UNIQUE race is logged by EF Core at Error (events 20102/10000), followed by the app's Information line "Idempotency race for …" | Behavior is correct (200/409); broad suppression would hide genuine database failures (TD-22) | Alert filtering correlated with the race log |
| Scope assumption | One Worker instance | Several instances could claim the same row | Atomic database claim (conditional update / rowversion) and a lease (TD-09) |
| UX trade-off | The portal refreshes every 3 s | Status changes appear with up to 3 s delay | Server push (SSE/SignalR) if scale or latency requires it (TD-21) |

Closed findings: StringLength before trim (fixed in Phase 02.2), HTTP 500 regression test (Phase 03.3), portal
responsive and keyboard review (Phase 04.3), frontend run instructions (Phase 04.4); EF race-log review resolved as
TD-22 (Phase 05.2). Requirement coverage: [`docs/requirements-traceability.md`](docs/requirements-traceability.md).

## AI usage

AI assistants were used as accelerators, not as a source of requirements: ChatGPT for challenge analysis, architecture
alternatives and phase planning; Claude Code for repository-aware implementation, test writing, documentation and
phase-by-phase reviews of its own changes. Nothing was considered done until the relevant checks had actually run:
builds, the automated suites (102 tests), SQL Server checks (migrations, indexes, collation, query plans), temporary
mutations to prove tests fail when the behavior breaks, real-browser acceptance on the real stack, and diff review.

Material corrections and rejections recorded along the way include: a malformed LocalDB connection string, length
validation on untrimmed input, ProblemDetails returned with the wrong media type (found by the HTTP tests), a startup
outage fix that kept the Worker alive but skipped recovery (found in review and corrected), a cancellable final state
write after a provider success, and a frontend error flag not tied to its filter; rejected options include placing the
processor (and therefore Polly) in Core and broadly suppressing EF error logs. Per-phase details:
[`docs/ai/ai-usage.md`](docs/ai/ai-usage.md). No prompts or conversation transcripts are stored in the repository.
