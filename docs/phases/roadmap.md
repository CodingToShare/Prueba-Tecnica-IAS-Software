# Roadmap — Time-Boxed Implementation Plan

Planned work: **115 min** · Contingency reserve: **5 min** · Hard stop: **120 min**
(rebalanced when Phase 00.1 was added; reserve was 10 min).
Real timing evidence comes from Git commit history; elapsed times are never fabricated.

Statuses: `Not Started` · `In Progress` · `Complete` · `Blocked` · `Deferred`.

| Phase | Scope | Target | Status |
| --- | --- | --- | --- |
| 00 | Agent governance and technical plan | 8 min | Complete |
| 00.1 | AI governance and usage evidence | 5 min | Complete |
| 01 | Solution foundation and persistence | 15 min | Complete |
| 01.1 | Solution, domain and persistence development | 7 min | Complete |
| 01.2 | Requirement, architecture and implementation review | 3 min | Complete |
| 01.3 | Unit, integration and functional validation | 5 min | Complete |
| 02 | Idempotent API | 22 min | Complete |
| 02.1 | Development | 10 min | Complete |
| 02.2 | Technical/requirements review | 4 min | Complete |
| 02.3 | Automated + functional tests | 6 min | Complete |
| 02.4 | Documentation/evidence | 2 min | Complete |
| 03 | Background processing and resilience | 20 min | Complete |
| 03.1 | Development | 9 min | Complete |
| 03.2 | Technical/resilience review | 3 min | Complete |
| 03.3 | Automated + functional validation | 6 min | Complete |
| 03.4 | Documentation/evidence | 2 min | Complete |
| 04 | React portal | 18 min | In Progress |
| 04.1 | Development | 8 min | Complete |
| 04.2 | UX / architecture / contract review | 3 min | Complete |
| 04.3 | Build + functional + visual validation | 5 min | Not Started |
| 04.4 | Documentation / evidence / AI | 2 min | Not Started |
| 05 | Critical test hardening | 12 min | Not Started |
| 06 | Documentation and legacy review | 10 min | Not Started |
| 07 | Final validation and delivery | 5 min | Not Started |
| — | Contingency reserve | 5 min | — |

## Phase Scopes

**00 — Governance.** AGENTS.md, CLAUDE.md, project context, 10 skills (incl. `ui-design-system`), design contract +
reference HTML, roadmap, technical decisions (+ assumptions, open decisions), requirements traceability, .gitignore.
No product code.

**00.1 — AI governance.** Tracked AI usage log (`docs/ai/ai-usage.md`), `ai-governance` skill, minimal routing updates
(AGENTS.md, CLAUDE.md, project context, phase-governance, documentation, code-review), verification and
correction/rejection evidence policy. No product code.

**01 — Foundation + persistence.** Solution + 4 src projects + test project; project references; `ServiceRequest` entity
with guarded transitions (+ transition tests); DbContext, mapping, UNIQUE `ExternalReference`, `Status` index; initial
migration applied to SQL Server/LocalDB; configuration without secrets.

**02 — Idempotent API.** `POST /api/requests` (201/200/409), `GET /api/requests[?status=]`, DTO validation, ProblemDetails,
unique-violation race handling, CORS for frontend dev origin; idempotency + validation tests.

**03 — Worker + resilience.** Provider interface + idempotent simulated provider with configurable transient failures;
processor (claim → provider → Completed/Failed); Polly bounded retry on transient only; BackgroundService host with
Windows Service support; startup resume of orphaned `Processing` requests; structured logs; retry/exhaustion tests.

**04 — React portal.** Vite React TS app styled per `docs/design/design-system.md` using the reference HTML patterns
(tokens.css, brand bar, form panel, segmented filter, stacked table, status badges, submit alerts);
loading/empty/no-results/error/sending states; build passes; design review at ~1440/768/390 px side by side with
`docs/design/reference/design-system-v3.html` + keyboard pass.

**05 — Test hardening.** Fill gaps in highest-risk scenarios (TheoryData/MemberData where readable); full regression.

**06 — Documentation + legacy review.** README run instructions, test note, Windows Service approach, NoSQL alternative,
assumptions, trade-offs, legacy WebForms/MVC review, AI usage note, deferred work.

**07 — Final validation.** Run the delivery checklist below, final diff review, delivery commit.

## Delivery Checklist (Phase 07)

Every item has a command or a place to look; record results in Phase 07 evidence.

- [ ] `git status --short` shows no unintended changes; no remote/push.
- [ ] No secrets or local artifacts tracked:
      `git ls-files | grep -iE '(^|/)\.env$|\.pfx$|\.key$|appsettings\..*\.local\.json|bin/|obj/|node_modules/|TestResults/'` prints nothing.
- [ ] Committed config has no passwords: `git grep -niE 'password=|pwd=' -- '*.json'` prints nothing.
- [ ] Clean database path works: drop/recreate DB → apply migrations exactly as README says.
- [ ] `dotnet build ServiceRequests.sln` and `dotnet test` pass (report counts).
- [ ] `npm ci` + `npm run build` pass in `frontend/service-requests-web`.
- [ ] Manual smoke, following README: create → 201; same payload → 200 (no duplicate row); different payload → 409;
      Worker moves Pending → Completed (and a forced transient failure path → Failed); UI filter shows each status.
- [ ] Requirements traceability: every row Verified or Deferred with reason.
- [ ] README instructions, test note, NoSQL paragraph, Windows Service, assumptions, limitations, legacy review and
      AI usage note match the final behavior.

## Baseline Validation (grows per phase)

| Since | Command |
| --- | --- |
| 00 | `git status`, `git diff --check` |
| 01 | `dotnet tool restore`, `dotnet build ServiceRequests.sln`, `dotnet test ServiceRequests.sln` (persistence tests need LocalDB or `SERVICEREQUESTS_TEST_SQLSERVER`) |
| 04 | `npm run build` (in `frontend/service-requests-web`) |

## Phase Evidence

### Phase 00 — Complete

