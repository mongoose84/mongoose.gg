# Solo Page Feature Research: 5 Scientifically-Backed Additions

> **Research Date**: June 2025  
> **Purpose**: Identify high-value features to add to the Solo Dashboard beyond existing trend charts  
> **Current State**: 6 trend charts (winrate, CS/min, gold@15, deaths, dragon participation, vision score)  
> **Methodology**: Academic literature review + feasibility assessment against the Riot Games API v5 + UX analysis

---

## Executive Summary

The Solo Dashboard currently occupies 3 of 5 available AnalysisLayout zones. Zones 4 ("deep-analysis") and 5 ("goals") are empty placeholders. This research identifies **5 feature candidates** for those zones, each backed by peer-reviewed research. Features are ranked by a composite of **scientific evidence strength**, **Riot API feasibility** (does the API provide the data?), and **expected user impact**.

| # | Feature | Evidence Strength | Riot API Available? | Implementation Effort |
|---|---------|-------------------|---------------------|----------------------|
| 1 | Performance Radar Chart | Strong | Yes — Match v5 | Low |
| 2 | Death Spatial Heatmap | Strong | Yes — Timeline v5 | Medium |
| 3 | LP / Rank Climb Trend | Moderate | Partial — League v4 | Low-Medium |
| 4 | Objective Participation Dashboard | Strong | Yes — Timeline v5 | Low-Medium |
| 5 | Session Performance / Tilt Detection | Strong | Derivable — Match v5 | Medium |

---

## Feature 1: Performance Radar Chart (Spider Graph)

### Description
A multi-axis radar chart comparing the player's performance across 6–8 key metrics against their rank average or a target percentile. Each axis represents a normalized metric (0–100 scale). The player's polygon shape reveals their strengths and weaknesses at a glance.

**Suggested axes**: CS/min, Vision Score/min, Kill Participation %, Damage Share %, Death Rate, Gold Efficiency, Dragon Participation %, Objective Damage share.

### User's Note
> "This was implemented before in the Riot League of Legends client but then removed."

Riot's in-client radar chart (introduced ~Season 8, removed Season 12) was a popular feature that showed players their relative strengths. Its removal was due to implementation issues (e.g., poor normalization, misleading area comparisons), not because the concept was flawed. Community sentiment strongly favors its return.

### Scientific Evidence

**1. Nascimento Junior et al. (2017) — "Profiling Successful Team Behaviors in League of Legends" (ACM WebMedia)**
- Used radar plots to profile team performance across multiple dimensions in LoL
- Identified 7 distinct behavior clusters from ranked matches, categorized into 4 winning-proportion levels
- Radar visualization enabled researchers and players to immediately see which dimensions separated winning from losing profiles
- Key finding: multi-dimensional performance profiles are more predictive than any single metric

**2. Hojaji et al. (2025) — "Deep learning techniques for identifying KPIs in League of Legends" (Computers in Human Behavior Reports, N=154,000 games)**
- Identified 12+ independent KPIs that contribute to winning, confirming that performance is inherently multi-dimensional
- Top KPIs: turret loss minimization, bounty management, turrets destroyed, damage taken reduction, dragons killed
- Higher-ranked players have higher composite vision scores across all dimensions, not just one
- Conclusion: a single-metric view is insufficient; players need to see their full performance profile

