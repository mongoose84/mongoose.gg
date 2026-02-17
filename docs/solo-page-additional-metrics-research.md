# Solo Page — Additional Metrics Research

> **Purpose**: Identify 5 additional data visualizations for the solo performance dashboard, grounded in scientific research and leveraging under-utilized Riot API data.
>
> **Date**: July 2025

---

## Current State

The solo page currently displays 6 trend charts:

| Chart | Question It Answers | Data Source |
|---|---|---|
| Winrate | "How am I doing overall?" | `participants.win` |
| Deaths | "What's the #1 thing I can fix?" | `participants.deaths` |
| Dragon Participation | "Am I showing up for objectives?" | `participant_objectives` |
| Vision Score | "Am I giving myself information?" | `participant_metrics.vision_score` |
| Gold at 15 | "Am I winning my lane?" | `participant_checkpoints` (min 15) |
| CS Per Minute | "Am I farming efficiently?" | `participants.total_cs / game_duration` |

These were informed by [win-prediction-metrics-research.md](win-prediction-metrics-research.md), which established gold differential, first objectives, deaths, CS, and vision as the primary win-predictive metrics.

---

## Research Sources

The following peer-reviewed publications and datasets informed these recommendations:

1. **Hojaji, F., McIlroy, R.E., Dupuy, A., Pedroni, G., Toth, A.J., & Campbell, M.J. (2025).** "Deep learning techniques for identifying KPIs in League of Legends: Win prediction, map navigation, and vision control." *Computers in Human Behavior Reports*, 19, 100718. — 154,000 matches, 97% prediction accuracy. Used SHAP values on a two-hidden-layer neural network to rank feature importance.

2. **Smithies, T.D., Campbell, M.J., Ramsbottom, N., & Toth, A.J. (2024).** "The SIDO Performance Model for League of Legends." *arXiv:2403.04873v2*. — Hierarchical Bayesian model separating player skill (S), individual contribution (I), duo synergy (D), and opponent difficulty (O). Uses gold earned and damage dealt as primary skill axes.

3. **Bahrololloomi, F., Klonowski, F., Sauer, S., Horst, R., & Dörner, R. (2023).** "E-sports player performance metrics for predicting the outcome of League of Legends matches considering player roles." *SN Computer Science*, 4(3), 238. — 86% prediction accuracy using role-specific performance metrics. Cited 27 times.

4. **Maymin, P. (2021).** "Smart kills and worthless deaths: eSports analytics for League of Legends." *Journal of Quantitative Analysis in Sports*, 17(1), 11–27. — Demonstrated that contextual quality of kills/deaths matters more than raw counts. Cited 70 times.

5. **Novak, A.R., Bennett, K.J.M., Pluss, M.A., & Fransen, J. (2020).** "Performance analysis in esports: Modelling performance at the 2018 League of Legends World Championship." *International Journal of Sports Science & Coaching*, 15(5-6), 809–817. — Professional-level match analysis identifying damage output, objective control, and team fight contribution as differentiators. Cited 52 times.

6. **Perez, M., Diaz, C.O., Soler, P., & Mier, A. (2024).** "Assessing player contributions in League of Legends matches: An analytical approach." *SN Computer Science*, 5(8), 1–14. — Framework for evaluating individual player contributions beyond simple KDA.

7. **Existing Mongoose.gg research** — [win-prediction-metrics-research.md](win-prediction-metrics-research.md), covering session deterioration effects (10%+ performance decline), first-objective correlations, and gold differential analysis.

---

## 5 Proposed Additional Metrics

### 1. Damage Share (% of Team Damage Dealt to Champions)

**Question it answers**: *"Am I pulling my weight in fights?"*

#### Scientific Basis

The SIDO model (Smithies et al., 2024) uses **damage dealt to champions** as one of two primary skill axes (alongside gold earned), finding it has a **63–73% correlation with winning** depending on game phase. Hojaji et al. (2025) identified **reducing damage taken** as the 4th most important KPI overall (after turret metrics and bounty level). Bahrololloomi et al. (2023) demonstrated that damage contribution is role-dependent — ADCs and mages are expected to output more, while supports and tanks contribute differently — making a percentage-of-team metric more meaningful than a raw number.

