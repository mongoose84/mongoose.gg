# Database Schema v2 - LoL Improvement Tracker

## Overview

This document defines the complete Database v2 schema for the LoL Improvement Tracker, supporting solo, duo, and team analytics with AI-powered insights. The schema is designed to store Riot match data, derive performance metrics, and power advanced dashboards with minimal real-time computation.

**Schema Domains:**
1. Core Identity & Season Context
2. Match Core Data
3. Participants (Base Stats)
4. Timeline-Derived Checkpoints
5. Derived Performance Metrics
6. Objective & Macro Responsibility
7. Duo Analytics
8. Team Analytics
9. AI Snapshots

---

## 1. Core Identity & Season Context

### `users`

Stores application user accounts and authentication credentials.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `user_id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique user identifier |
| `email` | VARCHAR(255) | UNIQUE NOT NULL | User email address (login) |
| `username` | VARCHAR(50) | UNIQUE NOT NULL | Display username |
| `password_hash` | VARCHAR(255) | NOT NULL | Bcrypt/Argon2 hashed password |
| `email_verified` | BOOLEAN | DEFAULT FALSE | Whether email has been verified |
| `is_active` | BOOLEAN | DEFAULT TRUE | Account active status |
| `tier` | ENUM('free', 'pro') | DEFAULT 'free' | Subscription tier for quick access |
| `mollie_customer_id` | VARCHAR(255) | NULL | Mollie customer identifier |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Account creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP | Last update time |
| `last_login_at` | TIMESTAMP | NULL | Last successful login time |

**Indexes:**
- PRIMARY KEY: `user_id`
- UNIQUE INDEX: `idx_email` ON (`email`)
- UNIQUE INDEX: `idx_username` ON (`username`)
- UNIQUE INDEX: `idx_mollie_customer_id` ON (`mollie_customer_id`)
- INDEX: `idx_email_verified` ON (`email_verified`)
- INDEX: `idx_is_active` ON (`is_active`)
- INDEX: `idx_tier` ON (`tier`)

**Notes:**
- `password_hash` stores hashed passwords using bcrypt (recommended) or Argon2
- Never store plaintext passwords
- Use strong password requirements (min 8 chars, complexity rules)
- `tier` is denormalized from subscriptions table for fast access in queries
- `mollie_customer_id` links to Mollie payment system (European payment provider)
- Consider adding: `password_reset_token`, `password_reset_expires`, `failed_login_attempts` for security features

---

### `riot_accounts`

Stores Riot account identity information linked to user accounts.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `puuid` | VARCHAR(78) | PRIMARY KEY | Riot Player Universally Unique Identifier |
| `user_id` | BIGINT | NOT NULL | Foreign key to users |
| `game_name` | VARCHAR(100) | NOT NULL | Riot account game name (e.g., "Faker") |
| `tag_line` | VARCHAR(10) | NOT NULL | Riot account tag line (e.g., "KR1") |
| `summoner_name` | VARCHAR(100) | NOT NULL | Display name (game_name#tag_line) |
| `region` | VARCHAR(10) | NOT NULL | Region code (e.g., 'na1', 'euw1') |
| `is_primary` | BOOLEAN | DEFAULT FALSE | Whether this is the user's primary account |
| `sync_status` | ENUM('pending', 'syncing', 'completed', 'failed') | DEFAULT 'pending' | Match sync status |
| `sync_progress` | INT | NOT NULL DEFAULT 0 | Number of matches synced in current operation |
| `sync_total` | INT | NOT NULL DEFAULT 0 | Total number of matches to sync in current operation |
| `last_sync_at` | TIMESTAMP | NULL | Last successful match sync time |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP | Last update time |

**Indexes:**
- PRIMARY KEY: `puuid`
- INDEX: `idx_user_id` ON (`user_id`)
- INDEX: `idx_game_name_tag` ON (`game_name`, `tag_line`)
- INDEX: `idx_summoner_name` ON (`summoner_name`)
- INDEX: `idx_region` ON (`region`)
- INDEX: `idx_user_primary_created` ON (`user_id`, `is_primary`, `created_at`) -- covers ORDER BY is_primary DESC, created_at ASC
- INDEX: `idx_sync_status` ON (`sync_status`)
- INDEX: `idx_sync_status_updated` ON (`sync_status`, `updated_at`)

**Foreign Keys:**
- `user_id` → `users(user_id)` ON DELETE CASCADE

**Notes:**
- One user can link multiple Riot accounts
- One Riot account (puuid) belongs to one user
- `is_primary` flag identifies the main account for the user
- `sync_status` tracks match history synchronization state
- `sync_progress` and `sync_total` track the progress of match history sync jobs
- `game_name` and `tag_line` reflect new Riot ID format (gameName#tagLine)

**Source:** Riot summoner-v4 / account-v1 API

---

### `subscriptions`

Tracks user subscription status, tier, and billing information.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `subscription_id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique subscription record ID |
| `user_id` | BIGINT | NOT NULL | Foreign key to users |
| `tier` | ENUM('free', 'pro') | NOT NULL | Subscription tier |
| `status` | ENUM('active', 'trialing', 'past_due', 'canceled', 'paused') | NOT NULL | Current subscription status |
| `mollie_subscription_id` | VARCHAR(255) | NULL | Mollie subscription identifier |
| `mollie_plan_id` | VARCHAR(255) | NULL | Mollie plan identifier |
| `current_period_start` | TIMESTAMP | NULL | Current billing period start |
| `current_period_end` | TIMESTAMP | NULL | Current billing period end |
| `trial_start` | TIMESTAMP | NULL | Trial period start date |
| `trial_end` | TIMESTAMP | NULL | Trial period end date (30 days) |
| `is_founding_member` | BOOLEAN | DEFAULT FALSE | Founding member discount flag |
| `cancel_at_period_end` | BOOLEAN | DEFAULT FALSE | Whether subscription cancels at period end |
| `canceled_at` | TIMESTAMP | NULL | When subscription was canceled |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Subscription creation time |
| `updated_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP | Last update time |

**Indexes:**
- PRIMARY KEY: `subscription_id`
- INDEX: `idx_user_id` ON (`user_id`)
- UNIQUE INDEX: `idx_mollie_subscription_id` ON (`mollie_subscription_id`)
- INDEX: `idx_status` ON (`status`)
- INDEX: `idx_tier` ON (`tier`)
- INDEX: `idx_trial_end` ON (`trial_end`)
- INDEX: `idx_current_period_end` ON (`current_period_end`)

**Foreign Keys:**
- `user_id` → `users(user_id)` ON DELETE CASCADE

**Notes:**
- One user has one active subscription record (current state)
- Free tier users may not have a subscription record until they upgrade
- `status` values:
  - `active`: Paid and current
  - `trialing`: In 30-day trial period
  - `past_due`: Payment failed, grace period
  - `canceled`: Subscription ended
  - `paused`: Temporarily paused (future feature)
- Trial period: 30 days from `trial_start`
- `is_founding_member`: First 100 users get permanent €2.99/month pricing
- When subscription changes, update `users.tier` for quick access

---

### `subscription_events`

Audit log of subscription lifecycle events.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `event_id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique event record ID |
| `subscription_id` | BIGINT | NOT NULL | Foreign key to subscriptions |
| `event_type` | VARCHAR(50) | NOT NULL | Event type (created, updated, canceled, etc.) |
| `old_tier` | ENUM('free', 'pro') | NULL | Previous tier (for upgrades/downgrades) |
| `new_tier` | ENUM('free', 'pro') | NULL | New tier |
| `old_status` | VARCHAR(20) | NULL | Previous status |
| `new_status` | VARCHAR(20) | NULL | New status |
| `mollie_event_id` | VARCHAR(255) | NULL | Mollie webhook event ID |
| `metadata` | JSON | NULL | Additional event data |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Event timestamp |