- Git: repository initialized locally (no remote). User identity preserved.
- Environment inspected: Git 2.54, Node 24.21, npm 11.19, .NET SDK 8.0.403 only, SQL Server LocalDB `MSSQLLocalDB` present.
- **Open item for Phase 01 entry:** .NET 10 SDK (approved stack) is not installed — user must install it or approve a
  .NET 8 LTS fallback decision before Phase 01 starts.
- Files: AGENTS.md, CLAUDE.md, `.agents/context/project-context.md`, 9 skills, this roadmap, `docs/technical-decisions.md`, `.gitignore`.
- Validation: `git diff --check` clean; actual staged diff reviewed; no application code or dependencies introduced.
- Timing: see Phase 00 commit timestamp.
- **Follow-up (user review):** the design part of the reference `.agents` was missing. Added the `ui-design-system`
  skill and `docs/design/design-system.md` (navy `#1E3A5F`, Plus Jakarta Sans, semantic tokens, states, responsive
  and accessibility rules). The reference's HTML design file was not in the zip — non-brand tokens are derived (TD-13).
  Also adapted from the reference: structured logging rules (TD-14), Worker startup recovery of orphaned `Processing`
  requests (TD-09), testing hygiene (descriptive names, no hidden skips, no fake evidence, `TimeProvider`),
  documentation categories + README outline, security/config rules, dependency pinning, tracked-governance rule.
  Discarded as not applicable: Go/PostgreSQL/Next.js/TanStack/ECharts specifics, analytics/LLM skills, Docker, CI,
  Prometheus metrics, Copilot entry file.
- **Follow-up 2 (`docs.zip` provided):** the original design files were found. `docs/design/reference/design-system-v3.html`
  copied unmodified (SHA-256 identical to the source; inline JS reviewed: menu/filter/chart demo only, no network calls).
  `docs/design/design-system.md` rewritten from the original contract with the real reference tokens (replacing the
  derived values), request-status → tone mapping, submit-outcome → alert mapping, component subset and a recorded
  deviation (brand bar instead of sidebar). Also adapted from `docs.zip`: Definition of Done categories (AGENTS.md §8),
  requirements traceability matrix (`docs/requirements-traceability.md`), delivery checklist (above), assumptions /
  open-decisions registers, and the decision format required by challenge §6 (alternatives + risks were missing).
  Not adopted: ADR directory, per-phase documents, OpenAPI file, Playwright/axe, Compose/CI, product/AI docs of the other domain.
- **Follow-up 3 (design decision, owner request):** the reference HTML was rewritten as the Portal de Solicitudes
  edition of v3 — same tokens/patterns; content now shows this product: brand bar, request form, segmented status
  filter, request table, lifecycle (Completed terminal), background processing with transient retries, colors mapped
  to request states, submit-outcome alerts (201/200/409/400/network) and view states; energy content, KPIs, charts,
  queue and sidebar removed. Validation: headless Edge renders at
  1440/768/390 px reviewed (fixed: codes wrapping at hyphens, stacked reference cell); tag balance, unique ids and
  CSS variables checked by script. Contract, skill, AGENTS.md, project context and TD-13 updated accordingly.

### Phase 00.1 — Complete

- Scope: created `docs/ai/ai-usage.md` (policy + usage log, rows for 00 and 00.1 based on existing roadmap evidence)
  and `.agents/skills/ai-governance/SKILL.md`; minimal updates to AGENTS.md (AI authority rule, routing, workflow,
  DoD §8.10), CLAUDE.md, project context, phase-governance, documentation and code-review skills; D-06 evidence pointer
  (status stays Planned until Phase 06); timebox rebalanced (115 planned + 5 reserve).
- Validation: `git diff --check` clean; `git check-ignore -v docs/ai/ai-usage.md` → no rule (tracked); new files
  scanned for secrets/paths (only false positives: design "tokens", policy wording); staged diff reviewed.
- Not touched: the pre-existing user modification of `docs/design/reference/design-system-v3.html` (unstaged).
- No product code, packages or assistant-specific config added. OD-01 still open; Phase 01 Not Started.
- Timing: see Phase 00.1 commit timestamp.

### Phase 01.1 — Complete (Phase 01 In Progress)

- Entry: first attempt blocked (only SDK 8.0.403). User installed the .NET 10 SDK; retry found 10.0.404 as the active
  SDK (`dotnet --version`). OD-01 resolved without changing TD-01.
- Scope: `ServiceRequests.sln` (classic `.sln` format) with Core, Infrastructure, Api (controllers), Worker and Tests
  (xUnit), all `net10.0`, nullable + implicit usings on. Template samples removed (WeatherForecast, `.http`, Class1,
  Worker.cs, UnitTest1, Worker `UserSecretsId`). References: Infrastructure → Core; Api/Worker → Infrastructure;
  Tests → Core + Infrastructure. Core has no package or project references.
- Domain: `ServiceRequestStatus`; `ServiceRequest` (Guid v7 id, trimmed non-blank business fields, private setters,
  starts Pending; `MarkProcessing`/`Complete`/`Fail(reason)` throw `InvalidOperationException` on invalid transitions;
  Completed and Failed final; failure reason truncated to 500).
- Persistence: `ServiceRequestsDbContext` + explicit `ServiceRequestConfiguration` (lengths 100/50/50/1000/500,
  status as `nvarchar(20)` text, `UX_ServiceRequests_ExternalReference` UNIQUE, `IX_ServiceRequests_Status`);
  `AddInfrastructure` registers only the DbContext and fails fast without `ConnectionStrings:ServiceRequests`.
  LocalDB trusted connection default in Api/Worker `appsettings.json`. EF Core 10.0.15; local tool `dotnet-ef` 10.0.15
  (`dotnet-tools.json`). `InitialCreate` generated by `dotnet ef migrations add` (not hand-written).