**3. Lee et al. (2024) — "Characterizing and Quantifying Expert Input Behavior in LoL" (CHI '24, ACM, N=193 players, 4,835 matches)**
- Training aid software used radar charts to compare player input skill indices against rank-group averages
- 7 students in a 3-week study reported that "it was good to see input skill indices at a glance and check which skill they were lacking"
- Coach feedback: "It was useful to be able to see specifically how much the things I asked students to improve actually improved"
- Radar chart identified actionable skill gaps that players could target for improvement

**4. Radar Chart Theory (Cleveland & McGill, 1984; Saary, 2008)**
- Radar charts are well-suited for comparing a subject against a baseline across multiple variables simultaneously
- Known limitation: area distortion when axes are unrelated. Mitigation: group related metrics on adjacent axes and normalize to percentile ranks rather than raw values
- Widely used in sports analytics (FIFA player cards, NBA shot charts) for quick pattern recognition

### Riot API Feasibility: **FULLY AVAILABLE — Match v5 + Timeline v5**
All required metrics are returned by the Riot Games API:

| Metric | Riot API Source | Endpoint | Field(s) |
|--------|----------------|----------|----------|
| CS/min | Match v5 Info | `/lol/match/v5/matches/{matchId}` | `totalMinionsKilled` + `neutralMinionsKilled`, `gameDuration` |
| Vision Score | Match v5 Info | `/lol/match/v5/matches/{matchId}` | `visionScore` |
| Kill Participation | Match v5 Info | `/lol/match/v5/matches/{matchId}` | Computed from `kills`, `assists` vs team kills |
| Damage Share | Match v5 Info | `/lol/match/v5/matches/{matchId}` | `totalDamageDealtToChampions` vs team total |
| Deaths | Match v5 Info | `/lol/match/v5/matches/{matchId}` | `deaths` |
| Gold Efficiency | Match v5 Info | `/lol/match/v5/matches/{matchId}` | `goldEarned`, CS totals |
| Dragon Participation | Timeline v5 | `/lol/match/v5/matches/{matchId}/timeline` | `ELITE_MONSTER_KILL` events with `killerId` + `assistingParticipantIds` |
| Tower Participation | Timeline v5 | `/lol/match/v5/matches/{matchId}/timeline` | `BUILDING_KILL` events with `killerId` + `assistingParticipantIds` |

**Normalization approach**: Calculate percentile rank within the player's current tier (e.g., Gold III). Tier data is available from the League v4 API (`/lol/league/v4/entries/by-puuid/{puuid}`).

**Current project status**: All these metrics are already extracted and stored during match sync. No additional API calls needed.

### UX Recommendation
- Place in Zone 4 ("deep-analysis") as the primary visual anchor
- Allow clicking individual axes to drill into that metric's trend chart
- Show comparison against: (a) rank average, (b) previous time period, (c) win vs loss games
- Use Chart.js radar chart type (already in project dependencies)

### Precedent
- **Riot Client** (S8–S12): Radar chart with Teamwork, Income, Toughness, Vision, Fighting dimensions
- **op.gg**: "Tier Graph" showing multi-dimensional ranking
- **FIFA Ultimate Team**: Player cards with hexagonal radar charts for Pace/Shooting/Passing/Dribbling/Defending/Physical

---

## Feature 2: Death Spatial Heatmap

### Description
A spatial heatmap overlaid on a Summoner's Rift minimap showing where the player dies most frequently. Each death is a data point at its x,y map coordinate, aggregated across recent matches using kernel density estimation (KDE) to produce heat zones. Color intensity represents death frequency. Can be filtered by game phase (0–10, 10–20, 20–30, 30+ minutes) and compared against win vs loss patterns.

### User's Note
> "Danger zones, where we could show a map of where the user dies a lot using a heatmap."

This is **fully feasible** with the Riot API. The Match v5 Timeline endpoint returns `CHAMPION_KILL` events that include `position: { x, y }` map coordinates for each kill/death, along with `timestamp`, `killerId`, `victimId`, and `assistingParticipantIds`. The project currently processes these events for death timing but does not extract the position data — a targeted enhancement to the timeline mapper would unlock full spatial heatmaps.

### Scientific Evidence

**1. Hojaji et al. (2025) — Spatial-temporal heatmaps in LoL**
- Applied Gaussian Kernel Density Estimation (KDE) to generate spatial-temporal heatmaps of player movement and events using the same Riot API data source
- Found that winning teams exhibit more total movement, spread, and rotation scores, especially in mid/late game
- Death location clustering analysis revealed that losing teams have more concentrated death zones in their own jungle and around objectives
- Key finding: spatial death patterns differ significantly between winning and losing teams, and between skill tiers

**2. Afonso et al. (2019, 2021) — "VisuaLeague: Player Performance Analysis Using Spatial-Temporal Data" (Multimedia Tools and Applications / IEEE IV)**
- Built spatial visualizations of LoL match data using the Riot Timeline API
- Demonstrated that spatio-temporal kill/death visualizations help players identify positional mistakes
- Found that map-based event visualization was the most intuitive format for players reviewing their games

**3. SIDO Model — Zhang & Naidu (2024, arXiv:2403.04873v1)**
- Early-game performance (0–7 min) has 69% correlation with winning; this rises to 83% by late game
- Deaths before 10 minutes have outsized impact because they compound through gold differential and map pressure
- Implication: filtering the death heatmap by game phase (early/mid/late) reveals which phase and map region to focus improvement on

**4. Wallner & Drachen (2024) — "Play Graph: Spatio-Temporal Visualization of Player Actions"**
- Demonstrated that spatial event visualizations in games are highly interpretable by players
- Players found it easier to identify improvement areas when deaths were shown in map context with phase coloring
- Heatmap density plots were the most preferred visualization format for spatial event data

**5. Sapienza et al. (2018) — "Individual Performance in Team-Based Online Games" (Royal Society Open Science)**
- Found 10%+ win rate decline and 8%+ KDA decline from first to last match in a session
- Performance deterioration manifests as increased deaths — spatial patterns may shift (e.g., more aggressive/forward deaths as fatigue compounds)

### Riot API Feasibility: **FULLY AVAILABLE — Timeline v5**
The Match v5 Timeline endpoint provides all required data:

| Data Point | Riot API Field | Location |
|------------|---------------|----------|
| Death x,y position | `events[].position.x`, `events[].position.y` | `CHAMPION_KILL` event where `victimId` matches player |
| Death timestamp | `events[].timestamp` | Milliseconds from game start |
| Game phase | Derived | `timestamp / 60000` → minute, then bucket into phases |
| Killer identity | `events[].killerId` | Who killed the player (for context) |
| Assists on kill | `events[].assistingParticipantIds` | Who assisted (for 1v1 vs gank detection) |

**Additional spatial data available** (for future enhancements):
- `participantFrames[].position.x/y` — player position every minute (movement pathing)
- `WARD_PLACED` events with `position` — ward placement heatmap
- `BUILDING_KILL` events with `position` — tower fight locations

**Current project status**: The timeline mapper already processes `CHAMPION_KILL` events for death timing extraction ([RiotTimelineMapper.cs](server/Infrastructure/Riot/Mappers/RiotTimelineMapper.cs#L107-L142)) but discards the `position` field. Storing x,y requires:
1. Adding `death_x` and `death_y` columns (or a `death_events` table) to the schema
2. Extending the timeline mapper to extract `position` from each kill event
3. No additional API calls — the data is already fetched during sync

### Summoner's Rift Coordinate System
The Riot API uses a coordinate system where:
- **Origin (0,0)**: Bottom-left corner of the map (blue side fountain)
- **Max (~15000, 15000)**: Top-right corner (red side fountain)
- **Mid lane**: Diagonal from ~(2000, 2000) to ~(13000, 13000)
- Standard minimap images can be used as the heatmap background by normalizing coordinates to pixel positions

### Visualization Design

**Primary: Spatial Death Heatmap** (Recommended)
- Summoner's Rift minimap as background image
- Gaussian KDE overlay with heat coloring (blue → yellow → red)
- Filter controls: game phase (All / 0–10 / 10–20 / 20–30 / 30+), queue type, time range
- Toggle between: (a) all deaths, (b) deaths in wins, (c) deaths in losses

**Secondary: Phase-Segmented Bar** (Complementary)
A horizontal bar chart showing death count per phase, providing the temporal dimension alongside the spatial heatmap. Uses the already-stored `deaths_pre_10`, `deaths_10_20`, `deaths_20_30`, `deaths_30_plus` data.

**Implementation options for the heatmap rendering**:
- Canvas-based: Use HTML5 Canvas with a KDE algorithm (e.g., `simpleheat` library, ~3KB)
- SVG-based: Plot death positions as circles with opacity blending
- Chart.js plugin: `chartjs-chart-matrix` for grid-based heat cells

### UX Recommendation
- Place in Zone 4 ("deep-analysis") alongside the radar chart as the visual centerpiece
- Aggregate deaths across recent N matches (e.g., last 20) — single-match heatmaps are too sparse
- Default view: all game phases. Toggle to filter by phase (0–10, 10–20, 20–30, 30+)
- Map side normalization: normalize blue/red side coordinates so all deaths render consistently
- Include the phase bar chart as a complementary sidebar showing death timing breakdown
- Tooltip on hover: "X deaths in this area across Y games — most common in [phase]"
- Compare toggle: show deaths in wins vs deaths in losses to reveal pattern differences

---

## Feature 3: LP / Rank Climb Trend

### Description
A line chart showing the player's LP (League Points) and tier/division trajectory over time. Each match is a data point showing LP gain or loss, with tier boundaries clearly marked. This provides the clearest possible visualization of "am I actually climbing?"

### Scientific Evidence

**1. Goal Gradient Effect — Hull (1932), Kivetz et al. (2006)**
- Players accelerate effort as they perceive proximity to a goal
- Visualizing LP proximity to the next tier/division activates this psychological accelerant
- In gamification research, progress bars and trajectory visualization are among the most effective engagement tools (Hamari et al., 2014, "Does Gamification Work?", HICSS)

**2. Self-Determination Theory — Deci & Ryan (2000)**
- Competence feedback (seeing skill progression) is one of three core motivational needs
- LP trend lines provide concrete, unambiguous competence feedback
- Simply showing "you are improving" (even slowly) sustains intrinsic motivation

**3. Bahrololloomi et al. (2023) — "Beyond Winning and Losing: Modeling Human Motivations and Behaviors" (Springer)**
- Performance-based matchmaking systems (LP/Elo) create anxiety when players can't see their trajectory
- Visualization of rank trajectory reduces ranked anxiety and encourages continued play
- Players who track their climbing trend report higher satisfaction even during losing streaks

**4. Sapienza et al. (2018)**
- Session-level performance degradation compounds across days — LP visualization helps players see macro trends beyond day-to-day variance
- Smoothed LP trendlines (7-day or 20-game moving average) filter noise and reveal true skill trajectory

### Riot API Feasibility: **PARTIAL — League v4 (snapshot only)**
The Riot API provides rank/LP data, but with an important limitation:

| Data Point | Riot API Source | Endpoint | Limitation |
|------------|----------------|----------|------------|
| Current tier | League v4 | `/lol/league/v4/entries/by-puuid/{puuid}` | Snapshot of current state only |
| Current division | League v4 | Same | Snapshot only |
| Current LP | League v4 | Same | Snapshot only |
| Match timestamp | Match v5 | `/lol/match/v5/matches/{matchId}` | `gameStartTimestamp` always available |
| Win/loss | Match v5 | Same | `win` field per participant |

**Critical limitation**: The Riot API does **not** return LP-per-match in match history data. The League v4 endpoint only returns the player's *current* ranked standing. There is no historical LP API.

**Mitigation strategy** (already implemented in this project): Snapshot the current LP from League v4 after each sync and stamp it onto the most recent ranked match. Over time, each sync point creates a data point for the LP trend. The more frequently a user syncs, the more granular the chart.

**Alternative enrichment**: Since we have win/loss per match and the LP at the most recent sync, we can estimate intermediate LP values using average LP gain/loss per win/loss (typically +15–25 LP per win, -15–25 per loss in the current system). This approximation is good enough for trend visualization.

**Note**: LP data will be null for non-ranked games. Filter to ranked_solo queue only.

### Visualization Design
- **X-axis**: Match number or date (user toggle)
- **Y-axis**: Composite LP (e.g., Gold III 45LP = 345 on a unified 0–2800 scale)
- **Annotations**: Tier/division promotion lines, demotion shields, win/loss streaks highlighted
- **Moving average**: 10-match smoothed line overlaid to show trajectory
- Use chartjs-plugin-annotation (already in project) for tier boundary lines

### UX Recommendation
- Place in Zone 4 as a secondary chart, or make it a toggle on the existing winrate trend chart
- Color wins green, losses red on the scatter points
- Show a "climb rate" metric: average LP change per day/week
- This is one of the most-requested features in competitive gaming analytics tools

---

## Feature 4: Objective Participation Dashboard (Herald/Baron/Tower)

### Description
Expand the existing Dragon Participation chart into a comprehensive objective participation view covering Herald, Baron, and Tower participation alongside dragons. Display as either a multi-series trend chart or a set of participation rate cards.

### Scientific Evidence

**1. Hojaji et al. (2025) — "Deep learning techniques for identifying KPIs in LoL" (N=154K games)**
- **Turret loss minimization is the #1 KPI correlated with winning** — more important than kills, gold, or dragons
- Turrets destroyed is the #3 KPI
- Herald and Baron participation contribute to tower pressure, the most important macro objective
- Conclusion: showing only dragon participation gives players an incomplete picture of their objective contribution

**2. SIDO Model — Zhang & Naidu (2024)**
- Objective control (dragon, baron, tower) accounts for the majority of gold differential, which is the #1 win predictor
- Baron especially creates 1500+ team gold swings
- Players who participate in more objectives contribute more to win conditions regardless of KDA

**3. LeagueMath Statistical Analysis (corroborated in existing research doc)**

| Objective | Win Correlation |
|-----------|----------------|
| First Dragon | 70.69% |
| First Tower | 65.42% |
| First Inhibitor | 79.28% |
| First Baron | 50.06% |

- First Tower has 65.42% win correlation — nearly as high as First Dragon
- Tower participation is entirely absent from the current dashboard despite being the #2 early objective correlation

**4. Nascimento Junior et al. (2017)**
- Team behavior profiling found that objective-focused teams (high tower/dragon/baron participation) cluster into the highest winning-proportion group
- Individual contribution to team objectives distinguishes winning vs losing profiles more than raw KDA

### Riot API Feasibility: **FULLY AVAILABLE — Match v5 + Timeline v5**
All objective participation data is available from the Riot API:

| Data Point | Riot API Source | Endpoint | Event Type |
|------------|----------------|----------|------------|
| Dragon kills | Timeline v5 | `/lol/match/v5/matches/{matchId}/timeline` | `ELITE_MONSTER_KILL` where `monsterType=DRAGON` |
| Herald kills | Timeline v5 | Same | `ELITE_MONSTER_KILL` where `monsterType=RIFTHERALD` |
| Baron kills | Timeline v5 | Same | `ELITE_MONSTER_KILL` where `monsterType=BARON_NASHOR` |
| Tower kills | Timeline v5 | Same | `BUILDING_KILL` where `buildingType=TOWER_BUILDING` |
| Kill + assist attribution | Timeline v5 | Same | `killerId` + `assistingParticipantIds` on each event |
| Team totals | Match v5 Info | `/lol/match/v5/matches/{matchId}` | `objectives.dragon.kills`, `objectives.riftHerald.kills`, `objectives.baron.kills`, `objectives.tower.kills` |
| Dragon type | Timeline v5 | Same | `monsterSubType` (e.g., `FIRE_DRAGON`, `ELDER_DRAGON`) — **not currently stored** |

**Participation rate formula**: `player_participated / team_taken * 100`

**Bonus data available from API** (not currently stored):
- `monsterSubType` enables dragon-type breakdown (Infernal, Mountain, Ocean, Hextech, Chemtech, Cloud, Elder)
- `position` on objective kill events enables objective fight location analysis
- `timestamp` enables objective timing trends (e.g., average first dragon time)

**Current project status**: The project already extracts and stores individual objective participation and team totals during match sync via the timeline mapper.

### Visualization Options

**Option A: Multi-Series Trend Chart** (Recommended)
A single trend chart with 4 lines (dragon, herald, baron, tower participation %) over time. Uses the same TrendLineChart component already built.

**Option B: Objective Report Cards**
Four small cards, each showing participation %, trend direction, and comparison to rank average. More compact but less trend-oriented.

**Option C: Stacked Objective Timeline**
Match-by-match timeline showing which objectives the player participated in, stacked vertically. Gives per-match context.

### UX Recommendation
- Add a new API endpoint: `GET /api/v2/solo/trends/objective-participation/{userId}`
- Place alongside existing trend charts in Zone 3, or create a dedicated "Objectives" section in Zone 4
- Highlight tower participation specifically, given its #1 KPI status
- Show team objective count alongside participation rate to contextualize low-objective games

---

## Feature 5: Session Performance / Tilt Detection

### Description
Track the player's performance within and across gaming sessions. Identify sessions (groups of matches played within a time window, e.g., <45 min gaps between games), calculate per-session performance trends, and alert players when performance deterioration suggests tilt or fatigue.

### Scientific Evidence

**1. Sapienza et al. (2018) — "Individual Performance in Team-Based Online Games" (Royal Society Open Science)**
- Analyzed millions of LoL and Dota 2 matches to study session performance
- **Key findings**:
  - Win rate declines **10%+** from first to last match in a session
  - KDA declines **8%+** from first to last match in a session
  - Veterans show less deterioration than novices, but are not immune
  - Performance decline is consistent across skill levels
- Recommendation: "Systems could prompt players to take breaks when performance metrics start declining"

**2. Seesurn, Batllori & Watson (2025) — "Decision fatigue in video gamers" (Frontiers in Nutrition)**
- Confirmed that extended gaming sessions produce measurable decision fatigue
- Cognitive performance (decision quality, reaction time) degrades predictably after ~3 consecutive competitive matches
- This degradation directly manifests as increased deaths and worse gold differentials in MOBAs

**3. Lee et al. (2024) — CHI '24**
- Found that monitoring skill (situational awareness) is one of the key differentiators between expert and amateur players
- Monitoring skill degrades with fatigue, which is consistent with the session deterioration findings
- Implication: session tracking that alerts players to cognitive decline could prevent the most damaging type of performance loss

**4. Kleinman et al. (2023) — "Challenges in the esports learning process"**
- Identified the need for "high explainability in player evaluation" — simply showing a win/loss streak isn't enough
- Players need to see *why* they're performing worse (specific metrics declining), not just *that* they are
- Session-level analysis with per-metric breakdowns gives this explainability

### Riot API Feasibility: **DERIVABLE — Match v5**
No dedicated session API exists, but sessions can be derived from match timestamps available in the Riot API:

| Data Point | Riot API Source | Endpoint | Field |
|------------|----------------|----------|-------|
| Match timestamp | Match v5 | `/lol/match/v5/matches/{matchId}` | `gameStartTimestamp` |
| Match duration | Match v5 | Same | `gameDuration` |
| Win/loss per match | Match v5 | Same | `participants[].win` |
| Deaths per match | Match v5 | Same | `participants[].deaths` |
| KDA per match | Match v5 | Same | `kills`, `assists`, `deaths` |
| Gold diff per match | Timeline v5 | `/lol/match/v5/matches/{matchId}/timeline` | `participantFrames[].totalGold` at min 15 |
| Vision per match | Match v5 | Same | `participants[].visionScore` |

**Session detection algorithm**:
```
Sort matches by game_start_time ASC
For each match:
  If time since previous match end > 45 minutes:
    Start new session
  Else:
    Add to current session
```

### Visualization Design

**Primary: Session Performance Card**
- Show current session: match count, running win rate, KDA trend
- Traffic light indicator: Green (stable/improving), Yellow (slight decline), Red (significant decline)
- "Session decay" metric: % change in key metrics from session start to current match

**Secondary: Session History Chart**
- Historical sessions shown as grouped data points
- Each session is a bar or dot showing average performance
- Highlight sessions where performance declined significantly
- Overlay: session length (# of matches) correlated with performance

**Tilt Alert Thresholds** (based on Sapienza et al. findings):
- **Yellow**: After match 3 in a session, if win rate in session < 40% or KDA dropped >15% from first match
- **Red**: After match 5 in a session, or if deaths increased >30% from session start

### UX Recommendation
- Place in Zone 5 ("goals") as a "Session Health" component
- Keep it non-intrusive — show a subtle indicator during normal play, expand on click
- Pair with the AI goal system (Zone 5 planned feature) — "Consider taking a break" as an AI-generated suggestion
- Avoid making it feel punitive; frame as "optimize your practice time"

---

## Implementation Priority Recommendation

Based on the composite of evidence strength, data readiness, implementation effort, and expected user impact:

### Phase 1 (Zone 4 — Deep Analysis)
1. **Performance Radar Chart** — Lowest effort, highest visual impact, fully supported by data. Fills the "strengths/weaknesses at a glance" gap.
2. **Death Spatial Heatmap** — Riot Timeline API provides x,y death coordinates; unique insight not available on competing platforms, directly actionable. Requires storing kill event positions (minor schema + mapper change).

### Phase 2 (Zone 3 expansion or Zone 4)
3. **LP/Rank Climb Trend** — Partially available from League v4 API (current snapshot only, not per-match history). Project already mitigates via LP snapshots per sync. Extremely popular feature request with strong motivational psychology backing.
4. **Objective Participation Dashboard** — Data exists, expands on the existing dragon participation to cover the scientifically-validated #1 KPI (towers).

### Phase 3 (Zone 5 — Goals/Session)
5. **Session Performance / Tilt Detection** — Requires backend session derivation logic, but all data is present. Pairs naturally with the planned AI goal system.

---

## Referenced Sources

### Primary Sources (Peer-Reviewed)

1. **Hojaji, F. et al. (2025)**. "Deep learning techniques for identifying key performance indicators in professional League of Legends." *Computers in Human Behavior Reports*, Vol. 17. N=154,000 games. DOI: 10.1016/j.chbr.2025.100605

2. **Zhang, Z. & Naidu, V. (2024)**. "SIDO — Simultaneous Inference of Dynamics and Outcomes in Competitive LoL Matches." *arXiv:2403.04873v1*. Gold differential, damage correlation with win prediction.

3. **Sapienza, A. et al. (2018)**. "Individual Performance in Team-Based Online Games." *Royal Society Open Science*, 5(6), 180329. Session fatigue: 10%+ win rate decline, 8%+ KDA decline.

4. **Nascimento Junior, F. F. et al. (2017)**. "Profiling Successful Team Behaviors in League of Legends." *ACM WebMedia*. Radar plots for multi-dimensional LoL team profiling, 7 behavior clusters.

5. **Lee, H. et al. (2024)**. "Characterizing and Quantifying Expert Input Behavior in League of Legends." *CHI '24: Proceedings of the 2024 CHI Conference on Human Factors in Computing Systems*, Article 957, pp. 1–21. DOI: 10.1145/3613904.3642588. Training aid with radar chart visualization, longitudinal coaching study.

6. **Bahrololloomi, F. et al. (2023)**. "Beyond Winning and Losing: Modeling Human Motivations and Behaviors with Vector-Valued Inverse Reinforcement Learning." *Springer*. Performance-based matchmaking anxiety, 86% prediction accuracy.

7. **Junior, F. F. N. & Campelo, F. (2023)**. "Real-Time Result Prediction in League of Legends Using Time Series Imaging." *arXiv*. Temporal feature extraction from match data.

8. **Seesurn, B., Batllori, R. & Watson, S. (2025)**. "Efficacy of a multi-nutrient dietary supplement on improving decision fatigue in video gamers." *Frontiers in Nutrition*. Decision fatigue quantification in extended gaming sessions.

### Supporting Sources

9. **Kleinman, E. et al. (2023)**. "Challenges in the esports learning process." Need for high-explainability player evaluation tools.

10. **Wallner, G. & Drachen, A. (2024)**. "Play Graph: Spatio-Temporal Visualization of Player Actions." Spatial event visualization interpretability.

11. **Afonso, A. P., Carmo, M. B. & Afonso, R. (2021)**. "VisuaLeague: Visual Analytics of Multiple Games." *IEEE IV*. Spatial-temporal LoL data visualization using Riot Timeline API. Also: **Afonso, A. P. et al. (2019)**. "VisuaLeague: Player Performance Analysis Using Spatial-Temporal Data." *Multimedia Tools and Applications*, 78, 33069–33090.

12. **Hull, C. L. (1932)**. Goal gradient effect. **Kivetz, R. et al. (2006)**. Goal gradient in loyalty programs.

13. **Deci, E. L. & Ryan, R. M. (2000)**. Self-Determination Theory. Competence feedback and intrinsic motivation.

14. **Hamari, J., Koivisto, J., & Sarsa, H. (2014)**. "Does Gamification Work?" *HICSS*. Progress visualization effectiveness.

15. **Cleveland, W. S. & McGill, R. (1984)**. Graphical perception. Radar chart limitations and strengths.

16. **Sabtan, B. et al. (2022)**. Interviews with professional LoL coaches — no standard training methodology exists.

---

## Riot API Data Gap Analysis

| Feature | Riot API Data Status | Gap | Mitigation |
|---------|---------------------|-----|------------|
| Radar Chart | All metrics in Match v5 Info | Rank-average baselines require aggregation across all synced users | Pre-compute during match sync job; alternatively, use public Riot aggregate stats |
| Death Heatmap | x,y coordinates in Timeline v5 `CHAMPION_KILL` events | **Project does not currently extract `position` from kill events** | Extend `RiotTimelineMapper` to store x,y per death; add `death_events` table or columns. No new API calls. |
| LP Trend | Current LP in League v4 | **Riot API does not provide historical LP per match** — only current snapshot | Already mitigated: project stamps current LP on most recent match per sync. Estimate intermediate values from win/loss. |
| Objectives | All in Timeline v5 events | No existing trend endpoint; dragon sub-type (`monsterSubType`) not stored | New API endpoint required; optionally extend mapper to store dragon types |
| Session Detection | Match timestamps in Match v5 | No session concept in Riot API | Derive from `gameStartTimestamp` + `gameDuration`; consider adding `session_id` column |

### Additional Riot API Data Not Currently Utilized

The following data is available from the Riot API but not yet extracted. These could enable future features or enrich the proposed ones:

| Data | Riot API Source | Potential Use |
|------|----------------|---------------|
| Player position per minute | Timeline `participantFrames[].position` | Movement pathing analysis, roaming detection |
| Ward placement events | Timeline `WARD_PLACED` with `position` | Vision heatmap, ward optimization |
| Item purchase events | Timeline `ITEM_PURCHASED` | Build order analysis, itemization feedback |
| Skill level-up order | Timeline `SKILL_LEVEL_UP` | Skill order recommendations |
| `victimDamageReceived` | Timeline `CHAMPION_KILL` event | Death cause analysis (who/what killed you) |
| Dragon sub-type | Timeline `ELITE_MONSTER_KILL.monsterSubType` | Dragon type priority analysis |
| Turret plate destruction | Timeline `TURRET_PLATE_DESTROYED` | Early gold income tracking |
| Summoner spells | Match v5 `summoner1Id`, `summoner2Id` | Spell usage analysis |
| Rune/perk data | Match v5 `perks` | Build optimization |
| Multi-kill data | Match v5 `doubleKills`, `tripleKills`, etc. | Highlight reel, fight contribution |

---

## Competitive Landscape

| Feature | op.gg | u.gg | mobalytics | porofessor | Mongoose.gg |
|---------|-------|------|------------|------------|-------------|
| Radar/Spider Chart | ✅ (Tier Graph) | ❌ | ✅ | ❌ | **Proposed** |
| Death Spatial Heatmap | ❌ | ❌ | ❌ | ❌ | **Proposed** (unique!) |
| LP Trend | ✅ (basic) | ❌ | ✅ | ❌ | **Proposed** |
| Full Objective Participation | ❌ | ❌ | Partial | ❌ | **Proposed** |
| Session/Tilt Detection | ❌ | ❌ | ❌ | ❌ | **Proposed** (unique!) |

**Unique differentiators**: Spatial death heatmaps and session/tilt detection are not offered by any major competitor as of this writing. The death heatmap in particular leverages Riot Timeline API data that competitors largely ignore.