**Indexes:**
- PRIMARY KEY: `event_id`
- INDEX: `idx_subscription_id` ON (`subscription_id`)
- INDEX: `idx_event_type` ON (`event_type`)
- INDEX: `idx_created_at` ON (`created_at`)
- UNIQUE INDEX: `idx_mollie_event_id` ON (`mollie_event_id`)

**Foreign Keys:**
- `subscription_id` → `subscriptions(subscription_id)` ON DELETE CASCADE

**Notes:**
- Provides full audit trail of subscription changes
- Useful for debugging billing issues and customer support
- `mollie_event_id` prevents duplicate webhook processing
- Common event types: `created`, `trial_started`, `trial_ending`, `upgraded`, `downgraded`, `payment_succeeded`, `payment_failed`, `canceled`, `renewed`

---

### `seasons`

Tracks League of Legends competitive seasons and patches.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `season_code` | VARCHAR(20) | PRIMARY KEY | Season identifier (e.g., 'S14', 'S14_SPLIT1') |
| `patch_version` | VARCHAR(20) | NOT NULL | Game patch version (e.g., '14.3.1') |
| `start_date` | DATE | NOT NULL | Season/split start date |
| `end_date` | DATE | NULL | Season/split end date (NULL for current) |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `season_code`
- INDEX: `idx_patch_version` ON (`patch_version`)
- INDEX: `idx_dates` ON (`start_date`, `end_date`)