#### Riot API Data

| Field | Location | Currently Extracted? |
|---|---|---|
| `totalDamageDealtToChampions` | `ParticipantDto` | ✅ Yes |
| `teamDamagePercentage` | `ChallengesDto` | ❌ No |
| `damagePerMinute` | `ChallengesDto` | ❌ No |

**Preferred approach**: Extract `teamDamagePercentage` directly from `ChallengesDto`. This is pre-calculated by Riot and accounts for team context. Alternatively, calculate from existing data: `player.totalDamageDealtToChampions / SUM(team.totalDamageDealtToChampions)`.

#### Database Status

Already partially stored: `participant_metrics.damage_share_pct` and `participant_metrics.damage_dealt` exist in the schema. The trend chart would query these over time.

#### Implementation Notes

- Display as a percentage (e.g., "24.3%") with role-specific benchmarks
- Show trend over time to reveal whether the player is becoming more or less impactful in fights
- Consider role context: a support at 8% is normal; a mid-laner at 8% is a problem
- Color coding: compare against role average for the player's rank

---

### 2. Kill Participation (% of Team Kills Involved In)

**Question it answers**: *"Am I involved in my team's plays?"*

#### Scientific Basis

Maymin (2021) demonstrated that the **context of kills matters more than raw counts** — being involved in team-created kills is more predictive of winning than solo kills alone. Bahrololloomi et al. (2023) found that kill participation, independent of role, is a strong predictor of match outcome at 86% accuracy. Perez et al. (2024) built an analytical framework specifically around **individual contribution measurement**, where kill participation serves as a proxy for map presence and team fight involvement.

The SIDO model further supports this: the "I" (Individual) component captures how a player contributes beyond what their lane matchup predicts, and kill participation is a direct behavioral signal of proactive game engagement.

#### Riot API Data

| Field | Location | Currently Extracted? |
|---|---|---|
| `kills` | `ParticipantDto` | ✅ Yes |
| `assists` | `ParticipantDto` | ✅ Yes |
| `killParticipation` | `ChallengesDto` | ❌ No |

**Preferred approach**: Extract `killParticipation` from `ChallengesDto` (pre-calculated ratio). Alternatively, calculate: `(kills + assists) / team_total_kills`. Handle edge case where team has 0 kills.

#### Database Status

Already stored: `participant_metrics.kill_participation_pct` exists in the schema. This metric can be charted as a trend immediately.

#### Implementation Notes

- Display as percentage (e.g., "67%")
- Trend over time shows whether the player is becoming more or less involved in team plays
- Higher is generally better, but context matters: a split-pushing top-laner may have lower KP by design
- Combine with damage share for a complete "fight contribution" picture
- This is one of the few metrics where the data already exists in the database and just needs a frontend chart

---

### 3. Turret Damage / Objective Damage

**Question it answers**: *"Am I helping take objectives that win games?"*

#### Scientific Basis

Hojaji et al. (2025) found that **turret-related metrics are the single most important predictors of match outcome** in their 154,000-match study with 97% prediction accuracy:

| Rank | KPI | Direction |
|---|---|---|
| #1 | Turrets lost | Minimize |
| #2 | Bounty level | Maximize |
| **#3** | **Turrets destroyed** | **Maximize** |
| #4 | Damage taken | Minimize |
| #5 | Dragons killed | Maximize |

This finding held across all 10 rank tiers from Iron to Challenger. The existing Mongoose.gg research corroborates this: **first tower correlates with a 65.42% win rate**. Yet the solo page currently has zero turret-related metrics.

Novak et al. (2020) found that at the professional level (2018 Worlds), teams that consistently secured turrets and objectives had significantly higher win rates, independent of kill counts. This suggests turret interaction is under-weighted in most player analytics tools.

#### Riot API Data

