# Technical Decisions, Assumptions And Open Decisions

Categories stay separate: **REQUIREMENT** (from the challenge — see `requirements-traceability.md`), **TECHNICAL DECISION**
(TD), **ASSUMPTION** (A), **OPEN DECISION** (OD), **OUT OF SCOPE**. An entry changes category only through a recorded decision.

Decision format (challenge §6): Context · Alternatives · Decision · Trade-off · Risks · Validation · Future evolution.
"Validation: pending" means accepted but not yet implemented or verified.

---

## Technical Decisions

### TD-01 — .NET 10 / ASP.NET Core

- **Context:** Backend must be C# + ASP.NET/Web API; any .NET generation allowed; must coexist with Windows-hosted services.
- **Alternatives:** ASP.NET Framework 4.8 Web API; .NET 8 LTS.
- **Decision:** .NET 10 with ASP.NET Core controllers and EF Core 10.
- **Trade-off:** Not runnable in-process inside legacy .NET Framework hosts; integration is via HTTP/DB.
- **Risks:** SDK initially missing on the dev machine (OD-01, resolved: user installed SDK 10.0.404). Visual Studio
  17.11 cannot open `net10.0` projects; the challenge is driven from the .NET CLI.
- **Validation:** Phase 01.1 — `dotnet build ServiceRequests.sln` with SDK 10.0.404, all projects `net10.0`, 0 warnings.
- **Future evolution:** none needed for scope.

### TD-02 — React + TypeScript (Vite)

- **Context:** One of Vue.js, React.js or AngularJS for a minimal UI.
- **Alternatives:** Vue 3; AngularJS (end-of-life since 2022).
- **Decision:** React + TypeScript + Vite, hooks and native `fetch`.
- **Trade-off:** No state/query library — manual loading/error handling.
- **Risks:** Low; one screen.
- **Validation:** pending (Phase 04 `npm run build` + manual smoke).
- **Future evolution:** add a query library if screens and caching needs grow.

### TD-03 — SQL Server instead of NoSQL

- **Context:** Real persistence required; SQL or NoSQL.
- **Alternatives:** document store (MongoDB / Cosmos DB); SQLite.
- **Decision:** SQL Server via EF Core migrations (LocalDB as overridable development default).
- **Trade-off:** Requires a SQL Server instance and schema migrations.
- **Risks:** Reviewer machine without SQL Server/LocalDB → documented connection-string override.
- **NoSQL alternative (README, 3–5 lines):** `externalReference` as document id (uniqueness by key), conditional
  writes/ETags for state transitions and the Worker claim, secondary index on status, schema versioned in code instead
  of migrations.
- **Validation:** pending (Phase 01 migration on empty database).

### TD-04 — Pragmatic four-project backend

- **Context:** Small domain, 2-hour limit, logic shared by API and Worker.
- **Alternatives:** single project; Clean Architecture with separate Domain/Application.
- **Decision:** `Core` (entity, transitions, use cases, interfaces), `Infrastructure` (EF Core, simulated provider), `Api`, `Worker`.
- **Trade-off:** Core mixes domain and application concerns.
- **Risks:** Low.
- **Validation:** pending (project references respect dependency direction).
- **Future evolution:** split Domain/Application only if the domain grows.

### TD-05 — `externalReference` as idempotency key

- **Context:** Users retry on slowness; the same operation must not create duplicate effects.
- **Alternatives:** `Idempotency-Key` header; always 409 on duplicates; silently return existing on any duplicate.
- **Decision:** new → 201; same reference + equivalent payload → 200 with existing; conflicting payload → 409.
- **Trade-off:** "Equivalent" must be defined (OD-02).
- **Risks:** A client reusing a reference for a different operation gets 409 (intended).
- **Validation:** pending (Phase 02 tests).

### TD-06 — Database UNIQUE constraint as final guard

- **Context:** An application `Exists()` check is racy under concurrent submissions.
- **Alternatives:** application lock; serializable transaction.
- **Decision:** UNIQUE index on `ExternalReference`; SQL errors 2601/2627 translated to the replay/conflict path.
- **Trade-off:** Exception-driven path for the rare race.
- **Risks:** Translation must not leak SQL details.
- **Validation:** pending (Phase 01 migration + Phase 02 tests).

### TD-07 — Separate Worker Service

- **Context:** Background processing runnable manually and deployable as a Windows Service.
- **Alternatives:** `IHostedService` inside the API; Hangfire/Quartz.
- **Decision:** `ServiceRequests.Worker` with `BackgroundService` and Windows Service hosting support.
- **Trade-off:** Two processes to run locally.
- **Risks:** Low.
- **Validation:** pending (Phase 03 manual run: Pending → Completed).
- **Future evolution:** production install via `sc.exe`/installer; health monitoring.

