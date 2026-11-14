---
name: sqlserver-efcore
description: EF Core + SQL Server mapping, migrations, UNIQUE externalReference, query-justified indexes, safe async data access.
---

# SQL Server + EF Core

## Use When

Changing the DbContext, entity configuration, migrations, queries or storage initialization.

## Responsibilities

- `ServiceRequest` mapping with explicit column lengths and required fields.
- UNIQUE index on `ExternalReference` (idempotency guard).
- Index on `Status` (list filter and Worker polling).
- Status stored as string for readability.
- Reproducible initialization: committed EF migrations + documented `dotnet ef database update`
  (or startup migration in Development — decide and record in Phase 01).

## Required Rules

- Fluent configuration in Infrastructure; Core entity stays persistence-ignorant (private setters are fine).
- Async queries with `CancellationToken`; `AsNoTracking()` for read-only lists.
- Detect unique violations via `DbUpdateException` with SQL error 2601/2627 — translate, don't leak.
- Optimistic concurrency token (rowversion) only if the Worker claim needs it; document if deferred.
- Connection string from configuration (`ConnectionStrings:ServiceRequests`), overridable by env var.
  LocalDB default is acceptable for development; no passwords committed.
- Transactions only where multiple writes must be atomic.

## Prohibited

String-concatenated SQL, raw SQL without parameters, generic repository wrappers, `EnsureCreated()` alongside migrations,
indexes without a query that needs them, committed credentials.

## Completion Checklist

- [ ] Migration committed and applies on a clean database
- [ ] UNIQUE index on ExternalReference present in migration
- [ ] Data survives API/Worker restart
- [ ] Init steps documented
