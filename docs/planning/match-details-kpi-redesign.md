# Match Details — Win Prediction KPI Redesign

> **Created**: April 6, 2026
> **Status**: Ready for implementation
> **Research**: [win-prediction-metrics-research.md](../win-prediction-metrics-research.md)

---

## Summary

Replace `ImpactStats` with a research-backed **Key Performance Indicators** section in the Match Details panel. Reorder the detail panel to lead with win prediction metrics. Enable the "View Analysis" button to navigate to Solo Stats. Update the backend DTO to include dragon participation data.

## Section Order Change

**Current**:
```
MatchHeader → TeamComparison → ImpactStats + MatchActions → MatchNarrative → StatSnapshot
```

**New**:
```
MatchHeader → WinPredictionStats + MatchActions → TeamComparison → MatchNarrative → StatSnapshot
```

## New Component: WinPredictionStats

**Layout**: 2 rows × 3 columns stacked stat grid (6 tiles)

```
┌──────────────────────────────────────────────────────────────┐
│  Key Performance Indicators                                  │
│  Metrics that most predict winning                           │
├──────────────────┬──────────────────┬────────────────────────┤
│  Deaths          │  Gold @15        │  Dragon Participation  │
│  3               │  +800            │  2/3 (67%)             │
│  ↓ -1 vs avg    │  Won lane        │  High involvement      │
│  [green]         │  [green]         │  [green]               │
├──────────────────┼──────────────────┼────────────────────────┤
│  CS/min          │  Vision Score    │  Deaths Before 10m     │
│  7.2             │  24              │  0                     │
│  +0.8 vs avg    │  -3 vs avg      │  Safe early game       │
│  [green]         │  [red]           │  [green]               │
└──────────────────┴──────────────────┴────────────────────────┘
```

### Row 1 — Tier 1 (highest impact + easiest to improve)

| Slot | Metric | Value Format | Positive | Negative | Comparison |
|------|--------|-------------|----------|----------|------------|
| 1 | Deaths | Integer | < baseline − 1 | > baseline + 1 | `±N vs avg` |
| 2 | Gold @15 | `+/-N` locale | ≥ +500 | ≤ −500 | "Won lane" / "Lost lane" / "Even lane". "N/A" if null |
| 3 | Dragon Part. | `X/Y (Z%)` | ≥ 67% | 0% when teamDragons > 0 | "High" / "Low" / "No dragons" (neutral if 0 team dragons) |

### Row 2 — Tier 2 (high impact + moderate difficulty)

| Slot | Metric | Value Format | Positive | Negative | Comparison |
|------|--------|-------------|----------|----------|------------|
| 4 | CS/min | 1 decimal | > baseline + 0.5 | < baseline − 0.5 | `±N vs avg` |
| 5 | Vision Score | Integer | > baseline (duration-adjusted, 15% threshold) | < baseline − 15% | `±N vs avg` (duration-adjusted) |
| 6 | Deaths Pre-10 | Integer | 0 | ≥ 2 | "Safe early" / "Risky early" / neutral for 1 |

### Tile Styling

Uses existing design system tokens and `ImpactStats` visual language:

- **Container**: `background: var(--color-surface)`, `border: 1px solid var(--color-border)`, `border-radius: var(--radius-sm)`, `padding: var(--spacing-sm)`
- **Sentiment tint**: `.positive` → green border + 5% green bg. `.negative` → red border + 5% red bg
- **Label**: `font-size: var(--font-size-xs)`, `color: var(--color-text-secondary)`
- **Value**: `font-size: var(--font-size-lg)`, `font-weight: var(--font-weight-semibold)`, `color: var(--color-text)`
- **Comparison**: `font-size: var(--font-size-xs)`, sentiment-colored
- **Arrow indicator**: `↑` / `↓` next to label (colorblind safe — not color-only)

### Section Header

- Title: `h3`, "Key Performance Indicators", `font-size: var(--font-size-sm)`, `font-weight: var(--font-weight-semibold)`
- Subtitle: "Metrics that most predict winning", `font-size: var(--font-size-xs)`, `color: var(--color-text-secondary)`

### Empty Baseline State

When `baseline` is null, show all 6 tiles with raw values but omit comparison lines and sentiment coloring. Dragon participation and Gold@15 don't depend on baseline — they always show sentiment.

## StatSnapshot Changes

**Remove** (now in WinPredictionStats): CS/min, Vision Score

**Add**:
- **Dmg/Gold**: `damageDealt / goldEarned`, 2 decimals. Positive ≥ 1.5, negative < 0.8. Suppress for supports.
- **Damage per Death** (non-support) / **Vision/min** (support): `damageDealt / max(1, deaths)` formatted with `formatNumber`. Positive ≥ 8000, negative < 3000. Supports show `visionScore / (gameDurationSec / 60)` instead.

**Revised 10-stat list**:

| # | Stat | Notes |
|---|------|-------|
| 1 | KDA Ratio | Unchanged |
| 2 | Kill Participation | Unchanged |
| 3 | Dmg/Gold | New — non-support only. Supports see Vision/min |
| 4 | Damage Dealt | Unchanged |
| 5 | Damage Share | Unchanged |
| 6 | Damage Taken | Unchanged |
| 7 | CS | Unchanged |
| 8 | Gold | Unchanged |
| 9 | Gold/min | Unchanged |
| 10 | Damage per Death | New — non-support only. Supports see Vision/min |

## View Analysis Button

Enable the primary "View Analysis" button in `MatchActions`:
- Navigate to `/app/solo` via `router.push({ name: 'app-solo' })`
- Remove `disabled` attribute and `title="Coming soon"`
- "View Goal Impact" stays disabled

## Implementation Order

1. Task 1 — Backend DTO (no frontend dependency)
2. Task 2 — New WinPredictionStats component (depends on Task 1)
3. Task 3 — MatchDetails reorder (depends on Task 2)
4. Task 4 — Enable View Analysis button (independent)
5. Task 5 — StatSnapshot update (independent)
6. Task 6 — Tests (after all implementation)
