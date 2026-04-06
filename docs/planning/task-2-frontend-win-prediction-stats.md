# Task 2: Frontend — Create WinPredictionStats Component

> **Parent**: [match-details-kpi-redesign.md](match-details-kpi-redesign.md)
> **Type**: Frontend
> **Dependencies**: Task 1 (dragonsParticipated field)
> **New file**: `client/src/components/matches/WinPredictionStats.vue`

---

## Objective

Create a new component that displays 6 win-prediction metrics in a 2×3 grid, replacing `ImpactStats`.

## Props

```js
defineProps({
  match: { type: Object, required: true },   // MatchDetailsItem
  baseline: { type: Object, default: null }   // RoleBaseline
})
```

## Template Structure

```
section.win-prediction-stats
  header
    h3 "Key Performance Indicators"
    span.subtitle "Metrics that most predict winning"
  div.kpi-grid (2 rows × 3 cols)
    KPI tile × 6
```

## KPI Tiles — Row 1 (Tier 1)

### Deaths
- **Value**: `match.deaths` (integer)
- **Baseline comparison**: `match.deaths - baseline.avgDeaths` → `±N vs avg`
- **Sentiment**: positive if `deaths < avgDeaths - 1`, negative if `deaths > avgDeaths + 1`
- **Note**: For deaths, lower = positive, so sentiment is inverted vs typical "up = good"

### Gold @15
- **Value**: `match.goldDiffAt15` formatted as `+/-N` with locale separators
- **Null handling**: Show "N/A" if `goldDiffAt15 === null` (game < 15m or no lane opponent)
- **Sentiment**: positive if ≥ +500, negative if ≤ −500, neutral otherwise
- **Comparison text**: "Won lane" / "Lost lane" / "Even lane" / "Game ended early" (null + short game)
- **No baseline dependency** — uses absolute thresholds

### Dragon Participation
- **Value**: `match.dragonsParticipated / match.teamDragons` displayed as `X/Y (Z%)`
- **Zero dragons**: Show "No dragons" with neutral sentiment when `teamDragons === 0`
- **Sentiment**: positive if participation ≥ 67%, negative if 0% and `teamDragons > 0`
- **Comparison text**: "High involvement" / "Low involvement" / "No dragons"
- **No baseline dependency** — uses absolute thresholds

## KPI Tiles — Row 2 (Tier 2)

### CS/min
- **Value**: `match.csPerMin` (1 decimal)
- **Baseline comparison**: `match.csPerMin - baseline.avgCsPerMin` → `±N vs avg`
- **Sentiment**: positive if `> avgCsPerMin + 0.5`, negative if `< avgCsPerMin - 0.5`
- **Suppress for supports**: show value but no sentiment/comparison

### Vision Score
- **Value**: `match.visionScore` (integer)
- **Duration-adjusted comparison**: `expectedVision = avgVisionScore × (gameDurationSec / avgGameDurationSec)`, diff = `visionScore - expectedVision` → `±N vs avg`
- **Sentiment**: positive if diff > 0 (15% threshold), negative if diff < −15%
- **Always shown** (relevant for all roles)

### Deaths Before 10m
- **Value**: `match.deathsPre10` (integer)
- **Sentiment**: positive if 0, negative if ≥ 2, neutral if 1
- **Comparison text**: "Safe early game" / "Risky early game" / (none for 1)
- **No baseline dependency** — uses absolute thresholds

## Styling

Use existing design tokens. CSS grid for the 2×3 layout:

```css
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: var(--spacing-xs);
}
```

Each tile reuses `ImpactStats` visual patterns:
- `.kpi-tile` — `background: var(--color-surface)`, `border: 1px solid var(--color-border)`, `border-radius: var(--radius-sm)`, `padding: var(--spacing-sm)`
- `.kpi-tile.positive` — `border-color: rgba(34, 197, 94, 0.3)`, `background: rgba(34, 197, 94, 0.05)`
- `.kpi-tile.negative` — `border-color: rgba(239, 68, 68, 0.3)`, `background: rgba(239, 68, 68, 0.05)`
- Arrow indicators (`↑`/`↓`) beside label text for colorblind safety

## Accessibility

- `data-testid="win-prediction-stats"` on root
- `data-testid="kpi-tile-deaths"`, `kpi-tile-gold15`, etc. on each tile
- Semantic `h3` for section title
- Sentiment communicated via color + arrow + text (triple redundancy)

## Acceptance Criteria

- [ ] 6 tiles render in 2×3 grid
- [ ] Sentiment classes apply correctly based on thresholds
- [ ] Dragon participation handles 0 teamDragons gracefully
- [ ] Gold@15 handles null gracefully
- [ ] No comparison lines when baseline is null (except Gold@15 and Dragon Part.)
- [ ] Supports role suppresses CS/min sentiment
