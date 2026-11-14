---
name: code-review
description: Review the actual git diff before each checkpoint and at final delivery, reporting findings ordered by severity.
---

# Code Review

## Use When

Before every phase checkpoint commit and in Phase 07.

## Responsibilities

Review `git diff` (staged + unstaged) against the challenge and accepted decisions for:

- Correctness and challenge requirements coverage
- Idempotency (201/200/409, unique index, race handling, provider idempotency)
- State integrity (no public setter, Completed terminal)
- Concurrency risks (Worker claim, concurrent create)
- Error handling (ProblemDetails, no leaked internals, no swallowed exceptions)
- Secrets and configuration
- Persistence (migrations, indexes, no string-built SQL)
- Unnecessary abstractions or dependencies
- Missing tests for risky behavior
- Frontend states (loading/empty/error/success)

## Required Rules

- Findings ordered: **Critical → High → Medium → Low**, each with file:line, concrete failure scenario and fix.
- Fix Critical/High inside the current phase scope; record the rest as deferred with risk.
- Review real diff output, not memory of what was written.

## Prohibited

Approving from file names, style nitpicks presented as defects, expanding scope under the label of review.

## Completion Checklist

- [ ] `git diff --check` clean
- [ ] Findings listed by severity (or "none")
- [ ] Critical/High resolved or reported as blockers
