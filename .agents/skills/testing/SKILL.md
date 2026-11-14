---
name: testing
description: xUnit + Moq tests focused on the highest-risk behavior (state transitions, retry, idempotency, input validation), written with each phase and kept deterministic.
---

# Testing

## Use When

Any phase that changes behavior, and Phase 05 hardening.

## Responsibilities

Risk-ordered coverage:
1. **State transitions** — `TheoryData` of (from, action, allowed?). Completed → anything rejected.
2. **Retry** — Moq provider: transient, transient, success → 3 calls, request Completed;
   always transient → Failed after max attempts; non-transient → no retry.
3. **Idempotency** — new → created; equivalent replay → existing, no second row; conflicting → conflict;
   unique-violation race path translated correctly.
4. **Input validation** — `MemberData` of invalid DTOs (missing/too long fields, bad status filter).

## Required Rules

- Tests live in `tests/ServiceRequests.Tests`; run with `dotnet test`.
- Deterministic: no real delays (zero backoff in tests), no randomness without fixed seed, no wall-clock dependence.
- Mock only boundaries (provider, clock if needed). Prefer real entity behavior over mocking it.
- Persistence-dependent tests run against SQL Server LocalDB on a dedicated test database built from the migrations
  (TD-17); not EF InMemory (does not enforce unique indexes) nor SQLite.
- Use `TheoryData`/`MemberData` where they improve readability, not by obligation.
- Each phase: focused tests for new code + full `dotnet test` regression.
- Descriptive names stating condition and expected outcome (e.g. `Complete_WhenAlreadyCompleted_Throws`).
- Inject time (`TimeProvider`) where timestamps matter in assertions.
- No hidden skipped tests: every skip is listed with a reason.
- No fake evidence: report only commands actually run after the final change.
- Frontend: no test framework added for the challenge; validated by `npm run build` (strict type-check) + manual
  smoke following the design contract (documented trade-off).

## Prohibited

Testing private implementation details, sleeping in tests, skipping failing tests silently, coverage-chasing trivial getters.

## Completion Checklist

- [ ] New behavior has tests in the same phase
- [ ] Full suite green (or failure reported)
- [ ] Scenarios listed in README test note