**Notes:**
- `patch_version` is derived from `info.gameVersion` in Riot match data
- `season_code` is derived internally from patch/date mapping

---

## 2. Match Core Data

### `matches`

Core match metadata for all game types.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `match_id` | VARCHAR(50) | PRIMARY KEY | Riot match identifier |
| `queue_id` | INT | NOT NULL | Riot numeric queue identifier |
| `game_duration_sec` | INT | NOT NULL | Match duration in seconds |
| `game_start_time` | BIGINT | NOT NULL | Unix timestamp (milliseconds) |
| `patch_version` | VARCHAR(20) | NOT NULL | Game version (e.g., '14.3.1') |
| `season_code` | VARCHAR(20) | NULL | Foreign key to seasons |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `match_id`
- INDEX: `idx_queue_id` ON (`queue_id`)
- INDEX: `idx_game_start_time` ON (`game_start_time`)
- INDEX: `idx_patch_version` ON (`patch_version`)
- INDEX: `idx_season_code` ON (`season_code`)
- INDEX: `idx_queue_patch` ON (`queue_id`, `patch_version`)

**Foreign Keys:**
- `season_code` → `seasons(season_code)`

**Queue ID Mapping:**

| Queue Type | Riot queue_id | Used For |
|------------|---------------|----------|
| Ranked Solo/Duo | 420 | Primary ranked ladder |
| Ranked Flex | 440 | Team ranked |
| Normal Draft | 400 | Unranked practice |
| Normal Blind | 430 | Unranked practice |
| ARAM | 450 | Fun mode analytics |

**Notes:**
- `queue_id` stored as numeric INT for exact Riot API matching
- Application layer maps to user-friendly names (ranked_solo, ranked_flex, normal, aram, all)
- All v2 dashboard endpoints support queue filtering via `queue_id`

---

## 3. Participants (Base Stats)

### `participants`

Per-player, per-match base statistics.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique participant record ID |
| `match_id` | VARCHAR(50) | NOT NULL | Foreign key to matches |
| `puuid` | VARCHAR(78) | NOT NULL | Foreign key to riot_accounts |
| `team_id` | INT | NOT NULL | Team identifier (100 or 200) |
| `role` | VARCHAR(20) | NULL | Team position (TOP, JUNGLE, MIDDLE, BOTTOM, UTILITY) |
| `lane` | VARCHAR(20) | NULL | Lane assignment (may differ from role) |
| `champion_id` | INT | NOT NULL | Riot champion numeric ID |
| `champion_name` | VARCHAR(50) | NOT NULL | Champion name |
| `win` | BOOLEAN | NOT NULL | Match outcome for this participant |
| `kills` | INT | NOT NULL | Total kills |
| `deaths` | INT | NOT NULL | Total deaths |
| `assists` | INT | NOT NULL | Total assists |
| `creep_score` | INT | NOT NULL | Total CS (minions + neutrals) |
| `gold_earned` | INT | NOT NULL | Total gold earned |
| `time_dead_sec` | INT | NOT NULL | Total time spent dead (seconds) |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- UNIQUE INDEX: `idx_match_puuid` ON (`match_id`, `puuid`)
- INDEX: `idx_puuid` ON (`puuid`)
- INDEX: `idx_match_id` ON (`match_id`)
- INDEX: `idx_champion_id` ON (`champion_id`)
- INDEX: `idx_role` ON (`role`)
- INDEX: `idx_team_id` ON (`team_id`)

**Foreign Keys:**
- `match_id` → `matches(match_id)` ON DELETE CASCADE
- `puuid` → `riot_accounts(puuid)`

**Notes:**
- `creep_score` = `totalMinionsKilled` + `neutralMinionsKilled` from Riot API
- `role` comes from `teamPosition` (preferred)
- `lane` comes from `lane` (may differ due to lane swaps)

---

## 4. Timeline-Derived Checkpoints

### `participant_checkpoints`

Gold, CS, and XP snapshots at key minute marks for tracking leads and deficits.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique checkpoint record ID |
| `participant_id` | BIGINT | NOT NULL | Foreign key to participants |
| `minute_mark` | INT | NOT NULL | Minute of the game (10, 15, 20, 25, etc.) |
| `gold` | INT | NOT NULL | Total gold at this minute |
| `cs` | INT | NOT NULL | Total CS at this minute |
| `xp` | INT | NOT NULL | Total XP at this minute |
| `gold_diff_vs_lane` | INT | NULL | Gold difference vs lane opponent |
| `cs_diff_vs_lane` | INT | NULL | CS difference vs lane opponent |
| `is_ahead` | BOOLEAN | NULL | Whether ahead of lane opponent |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- UNIQUE INDEX: `idx_participant_minute` ON (`participant_id`, `minute_mark`)
- INDEX: `idx_participant_id` ON (`participant_id`)
- INDEX: `idx_minute_mark` ON (`minute_mark`)