- Validation (executed): `dotnet restore` OK; `dotnet build ServiceRequests.sln --no-restore` → 0 warnings, 0 errors;
  `dotnet tool restore` OK; `dotnet ef migrations list --project src/ServiceRequests.Infrastructure --startup-project
  src/ServiceRequests.Api` → `20261114150901_InitialCreate (Pending)`; `dotnet test` runs and reports no tests
  available (expected until 01.3); all `appsettings*.json` parse as JSON.
- Not validated yet (Phase 01.3): `dotnet ef database update` on an empty database, schema inspection, entity tests.
- Not implemented (by scope): API endpoints, Worker processing, Polly, frontend.
- Finding (open): the design contract table has an "Actualizada" column, but the model has no timestamp field
  (not in the 01.1 field list). Decide in Phase 02 (add timestamps + migration) or adjust the column in Phase 04.
- Timing: see Phase 01.1 commit timestamp.

### Phase 01.2 — Complete (review gate; Phase 01 In Progress)

- Baseline (executed): SDK 10.0.404; `dotnet restore` OK; `dotnet build ServiceRequests.sln --no-restore` → 0 warnings,
  0 errors; `dotnet tool restore` OK; `dotnet ef migrations list` → `InitialCreate (Pending)` with no connection
  warnings (`--verbose`).
- Architecture (from `.csproj`): Core has no package/project references; Infrastructure → Core (+ EF SqlServer);
  Api/Worker → Infrastructure; Tests → Core + Infrastructure; no cycles. Scope scan (`grep` over src/tests) found no
  MediatR, AutoMapper, Polly, repositories, hosted services, startup migration, auth, endpoints or cache.
- Domain: Status/fields have private setters on a sealed class; the only mutators are `MarkProcessing`, `Complete`,
  `Fail`; each checks the single allowed source state, so Completed and Failed cannot move; constructor trims and
  rejects null/blank values and starts Pending. No public bypass found (only reflection/EF materialization).
- Persistence: migration Up matches the configuration (PK `Id`; required `nvarchar(100/50/50/1000)`; `Status`
  `nvarchar(20)`; nullable `FailureReason nvarchar(500)`; no `nvarchar(max)`; `UX_ServiceRequests_ExternalReference`
  UNIQUE; `IX_ServiceRequests_Status`). Designer model identical to snapshot;
  `dotnet ef migrations has-pending-model-changes` → no changes. Config: LocalDB trusted connection, no credentials.
- OD-03 consistent: no `Migrate()`/`EnsureCreated()` in code; TD-16 explicit command only.
- Testability: entity is directly constructible (pure unit tests); `ServiceRequestsDbContext` takes
  `DbContextOptions`, so tests can target a dedicated LocalDB database and apply migrations in a fixture. No
  production change needed. OD-04 resolved → TD-17 (LocalDB).
- Findings: no BLOCKER/HIGH/MEDIUM. LOW (fixed): `testing` and `sqlserver-efcore` skills still described OD-04/OD-03
  as undecided → aligned. Remaining LOW/NOTE items deferred below.
- Deferred findings (finding · impact · reason · next step):
  - `ExternalReference` uniqueness follows the database collation (SQL Server defaults are case-insensitive) · `REQ-1`
    and `req-1` would collide · idempotency semantics belong to Phase 02 · confirm collation in 01.3 and record the
    comparison rule with OD-02 in Phase 02.
  - Entity does not enforce max lengths · over-long input would fail at `SaveChanges` · HTTP validation is Phase 02 ·
    DTO data annotations reuse the `ServiceRequest.*MaxLength` constants.
  - No timestamps (carried from 01.1) · "Actualizada" column has no data · Phase 02/04 decision · decide with the
    list endpoint.
  - No concurrency token · double claim only with multiple Workers · TD-09 single-instance assumption · revisit only
    if the assumption changes.
  - Api template `https` launch profile kept without HTTPS redirection · none for local dev · harmless · revisit with
    CORS/dev origin in Phase 02.
- Timing: see Phase 01.2 commit timestamp.

### Phase 01.3 — Complete → Phase 01 Complete

- Baseline: `dotnet restore` OK; `dotnet build ServiceRequests.sln --no-restore` → 0 warnings, 0 errors.
- Tests added (`tests/ServiceRequests.Tests`):
  - Unit `Domain/ServiceRequestTests` (27): starts Pending with trimmed values; null/empty/whitespace rejected per
    parameter (`MemberData`); the 3 allowed transitions; all 9 disallowed transitions throw and keep status/reason
    (`TheoryData`; Completed terminal, Failed final per A-03); blank failure reason rejected; reason truncated to 500;
    no public setters.
  - Integration `Persistence/ServiceRequestPersistenceTests` (4, real LocalDB): all migrations applied on a clean
    database; `sys.indexes` shows UNIQUE on ExternalReference and non-unique on Status; duplicate ExternalReference
    from a distinct entity rejected with `SqlException` 2601/2627 and only one row stored; Failed status stored as
    text `Failed` with its reason.
  - Functional `Persistence/ServiceRequestRoundTripTests` (2): domain entity → EF Core → LocalDB → fresh context
    keeps business data and Pending; status query returns only Pending rows of its batch.
  - `LocalDbFixture`: unique database `ServiceRequestsDb_Tests_<guid>` built with `MigrateAsync`, dropped on dispose;
    server overridable via `SERVICEREQUESTS_TEST_SQLSERVER`.
- Regression: `dotnet build ServiceRequests.sln` → 0 warnings, 0 errors; `dotnet test ServiceRequests.sln --no-build`
  → 33 passed, 0 failed, 0 skipped (TRX counters).
- Test quality (temporary mutations, reverted with `git checkout`): `unique: false` in the migration → 2/2 index
  tests fail; transition guard disabled → 9/9 invalid-transition tests fail.
