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
- **technical-decisions.md**: Context · Decision · Why · Trade-off · Future evolution (+ validation evidence).
- **Legacy review**:
  - WebForms: SQL injection via concatenation → parameterized `SqlCommand` (`@customer`); also dispose reader/command.
  - MVC: `Find(id)` null → 404; unrestricted `status` string → validate against enum/allowed transitions;
    Completed can regress → enforce transition rule; entity returned directly → return a DTO; consider anti-forgery token.
  - Minimal safe changes only, no rewrites.
- **AI note**: what AI was used for, how output was verified, what was corrected or rejected. No sensitive data.
- **Deferred work**: current state · reason · risk · next concrete step.

## Required Rules

- Document only what exists and was validated; mark the rest as pending.
- Brief and verifiable: commands a reviewer can copy and run.
- No machine-specific paths or secrets.

## Prohibited

Claiming unimplemented features, ADR directory trees, marketing language, duplicating AGENTS.md.

## Completion Checklist

- [ ] Every run instruction executed at least once
- [ ] Assumptions and trade-offs listed
- [ ] Legacy review covers both snippets
- [ ] AI usage disclosed
- [ ] Deferred work explicit
