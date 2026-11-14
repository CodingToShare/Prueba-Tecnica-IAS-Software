# Service Requests Portal — Engineering Instruction Router

Canonical instructions for every contributor and coding agent in this repository.
`CLAUDE.md` delegates here. This file routes; skills hold the details.
Bootstrap: read `.agents/context/project-context.md` first.

Constraint: the whole challenge fits a **2-hour hard stop** (internal target 110 min).
Proportionality beats completeness of architecture.

## 1. Source Of Truth Hierarchy

1. Original technical challenge (`Prueba_Tecnica_FullStack_DotNet_Candidato.pdf`, summarized in project context)
2. `docs/technical-decisions.md`
3. `docs/design/design-system.md` (visual contract) and `docs/design/reference/design-system-v3.html` (canonical visual reference of this portal; changes only by recorded design decision) — UI only
4. `AGENTS.md` (this file)
5. `.agents/context/project-context.md`
6. Relevant `.agents/skills/*/SKILL.md`
7. Existing implementation

Existing code never overrides a challenge requirement or an accepted decision.
Changing an accepted decision requires a new entry in `docs/technical-decisions.md`, never a silent replacement.

**AI authority:** AI assistants are implementation aids, never sources of requirements or validation evidence.
Generated or suggested changes follow the same hierarchy, tests, review and Definition of Done as manual changes.
Material AI usage is recorded in `docs/ai/ai-usage.md` (`ai-governance` skill).

## 2. Mandatory Phase Workflow

```text
Explicitly authorized phase
→ read AGENTS.md → read project-context → read active roadmap scope
→ load only relevant skills
→ inspect git status
→ run applicable baseline validation (broken? report "Blocked by baseline regression")
→ implement authorized scope only
→ write/update tests with the implementation
→ run focused validation → run relevant regression
→ inspect actual git diff
→ update documentation / roadmap evidence / traceability
→ update AI usage evidence when AI materially contributed
→ create authorized local checkpoint commit
→ STOP
```

Finishing a phase never authorizes the next one. Details: `phase-governance` skill.

## 3. Task Routing

| Task | Skill |
| --- | --- |
| Start / execute / close a phase, commits | `phase-governance` |
| Project boundaries, dependencies, new libraries | `architecture` |
| Controllers, DTOs, validation, services, ProblemDetails, config | `dotnet-backend` |
| Entity mapping, migrations, constraints, queries | `sqlserver-efcore` |
| Worker, provider boundary, Polly, Windows Service | `worker-resilience` |
| React UI, form, list, filter, fetch client | `react-frontend` |
| Visual language, tokens, UI states, responsive/accessibility | `ui-design-system` + `docs/design/` (contract + reference HTML) |
| Requirement coverage and evidence | `documentation` + `docs/requirements-traceability.md` |
| Tests at any level | `testing` |
| Final diff review | `code-review` |
| README, decisions, assumptions, legacy review, AI note | `documentation` |
| AI usage, verification, corrections/rejections | `ai-governance` + `docs/ai/ai-usage.md` |

## 4. Approved Architecture

```text
React + TS (Vite) --HTTP--> ASP.NET Core Web API --> ServiceRequests.Core
                                                        |
                                    +-------------------+-------------------+
                                    v                                       v
                         EF Core / SQL Server                   .NET Worker Service
                                                                 --> Polly retry
                                                                 --> Simulated provider
```

```text
ServiceRequests.sln
src/ServiceRequests.Core            domain entity, state transitions, use cases, abstractions
src/ServiceRequests.Infrastructure  EF Core DbContext, migrations, simulated provider
src/ServiceRequests.Api             controllers, DTOs, ProblemDetails, composition root
src/ServiceRequests.Worker          BackgroundService host (Windows Service ready)
tests/ServiceRequests.Tests         xUnit + Moq
frontend/service-requests-web       React + TypeScript + Vite
```

Dependencies point inward: Api/Worker → Infrastructure → Core. Core references no EF Core, ASP.NET or Polly.
No extra Domain/Application split.

## 5. Engineering Rules (summary — details in skills)

