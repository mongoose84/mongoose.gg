-- ============================================================================
-- MIGRATION: Analytics Events V2 Schema
-- Date: 2026-05-17
-- Purpose: Add normalized analytics_events_v2 table with versioning support,
--          strict validation, and retention management
-- Compatibility: Maintains legacy analytics_events table during transition
-- ============================================================================

-- Create analytics_events_v2 table (normalized schema)
-- Optimized for time-range queries, event filtering, and retention purging
CREATE TABLE IF NOT EXISTS analytics_events_v2 (
  id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT 'Unique event ID',
  event_id CHAR(36) UNIQUE COMMENT 'UUID for idempotency (optional, client-provided)',
  
  -- Identifiers
  user_id BIGINT UNSIGNED NULL COMMENT 'User ID (NULL for anonymous)',
  session_id VARCHAR(64) NULL COMMENT 'Client session ID for grouping',
  
  -- Event Definition (indexed for queries)
  event_name VARCHAR(100) NOT NULL COMMENT 'Event type from schema registry',
  event_category VARCHAR(50) NOT NULL COMMENT 'Event category (navigation, auth, feature, etc.)',
  event_version INT NOT NULL DEFAULT 1 COMMENT 'Schema version',
  
  -- User Metadata
  tier VARCHAR(20) NOT NULL DEFAULT 'free' COMMENT 'User tier at event time (free|pro)',
  
  -- Payload (JSON normalized)
  payload_json LONGTEXT NULL COMMENT 'Event-specific payload (max 4KB)',
  
  -- Metadata (optional)
  client_version VARCHAR(50) NULL COMMENT 'Client app version',
  user_agent_hash VARCHAR(64) NULL COMMENT 'Hash of user agent for analytics',
  ip_anonymized VARCHAR(50) NULL COMMENT 'Anonymized IP for geo tracking (optional)',
  
  -- Timestamps (all UTC)
  client_timestamp_utc DATETIME NULL COMMENT 'Client timestamp (for skew detection)',
  server_timestamp_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Server timestamp (used for retention)',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Insertion time (denormalized for sorting)',
  
  -- Validation/Status (for observability)
  rejection_reason VARCHAR(100) NULL COMMENT 'If event was rejected: reason code',
  payload_size_bytes INT NULL COMMENT 'Serialized payload size (for monitoring)',
  
  -- Indexes (optimized for queries and retention)
  INDEX idx_event_name_created (event_name, created_at),
  INDEX idx_user_id_created (user_id, created_at),
  INDEX idx_session_id (session_id),
  INDEX idx_created_at (created_at),
  INDEX idx_event_category (event_category),
  INDEX idx_tier_created (tier, created_at),
  INDEX idx_server_timestamp (server_timestamp_utc),
  
  -- Retention purge support
  INDEX idx_retention_purge (event_category, server_timestamp_utc),
  
  CONSTRAINT fk_user_id FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Versioned analytics events with strict schema validation';

-- Create rejection tracking table (optional, for observability)
CREATE TABLE IF NOT EXISTS analytics_event_rejections (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  event_name VARCHAR(100) NOT NULL,
  rejection_reason VARCHAR(100) NOT NULL COMMENT 'Enum: MissingEventName, EventNameTooLong, etc.',
  payload_preview VARCHAR(500) NULL COMMENT 'First 500 chars of attempted payload (for debugging)',
  session_id VARCHAR(64) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  INDEX idx_rejection_reason_created (rejection_reason, created_at),
  INDEX idx_created_at (created_at),
  INDEX idx_event_name (event_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Rejection tracking for pipeline observability';

-- Create retention policy table (for configuration)
CREATE TABLE IF NOT EXISTS analytics_retention_policies (
  id INT AUTO_INCREMENT PRIMARY KEY,
  event_category VARCHAR(50) NOT NULL UNIQUE COMMENT 'Event category (navigation, auth, etc.)',
  retention_days INT NOT NULL COMMENT 'Days to retain events',
  purge_enabled BOOLEAN NOT NULL DEFAULT 1 COMMENT 'Enable auto-purge',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  
  INDEX idx_event_category (event_category)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Retention policy per event category';

-- Seed retention policies from schema registry
INSERT INTO analytics_retention_policies (event_category, retention_days, purge_enabled)
VALUES
  ('system', 7, 1),
  ('navigation', 90, 1),
  ('auth', 365, 1),
  ('feature', 90, 1),
  ('engagement', 180, 1),
  ('premium', 365, 1)
ON DUPLICATE KEY UPDATE retention_days = VALUES(retention_days);

-- ============================================================================
-- VIEWS FOR BACKWARD COMPATIBILITY AND REPORTING
-- ============================================================================

-- Materialized summary view (for dashboards)
-- Updated hourly via scheduled job
CREATE TABLE IF NOT EXISTS analytics_event_summary (
  id INT AUTO_INCREMENT PRIMARY KEY,
  event_name VARCHAR(100) NOT NULL,
  event_category VARCHAR(50) NOT NULL,
  date_hour DATETIME NOT NULL,
  count_total INT NOT NULL DEFAULT 0,
  count_authenticated INT NOT NULL DEFAULT 0,
  count_free_tier INT NOT NULL DEFAULT 0,
  count_pro_tier INT NOT NULL DEFAULT 0,
  unique_users INT NOT NULL DEFAULT 0,
  unique_sessions INT NOT NULL DEFAULT 0,
  avg_payload_size_bytes FLOAT DEFAULT 0,
  
  UNIQUE KEY uniq_event_hour (event_name, date_hour),
  INDEX idx_date_hour (date_hour),
  INDEX idx_event_category (event_category)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Hourly summary for fast dashboard queries';

-- ============================================================================
-- LEGACY COMPATIBILITY VIEW
-- Provides read compatibility for tools expecting old analytics_events schema
-- ============================================================================
CREATE OR REPLACE VIEW analytics_events_v2_compat AS
SELECT
  id,
  user_id,
  tier,
  event_name,
  payload_json,
  session_id,
  created_at
FROM analytics_events_v2
WHERE rejection_reason IS NULL
ORDER BY created_at DESC;

-- ============================================================================
-- STORED PROCEDURES FOR OPERATIONS
-- ============================================================================

-- Purge old events based on retention policy
DELIMITER $$
CREATE PROCEDURE IF NOT EXISTS sp_purge_old_events()
LANGUAGE SQL
DETERMINISTIC
READS SQL DATA
BEGIN
  DECLARE v_affected_rows INT DEFAULT 0;
  
  -- Delete events beyond retention period
  DELETE ae FROM analytics_events_v2 ae
  INNER JOIN analytics_retention_policies rp ON ae.event_category = rp.event_category
  WHERE rp.purge_enabled = 1
    AND ae.server_timestamp_utc < DATE_SUB(UTC_TIMESTAMP, INTERVAL rp.retention_days DAY);
  
  SET v_affected_rows = ROW_COUNT();
  
  -- Log purge operation
  INSERT INTO analytics_event_rejections (event_name, rejection_reason, created_at)
  VALUES ('system:purge', CONCAT('PURGE_COMPLETED:', v_affected_rows), UTC_TIMESTAMP);
END$$
DELIMITER ;

-- Get event acceptance statistics
DELIMITER $$
CREATE PROCEDURE IF NOT EXISTS sp_get_event_stats(
  IN p_days INT
)
LANGUAGE SQL
READS SQL DATA
BEGIN
  SELECT
    'total_events' as metric,
    COUNT(*) as value
  FROM analytics_events_v2
  WHERE created_at >= DATE_SUB(UTC_TIMESTAMP, INTERVAL p_days DAY)
  
  UNION ALL
  
  SELECT
    'accepted_events' as metric,
    COUNT(*) as value
  FROM analytics_events_v2
  WHERE rejection_reason IS NULL
    AND created_at >= DATE_SUB(UTC_TIMESTAMP, INTERVAL p_days DAY)
  
  UNION ALL
  
  SELECT
    'rejected_events' as metric,
    COUNT(*) as value
  FROM analytics_events_v2
  WHERE rejection_reason IS NOT NULL
    AND created_at >= DATE_SUB(UTC_TIMESTAMP, INTERVAL p_days DAY)
  
  UNION ALL
  
  SELECT
    CONCAT('rejection_', rejection_reason) as metric,
    COUNT(*) as value
  FROM analytics_events_v2
  WHERE rejection_reason IS NOT NULL
    AND created_at >= DATE_SUB(UTC_TIMESTAMP, INTERVAL p_days DAY)
  GROUP BY rejection_reason;
END$$
DELIMITER ;

-- ============================================================================
-- NOTES ON MIGRATION STRATEGY
-- ============================================================================
-- 1. PHASE 1 (Current):
--    - V2 schema deployed alongside legacy analytics_events
--    - New endpoints accept v2 contract
--    - Legacy endpoints continue to work (transform v1 → v2)
--    - Both tables populated via dual-write initially
--
-- 2. PHASE 2 (Fallback):
--    - If v2 table has issues, disable dual-write
--    - Analytics continue via v1 table (backward compatible)
--    - No user-facing impact
--
-- 3. PHASE 3 (Cutover):
--    - Once v2 stability confirmed (7+ days, 99%+ acceptance)
--    - Deprecate legacy v1 ingestion
--    - Migrate v1 data to v2 archive (optional)
--    - Keep legacy analytics_events for backward compat (read-only)
--
-- 4. Performance:
--    - V2 schema optimized for time-range queries
--    - Additional indexes on event_name, created_at, tier
--    - Retention purge scheduled nightly (2 AM UTC)