- Database cleanup: after the run `sys.databases` lists no `ServiceRequestsDb_Tests_*` database.
- Migration smoke (TD-16 path): `dotnet tool restore`; `dotnet ef database update --project
  src/ServiceRequests.Infrastructure --startup-project src/ServiceRequests.Api` → Done; `dotnet ef migrations list` →
  `20261114150901_InitialCreate` applied; `sqlcmd` on `ServiceRequestsDb`: `PK_ServiceRequests`,
  `UX_ServiceRequests_ExternalReference` (unique), `IX_ServiceRequests_Status`; `__EFMigrationsHistory` has InitialCreate.
- Collation confirmed `SQL_Latin1_General_CP1_CI_AS` (case-insensitive): `REQ-1` and `req-1` collide on the UNIQUE
  index → comparison rule to be recorded with OD-02 in Phase 02 (01.2 deferred finding).
- Phase 01 acceptance: .NET 10 build, framework-free Core (01.2), protected state, Completed terminal, Failed final,
  SQL Server persistence, migration on empty database, UNIQUE enforced by SQL Server, Status indexed, round trip,
  tests green, no Phase 02 code (no endpoints/Worker processing) — all met.
- Timing: see Phase 01.3 commit timestamp.

### Phase 02.1 — Complete (Phase 02 In Progress)

- Entry: SDK 10.0.404; baseline `dotnet build` 0 warnings/0 errors, `dotnet test` 33/33; working tree clean.
- Core: `ServiceRequest` gains `CreatedAt`/`UpdatedAt` (UTC, supplied as `now` by the caller's `TimeProvider`;
  transitions update only `UpdatedAt`) and `HasEquivalentPayload` (OD-02 rule); `IServiceRequestRepository`
  (find by reference, try-add, list); `ServiceRequestService` (lookup → compare → insert → on UNIQUE race re-read and
  compare with the same rule; outcomes Created/Replayed/Conflict). Core still has no package references.
- Infrastructure: `ServiceRequestRepository` (plain `==` lookup so the CI collation decides identity; `SqlException`
  2601/2627 → `TryAddAsync` returns false and detaches; list filtered in SQL, ordered `CreatedAt DESC, Id DESC`);
  timestamps mapped as required `datetimeoffset`; DI registers repository and `TimeProvider.System`.
- Migration `AddRequestTimestamps` generated by `dotnet ef migrations add`; `InitialCreate` untouched;
  `has-pending-model-changes` → none.
- Api: `ServiceRequestsController` — `POST /api/requests` (201 / 200 / 409 ProblemDetails), `GET /api/requests[?status=]`
  (names case-insensitive; unknown or numeric → 400 ValidationProblemDetails; empty → no filter); DTOs
  `CreateServiceRequestRequest` (business fields only, `[Required]` + `[StringLength]` from entity constants) and
  `ServiceRequestResponse` (id, fields, status as string, failureReason, createdAt, updatedAt); `AddProblemDetails` +
  `UseExceptionHandler` + `UseStatusCodePages`; CORS policy from `Cors:AllowedOrigins` (default
  `http://localhost:5173`, GET/POST, Content-Type); structured logs for created/replayed/conflict; EF SQL command log
  lowered to Warning.
- Tests during development: existing tests adapted to the `now` parameter; new `ServiceRequestTimestampAndReplayTests`
  (timestamps, rejected transition keeps `UpdatedAt`, OD-02 matrix). Full validation belongs to 02.3.
- Validation (executed): `dotnet build ServiceRequests.sln` → 0 warnings, 0 errors; `dotnet test` → 43 passed, 0
  failed, 0 skipped; `dotnet tool restore`; `dotnet ef migrations list` → `InitialCreate`, `AddRequestTimestamps`;
  `dotnet ef database update` applied it to `ServiceRequestsDb`.
- Manual smoke (API on `http://localhost:5199`, Development): new → 201; replay with `req` lower-cased and padded →
  200 same id; `customerId` case change → 409 ProblemDetails; blank/missing fields → 400 with field errors; client
  `status` ignored (created Pending); `?status=pending` filters; `?status=Bogus` and `?status=1` → 400; CORS preflight
  allowed for `http://localhost:5173` only; rows stored once with UTC timestamps. Smoke rows deleted afterwards.
- Open findings: `AddRequestTimestamps` gives rows that existed before it `0001-01-01` timestamps (no such rows in any
  database here) · DTO `[StringLength]` measures the untrimmed value · 500 path not yet exercised (02.3).
- Timing: see Phase 02.1 commit timestamp.

### Phase 02.2 — Complete (review gate; Phase 02 In Progress)

- Baseline: `dotnet build` 0 warnings/0 errors; `dotnet test` 43/43.
- Code review (actual files): one equivalence rule (`CompareWithExisting` → `HasEquivalentPayload`) used on the first
  lookup and after the UNIQUE race; `SqlException`/2601/2627 only in Infrastructure; `CancellationToken` flows
  controller → service → repository → EF; no case-folding for identity (the only `IgnoreCase` parses the status
  filter); request DTO has no id/status/timestamp fields; `InitialCreate` last changed in `27b5112` (Phase 01.1).
- Idempotency challenge (API on LocalDB, Development): new `REVIEW-…` → 201; identical replay → 200; lower-cased
  reference → 200; lower-cased reference + `cust-01` → 409; changed ServiceCode → 409; changed Description → 409;
  `sqlcmd`: one row, original payload unchanged. 12 concurrent POSTs of a new reference → 1×201, 11×200, one row
  (the burst did not reach the UNIQUE-race branch; deterministic race test required in 02.3).
- Collation: `SQL_Latin1_General_CP1_CI_AS`; EF sends `WHERE [s].[ExternalReference] = @externalReference`
  (`nvarchar(100)`, value not logged); `SHOWPLAN_TEXT` for a different-case lookup → Index Seek on
  `UX_ServiceRequests_ExternalReference`.
- Errors (unreachable server via `ConnectionStrings__ServiceRequests`, Development): GET → 500 ProblemDetails with
  only type/title/status/traceId (JSON and HTML requests; no stack, SQL or host); exception logged server-side;
  unknown route → 404 ProblemDetails; malformed JSON → 400 ValidationProblemDetails.