**Foreign Keys:**
- `participant_id` → `participants(id)` ON DELETE CASCADE

**Notes:**
- Derived from Timeline API `participantFrames[n]` data
- Timeline frames occur every minute; select nearest frame ≥ target minute
- Lane opponent identified via matching `role` on opposite `team_id`
- Diff fields are NULL for roles without clear lane opponent (e.g., jungle)

---

## 5. Derived Performance Metrics

### `participant_metrics`

Advanced calculated metrics for damage, vision, and death timing analysis.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique metrics record ID |
| `participant_id` | BIGINT | NOT NULL UNIQUE | Foreign key to participants (1:1) |
| `kill_participation_pct` | DECIMAL(5,2) | NOT NULL | (kills + assists) / team kills * 100 |
| `damage_share_pct` | DECIMAL(5,2) | NOT NULL | damage dealt / team damage * 100 |
| `damage_taken` | INT | NOT NULL | Total damage taken |
| `damage_mitigated` | INT | NOT NULL | Damage self-mitigated |
| `vision_score` | INT | NOT NULL | Vision score from Riot |
| `vision_per_min` | DECIMAL(5,2) | NOT NULL | Vision score / (game duration / 60) |
| `deaths_pre_10` | INT | NOT NULL | Deaths before 10 minutes |
| `deaths_10_20` | INT | NOT NULL | Deaths between 10-20 minutes |
| `deaths_20_30` | INT | NOT NULL | Deaths between 20-30 minutes |
| `deaths_30_plus` | INT | NOT NULL | Deaths after 30 minutes |
| `first_death_minute` | INT | NULL | Minute of first death (NULL if no deaths) |
| `first_kill_participation_minute` | INT | NULL | Minute of first kill or assist |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- UNIQUE INDEX: `idx_participant_id` ON (`participant_id`)
- INDEX: `idx_kill_participation` ON (`kill_participation_pct`)
- INDEX: `idx_deaths_pre_10` ON (`deaths_pre_10`)

**Foreign Keys:**
- `participant_id` → `participants(id)` ON DELETE CASCADE

**Notes:**
- Kill participation and damage share require summing team totals
- Death timing fields require Timeline API `CHAMPION_KILL` events
- First death/kill participation derived from Timeline event timestamps

---

## 6. Objective & Macro Responsibility

### `team_objectives`

Team-level objective counts per match.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique team objectives record ID |
| `match_id` | VARCHAR(50) | NOT NULL | Foreign key to matches |
| `team_id` | INT | NOT NULL | Team identifier (100 or 200) |
| `dragons_taken` | INT | NOT NULL | Total dragons killed |
| `heralds_taken` | INT | NOT NULL | Total rift heralds killed |
| `barons_taken` | INT | NOT NULL | Total barons killed |
| `towers_taken` | INT | NOT NULL | Total towers destroyed |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- UNIQUE INDEX: `idx_match_team` ON (`match_id`, `team_id`)
- INDEX: `idx_match_id` ON (`match_id`)

**Foreign Keys:**
- `match_id` → `matches(match_id)` ON DELETE CASCADE

**Notes:**
- Derived from `teams[].objectives` in Riot match data
- No Timeline API needed for team totals

---

### `participant_objectives`

Individual player participation in objectives.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique participant objectives record ID |
| `participant_id` | BIGINT | NOT NULL UNIQUE | Foreign key to participants (1:1) |
| `dragons_participated` | INT | NOT NULL | Dragons killed or assisted |
| `heralds_participated` | INT | NOT NULL | Heralds killed or assisted |
| `barons_participated` | INT | NOT NULL | Barons killed or assisted |
| `towers_participated` | INT | NOT NULL | Towers destroyed or assisted |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- UNIQUE INDEX: `idx_participant_id` ON (`participant_id`)

**Foreign Keys:**
- `participant_id` → `participants(id)` ON DELETE CASCADE

**Notes:**
- Derived from Timeline API `ELITE_MONSTER_KILL` and `BUILDING_KILL` events
- Participation = killer + assistingParticipantIds
- Optional: include nearby teammates within proximity threshold

---

## 7. Duo Analytics

### `duo_metrics`

