# Task 5: Frontend — Update StatSnapshot (Remove Duplicates, Add New Stats)

> **Parent**: [match-details-kpi-redesign.md](match-details-kpi-redesign.md)
> **Type**: Frontend
> **Dependencies**: None (can be done in parallel with Tasks 2–3)
> **File**: `client/src/components/matches/StatSnapshot.vue`

---

## Objective

Remove stats that are now shown in `WinPredictionStats` and add two new stats to maintain 10 total.

## Remove (2 stats)

- **CS/min** (currently slot 7) — now in WinPredictionStats
- **Vision Score** (currently slot 10) — now in WinPredictionStats

## Add (2 stats)

### Dmg/Gold (slot 3)

- **Formula**: `match.damageDealt / match.goldEarned`
- **Format**: 2 decimal places
- **Sentiment**: positive if ≥ 1.5, negative if < 0.8
- **Comparison text**: "Efficient carry" / "Low output" / "Average output"
- **Support handling**: Suppress trend/comparison for supports (role === 'UTILITY' or 'SUPPORT')

### Damage per Death / Vision per min (slot 10)

**Non-support**:
- **Label**: "Dmg/Death"
- **Formula**: `match.damageDealt / Math.max(1, match.deaths)`
- **Format**: `formatNumber` (e.g., "8,234")
- **Sentiment**: positive if ≥ 8000, negative if < 3000
- **No baseline comparison** — absolute thresholds only

**Support**:
- **Label**: "Vision/min"
- **Formula**: `match.visionScore / (match.gameDurationSec / 60)`
- **Format**: 1 decimal place
- **Sentiment**: positive if ≥ 2.5, negative if < 1.5
- **Comparison text**: "Great vision" / "Low vision" / "Average vision"

## Revised Stat List (10 total)

| # | Stat | Status |
|---|------|--------|
| 1 | KDA Ratio | Unchanged |
| 2 | Kill Participation | Unchanged |
| 3 | **Dmg/Gold** | **New** |
| 4 | Damage Dealt | Unchanged |
| 5 | Damage Share | Unchanged |
| 6 | Damage Taken | Unchanged |
| 7 | CS | Unchanged |
| 8 | Gold | Unchanged |
| 9 | Gold/min | Unchanged |
| 10 | **Dmg/Death** or **Vision/min** | **New** (role-dependent) |

## Acceptance Criteria

- [ ] CS/min and Vision Score no longer appear in StatSnapshot
- [ ] Dmg/Gold appears at position 3 with correct formula and sentiment
- [ ] Dmg/Death appears at position 10 for non-support roles
- [ ] Vision/min appears at position 10 for support roles
- [ ] Stat count header shows "10 metrics"
- [ ] Existing baseline comparison logic unchanged for carried-over stats
