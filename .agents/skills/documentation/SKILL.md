---
name: documentation
description: README, technical decisions, assumptions, trade-offs, legacy code review, AI usage note, execution instructions and deferred work — describing only implemented behavior.
---

# Documentation

## Use When

Updating `README.md`, `docs/technical-decisions.md`, the roadmap evidence, or writing the legacy review / AI note.

## Responsibilities

- **README**: prerequisites; reproducible steps for database, API, Worker, frontend, tests; test scenario note;
  Windows Service deployment approach; NoSQL alternative (3–5 lines); assumptions; known limitations; AI usage note.
- **technical-decisions.md**: Context · Alternatives · Decision · Trade-off · Risks · Validation · Future evolution
  (challenge §6 format), plus Assumptions (A-xx) and Open Decisions (OD-xx) registers.
- **requirements-traceability.md**: evidence column filled only with checks actually run; status Verified/Deferred.
- **Demo walkthrough** (README): create → replay (200) → conflict (409) → Worker processes → filter by status.
- **Legacy review**:
  - WebForms: SQL injection via concatenation → parameterized `SqlCommand` (`@customer`); also dispose reader/command.
  - MVC: `Find(id)` null → 404; unrestricted `status` string → validate against enum/allowed transitions;
    Completed can regress → enforce transition rule; entity returned directly → return a DTO; consider anti-forgery token.
  - Minimal safe changes only, no rewrites.
- **AI note**: what AI was used for, how output was verified, what was corrected or rejected. No sensitive data.
  `docs/ai/ai-usage.md` = incremental factual evidence (per phase, `ai-governance`); README AI section = concise final
  disclosure summarized from that log in Phase 06 — never reconstructed from memory.
- **Deferred work**: current state · reason · risk · next concrete step.

**README outline:** what it is (1 paragraph) · architecture at a glance (diagram) · prerequisites · run
(database → API → Worker → frontend) · tests table (suite / command / needs / scenarios) · key decisions (links to
`technical-decisions.md`) · Windows Service · NoSQL alternative · assumptions · known limitations · legacy review · AI usage.

## Required Rules

- Keep categories explicit: REQUIREMENT (from the challenge), TECHNICAL DECISION, ASSUMPTION, OPEN DECISION,
  OUT OF SCOPE. Never promote an assumption to a requirement silently.
- Resolving an open decision records the decision, rationale and date, and updates dependent documents.
- Decisions belong to the project: AI tooling is disclosed only in the AI usage note, not attributed elsewhere.
- Document only what exists and was validated; mark the rest as pending.
- Brief and verifiable: commands a reviewer can copy and run.
- No machine-specific paths or secrets.

## Prohibited

Claiming unimplemented features, ADR directory trees, marketing language, duplicating AGENTS.md.

## Completion Checklist

- [ ] Every run instruction executed at least once
- [ ] Assumptions and trade-offs listed
- [ ] Legacy review covers both snippets
- [ ] AI usage disclosed (README note derived from `docs/ai/ai-usage.md`)
- [ ] Deferred work explicit