Synergy and performance metrics for two-player pairs.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique duo metrics record ID |
| `match_id` | VARCHAR(50) | NOT NULL | Foreign key to matches |
| `participant_id_1` | BIGINT | NOT NULL | Foreign key to participants |
| `participant_id_2` | BIGINT | NOT NULL | Foreign key to participants |
| `early_gold_delta_10` | INT | NULL | Combined gold lead at 10 min |
| `early_gold_delta_15` | INT | NULL | Combined gold lead at 15 min |
| `assist_synergy_pct` | DECIMAL(5,2) | NULL | % of kills where both participated |
| `shared_objective_participation_pct` | DECIMAL(5,2) | NULL | % objectives both participated in |
| `win_when_ahead_at_15` | BOOLEAN | NULL | Whether won when ahead at 15 |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- INDEX: `idx_match_id` ON (`match_id`)
- INDEX: `idx_participant_1` ON (`participant_id_1`)
- INDEX: `idx_participant_2` ON (`participant_id_2`)
- INDEX: `idx_duo_pair` ON (`participant_id_1`, `participant_id_2`)

**Foreign Keys:**
- `match_id` → `matches(match_id)` ON DELETE CASCADE
- `participant_id_1` → `participants(id)` ON DELETE CASCADE
- `participant_id_2` → `participants(id)` ON DELETE CASCADE

**Notes:**
- Only created for participants on the same team
- `early_gold_delta` derived from `participant_checkpoints`
- Assist synergy calculated from Timeline kill events where both are killer or assisters

---

## 8. Team Analytics

### `team_match_metrics`

Per-match, per-team aggregated metrics for 5-player team analysis.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique team match metrics record ID |
| `match_id` | VARCHAR(50) | NOT NULL | Foreign key to matches |
| `team_id` | INT | NOT NULL | Team identifier (100 or 200) |
| `gold_lead_at_15` | INT | NULL | Team gold lead at 15 minutes |
| `largest_gold_lead` | INT | NULL | Largest gold lead during match |
| `gold_swing_post_20` | INT | NULL | Gold difference change after 20 min |
| `win_when_ahead_at_20` | BOOLEAN | NULL | Whether won when ahead at 20 |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- UNIQUE INDEX: `idx_match_team` ON (`match_id`, `team_id`)
- INDEX: `idx_match_id` ON (`match_id`)

**Foreign Keys:**
- `match_id` → `matches(match_id)` ON DELETE CASCADE

**Notes:**
- Derived from aggregating `participant_checkpoints` across team members
- Requires Timeline API for minute-by-minute gold tracking

---

### `team_role_responsibility`

Role-specific contributions within a team for a match.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique team role record ID |
| `match_id` | VARCHAR(50) | NOT NULL | Foreign key to matches |
| `team_id` | INT | NOT NULL | Team identifier (100 or 200) |
| `role` | VARCHAR(20) | NOT NULL | Role (TOP, JUNGLE, MIDDLE, BOTTOM, UTILITY) |
| `deaths_share_pct` | DECIMAL(5,2) | NOT NULL | % of team deaths from this role |
| `gold_share_pct` | DECIMAL(5,2) | NOT NULL | % of team gold earned by this role |
| `damage_share_pct` | DECIMAL(5,2) | NOT NULL | % of team damage from this role |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- UNIQUE INDEX: `idx_match_team_role` ON (`match_id`, `team_id`, `role`)
- INDEX: `idx_match_id` ON (`match_id`)
- INDEX: `idx_role` ON (`role`)

**Foreign Keys:**
- `match_id` → `matches(match_id)` ON DELETE CASCADE

**Notes:**
- Calculated from `participants` table per team
- Useful for identifying weakest role or resource distribution issues

---

## 9. AI Snapshots

### `ai_snapshots`

Pre-aggregated statistical summaries for AI goal recommendation inputs.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `id` | BIGINT | PRIMARY KEY AUTO_INCREMENT | Unique AI snapshot record ID |
| `puuid` | VARCHAR(78) | NOT NULL | Foreign key to riot_accounts (solo) |
| `context_type` | ENUM('solo', 'duo', 'team') | NOT NULL | Analysis context |
| `context_puuids` | JSON | NULL | Array of puuids for duo/team context |
| `queue_id` | INT | NULL | Filtered queue (NULL = all queues) |
| `summary_text` | TEXT | NOT NULL | Human-readable stats summary |
| `goals_json` | JSON | NULL | AI-generated goal recommendations |
| `snapshot_date` | DATE | NOT NULL | Date snapshot was generated |
| `created_at` | TIMESTAMP | DEFAULT CURRENT_TIMESTAMP | Record creation time |

