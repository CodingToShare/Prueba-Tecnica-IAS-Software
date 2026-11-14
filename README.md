# Service Requests Portal

Internal portal for a field-services company: requests are registered through an API and processed in the
background against an external provider. Users retry when the portal feels slow and the provider fails transiently,
so the same business operation (`externalReference`) must never create duplicate effects, and a `Completed`
request never moves back.

> **Status:** Phases 00–04 complete — domain, SQL Server persistence, idempotent HTTP API, background Worker (retries,
> startup recovery, Windows Service ready) and the React portal are implemented and validated. Progress and evidence:
> [`docs/phases/roadmap.md`](docs/phases/roadmap.md).

## Architecture

```text
React + TS portal (Vite, :5173)
        │  fetch  POST /api/requests · GET /api/requests[?status=]
        ▼
ASP.NET Core Web API (:5164) ──► ServiceRequests.Core ──► EF Core ──► SQL Server (LocalDB)
                                                                        ▲
.NET Worker (BackgroundService) ── claim / complete / fail ─────────────┘
        │
        ▼
Polly retry (transient only) ──► simulated external provider
```

| Project | Responsibility |
| --- | --- |
| `src/ServiceRequests.Core` | `ServiceRequest` entity and state transitions, replay rule, `ServiceRequestService`, repository and provider contracts. No framework packages. |
| `src/ServiceRequests.Infrastructure` | EF Core `DbContext`, mapping, migrations, repository (SQL Server duplicate-key translation), simulated provider, DI registration. |
| `src/ServiceRequests.Api` | Controller, DTOs, ProblemDetails, CORS. |
| `src/ServiceRequests.Worker` | `RequestProcessingWorker` (hosted loop), `ServiceRequestProcessor`, Polly pipeline, Windows Service hosting. |
| `tests/ServiceRequests.Tests` | xUnit + Moq unit tests, LocalDB integration tests, functional HTTP and Worker tests. |
| `frontend/service-requests-web` | React + TypeScript + Vite portal: native `fetch`, hooks, plain CSS from the design tokens. |

Dependencies point inward: Api/Worker → Infrastructure → Core. Decisions: [`docs/technical-decisions.md`](docs/technical-decisions.md).

## Prerequisites (validated environment)

- .NET 10 SDK (validated with 10.0.404) — the solution is driven from the .NET CLI.
- SQL Server LocalDB (`MSSQLLocalDB`, validated with SQL Server 2019 LocalDB) or another SQL Server reachable with a
  connection string.
- Node.js and npm for the portal (validated with Node 24.21 and npm 11.19).
- Git. No Docker or cloud services are needed.

## Run the full stack locally

From the repository root (Windows; one terminal per process):

```bash
# 1. .NET tools and packages
dotnet tool restore
dotnet restore

# 2. Database — explicit EF Core migrations (TD-16); the API and Worker never migrate on startup
dotnet ef database update --project src/ServiceRequests.Infrastructure --startup-project src/ServiceRequests.Api

# 3. API — terminal 1, http://localhost:5164 (launch profile "http")
dotnet run --project src/ServiceRequests.Api

# 4. Worker — terminal 2, processes Pending requests
dotnet run --project src/ServiceRequests.Worker

# 5. Portal — terminal 3
cd frontend/service-requests-web
npm ci
cp .env.example .env.local        # VITE_API_BASE_URL=http://localhost:5164
npm run dev                       # http://localhost:5173
```

Open **http://localhost:5173**. Step 2 creates `ServiceRequestsDb` on `(localdb)\MSSQLLocalDB` with the migrations
`InitialCreate` and `AddRequestTimestamps` (UNIQUE index on `ExternalReference`, index on `Status`). The API allows the
portal origin `http://localhost:5173` (CORS). `VITE_API_BASE_URL` is public browser configuration — never put secrets
in it; `.env.local` is ignored by Git.

