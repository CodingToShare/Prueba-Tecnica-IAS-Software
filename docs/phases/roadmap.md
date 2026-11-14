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
| 02 | Idempotent API | 22 min | In Progress |
| 02.1 | Development | 10 min | Complete |
| 02.2 | Technical/requirements review | 4 min | Complete |
| 02.3 | Automated + functional tests | 6 min | Not Started |
| 02.4 | Documentation/evidence | 2 min | Not Started |
| 03 | Background processing and resilience | 20 min | Not Started |
| 04 | React portal | 18 min | Not Started |
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

## Unfinished Work Template

If the hard stop is reached: **current state · reason · risk · next concrete step**.
