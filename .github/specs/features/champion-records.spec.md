# Feature: Champion records in Champion Select

Status: approved 2026-10-26 · Replaces solo-trends.spec.md FR 24–25 (LP per champion)

## Problem Statement

Two pages judge champions and disagree. Champion Select recommends a pick from the M-Score (win rate, laning and KDA, per role, default "This season"). The Solo page's "LP per champion" card ranks by wins minus losses over every role, default "Last 20". With Flex selected, Champion Select says "play Tristana" while Solo says "Tristana cost you the most matches" (5–8 in the last 20, 16–18 over the season with 0–2 of it in mid). Both are true, but the player gets two competing verdicts on two pages, and neither says that the gap is often a coin flip.

## Proposed Solution

Every champion verdict lives on Champion Select. The Solo page keeps form and fixes (stat trends, focus, deaths, sessions) and loses its champion card.

1. Champion Select gets a **Your record** section: for the role shown, every champion with 3 or more matches, as wins minus losses, with recent form next to it.
2. The hero pick shows its recent form, and says so when it is clearly worse than the range.
3. The record's title only names a champion when the margin is clear, so a −2 swing never reads as "cost you the most".
4. The Solo page drops "LP per champion" and points to Champion Select instead.

## User Stories

- As a player picking a champion, I see in one place which pick is best by the numbers, how each champion actually did, and whether my recent games tell a different story.
- As a player on the Solo page, I see no champion verdict that contradicts Champion Select.

## Functional Requirements

**Records (in the `champion-select` response)**

1. **Rows.** Each role group carries `records`: every champion played in that role with at least 3 matches in the selected queue and time range. Each row has `championId`, `championName`, `matches`, `wins` and `net = wins − losses`. Sorted by `net` (largest first), then by matches.
2. **Left out.** Each role group carries `recordsLeftOut`: the names of champions in that role with 1–2 matches, most matches first.
3. **Recent form.** Every record row and every pick (`champions`) carries `recent: { matches, wins }`, counted over the player's last 20 matches in the selected queue (ignoring the time range), for that champion in that role. `null` when none of those 20 were on that champion in that role.
   - Recent form is **cold** when `recent.matches ≥ 5` and its win rate is at least 10 points below the row's win rate over the range; **hot** when at least 10 points above. (10, not more: 5–8 against a 50% season, the case that started this, must read as cold.) Otherwise it is steady.

**Your record section**

4. **Placement.** Below "Your picks", following the same role control. One row per record: champion icon, name, "13 matches · 38% win rate", a diverging bar for `net` around zero (scaled to the largest `|net|` in the role), the value (`+3`, `−3`, `±0`), and a recent form chip when `recent.matches ≥ 3`: "Last 20: 5–8", with "▼ Cold" (warn) or "▲ Hot" (positive) appended when FR 3 says so. The left-out names follow as a footnote, as before: "Left out with fewer than 3 matches: Ahri, Zed and 2 more."
5. **Title.** By the margin, so a small swing is never a verdict:
   - the top `net` is 3 or more → `{Champion} wins you the most {Role} matches`;
   - else the lowest `net` is −3 or less → `{Champion} costs you the most {Role} matches`;
   - else → `No {Role} champion clearly stands out`.

   Caption: `Wins minus losses, {queue} · {time range}`.
6. **Empty.** No records in the role → the section shows "Play 3 matches on a champion in this role to see its record here." No section skeleton of its own; it loads with the page.

**Hero**

7. **Chips.** The second chip shows recent form as "5–8 in your last 20" when the pick has `recent.matches ≥ 3`, else CS per minute as today.
8. **Cold pick.** When the pick's form is cold (FR 3), the hero text ends with "Lately you're {wins–losses} on {Champion} in your last 20, so watch this pick." The pick order (M-Score) does not change.

**Solo page**

9. The "LP per champion" card (solo-trends FR 24–25) is removed, and so are `champions` and `championsLeftOut` in the `climb` response. In its place, the Win factors card spans the row and a link line follows it: "Your champion records are in Champion Select →" to `/app/champion-select`.

## Technical Approach

### Backend

- `ChampionSelectRepository.GetMainChampionsByRoleAsync` already groups every champion by role with matches and wins; the records come from the same rows, no new aggregate query.
- One new query: the player's last 20 matches in the queue filter (no time filter) with role, champion and win, newest first, `LIMIT 20`. Same puuid, queue and minimum-duration predicates as the main query; parameterized.
- `MainChampionRecommender` (Core) builds records, left-out names and recent form per role from the rows plus the last-20 list, as a pure function.
- `MainChampionRoleGroup` gains `records` and `recordsLeftOut`; `MainChampionEntry` gains `recent`; a new `ChampionRecord` and `RecentForm` record (`JsonPropertyName` camelCase).
- `ClimbCalculator.Champions` and the climb DTO's `champions` / `championsLeftOut` are removed.

### Frontend

- `championSelectSummary.js`: `recentForm(row)` (cold / hot / steady, FR 3), `buildRecordTitle(records, role)`, `buildRecordCaption`, `formatNet`, `buildRecordLeftOut`, `buildPickChips` and `buildPickText` updated for FR 7–8.
- New `components/championSelect/ChampionSelectRecord.vue`, reusing `BaseDivergingBar`.
- `SoloStatsPage.vue`: remove `SoloChampionLp`, add the link line. Delete `SoloChampionLp.vue` and the champion-LP helpers in `soloSummary.js` with their tests.

### API contract (additions to `GET /api/v2/champion-select/{userId}`)

```json
{
  "mainChampions": [
    {
      "role": "BOTTOM",
      "champions": [
        { "championName": "Tristana", "championId": 18, "role": "BOTTOM", "winRate": 50.0, "gamesPlayed": 32,
          "mScore": 46.5, "avgKda": 3.1, "avgCsPerMin": 7.4, "recent": { "matches": 11, "wins": 5 } }
      ],
      "records": [
        { "championId": 222, "championName": "Jinx", "matches": 29, "wins": 15, "net": 1, "recent": null },
        { "championId": 18, "championName": "Tristana", "matches": 32, "wins": 16, "net": 0, "recent": { "matches": 11, "wins": 5 } }
      ],
      "recordsLeftOut": ["Ezreal"]
    }
  ]
}
```

## Testing Strategy

- **Core (xUnit):** records filter at 3 matches and sort by net then matches; left-out names; recent form counted per champion and role over the last 20 only; `null` without recent matches.
- **Endpoint (xUnit):** the response carries `records`, `recordsLeftOut` and `recent`; the climb response no longer carries `champions`.
- **Frontend (Vitest):** `recentForm` thresholds (5 matches, ±10 points); the three titles at the ±3 margin; hero chips and the cold sentence; `ChampionSelectRecord` rows, chip, footnote, empty state and role switching; the Solo page renders the link and no champion card.

## Validation Criteria

- With Flex and This season, Champion Select's record and hero agree with each other, and the Solo page shows no champion verdict.
- A role where every `|net|` is under 3 is titled "No {Role} champion clearly stands out".
- A pick that is 5–8 in the last 20 against 50% over the season shows "Last 20: 5–8 ▼ Cold" and the cold sentence.

## Open Questions

None. LP per champion is not carried over: LP belongs to the climb, and a record in wins and losses reads the same in every queue.