- Findings: no BLOCKER/HIGH/MEDIUM. LOW (fixed): `[StringLength]` measured untrimmed values, so a padded value that
  fits after trimming got 400 → request DTO now trims in `init` accessors before validation; smoke: padded 100-char
  reference → 201 stored with length 100, 101 chars → 400, blank/null → 400.
- Notes (deferred): UNIQUE-race branch not yet exercised (02.3) · when that branch runs, EF Core may log the handled
  `DbUpdateException` at Error level — confirm in 02.3 and decide if it needs filtering · malformed-JSON 400 includes
  the framework parser message (no internals) · `?status=` empty means no filter · no pagination by decision ·
  `AddRequestTimestamps` default for pre-existing rows (none exist).
- Smoke/review rows deleted from `ServiceRequestsDb` afterwards.
- Timing: see Phase 02.2 commit timestamp.

### Phase 02.3 — Complete (Phase 02 In Progress)

- Baseline: `dotnet build` 0 warnings; `dotnet test` 43/43.
- Test dependencies (pinned): Moq 4.20.72, Microsoft.AspNetCore.Mvc.Testing 10.0.15; Tests → Api reference;
  `public partial class Program` added for `WebApplicationFactory<Program>`.
- Unit (46 = 37 domain + 9 application): OD-02 matrix (trimmed, case-significant), timestamps;
  `Application/ServiceRequestServiceTests` with a strict Moq repository — new → Created with fixed `TimeProvider`
  timestamps; equivalent replay (case-variant reference, padded values) → Replayed, `TryAddAsync` never called;
  3 conflicting payloads → Conflict, never inserted; UNIQUE race (lookup null → insert false → re-read winner) →
  Replayed or Conflict with the same rule and exactly two lookups; race with missing winner → throws; token passed.
- Integration (12, LocalDB): Phase 01 tests plus `Persistence/ServiceRequestRepositoryTests` — both migrations
  applied and `CreatedAt`/`UpdatedAt` are non-null `datetimeoffset`; `REQ…` then `req…` as a distinct row rejected
  by SQL Server (2601/2627); repository `TryAddAsync` with a case-variant duplicate returns false and keeps one row;
  different-case lookup returns the stored casing; timestamps round-trip exactly in UTC; SQL status filter and
  ordering `CreatedAt DESC` then `Id DESC` (ties checked with `SqlGuid` ordering).
- Functional HTTP (13, `WebApplicationFactory` + LocalDB): POST new → 201 Pending with equal timestamps, one row;
  same request → 200 same id, one row; padded lower-case reference → 200 same id and stored casing; lower-case
  reference + `cust-01` → 409 `application/problem+json`, original row unchanged; 5 invalid bodies (`MemberData`)
  → 400 `application/problem+json` with the field in `errors`; GET lists persisted data; `?status=Pending` /
  `?status=processing` return only that status; `?status=Banana` and `?status=1` → 400.
- Product correction found by the tests: `[Produces("application/json")]` on the controller forced 400/409
  ProblemDetails to `application/json`; attribute removed → `application/problem+json`.
- Regression: `dotnet build ServiceRequests.sln` → 0 warnings, 0 errors; `dotnet test ServiceRequests.sln --no-build`
  → total 71, passed 71, failed 0, skipped 0 (TRX counters). Test host used the per-run test database (dev database
  row count 0 before and after).
- Quality (temporary mutations, each reverted with `git checkout`): case-sensitive collation on ExternalReference
  in the migration → 4/4 case tests fail; no re-read after the race → 3/3 race tests fail; payload comparison made
  case-insensitive → 5/11 targeted tests fail (the casing cases); status filter removed → 2/2 filter tests fail.
- 02.2 note resolved: a temporary log probe (deleted, not committed) showed that a handled UNIQUE race makes EF Core
  log two Error entries (`CommandError`, `SaveChangesFailed`, parameter values hidden). Deferred LOW finding:
  misleading error noise on a handled replay race · reason: changing EF log levels affects all failures and needs
  its own decision · next step: Phase 05 — demote those events or filter them for the duplicate case, and record it.
- Migration smoke: `dotnet ef database drop --force` (challenge database, 0 rows) → `dotnet tool restore` →
  `dotnet ef database update --project src/ServiceRequests.Infrastructure --startup-project src/ServiceRequests.Api`
  → Done; history has `InitialCreate` and `AddRequestTimestamps`; PK, UNIQUE and Status indexes present; timestamp
  columns `datetimeoffset`. No `ServiceRequestsDb_Tests_*` databases left.
- Timing: see Phase 02.3 commit timestamp.

### Phase 02.4 — Complete → Phase 02 Complete

- `README.md` created as the current execution guide (purpose, architecture, validated prerequisites, database
  initialization, build/run/test, API, idempotency semantics, configuration, status, deferred work, AI link).
  Every command was executed in this phase or earlier: `dotnet tool restore`, `dotnet restore`,
  `dotnet ef database update …`, `dotnet build ServiceRequests.sln`, `dotnet run --project src/ServiceRequests.Api`
  (launch profile → `http://localhost:5164`, GET → 200), `dotnet test ServiceRequests.sln`; startup with an empty
  `ConnectionStrings__ServiceRequests` fails with the configured `InvalidOperationException`.
- Decisions: TD-03, TD-05, TD-06, TD-17, TD-18, TD-19 completed with "Future evolution"; TD-03 validation updated
  (was still "pending"). OD-01…OD-04 all resolved with dates.
- Traceability: TC-03, TC-06, D-02, D-03, D-04 updated with executed evidence; final README items remain Phase 06.
- AI log: Phase 02 rows checked against history; missing row for the `f0c84ab` documentation follow-up added.
- Design reference: the owner's deletion of the "changes" section was already committed in `f0c84ab` together with
  the documentation cleanup; no HTML diff remains and no stale references are found (`grep`).
