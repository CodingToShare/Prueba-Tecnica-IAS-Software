# AI Usage

Tracked delivery evidence (challenge §6). This file is the factual source for the final README AI disclosure (Phase 06).

## Purpose

AI assistants are used as engineering accelerators during planning, implementation, testing and review.
They are not a source of project requirements, and their output is not accepted without verification.
The original challenge and the repository source-of-truth hierarchy (`AGENTS.md` §1) remain authoritative.

## Tools

### Planning assistance — ChatGPT (OpenAI)

- **Used for:** challenge analysis, architecture alternatives, scope and timebox planning, agent/governance design,
  review of engineering trade-offs.
- **Verification:** checked against the original challenge; accepted decisions recorded in
  `docs/technical-decisions.md`; unnecessary technologies rejected instead of copied.

### Implementation assistance — Claude Code (Anthropic)

- **Used for:** repository-aware implementation, code and test generation/modification, documentation updates,
  diff and implementation review.
- **Verification:** builds, automated tests, database checks, manual acceptance and UI review where applicable,
  actual Git diff review, Definition of Done (`AGENTS.md` §8).

## Verification Policy

AI assistance and verification are different things. AI-generated or AI-suggested work is not verified because it
looks plausible, or because an AI reviewed it. A claim becomes project evidence only after the relevant check has
actually been executed and its result observed (build, tests, database check, manual acceptance, UI review, diff review).
This log never replaces requirements traceability, phase evidence, test or build output.

## Correction And Rejection Policy

Record material AI suggestions that were corrected or deliberately rejected when they explain engineering judgment
(e.g. overengineered architecture rejected, unsafe retry corrected, wrong EF assumption fixed, test that proved nothing
replaced, unneeded dependency rejected, UI corrected against the reference, unverified doc claim removed).
Do not record cosmetic edits. Never invent a correction; "None material" is a valid entry.

## Usage Log

One concise row per phase where AI materially contributed, added before the phase checkpoint commit.