### Reviewer demo (in the portal)

1. Register `REQ-1` / `CUS-1042` / `MANT-PREV` / any description → green "Solicitud registrada · 201 Created"; the
   row moves Pending → Processing → Completed by itself (the list refreshes every 3 s).
2. Submit exactly the same data again → blue "Ya existía una solicitud… no se creó un duplicado · 200 OK"; still one
   row (also with `req-1` in lower case: same identity).
3. Same reference with another customer → amber "ya existe con datos diferentes · 409 Conflict"; the form keeps your
   input and the stored request is unchanged.
4. Register a request with service code `UNSUPPORTED` → it ends **Failed** (permanent provider failure, no retry).
5. Use the filter (Todas / Pending / Processing / Completed / Failed) — each option queries the API with `?status=`.

With the default Worker settings a request completes in about a second (one simulated transient failure, a 200 ms
retry), so Pending/Processing can be brief. To watch them, start the Worker with slower settings (validated in 04.3):

```powershell
$env:Worker__PollingIntervalMilliseconds = "5000"; $env:Worker__RetryDelayMilliseconds = "5000"
dotnet run --project src/ServiceRequests.Worker
```

API-only walkthrough (both processes running):

```bash
curl -s -H "Content-Type: application/json" http://localhost:5164/api/requests \
  -d '{"externalReference":"REQ-1","customerId":"CUST-01","serviceCode":"INST","description":"Install service"}'
curl -s "http://localhost:5164/api/requests?status=Completed"     # REQ-1 appears after one simulated retry
```

The Worker log shows the claim, one transient failure (`attempt 1`) and `Completed REQ-1 after 2 attempt(s)`.

## Tests and build verification

```bash
dotnet build ServiceRequests.sln
dotnet test ServiceRequests.sln                       # backend: unit, LocalDB integration, functional
cd frontend/service-requests-web && npm run build && npm run lint   # strict TypeScript + Vite build, oxlint
```

| Suite | Needs | Covers |
| --- | --- | --- |
| Unit (`Domain/`, `Application/`, `Worker/` processor, provider, loop) | nothing | transitions (Completed terminal, Failed final), invariants, timestamps, replay rule, idempotent create incl. simulated UNIQUE race, retry/exhaustion/permanent/unexpected/cancellation, claim before provider call, startup recovery |
| Integration (`Persistence/`, `Worker/WorkerProcessingTests` processor cases) | LocalDB | migrations, UNIQUE and case-insensitive identity, timestamps, SQL filter and ordering, processing persisted to SQL Server |
| Functional (`Api/`, `Worker/WorkerProcessingTests` host cases) | LocalDB | 201 / 200 / 409 / 400 / filter / 500 through the real pipeline; API POST → Worker host → GET observes Completed (with retries) or Failed |

Persistence, HTTP and Worker tests create throwaway databases `ServiceRequestsDb_Tests_<guid>` from the migrations and
drop them afterwards. To use another SQL Server, set `SERVICEREQUESTS_TEST_SQLSERVER` to a connection string without a
database name. Last validated run: 100 passed, 0 failed, 0 skipped.

The portal has no automated UI test suite by design for this timed challenge (TD-12): it is validated by the strict
production build, oxlint and real-browser acceptance against the real API, SQL Server and Worker (create, replay, 409,
validation, filter, Failed, API outage and recovery, automatic refresh) plus review at 1440 / 768 / 390 px against the
design reference — see the Phase 04.3 evidence in the roadmap.

## Portal

- One screen per the design contract ([`docs/design/design-system.md`](docs/design/design-system.md)) and its
  reference: navy brand bar, "Nueva solicitud" form, "Solicitudes" table with a status filter; two columns from 980 px,
  stacked below, labeled rows at ≤ 640 px.
- Feedback by API result: 201 normal (green), 200 replay informational (blue), 409 warning (amber), 400 field errors,
  network/5xx generic error with "Reintentar" (server details are never shown).