### TD-08 — Polly only around transient provider failures

- **Context:** Provider fails transiently; retrying everything hides bugs and can duplicate effects.
- **Alternatives:** hand-written retry loop; retry all exceptions.
- **Decision:** bounded retry (few attempts, short backoff) for a modeled transient failure type at the provider boundary only.
- **Trade-off:** Long outages end as `Failed`; no automatic re-drive.
- **Risks:** Misclassifying a permanent error as transient → covered by tests.
- **Validation:** pending (Phase 03 Moq tests).
- **Future evolution:** circuit breaker; scheduled re-drive of Failed requests.

### TD-09 — Single Worker instance assumption

- **Context:** Distributed coordination is out of scope for 2 hours.
- **Alternatives:** atomic conditional UPDATE claim; `UPDLOCK, READPAST` queue pattern.
- **Decision:** one Worker instance; claim by persisting `Processing` before calling the provider.
- **Trade-off:** Multiple instances could double-claim.
- **Risks:** A crash can leave requests in `Processing`. Mitigation (planned, Phase 03): resume them at Worker startup —
  safe because the provider is idempotent (TD-10). If cut for time, documented as a limitation.
- **Validation:** pending.
- **Future evolution:** atomic claim + lease timeout.

### TD-10 — Simulated provider idempotent by `externalReference`

- **Context:** A DB UNIQUE constraint does not guarantee exactly-once external effects (provider success → crash → retry).
- **Alternatives:** transactional outbox; reconciliation job.
- **Decision:** the simulated provider records processed references and returns the prior result on repeat.
- **Trade-off:** Real integration depends on provider support or reconciliation.
- **Risks:** In-memory record is lost on restart (acceptable for a simulation; documented).
- **Validation:** pending (Phase 03 test).

### TD-11 — Deliberately excluded

| Excluded | Reason |
| --- | --- |
| Outbox / message broker | Single DB + single Worker; DB polling is enough; documented as evolution |
| YARP | No gateway/routing need |
| Cache | Tiny dataset, status must be fresh |
| ETL / SSIS | No bulk data movement |
| Authentication | Not required; inputs, errors, config and secrets still handled safely |
| Docker / CI/CD / cloud | Not required; would displace core scope |

### TD-12 — Tests concentrated on highest risk

- **Context:** Limited time; costliest bugs are duplicate effects and illegal state changes.
- **Alternatives:** broad coverage including UI/E2E tests.
- **Decision:** xUnit + Moq on state transitions, retry, idempotency, input validation; written with each phase.
- **Trade-off:** No automated UI/E2E tests; frontend validated by build + manual smoke.
- **Validation:** pending.

### TD-13 — Visual language from design system v3

- **Context:** A professional, consistent UI helps reviewers; time is limited. The author's design system v3 exists
  with an HTML reference and a contract.
- **Alternatives:** unstyled HTML; a component library (MUI, Bootstrap).
- **Decision:** adapt the v3 HTML reference into a **Portal de Solicitudes edition** (`docs/design/reference/design-system-v3.html`):
  same tokens and patterns, content rewritten for this business (portal screen, lifecycle, processing with retries,
  submit-outcome alerts, view states), and apply it through `docs/design/design-system.md` with plain CSS.
  Status mapping: Pending neutral, Processing informational, Completed normal, Failed critical; warning for 409 and
  transient retries. Brand bar instead of sidebar (one screen). Design decision recorded 2026-11-14 at the owner's request.
- **Trade-off:** Reference and contract must be kept in sync manually.
- **Risks:** Design polish eating Phase 04 time → contract limits the component set.
- **Validation:** reference rendered in headless Edge at 1440, 768 and 390 px (no horizontal overflow; stacked table at
  390 px); structure checked (balanced tags, unique ids, no undefined CSS variables). App validation pending (Phase 04).

### TD-14 — Built-in logging only

- **Context:** Background processing and retries must be diagnosable; observability platforms are out of scope.
- **Alternatives:** Serilog; OpenTelemetry.
- **Decision:** `Microsoft.Extensions.Logging` with structured templates; no metrics or tracing.
- **Trade-off:** Console output only.
- **Validation:** pending.
- **Future evolution:** Windows Event Log provider when hosted as a Windows Service.

### TD-15 — Dependency versions pinned when introduced

