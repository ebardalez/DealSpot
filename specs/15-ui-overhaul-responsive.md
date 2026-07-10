# 15 — UI Overhaul & Responsive Design

**Phase:** 4 · **Status:** ✅ Done · **Depends on:** — (purely CSS/markup, no backend)

## Problem

The app ships with **zero CSS** — no stylesheet exists in `wwwroot/`. Every page renders with browser defaults: images blow up to native resolution, no grid, no spacing, forms stretch uncontrolled, and nothing is responsive. The site is unusable on mobile and unprofessional on desktop.

## Goal

Add a single `site.css` stylesheet that makes all **user-facing pages** visually polished and mobile-first responsive. Give the deal feed and cards a modern "deals aggregator" look inspired by Slickdeals/Honey/Kinja Deals — punchy discount badges, constrained product images, clear pricing hierarchy, and a scannable card grid.

## Non-Goals

- **Admin pages** (`/admin/*`) — leave unstyled or minimal; admin polish is a separate spec.
- **Component library / CSS framework** — no Tailwind, Bootstrap, MudBlazor. One hand-written CSS file using CSS custom properties already wired via `--color-primary` / `--color-secondary` from tenant branding.
- **Dark mode** — defer. Design with light backgrounds; ensure CSS vars make dark mode easy later.
- **Animations / JS transitions** — defer. CSS-only hover states are fine.

## Scope — User-facing pages

| Page | Key UI concerns |
|------|----------------|
| **MainLayout** (header + footer) | Sticky header, responsive nav (hamburger on mobile), logo sizing, auth buttons |
| **Home** (`/`) | Hero heading, deal card grid (responsive columns), skeleton loaders |
| **DealCard** (shared) | Fixed-aspect image container, text truncation, pricing line, discount pill, score badge, vote display |
| **DealDetail** (`/deals/{slug}`) | Two-column layout (image left, info right → stacks on mobile), breadcrumb, price history chart, CTA button |
| **CategoryList** (`/categories`) | Category card grid |
| **CategoryDetail** (`/categories/{slug}`) | Breadcrumb, watch button, deal grid (reuse Home grid) |
| **Watchlist** (`/watchlist`) | Form row, responsive table (cards on mobile) |
| **SubmitDeal** (`/submit`) | Centered form, max-width container, field spacing, rep badge |
| **NotificationSettings** (`/settings/notifications`) | Toggle switches, section grouping, save button |

## Design Direction

### Visual identity
- **Palette:** use tenant CSS vars (`--color-primary`, `--color-secondary`) for accents. Neutral background `#f5f5f7`, white card surfaces, dark text `#1a1a2e`.
- **Typography:** system font stack (`-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`). Price = bold/large. Titles = `clamp()` fluid sizing.
- **Spacing:** 8px base unit. Card padding 16px. Grid gap 16–24px.
- **Radius:** cards `12px`, buttons `8px`, pills `9999px`.
- **Shadows:** cards get subtle `0 1px 3px rgba(0,0,0,.08)` with stronger on hover.

### Deal card (the hero component)
- **Image container:** fixed `aspect-ratio: 4/3`, `object-fit: contain`, white background, max-height capped so images never dominate.
- **Title:** 2-line clamp (`-webkit-line-clamp`).
- **Pricing row:** current price large + bold + `--color-primary`, original price struck through + muted, discount pill in accent red/orange.
- **Footer:** vote score, retailer, score badge — all inline, small type.
- **Grid:** `auto-fill, minmax(280px, 1fr)` — 4 cols on wide, 2 on tablet, 1 on phone.

### Deal detail
- **Desktop:** CSS grid `1fr 1fr` (image | info). Image constrained to `max-height: 400px`, `object-fit: contain`.
- **Mobile (<768px):** single column, image on top.
- **CTA button:** full-width on mobile, accent gradient, large touch target (48px min height).
- **Price history chart:** keep existing inline-style bar chart but add axis labels and cap container height.

### Forms (Watchlist, SubmitDeal, NotificationSettings)
- Max-width `640px`, centered via `margin: 0 auto`.
- Input fields: full-width, `padding: 12px 16px`, border `1px solid #ddd`, focus ring with `--color-primary`.
- `.form-row` → flexbox row on desktop, stack on mobile.
- Buttons: padding `12px 24px`, bold, primary filled, secondary outlined.
- Toggle switches for notification settings (CSS-only checkbox styling).

### Header
- Flexbox nav. Logo left, links center, auth right.
- Mobile (<768px): hamburger toggle (CSS-only `<details>` or checkbox hack — no JS).
- Sticky `position: sticky; top: 0` with backdrop blur.

### Footer
- Simple centered text, muted color, `padding: 2rem`.

## Breakpoints

| Token | Width | Behavior |
|-------|-------|----------|
| `sm`  | ≤640px | 1-col cards, stacked forms, hamburger nav |
| `md`  | 641–1024px | 2-col cards, side-by-side form rows |
| `lg`  | ≥1025px | 3–4 col cards, 2-col deal detail, full nav |

## Changes

### Web — new file
- `wwwroot/css/site.css` — single stylesheet, mobile-first (`min-width` media queries). Organized by section: reset, typography, layout, header, footer, deal-card, deal-detail, categories, forms, alerts, utilities, skeleton loaders, responsive overrides.

### Web — markup tweaks (minimal)
- `App.razor` — add `<link rel="stylesheet" href="css/site.css" />` in `<head>`.
- `MainLayout.razor` — wrap nav links + auth in a `<details class="nav-mobile">` for hamburger on mobile (or add a checkbox-based toggle). Add a `<div class="container">` wrapper inside `<main>`.
- `DealCard.razor` — no structural changes expected; CSS targets existing class names (`.deal-card`, `.deal-card__image`, `.deal-card__body`, etc.).
- `Watchlist.razor` — wrap table in `<div class="table-responsive">` for horizontal scroll on mobile; or convert to card layout below `sm`.
- `DealDetail.razor` — wrap image + info in `<div class="deal-detail-grid">` (already exists, just needs CSS).
- `Home.razor` — `.deals-grid` already exists, just needs grid CSS.

### Web — nothing else
No new components, no new NuGet packages, no backend changes.

## Acceptance

- [ ] All user-facing pages render cleanly at 375px (iPhone SE), 768px (iPad), and 1440px (desktop).
- [ ] Deal card images are constrained: no image exceeds its card boundary or dominates the feed.
- [ ] Discount percentage badges are visually prominent (pill, contrasting color).
- [ ] Current price is the most prominent text on each card.
- [ ] Forms (submit deal, watchlist, notification settings) are usable on mobile — fields don't overflow, touch targets ≥44px.
- [ ] Header navigation collapses to a hamburger/toggle on mobile.
- [ ] No horizontal scroll on any page at any breakpoint.
- [ ] Skeleton loaders on Home and CategoryDetail still work and are styled.
- [ ] Existing interactive components (VoteButton, ExpireReportButton, WatchCategoryButton) remain functional.
- [ ] `--color-primary` and `--color-secondary` CSS vars from tenant branding propagate throughout the design.
- [ ] No JS added (CSS-only responsive behavior).
- [ ] No admin pages touched.

## Test

Visual-only — no automated tests. Verify manually at each breakpoint. Browser DevTools device toolbar is sufficient.