- **Phase 02 summary:** build 0 warnings/0 errors; tests 71/71 (46 unit, 12 LocalDB integration, 13 HTTP), 0 skipped;
  migrations `InitialCreate` + `AddRequestTimestamps` applied to a recreated empty database; HTTP 201/200/409/400 and
  filter verified; case-variant duplicate rejected by SQL Server (`SQL_Latin1_General_CP1_CI_AS`); race re-read
  verified by unit tests; review findings: DTO trim before length validation (02.2), ProblemDetails media type
  (02.3); deferred: EF error logs on a handled race (Phase 05). Commits: 02.1 `a491ccc`, 02.2 `47b8067`,
  02.3 `147c7db`, 02.4 this checkpoint (`git log`).
- Timing: see Phase 02.4 commit timestamp.

### Phase 03.1 — Complete (Phase 03 In Progress)

- Entry: SDK 10.0.404; baseline build 0 warnings, tests 71/71; working tree clean.
- Core: `IExternalProvider` (takes the request; `ExternalReference` is the idempotency key),
  `TransientProviderException`, `PermanentProviderException`; repository gains `FindNextPendingAsync` (oldest first,
  tracked), `FindByIdAsync` (tracked) and `UpdateAsync`. Core still has no package references.
- Infrastructure: `SimulatedExternalProvider` + `ExternalProviderOptions` (`TransientFailuresBeforeSuccess`,
  `PermanentFailureServiceCodes`); attempts and applied effects tracked per reference (case-insensitive, thread-safe,
  in memory only); a processed reference replays as success without a second effect.
- Worker: `ServiceRequestProcessor` (claim → persist Processing → provider through Polly → Complete/Fail → persist;
  resume path for Processing without re-claim; unexpected exceptions propagate and leave Processing);
  `CreateProviderPipeline` = Polly retry handling only `TransientProviderException`, `MaxRetryAttempts` 2, constant
  delay, no jitter; `RequestProcessingWorker` (startup recovery of Processing ids, then one Pending at a time, scope per
  unit, errors logged and loop continues, cancellation = normal shutdown, `Task.Delay` with `TimeProvider`);
  `WorkerOptions` + provider options validated on start; `AddWindowsService` with service name
  `ServiceRequestsWorker`; Worker `appsettings.json` keeps EF SQL command logs at Warning.
- Packages (pinned): `Polly.Core` 8.9.1, `Microsoft.Extensions.Hosting.WindowsServices` 10.0.15 (Worker). Tests now
  reference the Worker project.
- Focused tests: `Worker/ServiceRequestProcessorTests` (9) and `Worker/SimulatedExternalProviderTests` (6).
- Validation (executed): `dotnet build` 0 warnings/0 errors; `dotnet test` 86/86, 0 skipped. Manual run (API +
  Worker, Development, default config): normal request → claimed, transient failure on attempt 1, Completed on
  attempt 2; `serviceCode` `UNSUPPORTED` → Failed after one call with the reason stored; both `UpdatedAt > CreatedAt`.
  Startup recovery: a row inserted directly as Processing was resumed (no re-claim) and Completed. Processes stopped
  and test rows deleted.
- Self-review fix before commit: a database failure while listing Processing rows at startup escaped the loop and
  would stop the host; now logged and polling continues. Checked with an unreachable server
  (`ConnectionStrings__ServiceRequests` override): recovery error logged, polling errors logged each interval, process
  stays alive, no host failure.
- Open findings: graceful-shutdown logging not observed (processes were force-stopped) — check in 03.3 ·
  an unexpected failure leaves its request in Processing until the next Worker start (by design) · simulated
  provider state is lost on restart (documented limitation) · Phase 02 findings 1, 2 unchanged; 500 regression test
  scheduled for 03.3.
- Timing: see Phase 03.1 commit timestamp.

### Phase 03.2 — Complete (review gate; Phase 03 In Progress)

- Baseline: build 0 warnings; tests 86/86.
- Reviewed actual code: Core/Infrastructure reference no Worker or Polly (TD-20 holds; transitions stay in the
  entity); claim is awaited and persisted before the provider call, so a failed claim save throws before any provider
  call; Polly handles only `TransientProviderException` (2 retries, 3 calls; attempts logged 1..3); cancellation
  (`OperationCanceledException`) is not handled by the pipeline or the processor, never becomes `Failed`, and the
  Worker treats it as shutdown; Worker holds only singletons and opens one disposed scope per unit;
  one `TimeProvider` for timestamps and delays; `AddWindowsService("ServiceRequestsWorker")` keeps console execution.
- **B-01 (BLOCKER, fixed):** when listing Processing rows failed at startup (database unavailable), the 03.1 code
  logged and went straight to Pending polling; recovery was never retried, so Processing rows stayed stranded until
  the next restart and newer Pending requests ran first. The 03.1 evidence ("process stays alive") was true but the
  recovery guarantee was not. Fix: recovery is retried every polling interval until the list succeeds; Pending
  polling starts only afterwards; orphans resumed oldest first. New `Worker/RequestProcessingWorkerTests`: list fails
  twice then succeeds → recovered before the first Pending poll (fails against the 03.1 code: verified by running it
  on the previous file); one orphan with an unexpected error does not prevent recovering the next.
- **M-01 (MEDIUM, fixed):** the final Completed/Failed write used the shutdown token; a stop right after a provider
  success could cancel it and leave the request Processing (second provider call after restart). The final write now
  uses `CancellationToken.None` (bounded by the host shutdown timeout); provider call, retry delays, claim and polling
  still honor the token.
- LOW: no test yet for "claim save fails → provider not called" (code order is correct) — add in 03.3 · a database
  outage in the middle of recovery leaves the affected rows Processing until restart (same accepted trade-off).
- Notes / accepted: unexpected exception → not retried, logged, request stays Processing until the next Worker start
  (single-Worker trade-off, TD-20; not retried within the same run) · permanent service-code match is
  case-insensitive by provider configuration (documented in `ExternalProviderOptions`), independent of OD-02 ·
  simulated provider records an effect with an atomic `TryAdd`, so concurrent calls for one reference produce at most
  one effect; state is per process only · Phase 02 findings: timestamp backfill and StringLength-before-trim remain
  non-blocking, 500 regression still pending 03.3, EF command logs stay at Warning.