**Indexes:**
- PRIMARY KEY: `id`
- INDEX: `idx_puuid` ON (`puuid`)
- INDEX: `idx_context_type` ON (`context_type`)
- INDEX: `idx_snapshot_date` ON (`snapshot_date`)
- INDEX: `idx_puuid_context` ON (`puuid`, `context_type`, `snapshot_date`)

**Foreign Keys:**
- `puuid` → `riot_accounts(puuid)`

**Notes:**
- `context_puuids` is JSON array: `["puuid2"]` for duo, `["puuid2","puuid3","puuid4","puuid5"]` for team
- `summary_text` is input for LLM prompt
- `goals_json` is LLM response (array of goal objects)
- Snapshots can be regenerated or cached for rate limit optimization

---

## Entity Relationship Diagram

```
    ┌──────────────────┐
    │ users            │
    │  - user_id (PK)  │
    │  - email         │
    │  - password_hash │
    │  - tier          │
    └────┬─────────┬───┘
         │         │
         │ 1:1     │ 1:M
         │         ▼
         │    ┌─────────────────┐         ┌──────────┐
         │    │ riot_accounts   │         │ seasons  │
         │    │  - puuid (PK)   │         │  - code  │
         │    │  - user_id (FK) │         └────┬─────┘
         │    └────────┬────────┘              │
         │             │                       │ 1:M
         │             │ 1:M                   │
         ▼             ▼                       ▼
    ┌──────────────────────┐
    │ subscriptions        │
    │  - subscription_id   │
    │  - user_id (FK)      │
    │  - tier              │
    │  - status            │
    │  - trial_start/end   │
    │  - mollie_ids        │
    └──────────┬───────────┘
               │
               │ 1:M
               ▼
    ┌──────────────────────┐
    │ subscription_events  │
    │  - event_id          │
    │  - event_type        │
    │  - old/new tier      │
    └──────────────────────┘
    ┌────────────────────────────────┐
    │ matches                        │
    │  - match_id (PK)               │
    │  - queue_id (INT) ◄── CRITICAL │
    │  - game_start_time             │
    │  - patch_version               │
    │  - season_code (FK)            │
    └────────┬───────────────────────┘
             │
             │ 1:M
             ▼
    ┌────────────────────────┐
    │ participants           │
    │  - id (PK)             │
    │  - match_id (FK)       │
    │  - puuid (FK)          │
    │  - team_id             │
    │  - role                │
    │  - champion_id         │
    │  - kills/deaths/assists│
    └────────┬───────────────┘
             │
             ├──1:M──► participant_checkpoints
             │           - minute_mark (10,15,20,25)
             │           - gold, cs, xp
             │           - gold_diff_vs_lane
             │
             ├──1:1──► participant_metrics
             │           - kill_participation_pct
             │           - damage_share_pct
             │           - deaths_pre_10, deaths_10_20, etc.
             │
             └──1:1──► participant_objectives
                         - dragons/barons/heralds/towers participated

    ┌────────────────────────┐
    │ duo_metrics            │
    │  - match_id (FK)       │
    │  - participant_id_1    │◄──┐
    │  - participant_id_2    │◄──┤ M:M relationship
    │  - assist_synergy_pct  │   │ (same match, same team)
    └────────────────────────┘   │
                                 │
         ┌───────────────────────┘
         │
         │
    ┌────────────────────────┐
    │ team_match_metrics     │
    │  - match_id (FK)       │
    │  - team_id             │
    │  - gold_lead_at_15     │
    └────────────────────────┘

    ┌────────────────────────┐
    │ team_objectives        │
    │  - match_id (FK)       │
    │  - team_id             │
    │  - dragons/barons/etc  │
    └────────────────────────┘

    ┌────────────────────────┐
    │ team_role_responsibility│
    │  - match_id (FK)       │
    │  - team_id             │
    │  - role                │
    │  - deaths/gold/dmg %   │
    └────────────────────────┘

    ┌────────────────────────┐
    │ ai_snapshots           │
    │  - puuid (FK)          │
    │  - context_type        │
    │  - summary_text        │
    │  - goals_json          │
    └────────────────────────┘
```

---

## Index Strategy

### Critical Query Patterns & Indexes

#### 1. User Dashboard Queries (Highest Priority)

**Pattern:** Fetch all matches for a player, optionally filtered by queue
```sql
SELECT * FROM participants p
JOIN matches m ON p.match_id = m.match_id
WHERE p.puuid = ? AND m.queue_id = ?
ORDER BY m.game_start_time DESC
LIMIT 20;
```