| Field | Location | Currently Extracted? |
|---|---|---|
| `damageDealtToObjectives` | `ParticipantDto` | ❌ No |
| `damageDealtToBuildings` | `ParticipantDto` | ❌ No |
| `turretKills` | `ParticipantDto` | ❌ No |
| `turretTakedowns` | `ParticipantDto` | ❌ No |
| `turretPlatesTaken` | `ChallengesDto` | ❌ No |
| `firstTurretKilled` | `ChallengesDto` | ❌ No |
| `turretsTakenWithRiftHerald` | `ChallengesDto` | ❌ No |

**Preferred approach**: Extract `damageDealtToBuildings` (turret-specific damage) and `turretPlatesTaken` from `ChallengesDto`. Turret plates are an early-game economy metric (150g each, 5 plates per turret) that directly feeds into the gold lead the player builds.

#### Database Status

**Not currently stored.** Requires schema extension to store `damage_to_buildings` and/or `turret_plates_taken` in `participant_metrics` or a new column in `participants`.

#### Implementation Notes

- Two chart options:
  - **Turret plates per game** (early game focus, 0–14 minutes) — directly actionable, player can focus on taking plates
  - **Objective damage per game** (full game) — broader measure of objective focus
- Turret plates are particularly actionable because they're available every game before 14 minutes
- Requires backend changes: update `RiotMatchMapper.cs` to extract new fields, add database columns, update participant repository

---

### 4. Crowd Control Score (Time Spent CCing Opponents)

**Question it answers**: *"Am I disrupting enemies effectively?"*

#### Scientific Basis

The SIDO model (Smithies et al., 2024) explicitly identifies **enemy disruption** as a key differentiator for support and tank roles, noting that damage dealt alone fails to capture the contribution of players whose primary function is enabling teammates through crowd control. The model's "I" (Individual) component accounts for role-specific contributions that go beyond raw damage.

Bahrololloomi et al. (2023) found that **role-specific metrics significantly improve prediction accuracy** — different roles contribute to wins through different mechanisms. For tanks and supports, crowd control time is a direct measure of their primary job. Even for damage-dealing roles, CC contribution (from abilities like stuns, roots, slows) is a secondary but important contribution.

Hojaji et al. (2025) found that higher-ranked players demonstrate more adaptive and efficient strategies, and their movement analysis showed that winning teams' advantage comes partly from control (forcing opponents into unfavorable positions), which CC directly enables.

#### Riot API Data

| Field | Location | Currently Extracted? |
|---|---|---|
| `timeCCingOthers` | `ParticipantDto` | ❌ No |
| `totalTimeCCDealt` | `ParticipantDto` | ❌ No |
| `enemyChampionImmobilizations` | `ChallengesDto` | ❌ No |

**Preferred approach**: Extract `timeCCingOthers` (seconds spent applying CC to enemy champions). This is the most direct measure. `enemyChampionImmobilizations` from `ChallengesDto` is a count-based alternative that may be easier to interpret.

#### Database Status

**Not currently stored.** Requires a new column in `participant_metrics` for `cc_time_seconds` or `enemy_immobilizations`.

#### Implementation Notes

- Display as seconds per minute (normalized for game length) or total immobilizations per game
- Heavily role-dependent: supports/tanks will have much higher values than ADCs
- Consider showing with role context: "You CC'd for 42s this game. Average for Leona at your rank: 38s"
- This metric fills a gap for support/tank players who currently have limited actionable metrics on the solo page (most existing charts favor lane-dominant carries)
- Pairs well with kill participation: high KP + high CC = effective enabler

---

### 5. Time Spent Dead (Death Cost in Seconds)

**Question it answers**: *"How much game time am I losing to deaths?"*

#### Scientific Basis

