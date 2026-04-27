# Feature: Champion Card — Avg CS Stat Row + Games Played Pill

## Problem Statement
The champion card in Champion Select currently shows four stat rows: Win Rate, KDA, Games, and M-Score. Two of these overlap with M-Score inputs (KDA is 20% of M-Score; Win Rate is 50%). The Games row uses a progress bar, which implies a performance magnitude that doesn't apply — games played is a confidence qualifier, not a performance signal. This wastes a stat slot and misrepresents what the number means.

## Proposed Solution
1. **Replace the KDA stat row** with an **Avg CS stat row** — a metric completely independent of M-Score, universally understood, and meaningfully variable per champion.
2. **Remove the Games stat row** and move games played to a **pill badge** in the top-right corner of the card's image area — treating it as metadata about the champion rather than a ranked performance metric.

The result is three clean stat rows (Win Rate, CS, M-Score), each answering a distinct question with zero overlap, plus a lightweight games pill that contextualises confidence without competing with performance data.

## User Stories
### Primary User Story
As a ranked player in Champion Select, I want to see my average CS on each champion card so that I can gauge my farming consistency on that champion alongside win rate and M-Score.

### Additional User Stories
- As a player, I want to know how many games back a recommendation without it occupying a full stat row, so the card stays focused on performance.

## Requirements

### Functional Requirements
1. `avgCs` (average creep score per game, rounded to 1 decimal) must be added to the `MainChampionEntry` DTO and populated by `MainChampionRecommender`.
2. The champion card must display a CS stat row with a proportional progress bar (0–250 scale) and colour-coded value.
3. The KDA stat row must be removed from the champion card.
4. The Games stat row must be removed from the champion card.
5. A games played pill must appear in the top-right of the card's image area showing `{N}g` (e.g. `34g`).
6. All changes must be backward compatible — no database schema changes, no new API endpoints.

### Non-Functional Requirements
- **Performance**: No additional queries — `AvgCs` is already fetched in existing SQL.
- **Accessibility**: Pill must have readable contrast against the card background (WCAG AA). CS value colour classes must meet AA contrast on `bg-background-surface`.
- **Compatibility**: Desktop-first. No responsive breakpoint changes required.

## Technical Approach

### Backend Changes
**Language**: C#

**Components**:
- [ ] DTO: `server/Mongoose.Api/Core/QueryModels/SoloQueryModels.cs` — add `avgCs` field to `MainChampionEntry`
- [ ] Service: `server/Mongoose.Api/Core/Services/MainChampionRecommender.cs` — pass `Math.Round(s.AvgCs, 1)` into `MainChampionEntry` constructor

> No SQL changes needed. `AVG(p.creep_score) as AvgCs` is already selected in both `ChampionSelectRepository` and `SoloPerformanceRepository`, and `AvgCs` is already a field on `ChampionRoleStats`.

### Frontend Changes
**Framework**: Vue 3 (`<script setup>`)

**Components**:
- [ ] `client/src/components/MainChampionCard.vue` — all template and script changes

### Database Changes
None.

### API Contracts

No endpoint changes. The `MainChampionEntry` shape gains one new field:

```json
{
  "championName": "Ahri",
  "championId": 103,
  "role": "MID",
  "winRate": 58.3,
  "gamesPlayed": 34,
  "mScore": 74.0,
  "avgKda": 3.21,
  "avgCs": 187.4
}
```

`avgKda` remains in the DTO (it is used by other consumers such as the Solo dashboard). It is only removed from the **champion card UI**, not from the API response.

## UI/UX Requirements

All views must follow the existing design system defined in [UI/UX Spec](../ui-ux.spec.md). Use design tokens — never hardcode colors, spacing, or shadows.

### MainChampionCard — Card Image Area

**Layout**: Top-right corner of the existing image area. The rank badge (`#1 Pick` / `#2` / `#3`) currently occupies top-left; the games pill mirrors it on the right.

**Structure**:
```
┌──────────────────────────────────────┐
│  #1 Pick                    34g      │  ← pill top-right
│                                      │
│              [champion art]          │
│                                      │
└──────────────────────────────────────┘
```

**Components**:
- Games pill: `absolute top-2 right-2 py-0.5 px-2 bg-background-elevated rounded text-2xs font-medium text-text-secondary`
- Text content: `{{ champion.gamesPlayed }}g`
- `data-testid`: `games-pill-{championId}`

**Behaviour**:
- Always visible (gamesPlayed is always ≥ 1 for any champion that appears on the card).
- No tooltip needed — the meaning of `34g` is self-evident in context.

---

### MainChampionCard — Stat Rows

**Layout**: Replaces the current 4-row list (Win Rate / KDA / Games / M-Score) with a 3-row list (Win Rate / CS / M-Score).

