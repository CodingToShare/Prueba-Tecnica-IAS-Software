---
name: ui-design-system
description: Apply the approved navy / Plus Jakarta Sans visual language and the request-status semantic tokens from docs/design/design-system.md to every user-facing change.
---

# UI Design System

## Use When

Creating or modifying anything user-facing in `frontend/service-requests-web`: layout, header, form, table, status badges,
filter, feedback banners, loading/empty/error states, responsive behavior.

## Read First

1. `AGENTS.md`
2. `docs/design/design-system.md` (implementation contract: tokens, components, states)
3. `react-frontend` skill

## Responsibilities

Compose the single portal screen from the semantic tokens and the few primitives in the contract so it looks like one
coherent, professional internal tool and expresses request-status meaning (Pending, Processing, Completed, Failed)
and submit outcomes (created, replayed, conflict, invalid).

## Required Rules

- All colors, font, spacing, radius and shadow come from CSS custom properties defined once in `src/styles/tokens.css`.
  No raw hex values in components.
- Plus Jakarta Sans with a system-font fallback; the UI must stay readable if the web font fails to load.
- Navy `#1E3A5F` is the primary/brand color; the navy gradient is allowed only in the app header.
- Status is never conveyed by color alone: every badge has text and a glyph.
- Keep status semantics separate from feedback semantics and from destructive actions (there are none in this product).
- Required states: loading, empty, recoverable error with retry, disabled/sending, success feedback.
- Interaction states: hover, active, disabled, and always-visible `:focus-visible` ring.
- Forms: explicit `<label>` per field, validation messages next to the field, `aria-invalid` + `aria-describedby`.
- Keyboard-only use must work (form → submit → filter → refresh → table).
- Responsive at ~1440, ~768 and ~390 px: no page-level horizontal overflow; the table scrolls inside its own container.
- Respect `prefers-reduced-motion`; motion only for the Processing indicator.

## Prohibited

Component libraries, CSS frameworks, a second palette, gradients outside the header, heavy shadows, gratuitous animation,
dark mode work, navigation or modules the product does not need, spending more than the Phase 04 timebox on polish.

## Completion Checklist

- [ ] Tokens file is the only source of visual values
- [ ] Status badges: text + glyph + token colors
- [ ] All UI states visible and reachable
- [ ] Checked at ~1440 / ~768 / ~390 px (browser devtools), no horizontal page overflow
- [ ] Keyboard pass with visible focus
- [ ] Deviations from the contract recorded in the roadmap Phase 04 evidence
