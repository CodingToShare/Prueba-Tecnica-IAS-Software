---
name: ui-design-system
description: Apply the design system v3 visual language (navy #1E3A5F, Plus Jakarta Sans, reference tokens and component patterns) and the request-status semantics from docs/design/design-system.md, validated against the HTML reference.
---

# UI Design System

## Use When

Creating or modifying anything user-facing in `frontend/service-requests-web`: brand bar, layout, form, table,
status badges, segmented filter, alerts, loading/empty/error states, responsive behavior.

## Read First

1. `AGENTS.md`
2. `docs/design/design-system.md` (implementation contract — wins on conflict)
3. `docs/design/reference/design-system-v3.html` (canonical visual reference — open it in a browser; reuse its CSS patterns)
4. `react-frontend` skill

## Responsibilities

Build the single portal screen from the reference tokens and patterns so it matches the v3 visual language and
expresses request-status meaning (Pending → neutral, Processing → informational, Completed → normal, Failed → critical)
and submit outcomes (201 normal, 200 informational, 409 warning, 400/5xx error).

## Required Rules

- Tokens copied from the reference `:root` into `src/styles/tokens.css`; components use tokens only, no raw hex.
- The reference already shows this product's screen: build the app to match its "Simulación — Portal de solicitudes".
- Reuse the reference CSS patterns (`.brandbar`, `.panel`, `.field`/`.form-stack`, `.btn-*`, `.spinner`, `.segmented`,
  `.tbl`/`.stack`/`td.nowrap`, `.badge-{pending|processing|completed|failed}`, `.alert-*`, `.state`, `.skel`,
  `.skip-link`, `:focus-visible`) — copy and adapt, do not reinvent.
- Badges keep color + text + shape (dashed ring / circle / diamond); color is never the only carrier.
- `critical` (Failed state) and `destructive` (system errors) stay separate.
- One primary action per screen: "Registrar solicitud".
- States: loading skeleton, empty, no results for filter, recoverable error with retry, sending (disabled + spinner).
- Forms: label above field, error hint below, `aria-invalid` + `aria-describedby`.
- Keyboard-only use works; focus always visible; skip link to `main`.
- Responsive: two columns ≥ 980 px, stacked below; table → stacked labeled rows ≤ 640 px; no page-level horizontal overflow.
- `prefers-reduced-motion` respected.
- Navy brand bar instead of the reference sidebar (one screen, no navigation) — documented deviation.

## Prohibited

Changing the reference HTML without a recorded design decision; hard-coding its illustrative sample data; component libraries or CSS
frameworks; a second palette; gradients outside the brand bar; heavy shadows; gratuitous motion; navigation items the
product does not need; exceeding the Phase 04 timebox for polish.

## Completion Checklist

- [ ] `tokens.css` mirrors the contract; no raw colors in components
- [ ] Status badges and submit alerts follow the semantic mapping
- [ ] All UI states reachable
- [ ] Reviewed at ~1440 / ~768 / ~390 px and side by side with the reference HTML
- [ ] Keyboard pass with visible focus
- [ ] Deviations recorded in roadmap Phase 04 evidence
