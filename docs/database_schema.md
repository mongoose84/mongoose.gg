1. Core Identity & Season Context
riot_accounts
Field	Riot API	Status
puuid	summoner-v4 / account-v1	✅
summoner_name	summoner-v4.name	✅
region	App config	✅

✔ Fully valid.

seasons
Field	Riot API	Status
season_code	Derived (patch/date)	⚠️
patch_version	info.gameVersion	✅

📌 Recommendation
Use gameVersion (e.g. 14.3.1) → map to season internally.

2. Match Core Data
matches
Field	Riot API	Status
match_id	matchId	✅
queue_id	info.queueId	✅
game_duration_sec	info.gameDuration	✅
game_start_time	info.gameStartTimestamp	✅
patch_version	info.gameVersion	✅

📌 `queue_id` is numeric (Riot queueId). Prefer storing it as-is.

Derive a user-friendly queue grouping (e.g. ranked_solo/ranked_flex/normal/aram) in the API/domain layer.

Suggested queue groupings:

| Group | Riot queue ids |
|------|----------------|
| ranked_solo | 420 |
| ranked_flex | 440 |
| ranked | 420, 440 |
| normal | 400, 430 |
| aram | 450 |
| all | (no filter) |

3. Participants (Base Stats)
participants

All of these come directly from info.participants[].

Field	Riot API	Status
puuid	participant.puuid	✅
team_id	participant.teamId	✅
role	participant.teamPosition	✅
lane	participant.lane	✅
champion_id	participant.championId	✅
champion_name	participant.championName	✅
win	participant.win	✅
kills	participant.kills	✅
deaths	participant.deaths	✅
assists	participant.assists	✅
creep_score	totalMinionsKilled + neutralMinionsKilled	⚠️
gold_earned	participant.goldEarned	✅
time_dead_sec	participant.totalTimeSpentDead	✅

✔ Fully valid.
⚠️ CS must be summed, which you already do.

4. Timeline-Derived Checkpoints (CRITICAL)
participant_checkpoints
Field	Riot API	Status
minute_mark	Timeline frame timestamp	⚠️
gold	participantFrames[n].totalGold	✅
cs	minionsKilled + jungleMinionsKilled	✅
xp	participantFrames[n].xp	✅
gold_diff_vs_lane	Derived vs lane opponent	⚠️
cs_diff_vs_lane	Derived vs lane opponent	⚠️
is_ahead	Derived	⚠️

📌 Important validation

Timeline frames are every minute

You must:

Select nearest frame ≥ 10/15/20/25

Identify lane opponent via teamPosition

✔ Your schema matches Riot perfectly for a snapshot-based model.

5. Derived Performance Metrics
participant_metrics
Field	Riot API	Status
kill_participation_pct	kills+assists / team kills	⚠️
damage_share_pct	totalDamageDealtToChampions / team	⚠️
damage_taken	totalDamageTaken	✅
damage_mitigated	damageSelfMitigated	✅
vision_score	visionScore	✅
vision_per_min	derived	⚠️
deaths_pre_10	timeline death events	⚠️
deaths_10_20	timeline death events	⚠️
deaths_20_30	timeline death events	⚠️
deaths_30_plus	timeline death events	⚠️
first_death_minute	timeline event	⚠️
first_kill_participation_minute	timeline event	⚠️

📌 Key point

Deaths & kill participation timing require timeline events

Riot provides CHAMPION_KILL events with timestamps

✔ Everything is computable. No missing data.

6. Objective & Macro Responsibility
team_objectives
Field	Riot API	Status
dragons_taken	teams[].objectives.dragon.kills	✅
heralds_taken	teams[].objectives.riftHerald.kills	✅
barons_taken	teams[].objectives.baron.kills	✅
towers_taken	teams[].objectives.tower.kills	✅

✔ Fully supported from match info (no timeline needed).

participant_objectives
Field	Riot API	Status
dragons_participated	timeline ELITE_MONSTER_KILL	⚠️
heralds_participated	timeline ELITE_MONSTER_KILL	⚠️
barons_participated	timeline ELITE_MONSTER_KILL	⚠️
towers_participated	timeline BUILDING_KILL	⚠️

📌 Participation is inferred via:

Killer

Assisters

Nearby teammates (optional heuristic)

✔ This is exactly how other analytics sites do it.

7. Duo Analytics
duo_metrics
Field	Riot API	Status
early_gold_delta	derived @ 10/15	⚠️
assist_synergy_pct	assist overlap	⚠️
shared_objective_participation_pct	timeline events	⚠️
win_when_ahead_at_15	derived	⚠️

✔ Riot gives all raw signals needed.

8. Team Analytics
team_match_metrics
Field	Riot API	Status
gold_lead_at_15	derived from checkpoints	⚠️
largest_gold_lead	timeline aggregation	⚠️
gold_swing_post_20	derived	⚠️
win_when_ahead_at_20	derived	⚠️

✔ Valid — requires timeline math, not extra data.

team_role_responsibility
Field	Riot API	Status
deaths_share_pct	participant deaths / team deaths	⚠️
gold_share_pct	goldEarned / team	⚠️
damage_share_pct	damage dealt / team	⚠️

✔ Cleanly supported.

9. AI Snapshots
ai_snapshots
Field	Riot API	Status
summary_text	AI-generated	✅
goals_json	AI-generated	✅

No API dependency — this is your IP.

🚨 One Important Adjustment I Recommend
Add team_total_kills (derived, not stored raw)

Why:

Kill participation is used constantly

Avoid recomputing per AI run

You can:

Store it in team_match_metrics

Or cache per match per team

This is optional but very pragmatic.