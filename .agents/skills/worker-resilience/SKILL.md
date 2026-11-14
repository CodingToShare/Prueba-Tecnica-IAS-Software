---
name: worker-resilience
description: BackgroundService processing of Pending requests, state-transition orchestration, provider boundary, Polly retry on transient failures only, graceful shutdown and Windows Service readiness.
---

# Worker & Resilience

## Use When

Working on `ServiceRequests.Worker`, the request processor, the provider interface or the simulated provider.

## Responsibilities

```text
loop (configurable interval) → fetch Pending batch
  → MarkProcessing() + SaveChanges (claim)
  → provider.SubmitAsync(externalReference, ...) wrapped in Polly retry
  → success → Complete() + save
  → transient retries exhausted → Fail(reason) + save
```

- Processing logic in a testable class (Core), not inside `ExecuteAsync`.
- Provider interface in Core; simulated provider in Infrastructure, idempotent by `externalReference`
  (repeat call with same key returns the prior result without a second effect).
- Simulated failures configurable (e.g. transient failure rate) and deterministic in tests.

## Required Rules

- Polly only at the provider boundary; retry only a dedicated transient exception/result type.
- Bounded retries (e.g. 3 attempts) with short backoff suitable for a demo; values from configuration.
- Non-transient failures fail fast (no retry).
- Respect `stoppingToken`; one request's failure must not kill the loop (log and continue).
- New DI scope per iteration for DbContext.
- Host built with `Host.CreateApplicationBuilder` + `AddWindowsService()` so it can run as a Windows Service;
  `dotnet run` for the demo. Document `sc.exe create` / publish steps in README.
- Startup recovery (adapted from the reference's "recover orphaned runs at startup"): with the single-instance
  assumption, requests found in `Processing` when the Worker starts were orphaned by a crash and are resumed —
  safe because the provider is idempotent by `externalReference`. If cut for time, document it as a limitation.
- Single-instance assumption documented; multi-instance needs an atomic claim and lease timeout (future evolution).
- Structured logs via `ILogger`: claim, each retry (`{Attempt}`), final outcome, with `{ExternalReference}`;
  no payloads or secrets.

## Prohibited

Retrying all exceptions, unbounded retries, distributed locks, brokers, outbox, Thread.Sleep, processing logic only
testable through the host.

## Completion Checklist

- [ ] Pending → Completed demonstrated end to end
- [ ] Transient → retry → success test (Moq, call count asserted)
- [ ] Retries exhausted → Failed test
- [ ] Completed never reprocessed
- [ ] Windows Service path documented
