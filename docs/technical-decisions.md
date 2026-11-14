# Technical Decisions

Format: Context · Decision · Why · Trade-off · Future evolution · Evidence.
"Evidence: pending" means the decision is accepted but not yet implemented or validated.

---

## TD-01 — .NET 10 / ASP.NET Core

- **Context:** Backend must be C# + ASP.NET/Web API; any .NET generation allowed; must integrate with Windows-hosted services.
- **Decision:** .NET 10 with ASP.NET Core controllers and EF Core 10.
- **Why:** Current LTS; first-class Worker Service + Windows Service hosting; built-in ProblemDetails, DI, Options.
- **Trade-off:** Not directly runnable on legacy .NET Framework hosts; integration with older apps is via HTTP/DB, not in-process.
- **Future evolution:** None needed for scope.
- **Evidence:** pending. Phase 00 found only the .NET 8.0.403 SDK installed; Phase 01 entry requires installing the
  .NET 10 SDK or an explicit, recorded fallback to .NET 8 LTS.

## TD-02 — React + TypeScript (Vite) instead of AngularJS / Vue

- **Context:** One of Vue.js, React.js or AngularJS for a minimal UI.
- **Decision:** React + TypeScript + Vite, hooks and native `fetch`.
- **Why:** Typed contract with the API, fast scaffolding, no extra libraries needed for one screen. AngularJS is end-of-life.
- **Trade-off:** No state/query library — manual loading/error handling.
- **Future evolution:** Add a query library if screens and caching needs grow.
- **Evidence:** pending.

## TD-03 — SQL Server instead of NoSQL

- **Context:** Real persistence required; SQL or NoSQL.
- **Decision:** SQL Server via EF Core migrations (LocalDB as overridable dev default).
- **Why:** Relational model; filtering by status and uniqueness of `externalReference` are native relational concerns;
  strong UNIQUE guarantees under concurrency.
- **Trade-off:** Requires a SQL Server instance; schema migrations to maintain.
- **NoSQL alternative (to be expanded in README):** e.g. a document store keyed by `externalReference` as document id
  (uniqueness through the key or a unique index), conditional writes/ETags for state transitions and the Worker claim,
  a secondary index on status, and no EF migrations (schema versioning in code).
- **Evidence:** pending.

## TD-04 — Pragmatic four-project backend

- **Context:** Small domain, 2-hour limit, need to share logic between API and Worker.
- **Decision:** `Core` (entity, transitions, use cases, interfaces), `Infrastructure` (EF Core, simulated provider), `Api`, `Worker`.
- **Why:** API and Worker share Core/Infrastructure without duplication; boundaries are clear without ceremony.
- **Trade-off:** Core mixes domain and application concerns.
- **Future evolution:** Split Domain/Application only if the domain grows.
- **Evidence:** pending.

## TD-05 — `externalReference` as idempotency key

- **Context:** Users retry on slowness; the same operation must not create duplicate effects.
- **Decision:** New → 201; same reference + equivalent payload → 200 with existing; conflicting payload → 409.
- **Why:** Retries are safe and observable; real conflicts are not silently hidden.
- **Trade-off:** "Equivalent" must be defined (compare customerId, serviceCode, description after trimming — confirm in Phase 02).
- **Future evolution:** Idempotency-Key header / expiration policy if required by clients.
- **Evidence:** pending.

## TD-06 — Database UNIQUE constraint as final guard

- **Context:** An application `Exists()` check is racy under concurrent submissions.
- **Decision:** UNIQUE index on `ExternalReference`; unique-violation (2601/2627) translated to the replay/conflict path.
- **Why:** Only the database can serialize concurrent inserts reliably.
- **Trade-off:** Exception-driven path for the rare race.
- **Evidence:** pending.

## TD-07 — Separate Worker Service

- **Context:** Background processing must be runnable manually and deployable as a Windows Service.
- **Decision:** `ServiceRequests.Worker` with `BackgroundService`, Windows Service hosting support.
- **Why:** Independent lifecycle from the API (deploy, restart, scale separately); matches the team's Windows-service estate.
- **Trade-off:** Two processes to run locally.
- **Future evolution:** Production install via `sc.exe`/installer; health monitoring.
- **Evidence:** pending.

## TD-08 — Polly only around transient provider failures

- **Context:** Provider fails transiently; retrying everything hides bugs and can duplicate effects.
- **Decision:** Bounded retry (few attempts, short backoff) only for a modeled transient failure type at the provider boundary.
- **Why:** Resilience where failure is expected; fail fast elsewhere.
- **Trade-off:** Long outages end as `Failed`; no automatic later re-drive.
- **Future evolution:** Circuit breaker, scheduled re-drive of Failed requests.
- **Evidence:** pending.

## TD-09 — Single Worker instance assumption

- **Context:** Distributed coordination is out of scope for 2 hours.
- **Decision:** One Worker instance; claim by persisting `Processing` before calling the provider.
- **Why:** Simple and correct for one instance.
- **Trade-off:** Multiple instances could double-claim; a crash can leave requests in `Processing`.
- **Future evolution:** Atomic claim (conditional UPDATE / rowversion), lease timeout to recover stuck requests.
- **Evidence:** pending.

## TD-10 — Simulated provider is idempotent by `externalReference`

- **Context:** A DB UNIQUE constraint does not guarantee exactly-once external effects (provider success → crash → retry).
- **Decision:** Simulated provider records processed references and returns the prior result on repeat.
- **Why:** Models the contract a real provider must offer.
- **Trade-off:** Real integration still depends on provider support or reconciliation.
- **Evidence:** pending.

## TD-11 — Deliberately excluded

| Excluded | Reason |
| --- | --- |
| Outbox / message broker | Single DB + single Worker; DB-polling is enough; documented as evolution |
| YARP | No gateway/routing need |
| Cache | Tiny dataset, status must be fresh |
| ETL / SSIS | No bulk data movement |
| Authentication | Not required by the challenge; inputs, errors, config and secrets still handled safely |
| Docker / CI/CD / cloud | Not required; would displace core scope |

## TD-12 — Tests concentrated on highest risk

- **Context:** Limited time; the costliest bugs are duplicate effects and illegal state changes.
- **Decision:** xUnit + Moq on state transitions, retry behavior, idempotency, input validation; written with each phase.
- **Trade-off:** No E2E/UI tests; frontend validated by build + manual smoke.
- **Evidence:** pending.
