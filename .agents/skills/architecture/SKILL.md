---
name: architecture
description: Keep the pragmatic four-project backend structure, dependency direction and accepted technical decisions; reject speculative abstractions.
---

# Architecture

## Use When

Adding a project, reference, NuGet/npm package, abstraction or cross-cutting concern, or questioning an accepted decision.

## Responsibilities

- Guard boundaries: Api/Worker → Infrastructure → Core. Core has no EF Core, ASP.NET or Polly dependency.
- Core owns the `ServiceRequest` entity, its state transitions, use-case services and the interfaces they need
  (persistence access, provider). Infrastructure implements them.
- Keep `docs/technical-decisions.md` consistent with the code.

## Required Rules

- YAGNI/KISS: an abstraction must have a concrete consumer or a test seam need today.
- Interfaces only at real boundaries: persistence and the external provider.
- New dependency → justify by a concrete requirement and record it in `technical-decisions.md`.
- Changing an accepted decision → new decision entry, never a silent swap.
- Prefer framework built-ins (DI, Options, ProblemDetails, data annotations, `BackgroundService`).

## Prohibited

Extra Domain/Application split, generic repositories, MediatR, AutoMapper, FluentValidation, CQRS, event sourcing,
message brokers, outbox, distributed locks, YARP, caches, microservices — see AGENTS.md non-goals.

## Completion Checklist

- [ ] Project references respect dependency direction
- [ ] No unjustified package added
- [ ] Every new abstraction has a present-day reason
- [ ] Decisions document still matches the code
