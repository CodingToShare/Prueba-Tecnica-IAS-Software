# Roadmap — Time-Boxed Implementation Plan

Target implementation window: **110 min** · Contingency reserve: **10 min** · Hard stop: **120 min**.
Real timing evidence comes from Git commit history; elapsed times are never fabricated.

Statuses: `Not Started` · `In Progress` · `Complete` · `Blocked` · `Deferred`.

| Phase | Scope | Target | Status |
| --- | --- | --- | --- |
| 00 | Agent governance and technical plan | 8 min | Complete |
| 01 | Solution foundation and persistence | 15 min | Not Started |
| 02 | Idempotent API | 22 min | Not Started |
| 03 | Background processing and resilience | 20 min | Not Started |
| 04 | React portal | 18 min | Not Started |
| 05 | Critical test hardening | 12 min | Not Started |
| 06 | Documentation and legacy review | 10 min | Not Started |
| 07 | Final validation and delivery | 5 min | Not Started |
| — | Contingency reserve | 10 min | — |

## Phase Scopes

**00 — Governance.** AGENTS.md, CLAUDE.md, project context, 10 skills (incl. `ui-design-system`), design contract +
reference HTML, roadmap, technical decisions (+ assumptions, open decisions), requirements traceability, .gitignore.
No product code.

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
| 01 | `dotnet build ServiceRequests.sln`, `dotnet test` |
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

## Unfinished Work Template

If the hard stop is reached: **current state · reason · risk · next concrete step**.