The SIDO model (Smithies et al., 2024) identifies **cascading gold loss from deaths** as one of the most impactful negative outcomes in League of Legends — dying not only gives the opponent gold (and potentially bounty, which Hojaji et al. found to be the #2 KPI), but removes the player from the map for an increasing duration as the game progresses. A death at 30 minutes costs ~45 seconds of game time, during which the player can't farm, fight, or take objectives.

Hojaji et al. (2025) found that **bounty level** (accumulated from kill streaks without dying) is the **#2 most important KPI** for match outcome — directly connected to death avoidance. Their game-phase analysis showed that health management becomes increasingly critical in mid-to-late game, where a single death can cost a major objective.

The existing Mongoose.gg research established that **deaths are the most actionable improvement target** (the Deaths chart already reflects this). Time Spent Dead adds a second dimension: it's not just *how often* you die, but *when* you die. A death at 5 minutes costs ~13 seconds; a death at 35 minutes costs ~52 seconds. This time-cost perspective transforms "don't die" from a vague directive into a quantifiable resource loss.

#### Riot API Data

| Field | Location | Currently Extracted? |
|---|---|---|
| `totalTimeSpentDead` | `ParticipantDto` | ❌ No |
| `longestTimeSpentLiving` | `ParticipantDto` | ❌ No |

**Preferred approach**: Extract `totalTimeSpentDead` from `ParticipantDto`. This gives the raw seconds spent in death screen per game. Normalize by game duration: `totalTimeSpentDead / gameDuration` gives a "death cost ratio" — what percentage of the game you spent dead.

#### Database Status

**Partially stored**: `participant_metrics.time_dead_sec` already exists, along with death timing buckets (`death_time_early_sec`, `death_time_mid_sec`, `death_time_late_sec`). If `time_dead_sec` is already being populated, this chart can be built immediately.

#### Implementation Notes

- Display as percentage of game spent dead (e.g., "8.2% of game time lost to deaths")
- Trend over time reveals if the player is dying at more or less costly moments
- Complements the existing Deaths chart: Deaths = frequency, Time Spent Dead = severity
- Consider a combined view: "You died 4 times, costing you 2 minutes 15 seconds (7.5% of the game)"
- Can also display `longestTimeSpentLiving` as a positive metric ("You stayed alive for 12:45 at your best")
- Late-game deaths are worth dramatically more time, so this metric naturally emphasizes the deaths that matter most

---

## Implementation Priority

| Priority | Metric | Backend Work | Frontend Work | Rationale |
|---|---|---|---|---|
| 1 | **Kill Participation** | None (data exists) | New chart component | Zero backend cost, data already in `participant_metrics.kill_participation_pct` |
| 2 | **Damage Share** | None (data exists) | New chart component | Zero backend cost, data already in `participant_metrics.damage_share_pct` |
| 3 | **Time Spent Dead** | Verify `time_dead_sec` is populated | New chart component | Minimal backend cost if already populated; high complementary value with Deaths chart |
| 4 | **Turret Damage** | Mapper update + schema extension | New chart component | Requires backend changes but addresses the #1 win predictor per Hojaji et al. |
| 5 | **CC Score** | Mapper update + schema extension | New chart component | Requires backend changes; most role-dependent metric, highest complexity |

---

## Riot API Fields to Extract

To implement metrics 4 and 5, the following fields should be added to `RiotMatchMapper.cs`:

### From `ParticipantDto` (top-level)

```
damageDealtToObjectives     → int
damageDealtToBuildings      → int
turretKills                 → int
turretTakedowns             → int
timeCCingOthers             → int (seconds)
totalTimeCCDealt            → int (seconds)
totalTimeSpentDead          → int (seconds, if not already mapped)
```

### From `ChallengesDto` (nested)

```
teamDamagePercentage        → float (0.0–1.0)
killParticipation           → float (0.0–1.0)
damagePerMinute             → float
turretPlatesTaken           → int
enemyChampionImmobilizations → int
longestTimeSpentLiving      → float (seconds)
```

---

## Summary

These 5 metrics were selected based on three criteria:

1. **Scientific validation** — each is supported by multiple peer-reviewed studies as being predictive of match outcome or player skill
2. **Actionability** — each answers a specific question the player can act on ("Am I fighting enough?", "Am I taking turrets?", "Am I dying too long?")
3. **Complementarity** — each fills a gap in the current 6-chart lineup, covering team fight contribution (Damage Share, KP), objective play (Turret Damage), utility contribution (CC Score), and death severity (Time Spent Dead)

Together with the existing 6 charts, these would give players a comprehensive 11-metric dashboard covering: overall results (winrate), laning (gold@15, CS/min), fighting (damage share, kill participation, deaths, time dead), objectives (dragon participation, turret damage), utility (vision score, CC score).
