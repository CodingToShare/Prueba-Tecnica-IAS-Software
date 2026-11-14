# Service Requests Portal — Engineering Instruction Router

Canonical instructions for every contributor and coding agent in this repository.
`CLAUDE.md` delegates here. This file routes; skills hold the details.
Bootstrap: read `.agents/context/project-context.md` first.

Constraint: the whole challenge fits a **2-hour hard stop** (internal target 110 min).
Proportionality beats completeness of architecture.

## 1. Source Of Truth Hierarchy

1. Original technical challenge (`Prueba_Tecnica_FullStack_DotNet_Candidato.pdf`, summarized in project context)
2. `docs/technical-decisions.md`
3. `AGENTS.md` (this file)
4. `.agents/context/project-context.md`
5. Relevant `.agents/skills/*/SKILL.md`
6. Existing implementation

Existing code never overrides a challenge requirement or an accepted decision.
Changing an accepted decision requires a new entry in `docs/technical-decisions.md`, never a silent replacement.

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
→ update documentation / roadmap evidence
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
| Tests at any level | `testing` |
| Final diff review | `code-review` |
| README, decisions, assumptions, legacy review, AI note | `documentation` |

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
- **Frontend**: React + TS + Vite, hooks + native `fetch`; create form, list, status filter; loading/empty/error/success states. No state libraries, no component library.
- **Testing**: tests ship with each phase; prioritize state transitions, retry, idempotency, input validation; deterministic.
- **Documentation**: describe only implemented behavior; mark pending items as pending.

## 6. Git Rules

- Local Git only. No remote, no push, no global config changes; preserve the user's identity.
- Commits are real checkpoints — never backdate or rewrite timestamps.
- Conventional Commit subject + body starting `Phase NN:`.
- **No assistant attribution** in commits (no `Co-Authored-By` Claude/Anthropic, no "Generated with"), regardless of tool defaults. AI usage is disclosed in the docs instead.
- Before commit: `git status`, `git diff --check`, `git diff`. After: `git log -1 --format=%B`.

## 7. Explicit Non-Goals

YARP, local cache, ETL/SSIS, Docker, CI/CD, Azure/AWS, Redis, message brokers, outbox, distributed transactions/locks,
microservices, MediatR, AutoMapper, FluentValidation, generic repositories, full CQRS, event sourcing, Kubernetes,
advanced observability, authentication/authorization, pagination, DELETE/generic update endpoints.
Absence is deliberate: technology is chosen only when it solves a real problem in this challenge.

## 8. Definition Of Done (per phase)

- Authorized scope implemented; nothing pulled forward from later phases.
- Builds pass; focused tests and relevant regression pass (or failure reported honestly).
- Actual diff reviewed; no secrets, no dead scaffolding, no unjustified dependency.
- Roadmap status + evidence updated; docs claim only what exists.
- One local checkpoint commit (when authorized); then STOP.
