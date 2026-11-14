# Requirements Traceability

Maps every challenge requirement to the phase, component and verification that cover it. Evidence is filled **only**
with checks that actually ran, in the same change that produces them.

Levels: **U** unit test · **I** integration test · **M** manual run (recorded) · **R** repository/review check · **D** documentation.
Status: `Planned` → `In Progress` → `Verified` (or `Deferred` with reason).

## Expected Results (challenge §2)

| ID | Requirement | Phase | Component | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| R-01 | API registers a request | 02 | `POST /api/requests` | U, M | | Planned |
| R-02 | API queries requests by status | 02 | `GET /api/requests?status=` | U, M | | Planned |
| R-03 | Web UI creates a request and shows its status (React) | 04 | `frontend/service-requests-web` | M, R | | Planned |
| R-04 | Real persistence + reproducible storage initialization | 01 | EF Core migrations, SQL Server | I/M | 01.1: `InitialCreate` generated and listed by `dotnet ef migrations list` (Pending); 01.2: migration matches model (`has-pending-model-changes` → none); apply on empty DB pending 01.3 | In Progress |
| R-05 | Background process: Pending → simulated provider → status updated; Windows Service path clear | 03, 06 | `ServiceRequests.Worker` | U, M, D | | Planned |
| R-06 | Automated tests on the most critical behavior | 01–05 | `tests/ServiceRequests.Tests` | U | | Planned |
| R-07 | Brief log of decisions, assumptions, trade-offs, validations | 00, 06 | `technical-decisions.md`, README | D | | In Progress |

## Functional Rules (challenge §3)

| ID | Rule | Phase | Component | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- |
| BR-01 | Request has externalReference, customerId, serviceCode, description, status | 01, 02 | entity, DTOs, migration | U, R | 01.1: entity, mapping and migration implemented; tests 01.3, DTOs 02 | In Progress |
| BR-02 | States Pending, Processing, Completed, Failed | 01 | `ServiceRequestStatus` | U | 01.1: enum implemented; tests pending 01.3 | In Progress |
| BR-03 | Retry of the same operation never creates two provider effects | 02, 03 | UNIQUE index, idempotent create, idempotent provider | U, I | 01.1: UNIQUE `UX_ServiceRequests_ExternalReference` in migration (not yet applied) | In Progress |
| BR-04 | Simulated provider returns success or transient failure | 03 | simulated provider | U | | Planned |
| BR-05 | A Completed request never returns to an earlier state | 01, 03 | entity transitions | U | 01.1: guarded transitions implemented; tests pending 01.3 | In Progress |
| BR-06 | Screen shows externalReference, customerId, serviceCode, status and filters by status | 04 | table + segmented filter | M | | Planned |
| BR-07 | Ambiguities documented as assumptions | 00, 06 | `technical-decisions.md` (A-xx, OD-xx) | D | | In Progress |

## Technical Constraints (challenge §4)

| ID | Constraint | Phase | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| TC-01 | C# + ASP.NET Core Web API backend | 01 | R | 01.1: `net10.0` Web API (controllers) project builds, 0 warnings | In Progress |
| TC-02 | Exactly one frontend technology (React) | 04 | R | | Planned |
| TC-03 | SQL implemented + 3–5 lines on the NoSQL alternative | 01, 06 | R, D | | Planned |
| TC-04 | Inputs, errors, configuration and secrets handled safely | 02–04 | U, R | | Planned |
| TC-05 | Worker runnable manually + Windows Service approach documented | 03, 06 | M, D | | Planned |
| TC-06 | No Docker/cloud/CI required to run | all | R | | Planned |
| TC-07 | Executable path prioritized; unfinished parts documented (state · decision · next step) | 07 | D | | Planned |

## Legacy Review (challenge §5)

| ID | Item | Phase | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| LR-01 | WebForms: concrete risk (SQL injection) + minimal fix (parameterized `SqlCommand`) | 06 | D | | Planned |
| LR-02 | MVC: concrete risks (null `Find`, unrestricted status, Completed regression, entity returned) + minimal fix | 06 | D | | Planned |

## Delivery (challenge §6)

| ID | Item | Phase | Levels | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| D-01 | Git history shows the evolution of the solution | all | R | Local phase checkpoint commits (`git log`) | In Progress |
| D-02 | Brief reproducible instructions: backend, frontend, storage, Worker | 06 | M, D | | Planned |
| D-03 | Executable tests + note on covered scenarios | 05, 06 | U, D | | Planned |
| D-04 | Decision log: context, alternatives, decision, trade-offs, risks, validation | 00, 06 | D | Format established in `technical-decisions.md` | In Progress |
| D-05 | Business/technical assumptions | 00, 06 | D | A-01…A-06 registered | In Progress |
| D-06 | AI usage note (what for, how verified, what corrected/rejected) | 06 | D | `docs/ai/ai-usage.md` — incremental evidence; final README disclosure pending Phase 06 | Planned |