**Required Indexes:**
- `participants.idx_puuid` ON (`puuid`) ✅
- `participants.idx_match_puuid` ON (`match_id`, `puuid`) UNIQUE ✅
- `matches.idx_queue_id` ON (`queue_id`) ✅
- `matches.idx_game_start_time` ON (`game_start_time`) ✅

**Composite Index for Optimization:**
- Consider: `matches.idx_queue_time` ON (`queue_id`, `game_start_time DESC`)

---

#### 2. Timeline Checkpoint Lookups

**Pattern:** Get gold/CS at specific minute marks across many matches
```sql
SELECT pc.* FROM participant_checkpoints pc
JOIN participants p ON pc.participant_id = p.id
WHERE p.puuid = ? AND pc.minute_mark = 15;
```

**Required Indexes:**
- `participant_checkpoints.idx_participant_minute` ON (`participant_id`, `minute_mark`) UNIQUE ✅
- `participant_checkpoints.idx_minute_mark` ON (`minute_mark`) ✅

---

#### 3. Duo Pair Analysis

**Pattern:** Find all matches where two players played together
```sql
SELECT dm.* FROM duo_metrics dm
JOIN participants p1 ON dm.participant_id_1 = p1.id
JOIN participants p2 ON dm.participant_id_2 = p2.id
WHERE p1.puuid = ? AND p2.puuid = ?;
```

**Required Indexes:**
- `duo_metrics.idx_participant_1` ON (`participant_id_1`) ✅
- `duo_metrics.idx_participant_2` ON (`participant_id_2`) ✅
- `duo_metrics.idx_duo_pair` ON (`participant_id_1`, `participant_id_2`) ✅

---

#### 4. Team Match Aggregations

**Pattern:** Team-level stats for specific matches
```sql
SELECT * FROM team_match_metrics
WHERE match_id = ? AND team_id = 100;
```

**Required Indexes:**
- `team_match_metrics.idx_match_team` ON (`match_id`, `team_id`) UNIQUE ✅

---

#### 5. Champion Performance Analysis

**Pattern:** Stats for a player on specific champion(s)
```sql
SELECT AVG(kills), AVG(deaths), AVG(assists)
FROM participants
WHERE puuid = ? AND champion_id = ?;
```

**Required Indexes:**
- `participants.idx_puuid` ON (`puuid`) ✅
- `participants.idx_champion_id` ON (`champion_id`) ✅

**Composite Index for Optimization:**
- Consider: `participants.idx_puuid_champion` ON (`puuid`, `champion_id`)

---

#### 6. AI Snapshot Retrieval

**Pattern:** Get most recent snapshot for a player/context
```sql
SELECT * FROM ai_snapshots
WHERE puuid = ? AND context_type = 'solo' AND queue_id = 420
ORDER BY snapshot_date DESC
LIMIT 1;
```

**Required Indexes:**
- `ai_snapshots.idx_puuid_context` ON (`puuid`, `context_type`, `snapshot_date`) ✅

---

### Secondary Indexes (Add Based on Usage Patterns)

| Index | Use Case | Priority |
|-------|----------|----------|
| `participants.idx_puuid_champion` ON (`puuid`, `champion_id`) | Champion-specific stats | Medium |
| `matches.idx_queue_time` ON (`queue_id`, `game_start_time DESC`) | Paginated ranked match lists | High |
| `participant_metrics.idx_deaths_pre_10` ON (`deaths_pre_10`) | Early game analysis | Medium |
| `matches.idx_patch_version` ON (`patch_version`) | Patch meta analysis | Low |

---

### Index Maintenance Strategy

1. **Monitor Query Performance:** Use `EXPLAIN` on dashboard queries monthly
2. **Track Index Usage:** Query `information_schema.INDEX_STATISTICS` to identify unused indexes
3. **Composite Index Order:** Always put most selective column first (typically `puuid` or `match_id`)
4. **Covering Indexes:** For read-heavy tables like `participant_checkpoints`, consider covering indexes that include all SELECT columns

---

## Riot API Data Validation

### Validation Status