**Structure**:
```
┌──────────────────────────────────────┐
│  Win Rate   ████████░░   58%         │
│  CS         ██████░░░░   187         │
│  M-Score    ████████░░   74          │
└──────────────────────────────────────┘
```

**CS stat row spec**:
- Label: `CS`
- Bar scale: 0–250 (`width: Math.min((champion.avgCs / 250) * 100, 100)%`)
- Bar colour classes (by threshold):

| avgCs | Bar class | Text class |
|-------|-----------|------------|
| ≥ 200 | `bg-success` | `text-success` |
| ≥ 160 | `bg-[#84cc16]` | `text-[#84cc16]` |
| ≥ 130 | `bg-[#eab308]` | `text-[#eab308]` |
| ≥ 100 | `bg-[#f97316]` | `text-[#f97316]` |
| < 100 | `bg-error` | `text-error` |
| null | `bg-[rgba(255,255,255,0.2)]` | `text-text-secondary` |

- Display value: `Math.round(avgCs)` — no decimals needed at a glance
- Null/missing: display `—` with `text-text-secondary`

> CS thresholds are calibrated for summoners rift non-support roles. Support CS will typically be 0–30, which will always render red — this is acceptable and accurate.

**Removed rows**:
- KDA row: removed entirely from template. KDA helper functions (`formatKda`, `getKdaBarClass`, `getKdaColorClass`) removed from script.
- Games row: removed entirely from template. Games bar logic removed from script.

**M-Score tooltip** (no change required): The existing tooltip text already mentions "sample size" and "KDA" as M-Score inputs. After this change the KDA wording is mildly stale but not incorrect — leave for a future tooltip update.

## Testing Strategy

### Unit Tests (backend)
**Framework**: xUnit

No changes to `MainChampionRecommenderTests.cs` are required — existing tests use the `CreateStats` helper which already passes `avgCs` into `ChampionRoleStats`. The tests will continue to pass once `avgCs` is forwarded into `MainChampionEntry`.

One additional assertion is worth adding to `MScore_is_scaled_to_0_100_range` or a new test:
- [ ] `MainChampionEntry.AvgCs` is populated and equals `Math.Round(s.AvgCs, 1)` for a known input.

### Integration Tests (backend)
**File**: `server/Mongoose.Api.Tests/ChampionSelectEndpointTests.cs`

- [ ] Existing happy-path test should deserialise `avgCs` from the response without error (field is additive).
- No new endpoint tests are required (no route or auth changes).

### Unit Tests (frontend)
**Framework**: Vitest + Vue Test Utils  
No existing `MainChampionCard` spec file. A minimal spec should be created at:  
`client/test/unit/components/MainChampionCard.spec.js`

- [ ] Renders games pill with correct `{N}g` text
- [ ] Games pill has correct `data-testid`
- [ ] CS stat row is present
- [ ] KDA stat row is absent
- [ ] Games stat row is absent
- [ ] CS bar width reflects `avgCs / 250 * 100` capped at 100
- [ ] CS displays `—` when `avgCs` is null

### Manual Testing Scenarios
1. Open Champion Select page with a synced account — verify cards show CS row and games pill, no KDA row.
2. Switch queue filter (ranked solo → all) — verify CS values update with new data.
3. Inspect #1 Pick card and #2/#3 cards — pill visible on all three, styled consistently.
4. Test with an account that has support champions — verify red CS bar renders correctly.

## Validation Criteria
Feature is considered complete when:
- [ ] `avgCs` appears in the API response JSON for `/api/v2/champion-select/{userId}`
- [ ] Champion card shows CS stat row with bar and colour-coded value
- [ ] Champion card shows games played pill (top-right of image area)
- [ ] KDA stat row is gone
- [ ] Games stat row is gone
- [ ] All backend tests pass
- [ ] Frontend Vitest tests pass
- [ ] No TypeScript/ESLint errors

## Dependencies
### Internal Dependencies
- `MainChampionRecommender.ChampionRoleStats` — already has `AvgCs` field
- Existing SQL queries in `ChampionSelectRepository` and `SoloPerformanceRepository` — already select `AVG(p.creep_score)`

### External Dependencies
None.

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Support/ARAM roles always render red CS | Low | High | Acceptable — red CS on a support is accurate. Document in spec. |
| `avgCs` null for old matches without `creep_score` data | Low | Low | Null handling already established — show `—`, faint bar |
| `avgKda` removal from card surprises users accustomed to seeing it | Low | Medium | M-Score tooltip still references KDA as an input |

## References
- [Architecture spec — Champion Select DTOs](../architecture.spec.md#champion-select-dtos)
- [UI/UX spec](../ui-ux.spec.md)
- `server/Mongoose.Api/Core/QueryModels/SoloQueryModels.cs`
- `server/Mongoose.Api/Core/Services/MainChampionRecommender.cs`
- `client/src/components/MainChampionCard.vue`
