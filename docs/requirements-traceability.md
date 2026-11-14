# Requirements Traceability

Maps every challenge requirement to the phase, component and verification that cover it. Evidence is filled **only**
with checks that actually ran, in the same change that produces them.

Levels: **U** unit test · **I** integration test · **M** manual run (recorded) · **R** repository/review check · **D** documentation.
Status: `Planned` → `In Progress` → `Verified` (or `Deferred` with reason).

## Expected Results (challenge §2)

| ID | Requirement | Phase | Component | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| R-01 | API registers a request | 02 | `POST /api/requests` | U, M | 02.3: `ServiceRequestsApiTests` (WebApplicationFactory + LocalDB) — 201 new, 200 replay/case-variant replay with one row, 409 problem, 400 validation; 02.1 manual smoke | Verified |
| R-02 | API queries requests by status | 02 | `GET /api/requests?status=` | U, M | 02.3: HTTP tests for list, `?status=` filter (only that status) and unknown status → 400; repository test of SQL filter + ordering | Verified |
| R-03 | Web UI creates a request and shows its status (React) | 04 | `frontend/service-requests-web` | M, R | 04.3: browser acceptance against the real stack — create (201), replay (200), conflict (409), validation, status shown and updated automatically Pending → Processing → Completed; build/lint pass | Verified |
| R-04 | Real persistence + reproducible storage initialization | 01 | EF Core migrations, SQL Server | I/M | 01.3: `dotnet ef database update` applied `InitialCreate` to LocalDB `ServiceRequestsDb` (indexes checked with `sqlcmd`); LocalDB tests migrate a clean DB and round-trip data through a fresh context | Verified |
| R-05 | Background process: Pending → simulated provider → status updated; Windows Service path clear | 03, 06 | `ServiceRequests.Worker` | U, M, D | 03.3: `WorkerProcessingTests` — API POST → Worker host → GET observes Completed (with 2 retries) and Failed (permanent); LocalDB processor tests; 03.4 README walkthrough re-run (POST → Worker → GET Completed); Windows Service path documented in README (registration not executed) | Verified |
| R-06 | Automated tests on the most critical behavior | 01–05 | `tests/ServiceRequests.Tests` | U | 02.3: 71 tests pass (transitions, invariants, UNIQUE/collation, idempotency incl. race, HTTP contract); 03.3: 100 tests (65 unit, 15 LocalDB integration, 20 functional) incl. retry, exhaustion, permanent, unexpected, recovery, end-to-end processing, 500 | In Progress |
| R-07 | Brief log of decisions, assumptions, trade-offs, validations | 00, 06 | `technical-decisions.md`, README | D | | In Progress |

## Functional Rules (challenge §3)

| ID | Rule | Phase | Component | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| BR-01 | Request has externalReference, customerId, serviceCode, description, status | 01, 02 | entity, DTOs, migration | U, R | 01.3: entity + schema (unit, LocalDB round trip); 02.3: fields through POST/GET DTOs (HTTP tests) | Verified |
| BR-02 | States Pending, Processing, Completed, Failed | 01 | `ServiceRequestStatus` | U | 01.3: `ServiceRequestTests` transition matrix; statuses persisted as text | Verified |
| BR-03 | Retry of the same operation never creates two provider effects | 02, 03 | UNIQUE index, idempotent create, idempotent provider | U, I | 01.3: SQL Server rejects a duplicate ExternalReference (2601/2627, one row stored); 02.3: replay/case-variant replay → 200 with one row (HTTP), race re-read (unit), case-variant duplicate rejected by SQL Server (integration); 03.1/03.3: simulated provider replay has no second effect (effect count 1 after retries, end to end). Scope: API + single-process provider simulation; durable provider idempotency is a documented production assumption (TD-10) | Verified |
| BR-04 | Simulated provider returns success or transient failure | 03 | simulated provider | U | 03.1: `SimulatedExternalProviderTests` (configured transient failures then success, per reference; permanent code); 03.3: end-to-end retry → Completed | Verified |
| BR-05 | A Completed request never returns to an earlier state | 01, 03 | entity transitions | U | 01.3: entity rule verified (any transition from Completed throws, status kept); 03.1/03.3: processor only uses entity transitions; resume of a Completed request does nothing (unit) | Verified |
| BR-06 | Screen shows externalReference, customerId, serviceCode, status and filters by status | 04 | table + segmented filter | M | 04.3: columns present; filters requested as `?status=` Pending / Processing / Completed / Failed and "Todas" without filter; 1440/768/390 px reviewed | Verified |
| BR-07 | Ambiguities documented as assumptions | 00, 06 | `technical-decisions.md` (A-xx, OD-xx) | D | | In Progress |

