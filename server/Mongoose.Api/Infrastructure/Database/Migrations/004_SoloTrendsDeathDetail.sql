-- ============================================================================
-- MIGRATION: Solo trends death detail
-- Date: 2026-09-29
-- Purpose: More detail per death and the objective events from the timeline sync already
--          downloads, for the Solo page's death zones (.github/specs/features/solo-trends.spec.md,
--          FR 31-40). Existing events stay null until DeathDetailBackfillJob re-fetches their
--          timelines; nothing is migrated in SQL.
-- ============================================================================

-- Riot's participant ID (1-10), so timeline participant IDs map to rows
ALTER TABLE participants
    ADD COLUMN riot_participant_id TINYINT NULL COMMENT 'Riot participantId 1-10';

-- Who killed, who helped and who was close, per death
ALTER TABLE participant_death_events
    ADD COLUMN timestamp_sec INT NULL COMMENT 'Seconds into the match' AFTER minute_mark,
    ADD COLUMN killer_participant_id TINYINT NULL COMMENT 'Riot participantId 1-10; null for an execute' AFTER killer_champion_id,
    ADD COLUMN assisting_participant_ids VARCHAR(40) NULL COMMENT 'Comma-separated Riot participantIds' AFTER killer_participant_id,
    ADD COLUMN allies_nearby TINYINT NULL COMMENT 'Allies within 2,000 units in the closest participant frame' AFTER assisting_participant_ids,
    ADD KEY idx_death_events_participant_time (participant_id, timestamp_sec);

-- Dragons, Barons, Heralds, Void Grubs, towers and inhibitors with their time (shared with gold-over-time markers)
CREATE TABLE IF NOT EXISTS match_objective_events (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    match_id VARCHAR(50) NOT NULL,
    team_id INT NOT NULL COMMENT 'The team that took the objective',
    type ENUM('dragon', 'baron', 'herald', 'grubs', 'tower', 'inhibitor') NOT NULL,
    subtype VARCHAR(30) NULL COMMENT 'Dragon kind or tower lane',
    timestamp_sec INT NOT NULL,
    killer_participant_id TINYINT NULL COMMENT 'Riot participantId 1-10',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    KEY idx_objective_events_match_time (match_id, timestamp_sec),
    CONSTRAINT fk_objective_events_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- When the backfill finished an account's last 50 Summoner's Rift matches
ALTER TABLE riot_accounts
    ADD COLUMN death_detail_backfilled_at DATETIME(3) NULL COMMENT 'UTC; death detail backfill done' AFTER rank_checked_at;

-- Matches Riot no longer serves a timeline for, so the backfill doesn't retry them
CREATE TABLE IF NOT EXISTS death_detail_backfill_skips (
    match_id VARCHAR(50) NOT NULL PRIMARY KEY,
    reason VARCHAR(30) NOT NULL,
    skipped_at DATETIME(3) NOT NULL COMMENT 'UTC',
    CONSTRAINT fk_backfill_skips_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
