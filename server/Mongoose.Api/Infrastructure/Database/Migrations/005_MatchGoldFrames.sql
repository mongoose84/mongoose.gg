-- ============================================================================
-- MIGRATION: Match gold frames
-- Date: 2026-10-12
-- Purpose: Each team's total gold at every minute of the timeline sync already downloads, for the
--          Matches gold-lead chart (design-migration.plan.md, 4d). Matches synced before it have
--          no rows; the chart falls back to the 5-minute participant_checkpoints for them, and
--          DeathDetailWriter fills the rows whenever a timeline is (re)fetched.
-- ============================================================================

CREATE TABLE IF NOT EXISTS match_gold_frames (
    match_id VARCHAR(50) NOT NULL,
    minute SMALLINT NOT NULL COMMENT 'Timeline frame minute (0 = game start)',
    team100_gold INT NOT NULL COMMENT 'Sum of totalGold of participants 1-5',
    team200_gold INT NOT NULL COMMENT 'Sum of totalGold of participants 6-10',
    PRIMARY KEY (match_id, minute),
    CONSTRAINT fk_gold_frames_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
