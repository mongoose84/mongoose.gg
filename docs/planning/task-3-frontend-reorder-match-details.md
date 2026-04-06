# Task 3: Frontend — Reorder MatchDetails Sections

> **Parent**: [match-details-kpi-redesign.md](match-details-kpi-redesign.md)
> **Type**: Frontend
> **Dependencies**: Task 2 (WinPredictionStats component)
> **File**: `client/src/components/matches/MatchDetails.vue`

---

## Objective

Replace `ImpactStats` with `WinPredictionStats` and promote it above `TeamComparison` in the detail panel.

## Changes

### Template

Replace the current `details-sections` content:

```html
<!-- Current -->
<TeamComparison :match="match" />
<div class="impact-card">
  <ImpactStats :match="match" />
  <MatchActions />
</div>
<MatchNarrative :matchId="match?.matchId" :account-id="accountId" />
<StatSnapshot :match="match" :baseline="baseline" />
```

With:

```html
<!-- New -->
<div class="kpi-card">
  <WinPredictionStats :match="match" :baseline="baseline" />
  <MatchActions :match="match" />
</div>
<TeamComparison :match="match" />
<MatchNarrative :matchId="match?.matchId" :account-id="accountId" />
<StatSnapshot :match="match" :baseline="baseline" />
```

### Script

- Remove `import ImpactStats from './ImpactStats.vue'`
- Add `import WinPredictionStats from './WinPredictionStats.vue'`

### Style

Rename `.impact-card` to `.kpi-card` (same flex layout styles). Or reuse the existing class name if preferred — the visual behavior is identical.

## Acceptance Criteria

- [ ] WinPredictionStats renders above TeamComparison
- [ ] MatchActions still renders alongside the KPI section
- [ ] ImpactStats is no longer imported or rendered
- [ ] All existing match detail states (loading, error, empty, content) still work
