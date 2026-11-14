# Design System — Service Requests Portal

The visual and interaction contract for the React portal (`frontend/service-requests-web`).

## 1. Visual Reference vs Implementation Contract

| | Visual reference | Implementation contract |
| --- | --- | --- |
| Artifact | [`reference/design-system-v3.html`](reference/design-system-v3.html) — open it in a browser | This document |
| Role | Canonical visual sample: the look and feel to match | Binding rules for this product |
| Defines | Palette, typography, spacing, radii, shadows, buttons, fields, panels, badges, alerts, tables, segmented filter, states, focus/hover/active/disabled | Token subset, request-status semantics, submit-feedback mapping, layout, states, accessibility, responsive rules, validation |
| Does **not** define | Requirements, domain terms, data, navigation | — |

The reference is design system v3 (Energy Management edition) from a previous project of the same author. Its domain
content (meters, anomalies, KPIs, charts) and Spanish sample values are **illustrative** and irrelevant here: only its
**visual language** is reused. The file is kept unmodified; when the two differ, this document wins.

## 2. Visual Direction (Adopted From The Reference)

- **Typography:** Plus Jakarta Sans 400–800, Inter and system sans-serif fallbacks. Headings 700–800 with slight
  negative tracking; small bold labels; uppercase only in table headers and group labels; tabular numerals for dates/counts.
- **Primary:** navy `#1E3A5F` is the single primary interactive color (hover `#16314F`, active `#0F2238`, soft `#EAEFF5`,
  ring `rgba(30,58,95,.28)`).
- **Surfaces:** page `#F5F6FB`, white panels, muted `#F3F5FA` for table headers, subtle borders, restrained layered
  shadows, generous spacing, radii 10 / 14 / 20 / 26 px.
- **Shell:** navy brand bar (see §6). Only gradient allowed: navy → navy-900, and only on the brand bar.

## 3. Tokens (`src/styles/tokens.css`)

Copied from the reference `:root`, limited to what this product uses. Components consume tokens, never raw values.

```css
:root {
  /* Action */
  --brand-primary: #1e3a5f; --brand-primary-hover: #16314f; --brand-primary-active: #0f2238;
  --brand-primary-soft: #eaeff5; --brand-primary-ring: rgba(30, 58, 95, .28);
  --navy: #1e3a5f; --navy-900: #0f2238;

  /* Neutrals */
  --color-heading: #11203a; --color-text: #28324a; --color-text-muted: #5d6880;
  --color-text-subtle: #9aa3b8;            /* placeholders/decoration only, never informative text */
  --color-border: #e7eaf2; --color-border-strong: #d4d9e6;
  --page-bg: #f5f6fb; --surface: #ffffff; --surface-muted: #f3f5fa;

  /* Semantic tones (text ≥ 4.5:1 on its soft background) */
  --normal: #0f766e;        --normal-soft: #e3f4f1;
  --informational: #1e3a5f; --informational-soft: #eaeff5;
  --warning: #8a6100;       --warning-soft: #fcf2dd;
  --critical: #9f1239;      --critical-soft: #fdecef;
  --neutral: #5d6880;       --neutral-soft: #f3f5fa;

  /* Destructive: system errors only (no destructive actions exist in this product) */
  --destructive: #dc2626; --destructive-text: #b91c1c; --destructive-soft: #fdecec;

  /* Shape & type */
  --radius-sm: 10px; --radius-md: 14px; --radius-lg: 20px; --radius-xl: 26px;
  --shadow-xs: 0 1px 2px rgba(17,32,58,.05); --shadow-sm: 0 2px 6px rgba(17,32,58,.06);
  --shadow-md: 0 6px 20px rgba(17,32,58,.07);
  --font-sans: "Plus Jakarta Sans", "Inter", -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
}
```

Not used (reference-only): accents (mint/sky/violet/amber), `data-quality`, chart tokens, `radius-xl`/`shadow-lg` shell.
Font: Google Fonts `<link>` for Plus Jakarta Sans 400–800 (`display=swap`) in `index.html`; no npm dependency.

## 4. Request Semantics

Request status → reference tone (badge = color + text + shape; color is never the only carrier):

| Status | Tone | Badge shape | Rationale |
| --- | --- | --- | --- |
| `Pending` | neutral | dashed ring | Waiting, nothing has happened yet (reference "not analyzed yet") |
| `Processing` | informational | circle | In progress, no action required |
| `Completed` | normal | circle | Expected healthy outcome; terminal |
| `Failed` | critical | diamond | Needs attention: transient retries exhausted |

Submit outcome → reference alert:

| API result | Alert | Role | Message intent |
| --- | --- | --- | --- |
| 201 Created | `alert-normal` | `status` | Solicitud registrada |
| 200 OK (replay) | `alert-informational` | `status` | Ya existía una solicitud con esta referencia; se muestra la existente (no se duplicó) |
| 409 Conflict | `alert-warning` | `alert` | La referencia ya existe con datos diferentes |
| 400 Validation | field errors + `alert-error` summary | `alert` | Corrige los campos marcados |
| Network / 5xx | `alert-error` + "Reintentar" | `alert` | No se pudo contactar el servicio |

Rules (from the reference):
- `critical` (a business state needing attention) and `destructive` (system error) are separate tokens; never swap them.
- Danger styling is never used just to make something look important.

## 5. Components (reference classes, plain CSS)

| Pattern | Reference class | Use here |
| --- | --- | --- |
| Panel | `.panel`, `.panel-head`, `.panel-body` | "Nueva solicitud" and "Solicitudes" containers |
| Field | `.field` (label above, `.hint`, `.field.error`) | 4 form fields; error hint in `--destructive-text` |
| Button | `.btn-primary` (+ `.spinner` while sending), `.btn-secondary`, `.btn-outline`, `.btn-sm` | Register (primary, one per screen), Refresh / Retry (secondary/outline) |
| Segmented filter | `.segmented` + `aria-pressed` | Todas / Pending / Processing / Completed / Failed |
| Table | `.tbl-wrap`, `table.tbl`, `table.stack` + `data-label` | Request list; stacked labeled rows ≤ 640 px; row hover `primary-soft` |
| Badge | `.badge` + `.badge-{neutral\|informational\|normal\|critical}` | Status column |
| Alert | `.alert` + `.alert-{normal\|informational\|warning\|error}` | Submit feedback, load error |
| States | `.state`, `.skel`, `.glyph` | Loading skeleton, empty, no results for filter (with "Ver todas"), error with retry |
| Utilities | `.sr-only`, `.skip-link`, `:focus-visible` outline | Accessibility |

Not used: sidebar navigation, KPI cards, queue, progress/steps, story/evidence, charts, search, card hover lift.

## 6. Layout

```text
┌ brand bar (navy gradient) ─ "Portal de solicitudes" ───────────────────┐
├────────────────────────────────────────────────────────────────────────┤
│ page title + subtitle                                  [↻ Actualizar]  │
│ ┌ panel: Nueva solicitud ─┐ ┌ panel: Solicitudes ─────────────────────┐ │
│ │ externalReference       │ │ [Todas|Pending|Processing|Completed|    │ │
│ │ customerId              │ │  Failed]                                │ │
│ │ serviceCode             │ │ Referencia | Cliente | Servicio |       │ │
│ │ description (textarea)  │ │ Estado | Actualizada                    │ │
│ │ [Registrar solicitud]   │ │                                         │ │
│ │ alert (submit outcome)  │ │                                         │ │
│ └─────────────────────────┘ └─────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────────┘
```

- Content max width 1180 px (reference `.wrap`), page background `--page-bg`.
- ≥ 980 px: two columns (form ~380 px, list fills the rest). < 980 px: stacked, form first.
- ≤ 720 px: tighter padding (reference). ≤ 640 px: table becomes stacked labeled rows; filter wraps.
- **Recorded deviation:** the reference shell has a navy sidebar with navigation. This product has one screen, so it
  uses a navy brand bar instead (same gradient, no navigation items) — the reference forbids adding navigation the
  product does not need.

## 7. States And Accessibility

- States: loading (skeleton rows, `aria-busy`), empty ("Aún no hay solicitudes"), no results for the active filter
  (with "Ver todas"), recoverable error with "Reintentar", sending (primary button disabled + spinner + "Registrando…").
- WCAG 2.1 AA contrast (reference tones measured 4.82–9.95:1); visible focus (3 px navy outline / ring);
  full keyboard operation; `<header>`/`<main>` landmarks; skip link; labeled controls; `aria-invalid` + `aria-describedby`
  for field errors; `prefers-reduced-motion` disables spinner/skeleton animation.
- UI copy in Spanish (assumption A-01); status labels keep the challenge names (Pending, Processing, Completed, Failed).

## 8. Visual Validation (Phase 04 exit)

Lightweight, review-based, not pixel-perfect:
- Review at ~1440, ~768 and ~390 px (browser devtools): hierarchy, typography, spacing, surfaces, form, buttons,
  table/stacked rows, badges, alerts, states, no page-level horizontal overflow.
- Compare side by side with the reference HTML for visual language (not content).
- Keyboard-only pass with visible focus.
- Record deviations in the roadmap Phase 04 evidence.
