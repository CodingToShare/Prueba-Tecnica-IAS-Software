---
name: phase-governance
description: Start, execute and close a roadmap phase with an entry gate, timebox, baseline validation, exit gate, local checkpoint commit and a mandatory stop.
---

# Phase Governance

## Use When

Starting, executing, closing or changing the status of any phase in `docs/phases/roadmap.md`.

## Responsibilities

Only the explicitly authorized phase runs, on a verified baseline, inside its scope and timebox, and closes with real evidence.

## Required Rules

**Entry gate**
1. Confirm explicit user authorization for this phase.
2. Read `AGENTS.md`, `.agents/context/project-context.md`, the phase scope in the roadmap, relevant skills.
3. `git status` — preserve unrelated changes.
4. Run the baseline validation recorded in the roadmap (build/tests of earlier phases).
5. Baseline broken → report **Blocked by baseline regression**, investigate, do not continue silently.
6. Set phase status to In Progress.

**Execution**
- Implement only the phase scope; surface blockers instead of inventing behavior.
- Tests are written together with the implementation.
- Respect the timebox. Near the 120-min hard stop, cut optional work and document it as:
  current state · reason · risk · next concrete step.

**Exit gate**
- Build, focused tests, relevant regression; mark checks N/A only with a reason.
- UI phases: design validation against `docs/design/design-system.md` (widths, states, keyboard).
- A phase with a known blocking failure is not Complete. Never a subject-only checkpoint commit.
- Review the actual diff (`code-review` skill for later phases).
- Update roadmap status + evidence (commands run, results). Never fabricate elapsed times.
- Commit (when authorized): Conventional Commit subject, blank line, body starting `Phase NN:`.
  `git status` → `git diff --check` → `git diff` → commit → `git log -1 --format=%B`.
- No assistant attribution in commits. No remote, no push.
- STOP and report.

## Prohibited

Starting the next phase automatically; inferring authorization; marking Complete from file existence;
skipping baseline; pulling later scope forward; backdating commits; hiding failing tests.

## Completion Checklist

- [ ] Entry gate passed (or block reported)
- [ ] Scope implemented with tests
- [ ] Exit checks run and recorded
- [ ] Roadmap updated with evidence
- [ ] Local checkpoint commit verified
- [ ] Stopped; next phase awaits authorization
