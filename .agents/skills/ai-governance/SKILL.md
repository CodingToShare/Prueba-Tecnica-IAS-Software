---
name: ai-governance
description: Keep AI assistance subordinate to requirements, decisions and deterministic validation, and record concise, factual AI usage evidence per phase in docs/ai/ai-usage.md.
---

# AI Governance

## Use When

Any phase where AI materially assisted (analysis, design, code, tests, review, docs); writing or reviewing the AI
Usage Log; preparing the final README AI disclosure (with `documentation`).

## Responsibilities

- AI may assist with analysis, architecture alternatives, implementation, refactoring, test generation, review and
  documentation.
- Every material AI-assisted phase closes with one row in `docs/ai/ai-usage.md`:
  **assistance · actual verification · material correction/rejection** (or `None material`).
- Verification matches the affected risk:

  | Change | Verification |
  | --- | --- |
  | C# code | build + relevant tests |
  | EF migration | apply/check database when the phase requires it |
  | Worker retry | focused resilience tests |
  | React UI | build + required manual/visual validation |
  | Docs only | `git diff --check` + comparison with the source |

## Required Rules

- AI assistance ≠ project authority ≠ validation evidence. Generated or suggested changes follow the same
  source-of-truth hierarchy, tests, review and Definition of Done as manual changes.
- AI review is complementary to, not a replacement for, deterministic validation.
  Bad: "Claude reviewed the migration, so persistence is verified." Good: "Claude assisted with the migration.
  Verification: migration applied to an empty database; UNIQUE index inspected."
- Verification column lists only checks actually executed and observed in this phase.
- Keep entries concise (normally one row, under a minute); the log never substitutes traceability, phase evidence,
  test/build output or manual acceptance.
- AI usage is disclosed in `docs/ai/ai-usage.md` and the README AI note — never as commit trailers.

## Prohibited

Inventing requirements; silently changing accepted decisions; claiming validation not executed; inventing test/build
results, elapsed time or human review; fabricating corrections/rejections; storing prompts, conversations, reasoning
or tool session state in the repository; committing secrets, personal data or machine-specific paths; overriding the
source-of-truth hierarchy; adding instruction files for assistants not actually in use.

## Completion Checklist

- [ ] Usage Log row added if AI materially contributed
- [ ] Verification column matches commands/checks actually run
- [ ] Correction/rejection is real or `None material`
- [ ] No prompts, conversations or sensitive data committed
