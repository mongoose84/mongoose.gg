-- ============================================================================
-- MIGRATION: Rank snapshots
-- Date: 2026-09-28
-- Purpose: Store League-v4 rank readings so the LP after each ranked match can be
--          attributed to that match (.github/specs/features/rank-snapshots.spec.md)
-- ============================================================================

CREATE TABLE IF NOT EXISTS rank_snapshots (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    puuid VARCHAR(78) NOT NULL,
    queue_id INT NOT NULL COMMENT '420 Ranked Solo/Duo, 440 Ranked Flex',
    tier VARCHAR(20) NOT NULL,
    division VARCHAR(10) NULL,
    lp INT NOT NULL,
    wins INT NOT NULL,
    losses INT NOT NULL,
    captured_at DATETIME(3) NOT NULL COMMENT 'UTC',
    window_start_at DATETIME(3) NULL COMMENT 'UTC; pending only: the previous reading, where the match window starts',
    source ENUM('poll', 'sync', 'login') NOT NULL,
    status ENUM('baseline', 'pending', 'attributed', 'skipped', 'no_match') NOT NULL,
    match_id VARCHAR(50) NULL COMMENT 'Set when attributed',
    KEY idx_rank_snapshots_account_queue_time (puuid, queue_id, captured_at),
    KEY idx_rank_snapshots_status (status, captured_at),
    CONSTRAINT fk_rank_snapshots_account FOREIGN KEY (puuid) REFERENCES riot_accounts(puuid) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- When the account's rank was last read (poll, sync or login); the snapshot job polls by it
ALTER TABLE riot_accounts
    ADD COLUMN rank_checked_at DATETIME(3) NULL COMMENT 'UTC' AFTER last_sync_at,
    ADD KEY idx_rank_checked_at (rank_checked_at);
