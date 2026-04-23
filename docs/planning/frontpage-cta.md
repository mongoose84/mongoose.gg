# Feature: Main Domain CTA Page (frontpage)

## Problem Statement

`mongoose.gg` (the apex domain) currently has no web presence. Users who visit the root domain see nothing. The beta app lives at `beta.mongoose.gg`, but there is no bridge between the two — a visitor who types `mongoose.gg` directly has no way to discover or join the beta.

## Proposed Solution

A standalone, static `index.html` file hosted at `mongoose.gg` — no build pipeline, no framework, no dependencies. A single full-viewport page with the Mongoose.gg brand, a short compelling statement, and one primary CTA button that sends the visitor to `https://beta.mongoose.gg`.

The design follows the exact same token values used in the Vue app (colours, typography, spacing), implemented inline so the file is fully self-contained.

---

## User Stories

### Primary User Story
As a visitor who types `mongoose.gg` into their browser, I want to immediately understand what Mongoose.gg is and be given a clear path to join the beta, so that I don't land on a blank page and bounce.

### Additional User Stories
- As a product owner, I want the main domain to feel polished and on-brand so that first impressions match the quality of the beta app
- As a developer, I want the file to be self-contained and zero-dependency so that it can be deployed to any static host without a build step

---

## Requirements

### Functional Requirements
1. Full-viewport hero section centred horizontally and vertically
2. Mongoose.gg logo (`/mongoose.png`) displayed above the headline — same asset used in the Vue app
3. Headline: **"The Solo Queue Improvement Tracker"**
4. Sub-headline: **"Built to Help You Climb"** (styled with the primary gradient, matching the Vue app hero)
5. A short supporting paragraph describing what the beta delivers today (champ select tips, post-game breakdowns, match history)
6. A single primary CTA button: **"Join the Beta →"** linking to `https://beta.mongoose.gg`
7. A subtle footer line: Mongoose.gg Beta · Not affiliated with Riot Games
8. Background: matches the app — pure black (`#000000`) with the `/hero-bg.svg` fixed cover image and a dark overlay (`rgba(0,0,0,0.30)`)
9. No nav bar, no sidebar, no multi-section scroll — one screen, one action

### Non-Functional Requirements
- **Zero dependencies** — pure HTML + inline CSS, no external JS, no CDN fonts beyond Google Fonts (already used by the app)
- **Self-contained** — the file must work when opened directly from disk (no server required for local preview)
- **Performance** — page weight under 15 KB excluding the logo image
- **Accessibility** — WCAG AA contrast on all text, `lang="en"` on `<html>`, descriptive `alt` on the logo, button is a real `<a>` with `role="button"` and visible focus ring
- **Responsive** — readable and usable on viewport widths from 320 px to 2560 px; font sizes use `clamp()`
- **No hardcoded fake data** — no user counts, no ratings; nothing that becomes stale

---

## Technical Approach

### Backend Changes
None.

### Frontend Changes

**File placement**: `/frontpage/index.html` in the repository root.

> This folder is deployed separately to the apex domain (`mongoose.gg`). The `client/` folder continues to be deployed to `beta.mongoose.gg`. The `/frontpage/` folder has its own deployment target — no Vite, no npm.

**Assets needed at the deployment root of `mongoose.gg`**:
- `/mongoose.png` — copy from `client/public/mongoose.png`
- `/hero-bg.svg` — copy from `client/public/assets/hero-bg.svg` (if it exists), or omit the background image and fall back to pure black

**Design tokens used (inline, not imported)**:

| Token | Value |
|-------|-------|
| Background | `#000000` |
| Background overlay | `rgba(0,0,0,0.30)` |
| Surface | `rgba(255,255,255,0.03)` |
| Border | `rgba(109,40,217,0.15)` |
| Text primary | `#ffffff` |
| Text secondary | `#888888` |
| Primary | `#6d28d9` |
| Primary dark | `#5b21b6` |
| Primary gradient | `linear-gradient(135deg, #6d28d9, #00a8ff)` |
| Font | `'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif` |

---

## UI/UX Design

### Layout

