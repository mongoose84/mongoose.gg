-- Mongoose Database schema (MySQL)
-- Idempotent for clean databases: uses CREATE TABLE IF NOT EXISTS
-- Engine: InnoDB, Charset: utf8mb4

SET NAMES utf8mb4;
SET sql_mode = 'STRICT_ALL_TABLES';

-- CORE IDENTITY & SEASON CONTEXT
CREATE TABLE IF NOT EXISTS users (
    user_id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    username VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    email_verified BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    tier ENUM('free', 'pro') DEFAULT 'free',
    mollie_customer_id VARCHAR(255) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP NULL,
    UNIQUE KEY idx_email (email),
    UNIQUE KEY idx_username (username),
    UNIQUE KEY idx_mollie_customer_id (mollie_customer_id),
    KEY idx_email_verified (email_verified),
    KEY idx_is_active (is_active),
    KEY idx_tier (tier)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Verification tokens for email verification, password reset, etc.
CREATE TABLE IF NOT EXISTS verification_tokens (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    token_type ENUM('email_verification', 'password_reset', 'email_change') NOT NULL,
    code VARCHAR(6) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    used_at TIMESTAMP NULL,
    attempts INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    INDEX idx_user_type (user_id, token_type, used_at),
    INDEX idx_expires (expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS riot_accounts (
    puuid VARCHAR(78) PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    game_name VARCHAR(100) NOT NULL,
    tag_line VARCHAR(10) NOT NULL,
    summoner_name VARCHAR(100) NOT NULL,
    region VARCHAR(10) NOT NULL,
    summoner_id VARCHAR(100) NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    sync_status ENUM('pending', 'syncing', 'completed', 'failed') DEFAULT 'pending',
    sync_progress INT NOT NULL DEFAULT 0,
    sync_total INT NOT NULL DEFAULT 0,
    profile_icon_id INT NULL,
    summoner_level INT NULL,
    solo_tier VARCHAR(20) NULL,
    solo_rank VARCHAR(10) NULL,
    solo_lp INT NULL,
    flex_tier VARCHAR(20) NULL,
    flex_rank VARCHAR(10) NULL,
    flex_lp INT NULL,
    last_sync_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY idx_user_id (user_id),
    KEY idx_game_name_tag (game_name, tag_line),
    KEY idx_summoner_name (summoner_name),
    KEY idx_region (region),
    KEY idx_user_primary_created (user_id, is_primary, created_at),
    KEY idx_sync_status_updated (sync_status, updated_at),
    CONSTRAINT fk_riot_accounts_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS subscriptions (
    subscription_id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    tier ENUM('free', 'pro') NOT NULL,
    status ENUM('active', 'trialing', 'past_due', 'canceled', 'paused') NOT NULL,
    mollie_subscription_id VARCHAR(255) NULL,
    mollie_plan_id VARCHAR(255) NULL,
    current_period_start TIMESTAMP NULL,
    current_period_end TIMESTAMP NULL,
    trial_start TIMESTAMP NULL,
    trial_end TIMESTAMP NULL,
    is_founding_member BOOLEAN DEFAULT FALSE,
    cancel_at_period_end BOOLEAN DEFAULT FALSE,
    canceled_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY idx_user_id (user_id),
    UNIQUE KEY idx_mollie_subscription_id (mollie_subscription_id),
    KEY idx_status (status),
    KEY idx_tier (tier),
    KEY idx_trial_end (trial_end),
    KEY idx_current_period_end (current_period_end),
    CONSTRAINT fk_subscriptions_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS subscription_events (
    event_id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    subscription_id BIGINT UNSIGNED NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    old_tier ENUM('free', 'pro') NULL,
    new_tier ENUM('free', 'pro') NULL,
    old_status ENUM('active', 'trialing', 'past_due', 'canceled', 'paused') NULL,
    new_status ENUM('active', 'trialing', 'past_due', 'canceled', 'paused') NULL,
    mollie_event_id VARCHAR(255) NULL,
    metadata JSON NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_subscription_id (subscription_id),
    KEY idx_event_type (event_type),
    KEY idx_created_at (created_at),
    UNIQUE KEY idx_mollie_event_id (mollie_event_id),
    CONSTRAINT fk_subscription_events_subscription FOREIGN KEY (subscription_id) REFERENCES subscriptions(subscription_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- LP SNAPSHOTS (time-series rank tracking)
-- Records LP at each sync to track rank progression over time.
-- Not tied to specific matches since Riot API only provides current rank.
CREATE TABLE IF NOT EXISTS lp_snapshots (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    puuid VARCHAR(78) NOT NULL,
    queue_type ENUM('RANKED_SOLO_5x5', 'RANKED_FLEX_SR') NOT NULL,
    tier VARCHAR(20) NOT NULL,
    division VARCHAR(10) NOT NULL,
    lp INT NOT NULL,
    recorded_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_puuid_queue_recorded (puuid, queue_type, recorded_at),
    KEY idx_puuid_recorded (puuid, recorded_at),
    KEY idx_recorded_at (recorded_at),
    CONSTRAINT fk_lp_snapshots_riot_account FOREIGN KEY (puuid) REFERENCES riot_accounts(puuid) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS seasons (
    season_code VARCHAR(20) PRIMARY KEY,
    patch_version VARCHAR(20) NOT NULL,
    start_date DATE NOT NULL,
    end_date DATE NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_patch_version (patch_version),
    KEY idx_dates (start_date, end_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- MATCH CORE DATA
CREATE TABLE IF NOT EXISTS matches (
    match_id VARCHAR(50) PRIMARY KEY,
    queue_id INT NOT NULL,
    game_duration_sec INT NOT NULL,
    game_start_time BIGINT NOT NULL,
    patch_version VARCHAR(20) NOT NULL,
    season_code VARCHAR(20) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_queue_id (queue_id),
    KEY idx_game_start_time (game_start_time),
    KEY idx_patch_version (patch_version),
    KEY idx_season_code (season_code),
    KEY idx_queue_patch (queue_id, patch_version),
    CONSTRAINT fk_matches_season FOREIGN KEY (season_code) REFERENCES seasons(season_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- PARTICIPANTS (BASE STATS)
CREATE TABLE IF NOT EXISTS participants (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    match_id VARCHAR(50) NOT NULL,
    puuid VARCHAR(78) NOT NULL,
    team_id INT NOT NULL,
    role VARCHAR(20) NULL,
    lane VARCHAR(20) NULL,
    champion_id INT NOT NULL,
    champion_name VARCHAR(50) NOT NULL,
    win BOOLEAN NOT NULL,
    kills INT NOT NULL,
    deaths INT NOT NULL,
    assists INT NOT NULL,
    creep_score INT NOT NULL,
    gold_earned INT NOT NULL,
    time_dead_sec INT NOT NULL,
    lp_after INT NULL,
    tier_after VARCHAR(20) NULL,
    rank_after VARCHAR(10) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY idx_match_puuid (match_id, puuid),
    KEY idx_puuid (puuid),
    KEY idx_match_id (match_id),
    KEY idx_champion_id (champion_id),
    KEY idx_role (role),
    KEY idx_team_id (team_id),
    CONSTRAINT fk_participants_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- TIMELINE-DERIVED CHECKPOINTS
CREATE TABLE IF NOT EXISTS participant_checkpoints (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    participant_id BIGINT UNSIGNED NOT NULL,
    minute_mark INT NOT NULL,
    gold INT NOT NULL,
    cs INT NOT NULL,
    xp INT NOT NULL,
    gold_diff_vs_lane INT NULL,
    cs_diff_vs_lane INT NULL,
    is_ahead BOOLEAN NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY idx_participant_minute (participant_id, minute_mark),
    KEY idx_participant_id (participant_id),
    KEY idx_minute_mark (minute_mark),
    CONSTRAINT fk_checkpoints_participant FOREIGN KEY (participant_id) REFERENCES participants(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- DERIVED PERFORMANCE METRICS
CREATE TABLE IF NOT EXISTS participant_metrics (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    participant_id BIGINT UNSIGNED NOT NULL UNIQUE,
    kill_participation_pct DECIMAL(5,2) NOT NULL,
    damage_share_pct DECIMAL(5,2) NOT NULL,
    damage_taken INT NOT NULL,
    damage_mitigated INT NOT NULL,
    vision_score INT NOT NULL,
    vision_per_min DECIMAL(5,2) NOT NULL,
    deaths_pre_10 INT NOT NULL,
    deaths_10_20 INT NOT NULL,
    deaths_20_30 INT NOT NULL,
    deaths_30_plus INT NOT NULL,
    first_death_minute INT NULL,
    first_kill_participation_minute INT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_kill_participation (kill_participation_pct),
    KEY idx_deaths_pre_10 (deaths_pre_10),
    CONSTRAINT fk_metrics_participant FOREIGN KEY (participant_id) REFERENCES participants(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- OBJECTIVE & MACRO RESPONSIBILITY
CREATE TABLE IF NOT EXISTS team_objectives (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    match_id VARCHAR(50) NOT NULL,
    team_id INT NOT NULL,
    dragons_taken INT NOT NULL,
    heralds_taken INT NOT NULL,
    barons_taken INT NOT NULL,
    towers_taken INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY idx_match_team (match_id, team_id),
    KEY idx_match_id (match_id),
    CONSTRAINT fk_team_objectives_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS participant_objectives (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    participant_id BIGINT UNSIGNED NOT NULL UNIQUE,
    dragons_participated INT NOT NULL,
    heralds_participated INT NOT NULL,
    barons_participated INT NOT NULL,
    towers_participated INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_participant_objectives_participant FOREIGN KEY (participant_id) REFERENCES participants(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- DUO ANALYTICS
CREATE TABLE IF NOT EXISTS duo_metrics (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    match_id VARCHAR(50) NOT NULL,
    participant_id_1 BIGINT UNSIGNED NOT NULL,
    participant_id_2 BIGINT UNSIGNED NOT NULL,
    early_gold_delta_10 INT NULL,
    early_gold_delta_15 INT NULL,
    assist_synergy_pct DECIMAL(5,2) NULL,
    shared_objective_participation_pct DECIMAL(5,2) NULL,
    win_when_ahead_at_15 BOOLEAN NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_match_id (match_id),
    KEY idx_participant_1 (participant_id_1),
    KEY idx_participant_2 (participant_id_2),
    KEY idx_duo_pair (participant_id_1, participant_id_2),
    CONSTRAINT fk_duo_metrics_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE,
    CONSTRAINT fk_duo_metrics_participant1 FOREIGN KEY (participant_id_1) REFERENCES participants(id) ON DELETE CASCADE,
    CONSTRAINT fk_duo_metrics_participant2 FOREIGN KEY (participant_id_2) REFERENCES participants(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- TEAM ANALYTICS
CREATE TABLE IF NOT EXISTS team_match_metrics (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    match_id VARCHAR(50) NOT NULL,
    team_id INT NOT NULL,
    gold_lead_at_15 INT NULL,
    largest_gold_lead INT NULL,
    gold_swing_post_20 INT NULL,
    win_when_ahead_at_20 BOOLEAN NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY idx_match_team (match_id, team_id),
    KEY idx_match_id (match_id),
    CONSTRAINT fk_team_match_metrics_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS team_role_responsibility (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    match_id VARCHAR(50) NOT NULL,
    team_id INT NOT NULL,
    role VARCHAR(20) NOT NULL,
    deaths_share_pct DECIMAL(5,2) NOT NULL,
    gold_share_pct DECIMAL(5,2) NOT NULL,
    damage_share_pct DECIMAL(5,2) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY idx_match_team_role (match_id, team_id, role),
    KEY idx_match_id (match_id),
    KEY idx_role (role),
    CONSTRAINT fk_team_role_responsibility_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- AI SNAPSHOTS
CREATE TABLE IF NOT EXISTS ai_snapshots (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    puuid VARCHAR(78) NOT NULL,
    context_type ENUM('solo', 'duo', 'team') NOT NULL,
    context_puuids JSON NULL,
    queue_id INT NULL,
    summary_text TEXT NOT NULL,
    goals_json JSON NULL,
    snapshot_date DATE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_puuid (puuid),
    KEY idx_context_type (context_type),
    KEY idx_snapshot_date (snapshot_date),
    KEY idx_puuid_context (puuid, context_type, snapshot_date),
    CONSTRAINT fk_ai_snapshots_riot_account FOREIGN KEY (puuid) REFERENCES riot_accounts(puuid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ANALYTICS EVENTS (for user behavior tracking / Grafana dashboards)
CREATE TABLE IF NOT EXISTS analytics_events (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NULL,
    tier ENUM('free', 'pro') DEFAULT 'free',
    event_name VARCHAR(100) NOT NULL,
    payload_json JSON NULL,
    session_id VARCHAR(64) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_user_id (user_id),
    KEY idx_event_name (event_name),
    KEY idx_tier (tier),
    KEY idx_created_at (created_at),
    KEY idx_session_id (session_id),
    KEY idx_event_created (event_name, created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
-- END OF SCHEMA