- Status badges use color + text + shape (Pending dashed ring, Processing/Completed circle, Failed diamond in the
  `critical` tone, never the system-error red).
- "Actualizada" shows the server `updatedAt` (es-CO format; full timestamp on hover).
- Accessibility: skip link, labels, `aria-invalid`/`aria-describedby`, `aria-pressed` filter, visible focus,
  `prefers-reduced-motion`.

## API

| Method | Route | Result |
| --- | --- | --- |
| `POST` | `/api/requests` | `201` new · `200` equivalent replay (existing request) · `409` conflicting replay · `400` validation |
| `GET` | `/api/requests` | all requests, newest first (`createdAt` desc, then `id` desc) |
| `GET` | `/api/requests?status=Pending` | only that status (`Pending`, `Processing`, `Completed`, `Failed`; case-insensitive); unknown value → `400` |

Response body (201 and 200): `id`, `externalReference`, `customerId`, `serviceCode`, `description`, `status`
(string), `failureReason`, `createdAt`, `updatedAt`. Errors are RFC 7807 `application/problem+json`; unexpected
failures return a generic 500 without stack traces, SQL or connection details. Clients cannot set id, status or
timestamps.

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

## Background processing (Worker)

```text
Pending ──claim (persisted)──► Processing ──provider ok──► Completed (terminal)
                                   │
                                   ├─ transient failure → retry (Polly) → Completed, or Failed when retries run out
                                   ├─ permanent failure → Failed (no retry)
                                   └─ unexpected error  → logged; stays Processing; resumed on next Worker start
```

- **Claim before call:** the request is saved as `Processing` before the provider is called; if that save fails, the
  provider is not called.
- **Retries:** only the modeled `TransientProviderException` is retried. `Worker:MaxRetryAttempts` = 2 **retries**,
  i.e. at most **3 provider calls** (attempts logged 1–3), with a constant `Worker:RetryDelayMilliseconds` (200 ms).
  `PermanentProviderException` fails at once; any other exception and host cancellation are never retried and never
  turn a request into `Failed`.
- **State rules:** transitions only through the entity (`MarkProcessing`, `Complete`, `Fail`); `Completed` and
  `Failed` are final; `CreatedAt` never changes and `UpdatedAt` changes on each transition. Failure reasons are short
  messages, never stack traces.
- **Startup recovery:** before polling Pending requests, the Worker resumes every request left in `Processing`
  (oldest first) from the provider call, without claiming it again. This covers a stop or crash after the claim. If
  the database is unavailable at startup, recovery is retried every polling interval before any Pending request is
  processed. Safety depends on the provider being idempotent by `externalReference`.
- **Loop:** one request at a time, oldest first; a new DI scope (fresh `DbContext`) per unit of work; an error is
  logged and the loop continues after `Worker:PollingIntervalMilliseconds` (no tight error loop); shutdown stops at
  the next await. Once the provider has answered, the final state is saved even if shutdown was requested.
- **Single Worker (deliberate, A-04 / TD-09):** run one instance. Several instances would need an atomic database
  claim (conditional `UPDATE … WHERE Status = 'Pending'` or a rowversion/optimistic check) and a lease for stale
  `Processing` rows; not implemented.
- **Known trade-off:** a request hit by an unexpected error stays `Processing` until the next Worker start (it is not
  retried within the same run).

### Simulated provider and its limits

`SimulatedExternalProvider` is deterministic: each `externalReference` fails transiently
`ExternalProvider:TransientFailuresBeforeSuccess` times (default 1) and then succeeds; service codes listed in
`ExternalProvider:PermanentFailureServiceCodes` (default `UNSUPPORTED`, case-insensitive) fail permanently. A reference
that already succeeded returns success again without a second effect **while the process runs**. Its state is in
memory only and is lost when the Worker restarts, so the simulation does not prove exactly-once delivery. A real
provider must offer durable idempotency by `externalReference` (or an equivalent reconciliation process).