```
┌──────────────────────────────────────────────────────────┐
│  (full viewport, black + hero-bg fixed cover)            │
│                                                          │
│                  [Mongoose logo 210×100]                 │
│                   Mongoose.gg  [Beta]                    │
│                                                          │
│        The Solo Queue Improvement Tracker                │
│           Built to Help You Climb  ◀ gradient            │
│                                                          │
│   Better champ select picks, post-game breakdowns,       │
│   and a full match history — all in one place.           │
│                                                          │
│              [ Join the Beta → ]                         │
│                                                          │
│  ─────────────────────────────────────────────────────   │
│  Mongoose.gg Beta · Not affiliated with Riot Games       │
└──────────────────────────────────────────────────────────┘
```

### Button

- Background: `#6d28d9` (primary)
- Text: white, `font-weight: 600`, `font-size: clamp(1rem, 2vw, 1.125rem)`
- Padding: `1rem 2rem`
- Border-radius: `0.5rem`
- Hover: slight lift (`transform: translateY(-2px)`) + deeper shadow
- Focus: `outline: 2px solid #6d28d9`, `outline-offset: 4px`
- Arrow SVG inline (same chevron-right used in the Vue app CTA)

### Typography

| Element | Size | Weight |
|---------|------|--------|
| Mongoose.gg wordmark | `1.25rem` | 700 |
| H1 main line | `clamp(2.5rem, 5vw, 4rem)` | 700 |
| H1 gradient line | same | 700 |
| Supporting paragraph | `clamp(1rem, 2vw, 1.125rem)` | 400 |
| Footer | `0.75rem` | 400 |

### Gradient Text (H1 second line)

```css
background: linear-gradient(135deg, #6d28d9, #00a8ff);
-webkit-background-clip: text;
background-clip: text;
-webkit-text-fill-color: transparent;
```

---

## Testing Strategy

Manual only — no unit tests for a static HTML file.

### Manual Checklist
- [ ] Page loads correctly when `index.html` is opened directly from disk
- [ ] "Join the Beta →" button navigates to `https://beta.mongoose.gg`
- [ ] Logo image renders (path correct relative to deployment root)
- [ ] Hero background renders (or falls back to black gracefully if `hero-bg.svg` is absent)
- [ ] Gradient text renders on Chrome, Firefox, Safari
- [ ] Button hover state visible; focus ring visible on keyboard navigation
- [ ] Readable on mobile (320 px width), tablet (768 px), desktop (1440 px)
- [ ] No console errors

---

## Validation Criteria
- [ ] File exists at `/frontpage/index.html` in the repository
- [ ] Zero external JS or CSS dependencies (Google Fonts `<link>` is acceptable)
- [ ] CTA button href is `https://beta.mongoose.gg`
- [ ] No hardcoded fake data (no user counts, no star ratings)
- [ ] Design tokens match the Vue app exactly (colours, font, letter-spacing)
- [ ] WCAG AA contrast met on all text elements

---

## Open Questions

1. **`hero-bg.svg` availability** — Does `hero-bg.svg` need to be served from the apex domain, or should the frontpage use a CSS-only background (radial gradient or plain black)? If the SVG needs separate hosting, that affects the deployment setup.
2. **Redirect vs separate page** — Should `mongoose.gg` be a full redirect (`301`) to `beta.mongoose.gg`, or does the product owner want a distinct "main domain" presence that can eventually host a proper marketing site? This spec assumes the latter.
3. **`/mongoose.png` path** — Confirm that the logo asset will be copied/deployed alongside `index.html` at the apex domain root, so `src="/mongoose.png"` resolves correctly.

---

## Dependencies

### Internal
- `client/public/mongoose.png` — logo asset to copy into `/frontpage/`
- `client/public/assets/hero-bg.svg` — background asset (optional, page degrades gracefully without it)

### External
- Google Fonts CDN (`fonts.googleapis.com`) for Inter — same dependency the Vue app already has

---

## Risks and Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| DNS / hosting for apex domain not configured | High | Medium | Deployment target for `/frontpage/` needs to be agreed before work starts |
| `hero-bg.svg` not available at apex domain | Low | Medium | CSS fallback to plain black — page still looks intentional |
| Google Fonts blocked (corporate networks, etc.) | Low | Low | System font stack fallback already in the font-family declaration |
