---
name: react-frontend
description: Minimal professional React + TypeScript + Vite portal to create requests, list them, see status and filter by status using native fetch and hooks.
---

# React Frontend

## Use When

Working in `frontend/service-requests-web`.

## Responsibilities

- Create form: externalReference, customerId, serviceCode, description.
- Table: externalReference, customerId, serviceCode, status (+ description/updated time if cheap).
- Status filter (All / Pending / Processing / Completed / Failed) backed by `GET /api/requests?status=`.
- Small typed API client using native `fetch`; API base URL from `VITE_API_BASE_URL`.
- Manual refresh (and optionally light polling) so Worker progress is visible.

## Required Rules

- Local state with hooks only.
- Handle loading, empty, error (show ProblemDetails `title`/`detail`/validation errors) and success
  (distinguish 201 created vs 200 existing replay vs 409 conflict).
- Disable submit while sending (reduces accidental double submits; backend idempotency is still the guard).
- Client-side `required`/`maxLength` mirror the API, but the server stays authoritative.
- Visuals per `docs/design/design-system.md` via the `ui-design-system` skill: plain CSS, `src/styles/tokens.css`
  as the single source of visual values, no extra design work beyond that contract.
- TypeScript strict; no `any` as an escape hatch; API types mirror the backend DTOs.
- Suggested structure: `src/api/` (client + types), `src/components/` (form, filter, table, badge, banner), `src/styles/`.

## Prohibited

Redux, Zustand, React Query, component libraries, routing for a single screen, hard-coded secrets.

## Completion Checklist

- [ ] Create → appears in list with status
- [ ] Filter by status works
- [ ] Loading / empty / error / success visible
- [ ] `npm run build` passes (type-check included)
- [ ] `ui-design-system` checklist completed (tokens, widths, keyboard)