- Validation: focused Worker tests 17/17; full `dotnet build` 0 warnings, `dotnet test` 88/88, 0 skipped.
- Timing: see Phase 03.2 commit timestamp.

### Phase 03.3 — Complete (Phase 03 In Progress)

- Baseline: build 0 warnings; tests 88/88.
- Production change for testability only: Worker registrations moved from `Program.cs` into
  `AddRequestProcessing(IServiceCollection, IConfiguration)`; `Program` calls it (same behavior), so tests use the
  production composition.
- Unit (65): processor — transient, transient, success → exactly 3 calls, Completed, `CreatedAt` unchanged,
  `UpdatedAt` advanced, persisted [Processing, Completed]; always transient → 3 calls, Failed "after 3 attempts";
  permanent → 1 call, Failed; unexpected → 1 call, propagates, stays Processing; cancellation during the provider
  call → 1 call, not Failed; claim save fails → provider never called; claim persisted before the provider call
  (asserted inside the provider call); resume → no re-claim, Completed; simulated provider (6) and Worker loop (2).
- SQL Server integration (15 = 12 Phase 01–02 + 3): processor with the real repository on a separate per-run
  LocalDB database (`WorkerDb` collection): Pending → Completed (`CreatedAt` unchanged, `UpdatedAt` later, fresh
  context); exhausted transient failures → Failed with reason, no provider effect; Processing row resumed → Completed.
- Functional (20 = 14 API + 4 HTTP 500 + 2 Worker): POST via `WebApplicationFactory` → Pending → real
  `RequestProcessingWorker` started from the production composition (`TransientFailuresBeforeSuccess` 2) → GET
  `?status=Completed` observes it, two transient-failure warnings logged, "after 3 attempt(s)", provider effect count 1,
  `StopAsync` completes (graceful stop); `UNSUPPORTED` → GET observes Failed with reason.
- **Phase 02 finding "500 not covered by an automated test": Closed in Phase 03.3 regression validation** —
  `Api/UnhandledErrorTests` replaces the repository via `ConfigureTestServices` with one that throws an exception
  whose message contains a fake connection string and SQL; GET and POST in Testing and Development → 500
  `application/problem+json` without the host, password, SQL, exception type or stack frames.
- Phase 02 finding "StringLength before trim": already fixed in 02.2 (`47b8067`, DTO trims in `init` before
  validation); 03.3 adds `Post_PaddedValueThatFitsAfterTrim_IsAcceptedAndStoredTrimmed` (padded 100 chars → 201
  stored trimmed; 101 → 400). Phase 02 finding "AddRequestTimestamps backfill": unchanged documented limitation;
  the empty-database migration path still passes (fixtures migrate a new database each run).
- Quality (temporary mutations, restored with `git checkout`): retry every exception → 3 processor tests fail; one
  extra retry → 2 fail; Completed instead of Failed on exhaustion → 2/2 fail; final save removed → 5/5 Worker
  integration/functional tests fail; provider called before the claim → 2/2 fail; `UseExceptionHandler` removed → 4/4
  500 tests fail. (A run with `--no-build` right after the last mutation used stale binaries and showed 4 failures;
  rebuilding gave 100/100.)
- Regression: `dotnet build ServiceRequests.sln` → 0 warnings, 0 errors; `dotnet test ServiceRequests.sln --no-build`
  → total 100, passed 100, failed 0, skipped 0 (TRX). No `ServiceRequestsDb_Tests_*` databases left.
- Worker smoke: `dotnet run --project src/ServiceRequests.Worker` → "Application started", Development; stopped.
- Timing: see Phase 03.3 commit timestamp.

### Phase 03.4 — Complete → Phase 03 Complete

- README: Worker run command, processing flow, retry semantics (2 retries = at most 3 calls), simulated-provider
  limits (in-process idempotency, not durable, no exactly-once claim), startup recovery, single-Worker trade-off,
  Windows Service (validated vs documented), configuration table with the real keys and validated ranges.