- **Decision:** packages are chosen and pinned in the phase that first needs them, never during Phase 00.
- **Pinned so far (Phase 01.1):** `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15 (Infrastructure),
  `Microsoft.EntityFrameworkCore.Design` 10.0.15 (Api, private assets — design-time only), local tool `dotnet-ef`
  10.0.15 (`dotnet-tools.json`); template defaults kept: `Microsoft.Extensions.Hosting` 10.0.15 (Worker), xUnit 2.9.3,
  `xunit.runner.visualstudio` 3.1.4, `Microsoft.NET.Test.Sdk` 17.14.1, `coverlet.collector` 6.0.4 (Tests).

### TD-16 — Explicit database initialization through EF Core migrations (resolves OD-03)

- **Context:** Storage initialization must be reproducible; schema changes should be visible and deliberate.
- **Alternatives:** automatic `Database.Migrate()` at API startup; `EnsureCreated()`.
- **Decision:** committed EF Core migrations applied explicitly with the repository-local tool:
  `dotnet tool restore` then
  `dotnet ef database update --project src/ServiceRequests.Infrastructure --startup-project src/ServiceRequests.Api`.
  The API and Worker never migrate the database on startup. Recorded 2026-11-14 (Phase 01.1).
- **Trade-off:** one extra manual step for reviewers.
- **Risks:** forgetting the step → API fails on first query; mitigated by README run order.
- **Validation:** Phase 01.3 — `dotnet ef database update` created `ServiceRequestsDb` on LocalDB and applied
  `InitialCreate`; indexes confirmed with `sqlcmd`; the test fixture also migrates a clean database on every run.
- **Future evolution:** idempotent SQL script (`dotnet ef migrations script --idempotent`) for deployed environments.

### TD-17 — Persistence integration tests against SQL Server LocalDB (resolves OD-04)

- **Context:** Idempotency relies on the SQL Server UNIQUE index; tests must exercise real relational behavior.
- **Alternatives:** EF InMemory (does not enforce unique indexes or relational behavior); SQLite in-memory (different
  engine, collation and error codes); Testcontainers (requires Docker, out of scope).
- **Decision:** persistence integration tests run against SQL Server LocalDB on a dedicated test database, created
  from the committed migrations by the test fixture. Domain tests stay pure (no database). Recorded 2026-11-14 (Phase 01.2).
- **Trade-off:** those tests need Windows + LocalDB; a reviewer without it can point the tests to another SQL Server
  via configuration (to be documented in Phase 01.3/06).
- **Risks:** slower tests; leftover test databases → fixture drops its database.
- **Validation:** Phase 01.3 — 6 LocalDB tests pass (migration, indexes, UNIQUE rejection 2601/2627, mapping, round
  trip, status query); unique per-run database dropped afterwards; override variable `SERVICEREQUESTS_TEST_SQLSERVER`.

---

## Assumptions

| ID | Assumption | Impact if wrong |
| --- | --- | --- |
| A-01 | Portal users are Spanish speakers: UI copy in Spanish; status labels keep the challenge names. | Copy change only |
| A-02 | `customerId` and `serviceCode` are free-text identifiers (no catalog to validate against); length-limited. | Add catalog validation |
| A-03 | `Failed` is final for automatic processing (no automatic re-drive); a replay of its `externalReference` returns the existing Failed request. | Add re-drive endpoint/job |
| A-04 | One Worker instance in this delivery (TD-09). | Need atomic claim |
| A-05 | No authentication is expected for the challenge; the API runs on a trusted internal network. | Add auth |
| A-06 | Transient provider failure is the only modeled failure; non-transient failures fail fast to `Failed`. | Extend classification |

## Open Decisions

| ID | Question | Owner phase | Status |
| --- | --- | --- | --- |
| OD-01 | .NET 10 SDK is not installed (only 8.0.403): install it, or record a fallback to .NET 8 LTS (EF Core 8)? | Before Phase 01 (user) | Resolved 2026-11-14: user installed SDK 10.0.404; TD-01 unchanged |
| OD-02 | Which fields define an "equivalent" replay (proposal: `customerId`, `serviceCode`, `description` after trimming)? | 02 | Open |
| OD-03 | Database initialization: `dotnet ef database update` vs automatic migration at API startup in Development? | 01 | Resolved 2026-11-14 → TD-16 (explicit migrations) |
| OD-04 | Persistence tests: SQLite in-memory (enforces UNIQUE) vs LocalDB? | 02 | Resolved 2026-11-14 → TD-17 (LocalDB); evidenced in 01.3 |