### Running as a Windows Service

The Worker calls `AddWindowsService` (service name `ServiceRequestsWorker`): it runs as a console app with
`dotnet run` and as a Windows Service when started by the Service Control Manager.

- **Validated:** the Worker host runs with `dotnet run`, and `dotnet publish` produces `ServiceRequests.Worker.exe`.
- **Documented, not executed here:** service registration (requires an elevated prompt):

```powershell
dotnet publish src/ServiceRequests.Worker -c Release -o .\publish\worker
sc.exe create ServiceRequestsWorker binPath= "C:\path\to\publish\worker\ServiceRequests.Worker.exe" start= auto
sc.exe start ServiceRequestsWorker
# or: New-Service -Name ServiceRequestsWorker -BinaryPathName "C:\path\to\publish\worker\ServiceRequests.Worker.exe" -StartupType Automatic
```

The service account needs access to the database (LocalDB is per-user, so a service would normally point
`ConnectionStrings__ServiceRequests` to a SQL Server instance). Remove with `sc.exe stop` / `sc.exe delete
ServiceRequestsWorker`.

## Configuration

| Key | Used by | Default (`appsettings.json`) |
| --- | --- | --- |
| `ConnectionStrings:ServiceRequests` | API, Worker | `Server=(localdb)\MSSQLLocalDB;Database=ServiceRequestsDb;Trusted_Connection=True` — override with `ConnectionStrings__ServiceRequests`; startup fails if missing |
| `Cors:AllowedOrigins` | API | `["http://localhost:5173"]` (Vite dev server; GET/POST, `Content-Type`) |
| `Worker:PollingIntervalMilliseconds` | Worker | `1000` (allowed 100–60000) |
| `Worker:MaxRetryAttempts` | Worker | `2` retries → 3 calls (allowed 1–5) |
| `Worker:RetryDelayMilliseconds` | Worker | `200` (allowed 0–10000) |
| `ExternalProvider:TransientFailuresBeforeSuccess` | Worker | `1` (allowed 0–10) |
| `ExternalProvider:PermanentFailureServiceCodes` | Worker | `["UNSUPPORTED"]` |
| `VITE_API_BASE_URL` | Portal (`.env.local`, from `.env.example`) | `http://localhost:5164`; if unset, the page origin is used. Public value, never a secret |

Worker settings are validated on startup. No secrets are committed; LocalDB uses Windows authentication. EF Core SQL
command logs are kept at `Warning` in both hosts.

## Current status and deferred work

| Area | State |
| --- | --- |
| Domain, persistence, migrations | Done and tested (Phase 01) |
| Idempotent API | Done and tested (Phase 02) |
| Worker, simulated provider, Polly retries, startup recovery, Windows Service readiness | Done and tested (Phase 03); service registration documented, not executed |
| React portal | Done and validated in a real browser (Phase 04); no automated UI suite (TD-12) |
| Critical test hardening | Pending — Phase 05 |
| NoSQL alternative note, legacy code review, final README | Pending — Phase 06 |

Known items: a handled concurrent-insert race is logged by EF Core at Error level (review in Phase 05); rows created
before `AddRequestTimestamps` would get `0001-01-01` timestamps (no such rows exist); a request hit by an unexpected
Worker error stays Processing until the next Worker start; simulated provider state is not durable across restarts;
single Worker instance only; the portal refreshes every 3 s (status changes appear with up to that delay). Traceability: [`docs/requirements-traceability.md`](docs/requirements-traceability.md).

## AI usage

AI assistants (ChatGPT for planning, Claude Code for implementation) were used as accelerators; their output was
accepted only after builds, tests, database checks and diff review. Per-phase evidence, including corrections:
[`docs/ai/ai-usage.md`](docs/ai/ai-usage.md). The final disclosure is completed in Phase 06.