- Executed for the README: `dotnet publish src/ServiceRequests.Worker -c Release` (to a temp folder) →
  `ServiceRequests.Worker.exe`; walkthrough exactly as written (`dotnet run` API on 5164 + `dotnet run` Worker; POST
  `REQ-1` → 201; GET `?status=Completed` shows it; Worker log: claim, transient failure on attempt 1, "Completed REQ-1
  after 2 attempt(s)"); `Worker__MaxRetryAttempts=0` → `OptionsValidationException` at startup. Row deleted, processes
  stopped. Windows Service registration not executed (documented only).
- `.gitignore`: `publish/` added (documented publish output folder).
- Decisions: TD-07 and TD-08 updated (explicit retry semantics, publish/Windows Service evidence).
- Phase 02 findings: 500 regression — closed in 03.3; StringLength before trim — fixed in 02.2 (`47b8067`),
  regression test since 03.3; `AddRequestTimestamps` backfill — open documented limitation; EF SQL command logs at
  Warning — accepted operational choice.
- **Phase 03 summary:** build 0 warnings/0 errors; tests 100/100 (65 unit, 15 LocalDB integration, 20 functional),
  0 skipped; review corrections: startup recovery retried before Pending polling (B-01), final state write not
  cancelled after a provider outcome (M-01), host no longer stopped by a startup database failure (03.1 self-review);
  open: EF error logs on a handled UNIQUE race (Phase 05), unexpected-error requests wait for the next Worker start,
  simulated provider state not durable, single Worker. Commits: 03.1 `9c52fcc`, 03.2 `b6d2770`, 03.3 `163d99a`,
  03.4 this checkpoint (`git log`).
- Timing: see Phase 03.4 commit timestamp.

### Phase 04.1 — Complete (Phase 04 In Progress)

- Entry: Node 24.21, npm 11.19; backend baseline build 0 warnings, tests 100/100; working tree clean.
- Scaffold: `create-vite@9.3.3` (released 2026-10-27) `react-ts`, non-interactive; demo counter, logos, sample CSS,
  template README and icons removed. Exact versions pinned: react/react-dom 19.3.1, vite 8.3.8,
  @vitejs/plugin-react 6.3.0, typescript 7.0.2, oxlint 1.93.0 (scaffold linter), @types/* (react 19.3.0,
  react-dom 19.3.0, node 24.19.2). `npm audit`: 0 vulnerabilities. `"strict": true` added to `tsconfig.app.json`.
- Structure: `api/serviceRequestsApi.ts` (native fetch; `URL` + `URLSearchParams`; `ApiError` keeps status and
  ProblemDetails; network failure → status 0; abort passes through), `models/serviceRequest.ts` (contract types,
  max lengths), `hooks/useRequestList.ts` (status filter sent to the API; refresh every 3 s while the page is visible,
  next load scheduled only after the current one settles, `AbortController` + cleanup on unmount/filter change,
  manual refresh), components `CreateRequestForm`, `RequestStatusFilter`, `RequestsTable`, `FeedbackAlert`, `App`.
- Base URL from `VITE_API_BASE_URL` (`.env.example` → `http://localhost:5164`; falls back to the page origin);
  no `.env` committed.
- Create flow: labels, client required/length checks (convenience), `aria-invalid` + `aria-describedby`, submit
  disabled with spinner and "Registrando…" plus an in-flight guard; 201 → `alert-normal` + reset + list refresh;
  200 → `alert-informational` ("ya existía… no se creó un duplicado") + reset + refresh; 409 → `alert-warning`, input
  kept; 400 → field errors mapped from `errors` keys + `alert-error`; network/5xx → `alert-error` with "Reintentar"
  (resubmits the same values; server detail never shown).
- List: columns Referencia externa (+ description), Cliente, Servicio, Estado, Actualizada (`updatedAt`, `es-CO`
  `Intl.DateTimeFormat`, `<time datetime>` + full date in `title`); badges `badge-{pending|processing|completed|failed}`
  (color + text + shape); segmented filter with `aria-pressed`; skeleton loading (`aria-busy`), "Aún no hay
  solicitudes", "Sin resultados" + "Ver todas", error with "Reintentar" (rows kept when a refresh fails).
- Design: `styles/tokens.css` = contract §3 tokens + the few literal values from the reference component CSS
  (on-brand white/muted, secondary hover, skeleton highlight, primary shadow, destructive ring); `styles/app.css` adapts
  the reference patterns (brand bar gradient, panels, fields, buttons, spinner, segmented, table + stacked rows ≤ 640 px,
  `td.nowrap`, badges, alerts, states, skip link, focus-visible, reduced motion); two columns from 980 px (form 380 px).
  No raw colors outside `tokens.css`, no inline styles. Brand bar spans the page (the reference's framed `.app` card
  is only the simulation frame).
- Validation (executed): `npm install` (0 vulnerabilities); `npm run build` (`tsc -b` strict + `vite build`) OK;
  `npm run lint` (oxlint) 0 warnings, 0 errors on 11 files; `dotnet build` 0 warnings; `dotnet test` 100/100.
  Short smoke: API (`dotnet run`, 5164) + Worker + Vite (5173, `VITE_API_BASE_URL` from the environment); a request
  created via the API was processed by the Worker and the headless-Edge DOM of the portal showed it with
  `badge-completed` (CORS path works). Processes stopped, row deleted.
- Not done here (by scope): visual review at 1440/768/390 px and keyboard pass (04.3); README frontend section (04.4).
- Timing: see Phase 04.1 commit timestamp.

### Phase 04.2 — Complete (review gate; Phase 04 In Progress)

- Baseline: `npm run build` OK, `npm run lint` 0/0; `dotnet build` 0 warnings, `dotnet test` 100/100.
- Reviewed actual code: POST body is the form state with exactly the 4 business fields; success branch tests
  `status === 201` / `=== 200` explicitly (a replay is never shown as "created"; no `response.ok` shortcut); error
  bodies parsed only when the content type is JSON, inside try/catch (non-JSON → no problem object; network → status 0);
  server `title`/`detail` are never rendered; GET sends `status` via `URLSearchParams` with the enum literals
  (`Pending|Processing|Completed|Failed`); polling: one request in flight, cleanup sets a cancelled flag + aborts +
  clears the timer, so a response for a previous filter cannot overwrite the current one; form state is untouched by
  polling; Enter submits (native form); Failed uses `critical` tokens, system errors `destructive`; breakpoints
  980/720/640 px, no fixed widths except the 380 px form column (collapses below 980 px); dependencies: only react,
  react-dom and scaffold dev tooling; no `.env`, secrets or connection strings.
- Findings: no BLOCKER/HIGH/MEDIUM. LOW (fixed): L-01 focus did not move to the first invalid field after client or
  400 validation → now focused after the next render (so `aria-invalid`/error text are present); L-02 list "Reintentar"
  kept showing the error while retrying → manual refresh clears the failure (skeleton when no rows yet).
- Notes: a malformed success body would surface as the generic connection error with "Reintentar" (re-submitting is
  safe: the API answers 200 for the replay) · `role="status"` alerts are inserted on demand (announcement can vary by
  screen reader; checked in 04.3) · very long codes keep `nowrap` in stacked rows and scroll inside the table wrapper ·
  a newly created request is not visible when another status filter is active (filter respected) · README still says
  the portal is pending (04.4) · open backend findings unchanged (timestamp backfill, EF race log → 05, unexpected
  Worker error waits for restart, simulated provider state per process); StringLength and 500 remain closed.
- Validation after corrections: `npm run build` OK, `npm run lint` 0/0; `dotnet build` 0 warnings; `dotnet test` 100/100.
- Timing: see Phase 04.2 commit timestamp.

## Unfinished Work Template

If the hard stop is reached: **current state · reason · risk · next concrete step**.
