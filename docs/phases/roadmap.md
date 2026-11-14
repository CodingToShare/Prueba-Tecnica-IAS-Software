# Roadmap — Time-Boxed Implementation Plan

Planned work: **115 min** · Contingency reserve: **5 min** · Hard stop: **120 min**
(rebalanced when Phase 00.1 was added; reserve was 10 min).
Real timing evidence comes from Git commit history; elapsed times are never fabricated.

Statuses: `Not Started` · `In Progress` · `Complete` · `Blocked` · `Deferred`.

| Phase | Scope | Target | Status |
| --- | --- | --- | --- |
| 00 | Agent governance and technical plan | 8 min | Complete |
| 00.1 | AI governance and usage evidence | 5 min | Complete |
| 01 | Solution foundation and persistence | 15 min | In Progress |
| 01.1 | Solution, domain and persistence development | 7 min | Complete |
| 01.2 | Requirement, architecture and implementation review | 3 min | Complete |
| 01.3 | Phase 01 tests and database validation | — | Not Started |
| 02 | Idempotent API | 22 min | Not Started |
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
| 01 | `dotnet tool restore`, `dotnet build ServiceRequests.sln`, `dotnet test` (no tests exist until 01.3) |
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

## Unfinished Work Template

If the hard stop is reached: **current state · reason · risk · next concrete step**.
