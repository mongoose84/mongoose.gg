# Feature: Overview — Your champions

> **Purpose**: Phase 3, item 1 of the [design migration plan](../design-migration.plan.md). Adds the "Your champions" section to the Overview: the player's top three champions as ChampionCards plus an "Also played" list. Follows [Architecture Spec](../architecture.spec.md), [Database Schema Spec](../database-schema.spec.md) and the design system (`.claude/skills/mongoose-design/reference/`).
>
> **Triage**: ready-for-agent · decisions from the 2026-09-27 session

## Problem Statement

The design system's Overview order is hero → score rings → **your champions** → readiness + today's matches → insights. Phase 2 left "Your champions" out because `OverviewResponse` has no per-champion stats. Players see their most-played champion only as hero art, with no view of how each champion in their pool actually performs.

## Solution

The Overview endpoint returns a **champion pool**: the player's ranked champions this season, ordered by M-Score. The top three are shown as ChampionCards (centred art, win rate, matches, KDA, one strength tag); the next three go in a compact "Also played" list.

Cards are **static**. Picking a champion does nothing on the Overview; champion-focused analysis belongs to Champion Select. That means no `<button>`, no `aria-pressed`, no selected border and no `spotlight` hover (the system reserves the glow for clickable cards).

## Domain rules (Core)

Implemented in `Core/Services/ChampionPoolBuilder.cs`, reusing the M-Score from `MainChampionRecommender`.

### Window
- Current season (latest `season_code` with matches, same rule as `mostPlayedChampion`), Ranked Solo/Duo (420) and Ranked Flex (440) only, matches of at least `MinValidGameDurationSec`.
- Aggregated across all selected accounts (one account, or every linked account in Overall mode).

### Ordering
- One entry per champion, aggregated across roles. The champion's **primary role** (most matches, then most recent) decides the laning weights of the M-Score.
- Ranked by M-Score (`MainChampionRecommender.ComputeMScore`: win rate, laning, KDA, scaled by a sample-size confidence factor), then matches played, then champion name.
- Top 3 → `champions`, next 3 → `alsoPlayed`. Nothing beyond six is returned.

### Strength tag
Each card champion gets at most one tag, and no two cards share one.

| Metric | Source | Tag |
|---|---|---|
| Laning | avg `gold_diff_vs_lane` at minute 15 | Best laning |
| Damage | avg `damage_share_pct` | Most damage |
| KDA | (kills + assists) / max(1, deaths) | Best KDA |
| Farming | avg CS per minute | Best farming |
| Vision | avg `vision_per_min` | Best vision |
| Involvement | avg `kill_participation_pct` | Most involved |

1. **Baseline** = the player's own average for that metric over every ranked match in the window (all champions, weighted by the matches that have the metric).
2. **Lead** of a champion on a metric = `(champion − baseline) / baseline` for ratio metrics, and `(champion − baseline) / 1000` gold for laning (gold diff can be zero or negative).
3. A champion is eligible for a tag only with **at least 5 matches**; a metric counts only when the lead is **at least 0.10** (10% above your average, or +100 gold at 15 for laning) and the metric has data.
4. Assign greedily: sort every eligible (champion, metric) pair by lead, highest first; give the pair's tag when the champion has no tag yet and no other card holds that tag.
5. A champion with no qualifying metric gets no tag (`strengthTag: null`).

"Also played" entries never get a tag.

## API Contract

`GET /api/v2/overview/{userId}?accountId=` gains one field:

```json
"championPool": {
  "champions": [
    { "championId": 103, "championName": "Ahri", "role": "MIDDLE", "matches": 22, "wins": 14, "winRate": 63.6, "avgKda": 4.1, "mScore": 71.2, "strengthTag": "Best laning" }
  ],
  "alsoPlayed": [
    { "championId": 61, "championName": "Orianna", "role": "MIDDLE", "matches": 11, "wins": 5, "winRate": 45.5, "avgKda": 2.9, "mScore": 38.0, "strengthTag": null }
  ]
}
```

- `championPool` is always present; both arrays are empty when the player has no ranked matches this season.
- `winRate` is a percentage with one decimal; `avgKda` two decimals; `mScore` 0–100 with one decimal.

## Backend Changes

- [ ] `Core/QueryModels/OverviewQueryModels.cs`: `ChampionPoolStatsData` (per-champion aggregate incl. sample counts) and `ChampionRoleCountData`.
- [ ] `Core/Services/ChampionPoolBuilder.cs`: ordering + strength tags (pure, unit-tested).
- [ ] `Core/Services/MainChampionRecommender.cs`: expose the M-Score as `ComputeMScore` (no behaviour change).
- [ ] `IOverviewStatsRepository` / `OverviewStatsRepository`: `GetChampionPoolStatsAsync(puuids)`.
- [ ] `Application/DTOs/Overview/OverviewDto.cs`: `ChampionPool`, `PoolChampion`; `OverviewEndpoint` fetches in parallel with the other stats.

No schema change; the query uses `participants`, `matches`, `participant_checkpoints` (minute 15) and `participant_metrics`.

## Frontend Changes

- [ ] Port `mp-champ-card`, `mp-bar` to `client/src/style.css` (static variant: `cursor: default`, no spotlight).
- [ ] `components/base/ChampionCard.vue`: `<article>`, centred art (`getChampionCenteredUrl`), scrim, name, win rate, "N matches · KDA x.x", strength tag, win-rate bar (`role="meter"`).
- [ ] `components/overview/OverviewChampionPool.vue`: section "Your champions", 3 cards + 300px "Also played" card (44px square icons, name, matches, win rate; win rate under 50% in `warn-text`).
- [ ] `OverviewPage.vue`: section right after the hero (score rings will go between them later), skeleton in the loading frame, EmptyState "No ranked matches this season" → "Play ranked and your best champions show up here."
- [ ] Below 900px: one column, cards 220px tall, "Also played" under the cards.

## Testing

- **xUnit** `ChampionPoolBuilderTests`: ordering by M-Score, primary role, top 3 / next 3 split, tag uniqueness, 5-match floor, 10% lead threshold, null metrics, empty input.
- **xUnit** `OverviewEndpointTests`: `championPool` shape from the fake repository; empty arrays with no data.
- **Vitest**: `ChampionCard.spec.js`, `OverviewChampionPool.spec.js`, Overview view test for placement / empty state.
- **E2E** `overview-dashboard.spec.js`: section visible (content or empty state).

## Out of scope
- Picking a champion (focus switch). Cards stay static until a page needs a champion focus.
- Hero champion: the hero keeps `mostPlayedChampion` (all queues), so card #1 and the hero can differ.
- Design-system update for the static ChampionCard variant (tracked with the other Step 5 items in the migration plan).
