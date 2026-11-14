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
| 00.1 | Claude Code | AI governance skill, usage evidence model, routing updates | `git diff --check`, `git check-ignore` on this file, staged diff review | None material |