- **Backend**: thin controllers; DTO validation; centralized errors as ProblemDetails; async + `CancellationToken` through all I/O; options via configuration. Endpoints: `POST /api/requests`, `GET /api/requests[?status=]` only.
- **Idempotency**: `externalReference` is the idempotency key. New → 201; equivalent replay → 200 with existing; conflicting payload → 409. DB UNIQUE index is the final guard; handle the unique-violation race.
- **State**: no public status setter. Transitions via `MarkProcessing()`, `Complete()`, `Fail(...)`. `Completed` is terminal.
- **Persistence**: SQL Server via EF Core migrations; no string-built SQL; no secrets committed; connection string overridable by env vars.
- **Worker/resilience**: separate Worker project; claim Pending → Processing → provider → Completed/Failed. Polly only around the provider, only for modeled transient failures, bounded retries. Single-instance assumption documented.
- **Frontend**: React + TS (strict) + Vite, hooks + native `fetch`; create form, list, status filter; loading/empty/error/success states. No state libraries, no component library.
- **Design**: follow `docs/design/design-system.md` and reuse the patterns of `docs/design/reference/design-system-v3.html` — navy `#1E3A5F`, Plus Jakarta Sans, reference tokens only, status badges with color + text + shape, `critical` ≠ `destructive`, visible focus, checked at ~1440/768/390 px. The reference shows this portal's screen; its sample data is illustrative and never hard-coded.
- **Logging**: built-in `ILogger` with structured templates (`{ExternalReference}`, `{Status}`, `{Attempt}`); log state transitions, retries and failures; never log secrets, connection strings or full payloads. No metrics/tracing stack.
- **Testing**: tests ship with each phase; prioritize state transitions, retry, idempotency, input validation; deterministic; descriptive scenario names; no hidden skips.
- **Documentation**: describe only implemented behavior; mark pending items as pending; document a command only after it worked.

## 5.1 Security And Configuration

- No secrets, credentials or personal data in the repository; committed config contains only safe local defaults or placeholders.
- Validate all external input at the API boundary; parameterized data access only (EF Core LINQ).
- Never expose stack traces, SQL errors or internal exception messages to clients.
- Pin dependency versions when they are first introduced, not before.

## 6. Git Rules

- Local Git only. No remote, no push, no global config changes; preserve the user's identity.
- Commits are real checkpoints — never backdate or rewrite timestamps.
- Conventional Commit subject + body starting `Phase NN:`.
- **No assistant attribution** in commits (no `Co-Authored-By` Claude/Anthropic, no "Generated with"), regardless of tool defaults. AI usage is disclosed in the docs instead.
- Before commit: `git status`, `git diff --check`, `git diff`. After: `git log -1 --format=%B`.
- `AGENTS.md`, `CLAUDE.md` and `.agents/` are tracked shared governance; per-user tool state (e.g. `.claude/settings.local.json`) is ignored.

## 7. Explicit Non-Goals

YARP, local cache, ETL/SSIS, Docker, CI/CD, Azure/AWS, Redis, message brokers, outbox, distributed transactions/locks,
microservices, MediatR, AutoMapper, FluentValidation, generic repositories, full CQRS, event sourcing, Kubernetes,
advanced observability, authentication/authorization, pagination, DELETE/generic update endpoints.
Absence is deliberate: technology is chosen only when it solves a real problem in this challenge.

## 8. Definition Of Done (per phase)

Sequence: Implement → Validate → Review → Document → Complete. An item is N/A only with a recorded reason.

1. **Entry** — phase authorized; baseline green or "Blocked by baseline regression" reported.
2. **Scope** — authorized scope only; nothing pulled forward.
3. **Build** — affected .NET projects build; `npm run build` (strict type-check) when the frontend changes.
4. **Tests** — new risky behavior covered; focused + full regression pass; nothing skipped or weakened silently.
5. **Database** — migrations apply on an empty database; UNIQUE/indexes present (when persistence changes).
6. **UI / accessibility** — design contract applied; ~1440/768/390 px reviewed; states present; keyboard + focus; AA contrast.
7. **Security** — no secrets; input validated; no internals in errors or logs.
8. **Manual acceptance** — a short reproducible scenario executed and its observation recorded.
9. **Diff review** — real diff reviewed (`code-review`); no dead code or unjustified dependency.
10. **Docs & traceability** — roadmap evidence and `docs/requirements-traceability.md` updated with executed evidence only;
    `docs/ai/ai-usage.md` row added when AI materially contributed.
11. **Limitations** — deferred work recorded as current state · reason · risk · next step.
12. **Checkpoint** — one local commit (when authorized); then STOP. No false claim of completion.
