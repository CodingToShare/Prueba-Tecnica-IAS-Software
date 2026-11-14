# Design System — Service Requests Portal

Implementation contract for the React portal. Small on purpose: one screen, plain CSS, custom properties.

## Origin

Adapted from the visual language of a previous project's design system v3 (navy `#1E3A5F` primary, Plus Jakarta Sans,
light surfaces, restrained shadows, semantic tokens, accessibility-first states).
The canonical HTML reference of that system (`design-system-v3.html`) and its full contract were **not available**;
only its rules were. Therefore:

- **Taken from the reference:** primary navy `#1E3A5F`, Plus Jakarta Sans, light surfaces, restrained shadows,
  gradient only on the brand bar, semantic tokens, color never the only carrier of meaning, visible focus,
  responsive validation at ~1440/768/390 px.
- **Derived for this product (assumption):** every other token value below, chosen for WCAG AA contrast (≥ 4.5:1 for text).
- If the original HTML reference is provided later, align the token values to it through a recorded decision.

## Tokens (`src/styles/tokens.css`)

```css
:root {
  /* Brand */
  --color-primary: #1E3A5F;
  --color-primary-hover: #162D4A;
  --color-primary-soft: #E8EEF5;
  --gradient-brand: linear-gradient(135deg, #1E3A5F 0%, #2B5486 100%); /* header only */

  /* Surfaces & lines */
  --color-bg: #F5F7FA;
  --color-surface: #FFFFFF;
  --color-border: #DDE3EA;

  /* Text */
  --color-text: #1A2433;
  --color-text-muted: #5B6B7F;
  --color-text-inverse: #FFFFFF;

  /* Request status (badge foreground / background) */
  --status-pending-fg: #475569;    --status-pending-bg: #F1F5F9;
  --status-processing-fg: #1D4ED8; --status-processing-bg: #EFF6FF;
  --status-completed-fg: #15803D;  --status-completed-bg: #F0FDF4;
  --status-failed-fg: #B91C1C;     --status-failed-bg: #FEF2F2;

  /* Feedback banners (text / background / border) */
  --feedback-success-fg: #166534; --feedback-success-bg: #F0FDF4; --feedback-success-border: #BBF7D0;
  --feedback-info-fg: #1E40AF;    --feedback-info-bg: #EFF6FF;    --feedback-info-border: #BFDBFE;
  --feedback-warning-fg: #92400E; --feedback-warning-bg: #FFFBEB; --feedback-warning-border: #FDE68A;
  --feedback-error-fg: #991B1B;   --feedback-error-bg: #FEF2F2;   --feedback-error-border: #FECACA;

  /* Typography */
  --font-sans: "Plus Jakarta Sans", system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
  --font-mono: ui-monospace, "Cascadia Code", Consolas, monospace; /* externalReference */
  --text-xs: 0.75rem; --text-sm: 0.875rem; --text-md: 1rem; --text-lg: 1.25rem; --text-xl: 1.5rem;
  --weight-regular: 400; --weight-medium: 500; --weight-semibold: 600; --weight-bold: 700;

  /* Spacing, radius, elevation, focus */
  --space-1: 4px; --space-2: 8px; --space-3: 12px; --space-4: 16px; --space-6: 24px; --space-8: 32px;
  --radius-sm: 6px; --radius-md: 10px; --radius-pill: 999px;
  --shadow-card: 0 1px 2px rgba(16, 24, 40, 0.06), 0 1px 3px rgba(16, 24, 40, 0.08);
  --focus-ring: 0 0 0 3px rgba(30, 58, 95, 0.35);
}
```

Font loading: Google Fonts `<link>` (weights 400/500/600/700, `display=swap`) in `index.html`; no npm dependency.
The system fallback keeps the UI usable offline.

## Layout

```text
┌──────────────────────────────────────────────────────────────┐
│ Header (gradient-brand): "Portal de solicitudes" + subtitle  │
├──────────────────────────────────────────────────────────────┤
│ ┌─ Nueva solicitud (card) ─┐ ┌─ Solicitudes (card) ────────┐ │
│ │ externalReference        │ │ [Todas][Pending][Processing]│ │
│ │ customerId               │ │ [Completed][Failed] [↻]     │ │
│ │ serviceCode              │ │ table: ref | customer |     │ │
│ │ description (textarea)   │ │ service | status | updated  │ │
│ │ [Registrar solicitud]    │ │                             │ │
│ │ feedback banner          │ │                             │ │
│ └──────────────────────────┘ └─────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

- Max content width 1200 px, `--space-6` gutters.
- ≥ 1024 px: two columns (form ~360 px, list fills the rest). < 1024 px: stacked, form first.
- ≤ 640 px: filter buttons wrap; table keeps columns and scrolls horizontally inside `.table-wrapper` (`overflow-x: auto`).

## Components

| Component | Contract |
| --- | --- |
| Card | `--color-surface`, `1px solid --color-border`, `--radius-md`, `--shadow-card`, padding `--space-6` |
| Primary button | bg `--color-primary`, hover `--color-primary-hover`, text inverse, `--radius-sm`; disabled = 60% opacity + `cursor: not-allowed`; label changes to "Registrando…" while sending |
| Secondary button (refresh, retry) | surface bg, border, primary text |
| Input / textarea | border `--color-border`, `--radius-sm`; focus `--focus-ring`; invalid = error border + message below (`--feedback-error-fg`, `--text-sm`) |
| Status filter | button group, `aria-pressed` on the active one; active = `--color-primary-soft` bg + primary text |
| Status badge | pill, `--text-xs` semibold, glyph + label, colors from `--status-*` |
| Feedback banner | `role="status"` (success/info) or `role="alert"` (warning/error), colors from `--feedback-*` |
| Table | header `--text-xs` uppercase muted; rows separated by border; `externalReference` in `--font-mono` |

## Semantic Mapping

Request status (badge):

| Status | Glyph | Label | Tokens |
| --- | --- | --- | --- |
| Pending | ◷ | Pending | `--status-pending-*` |
| Processing | ↻ (spins unless reduced motion) | Processing | `--status-processing-*` |
| Completed | ✓ | Completed | `--status-completed-*` |
| Failed | ✕ | Failed | `--status-failed-*` |

Submit outcome (banner):

| API result | Banner | Message intent |
| --- | --- | --- |
| 201 Created | success | Request registered |
| 200 OK (replay) | info | A request with this reference already existed; showing the existing one (no duplicate created) |
| 409 Conflict | warning | Reference already used with different data |
| 400 Validation | error + field messages | Fix highlighted fields |
| Network / 5xx | error + retry | Could not reach the service |

## UI States

| State | Treatment |
| --- | --- |
| Loading list | muted "Cargando solicitudes…" row, container `aria-busy="true"` |
| Empty | "No hay solicitudes" (or "No hay solicitudes en estado X" when filtered) + hint to create one |
| Error | error banner with "Reintentar" button |
| Sending | submit disabled, label "Registrando…" |
| Success | banner per table above; list refreshes |

## Language

UI copy in Spanish (internal Spanish-speaking portal — assumption). Status labels keep the challenge's names
(Pending, Processing, Completed, Failed) so UI, API and documentation use one vocabulary.

## Validation (Phase 04 exit)

- Devtools at ~1440, ~768, ~390 px: hierarchy, spacing, no page-level horizontal overflow.
- Keyboard-only pass with visible focus.
- Contrast: text tokens meet AA on their backgrounds.
- Record any deviation from this contract in the Phase 04 evidence.