## Technical Constraints (challenge §4)

| ID | Constraint | Phase | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| TC-01 | C# + ASP.NET Core Web API backend | 01 | R | 01.1–01.3: `net10.0` ASP.NET Core Web API (controllers) builds with SDK 10.0.404, 0 warnings | Verified |
| TC-02 | Exactly one frontend technology (React) | 04 | R | 04.1–04.3: React 19 + TypeScript + Vite only; no state, query, form or component libraries (`package.json`) | Verified |
| TC-03 | SQL implemented + 3–5 lines on the NoSQL alternative | 01, 06 | R, D | SQL part: SQL Server via EF Core migrations, LocalDB tests (01.3, 02.3); NoSQL paragraph pending 06 (draft in TD-03) | In Progress |
| TC-04 | Inputs, errors, configuration and secrets handled safely | 02–04 | U, R | 02.3/03.3: 400/409/500 ProblemDetails without internals (HTTP tests); 02.2: CORS restricted; 03: concise Worker failure reasons; 04.3: UI never shows server internals (5xx/network generic message), only public `VITE_API_BASE_URL`, no secrets | Verified |
| TC-05 | Worker runnable manually + Windows Service approach documented | 03, 06 | M, D | 03.1/03.4: `dotnet run --project src/ServiceRequests.Worker` processes requests (manual, README walkthrough); `AddWindowsService`; `dotnet publish` → exe; `sc.exe` / `New-Service` steps in README (not executed) | Verified |
| TC-06 | No Docker/cloud/CI required to run | all | R | Phases 01–02: build, migrations, API and tests ran with only the .NET SDK + LocalDB (README commands); re-check at 07 | In Progress |
| TC-07 | Executable path prioritized; unfinished parts documented (state · decision · next step) | 07 | D | | Planned |

## Legacy Review (challenge §5)

| ID | Item | Phase | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| LR-01 | WebForms: concrete risk (SQL injection) + minimal fix (parameterized `SqlCommand`) | 06 | D | | Planned |
| LR-02 | MVC: concrete risks (null `Find`, unrestricted status, Completed regression, entity returned) + minimal fix | 06 | D | | Planned |

## Delivery (challenge §6)

| ID | Item | Phase | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| D-01 | Git history shows the evolution of the solution | all | R | Local phase checkpoint commits (`git log`) | In Progress |
| D-02 | Brief reproducible instructions: backend, frontend, storage, Worker | 06 | M, D | 04.4: README "Run the full stack locally" (tools, database, API, Worker, portal with `.env.local` + `npm run dev`) — each command executed (02.4, 03.4, 04.4); final README review in 06 | Verified |
| D-03 | Executable tests + note on covered scenarios | 05, 06 | U, D | 04.4: README tests table + frontend validation note; 100 backend tests pass; hardening pending 05 | In Progress |
| D-04 | Decision log: context, alternatives, decision, trade-offs, risks, validation | 00, 06 | D | `technical-decisions.md` TD-01…TD-19; Phase 01–02 decisions carry all seven fields with executed validation (02.4) | In Progress |
| D-05 | Business/technical assumptions | 00, 06 | D | A-01…A-06 registered | In Progress |
| D-06 | AI usage note (what for, how verified, what corrected/rejected) | 06 | D | `docs/ai/ai-usage.md` — incremental evidence through 02.4; README links it; final disclosure pending Phase 06 | Planned |
