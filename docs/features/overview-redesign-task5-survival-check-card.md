# Task 5: `SurvivalCheckCard.vue` component

> Source: [overview-redesign-frontend.md](overview-redesign-frontend.md) — extracted from Task 5

**Scope**: Build `SurvivalCheckCard.vue` in `client/src/components/overview/`. Self-contained card showing death/win-rate correlation.

**File to create**:
- `client/src/components/overview/SurvivalCheckCard.vue`

**Props**:
- `survivalStats` — the `survivalStats` object from the API (nullable)
- `loading` — boolean for skeleton state

**Visual structure**:
```
SURVIVAL CHECK
Avg 4.2 deaths/game
████████░░  32% before 10 min
≤3 deaths → 72% WR   (7 games)
5+ deaths → 38% WR   (8 games)
```

**Components**:
- Section label: `text-xs uppercase tracking-wide text-text-secondary`
- Avg deaths: `text-xl font-bold text-text`
- Progress bar: `div` with `bg-error` fill, `bg-elevated` track, `border-radius: var(--radius-sm)`
- Win rate rows: color-coded via `useWinRateColor()` — ≤3 deaths in success tones, 5+ in error tones
- Game counts: `text-xs text-text-secondary`
- If fewer than 5 games in a bucket: dim the row, show "limited data" tooltip

**Accessibility**:
- `aria-label="Survival check: death analysis"` on card section
- Progress bar: `role="meter"`, `aria-valuenow`, `aria-valuemin="0"`, `aria-valuemax="100"`, `aria-label="Percentage of deaths before 10 minutes"`
- Win rate values: numeric label always present (not color-only)

**Tests** (`test/unit/SurvivalCheckCard.spec.js`):
- [ ] Renders death stats and win rate rows
- [ ] Progress bar width matches `deathsBefore10Pct`
- [ ] Shows "limited data" tooltip when a bucket has < 5 games
- [ ] Shows loading skeleton when `loading` is true

> **User validation note**: SurvivalCheckCard is analytical for an orientation page. Ship it, then gather user feedback on whether the death-bucket breakdown is useful at this level or should be simplified to a single-sentence insight (e.g. "You win 72% when you die ≤3 times"). If users find it too dense, simplify to headline metric only and move the full breakdown to the Solo page.