| Phase | Tool | Assistance | Verification | Corrected / rejected |
| --- | --- | --- | --- | --- |
| 00 | ChatGPT + Claude Code | Architecture, scope, governance, design contract and reference adaptation, repository preparation | Challenge requirements comparison, `git diff --check`, staged diff review; reference HTML rendered in headless Edge at 1440/768/390 px and structure checked by script (roadmap Phase 00 evidence) | Design tokens first derived without the source were replaced by the real reference values; decision format corrected to include alternatives and risks; stack-specific reference content (Go/PostgreSQL/Next.js, Docker, CI, metrics, Copilot file) rejected as not applicable; reference layout defects (codes wrapping at hyphens, stacked reference cell) fixed after render review |
| 00 follow-up | Claude Code | Removed documentation references to the design-reference section deleted by the owner (`f0c84ab`) | `grep` for remaining references, diff review | None material |
| 00.1 | Claude Code | AI governance skill, usage evidence model, routing updates | `git diff --check`, `git check-ignore` on this file, staged diff review | None material |
| 01.1 | Claude Code | Solution scaffolding, domain entity with guarded transitions, EF Core mapping, DI registration, migration generation, docs | `dotnet restore`; `dotnet build` (0 warnings, 0 errors); `dotnet tool restore`; `dotnet ef migrations list` (InitialCreate Pending); appsettings JSON parse; diff review. DB apply and tests pending 01.3 | AI-written `appsettings.json` lost a backslash in the LocalDB server name; caught by the failing design-time host and fixed before generating the migration |
| 01.2 | Claude Code | Review of the 01.1 implementation against requirements and decisions; OD-04 decision record | `dotnet build` (0 warnings), `dotnet ef migrations list --verbose`, `dotnet ef migrations has-pending-model-changes`, Designer/snapshot diff, `.csproj` reference inspection, scope `grep`, diff review | None material in product code; two skills found stale about OD-03/OD-04 and aligned |
| 01.3 | Claude Code | Unit, LocalDB integration and functional tests; LocalDB fixture; evidence updates | `dotnet build` (0 warnings); `dotnet test` 33/33 passed, 0 skipped; temporary mutations (UNIQUE removed, transition guard disabled) made the targeted tests fail; `dotnet ef database update` + `sqlcmd` schema/collation check; leftover test DB check; diff review | A scripted traceability edit lost a regex escape and prepended rows instead of replacing them; caught in diff review, file restored from Git and edited again |
| 02.1 | Claude Code | Timestamps, replay rule, repository, idempotent create/list service, controller, DTOs, ProblemDetails, CORS, migration generation, focused domain tests, docs | `dotnet build` (0 warnings); `dotnet test` 43/43; `dotnet ef migrations list` (both migrations); `has-pending-model-changes` none; `dotnet ef database update`; manual API smoke with `curl` (201/200/409/400, filter, CORS) and `sqlcmd` row check; diff review | None material (a `grep` check of the LocalDB escaping gave a false negative due to shell escaping; verified with a JSON parse instead) |
| 02.2 | Claude Code | Review of the 02.1 API against requirements and decisions; DTO trimming fix | `dotnet build`/`dotnet test` 43/43; `curl` challenge of the six idempotency cases and a 12-request concurrent burst; `sqlcmd` row counts and `SHOWPLAN_TEXT`; forced 500 with an unreachable server; diff review | Length validation on untrimmed DTO values corrected (trim before validation). A smoke script hung because `wait` also waited for the API process; stopped and rerun separately |
| 02.3 | Claude Code | Unit (Moq), LocalDB integration and WebApplicationFactory HTTP tests; temporary log probe; evidence | `dotnet test` 71/71, 0 skipped; four temporary mutations each made the targeted tests fail; migration smoke on a dropped and recreated challenge database with `sqlcmd` schema check; diff review | The new HTTP tests exposed that `[Produces("application/json")]` turned ProblemDetails into `application/json`; attribute removed. Confirmed (not fixed) that EF logs a handled UNIQUE race at Error, deferred to Phase 05 |
| 02.4 | Claude Code | README execution guide, decision-format completion, traceability and AI-log review | Each README command executed (`dotnet tool restore`, `dotnet restore`, `dotnet ef database update`, `dotnet build`, `dotnet run` on the http profile → 200, `dotnet test` 71/71); fail-fast without connection string observed; Phase 02 log rows checked against history; diff review | TD-03 still said "Validation: pending" although validated in 01.3; corrected |
| 03.1 | Claude Code | Provider contract and failure types, simulated provider, repository additions, processor with Polly pipeline, Worker loop with startup recovery, Windows Service hosting, focused tests, docs | `dotnet build` (0 warnings); `dotnet test` 86/86; manual API + Worker run (retry → Completed, permanent → Failed) and startup recovery of a Processing row checked with `sqlcmd`; diff review | Processor placed in the Worker instead of Core as the skill text said, to keep Polly out of Core (TD-20; skill updated). Own diff review found that a database failure during startup recovery would stop the host; fixed and checked with an unreachable server |
| 03.2 | Claude Code | Review of the 03.1 Worker against the processing guarantees; two corrections; Worker loop tests | `dotnet test` 88/88; new recovery test run against the previous Worker file (fails) and the fix (passes); diff review | The 03.1 startup-outage fix was incomplete: it kept the process alive but skipped recovery for the whole run — corrected (recovery retried before Pending). Final state write made non-cancellable after a provider outcome |
| 03.3 | Claude Code | Unit, LocalDB integration and functional Worker tests; HTTP 500 regression; Worker composition extracted to `AddRequestProcessing`; evidence | `dotnet build` (0 warnings); `dotnet test` 100/100, 0 skipped; six temporary mutations each made the targeted tests fail; bounded `dotnet run` of the Worker; leftover test DB check; diff review | None material in product behavior. A test run with `--no-build` after a mutation used stale binaries (4 false failures); rebuilt and reran. The StringLength finding listed as open in the phase instructions had already been fixed in 02.2; recorded accurately instead of reopening it |
| 03.4 | Claude Code | README Worker operations (flow, retries, provider limits, recovery, Windows Service, configuration), decision/traceability/roadmap updates, Phase 03 AI-log review | `dotnet publish` of the Worker (exe produced); README walkthrough executed as written (API 5164 + Worker → GET Completed, log lines quoted); invalid Worker option → startup validation error; `dotnet test` 100/100; diff review | None material |
| 04.1 | Claude Code | Vite React TS scaffold cleanup, API client, list hook with polling, form/filter/table/alert components, tokens and CSS adapted from the reference, evidence | `npm run build` (strict tsc + vite) and `npm run lint` (0/0); `npm audit` 0; `dotnet build`/`dotnet test` 100/100; smoke API + Worker + Vite with headless-Edge DOM showing a processed request; raw-color/inline-style scan; diff review | Own review found the list hook kept a failure flag across filter changes (would show an old error instead of loading); fixed before build by tying the failure to its filter |
| 04.2 | Claude Code | Review of the 04.1 portal against the API contract, design contract and accessibility rules; two small corrections | `npm run build`, `npm run lint` 0/0; `dotnet build`/`dotnet test` 100/100; code inspection of POST body, 201/200 branch, error parsing, polling cancellation, dependencies; diff review | Focus now moves to the first invalid field after validation; list retry clears the error state so progress is visible |
| 04.3 | Claude Code | Browser acceptance of the portal on the real stack via a throwaway DevTools-protocol script (scratchpad), screenshots and reference comparison, evidence | `npm ci`/build/lint; `dotnet test` 100/100; scenarios 201, 200 (also case variant), 409, client and real server 400, filter (5 options, URLs checked), Failed, API stopped/restarted, automatic refresh timing, Actualizada = updatedAt; screenshots at 1440/768/390 inspected; keyboard and reduced-motion checks | None material in product code. Test-harness issues found and handled: first CDP target was the browser sync dialog (screenshot with account data deleted), a hidden tab paused refresh/rAF (re-validated with the tab visible), an unencoded file path, and a helper that closed the portal tab |
| 04.4 | Claude Code | README full-stack run and reviewer demo, frontend decision records, traceability and roadmap updates, Phase 04 AI-log review | `.env.local` + `npm run dev` path executed (env injection checked); `dotnet build`/`dotnet test` 100/100; `npm run build`; diff review | None material |