| Schema Domain | Riot API Source | Status |
|--------------|-----------------|---------|
| **users** | Application-managed | N/A Authentication |
| **subscriptions** | Mollie webhooks | N/A Billing (European provider) |
| **subscription_events** | Mollie webhooks | N/A Audit log |
| **riot_accounts** | summoner-v4 / account-v1 | ✅ Fully validated |
| **seasons** | Derived from gameVersion | ⚠️ Application logic |
| **matches** | match-v5 info | ✅ Fully validated |
| **participants** | match-v5 info.participants[] | ✅ Fully validated |
| **participant_checkpoints** | match-v5 timeline participantFrames | ✅ Fully validated |
| **participant_metrics** | match-v5 info + timeline events | ✅ Fully validated |
| **team_objectives** | match-v5 info.teams[].objectives | ✅ Fully validated |
| **participant_objectives** | match-v5 timeline ELITE_MONSTER_KILL / BUILDING_KILL | ✅ Fully validated |
| **duo_metrics** | Derived from checkpoints + timeline | ⚠️ Application logic |
| **team_match_metrics** | Derived from checkpoints | ⚠️ Application logic |
| **team_role_responsibility** | Derived from participants | ⚠️ Application logic |
| **ai_snapshots** | Application-generated | N/A AI-powered |

### Key Derived Fields

| Field | Calculation | Source |
|-------|-------------|--------|
| `creep_score` | `totalMinionsKilled` + `neutralMinionsKilled` | match-v5 participants |
| `kill_participation_pct` | `(kills + assists) / team_kills * 100` | match-v5 participants |
| `damage_share_pct` | `totalDamageDealtToChampions / team_total * 100` | match-v5 participants |
| `vision_per_min` | `visionScore / (game_duration_sec / 60)` | match-v5 participants |
| `deaths_pre_10` | Count CHAMPION_KILL events where victim, timestamp < 600000 | timeline events |
| `first_death_minute` | Earliest CHAMPION_KILL timestamp / 60000 | timeline events |
| `gold_diff_vs_lane` | `own_gold - lane_opponent_gold` at checkpoint | timeline participantFrames |
| `assist_synergy_pct` | `shared_kill_participations / total_kills * 100` | timeline CHAMPION_KILL |

---

## Implementation Notes

### Timeline API Usage

The Timeline API is **critical** for:
- `participant_checkpoints` (gold/CS/XP at minute marks)
- `participant_metrics` (death timing, first kill participation)
- `participant_objectives` (objective participation)
- `duo_metrics` (assist synergy, shared objectives)
- `team_match_metrics` (gold leads, swings)

**Rate Limiting:**
- Timeline API has same rate limits as match-v5
- Cache timeline data after first fetch
- Process timelines asynchronously in batch after match sync

### Data Integrity Rules

1. **Cascade Deletes:** All child tables use `ON DELETE CASCADE` to maintain referential integrity
2. **Null Handling:** Lane-specific metrics (e.g., `gold_diff_vs_lane`) are NULL for jungle/ambiguous roles
3. **Team Validation:** `duo_metrics` must only link participants from the same match and team
4. **Timestamp Consistency:** All timestamps use UTC; match times use milliseconds (Riot standard)

### Performance Considerations

1. **Partitioning Strategy:** Consider partitioning `participants` by `game_start_time` (monthly) for large datasets
2. **Archival:** Move matches older than 2 seasons to archive tables
3. **Materialized Views:** Consider materialized views for common aggregations (win rates, champion stats)
4. **Read Replicas:** Use read replicas for dashboard queries to reduce load on primary DB

---

## Migration Path

### From v1 to v2

1. **Phase 1:** Create v2 schema alongside v1
2. **Phase 2:** Backfill v2 tables from existing match data (process timeline data)
3. **Phase 3:** Deploy v2 API endpoints alongside v1
4. **Phase 4:** Migrate frontend to v2 endpoints
5. **Phase 5:** Deprecate and remove v1 tables

### Data Backfill Considerations

- Timeline data may not be available for all historical matches (expired after ~2 years)
- For historical matches without timelines, populate only non-timeline-dependent tables
- Flag matches with incomplete data using a `timeline_available` boolean in `matches` table

---

## Acceptance Criteria Summary

✅ **Consolidated ERD / schema documented in docs/database_schema_v2.md**  
✅ **Tables defined for:** users, subscriptions, subscription_events, riot_accounts, matches, participants, participant_checkpoints, participant_metrics, team_objectives, participant_objectives, duo_metrics, team_match_metrics, team_role_responsibility, ai_snapshots  
✅ **User authentication:** users table with secure password storage (bcrypt/Argon2 hashed)  
✅ **Subscription management:** subscriptions table with tier tracking, trial support (30 days), Mollie integration, and founding member flags  
✅ **matches.queue_id present** (numeric Riot queue id) and used for queue filtering across v2 dashboards  
✅ **Index strategy defined** for common filters (puuid, queue_id, season/patch, team_id, minute_mark)

---

## Next Steps

Proceed to **E2: Create MySQL schema scripts** to generate the actual DDL statements from this schema definition.
