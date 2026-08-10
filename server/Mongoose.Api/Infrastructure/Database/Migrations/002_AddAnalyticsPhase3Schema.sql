-- ============================================================================
-- MIGRATION: Analytics Phase 3 Schema
-- Date: 2026-05-18
-- Purpose: Add product analytics views with exploration dimensions, journey flows,
--          funnel tracking, and hourly rollups for trend analysis
-- ============================================================================

-- ============================================================================
-- 1. EVENT DIMENSIONS TABLE
-- Pre-computed dimension extraction for fast exploration queries
-- ============================================================================

CREATE TABLE IF NOT EXISTS analytics_event_dimensions (
  id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT 'Dimension record ID',
  event_id BIGINT NOT NULL UNIQUE COMMENT 'Link to analytics_events_v2',
  
  -- Event identification
  event_name VARCHAR(100) NOT NULL COMMENT 'Event type',
  event_category VARCHAR(50) NOT NULL COMMENT 'Event category',
  
  -- Navigation dimensions
  page_path VARCHAR(500) NULL COMMENT 'Current page path (from payload.page)',
  referrer_domain VARCHAR(255) NULL COMMENT 'Referrer domain (extracted)',
  referrer_path VARCHAR(500) NULL COMMENT 'Referrer path (extracted)',
  
  -- Device/Browser dimensions (parsed from user_agent_hash)
  device_type VARCHAR(20) NULL COMMENT 'mobile|tablet|desktop',
  browser_name VARCHAR(50) NULL COMMENT 'chrome|safari|firefox|edge|other',
  browser_version VARCHAR(20) NULL COMMENT 'e.g., 120.0',
  os_name VARCHAR(50) NULL COMMENT 'windows|mac|ios|android|linux|other',
  os_version VARCHAR(20) NULL COMMENT 'e.g., 14.1',
  
  -- Geography dimensions (optional, from IP geo service)
  country_code CHAR(2) NULL COMMENT 'ISO 3166-1 alpha-2',
  region_code VARCHAR(10) NULL COMMENT 'State/Province code',
  city VARCHAR(100) NULL COMMENT 'City name',
  
  -- User segment dimensions
  tier VARCHAR(20) NOT NULL COMMENT 'free|pro|premium',
  is_authenticated BOOLEAN NOT NULL DEFAULT 0 COMMENT 'Has user_id',
  
  -- Custom properties (JSON from payload, extracted keys)
  custom_properties JSON NULL COMMENT '{key: value, ...}',
  
  -- User context
  user_id BIGINT UNSIGNED NULL COMMENT 'User ID (nullable for anonymous)',
  session_id VARCHAR(64) NULL COMMENT 'Client session ID',
  
  -- Timestamps
  event_timestamp_utc DATETIME NOT NULL COMMENT 'When event occurred',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Extraction time',
  
  -- Indexes for exploration queries
  INDEX idx_event_name_timestamp (event_name, event_timestamp_utc),
  INDEX idx_page_path_timestamp (page_path, event_timestamp_utc),
  INDEX idx_device_type_timestamp (device_type, event_timestamp_utc),
  INDEX idx_browser_name_timestamp (browser_name, event_timestamp_utc),
  INDEX idx_country_code_timestamp (country_code, event_timestamp_utc),
  INDEX idx_tier_timestamp (tier, event_timestamp_utc),
  INDEX idx_session_id (session_id),
  INDEX idx_user_id (user_id),
  INDEX idx_category_timestamp (event_category, event_timestamp_utc),
  INDEX idx_created_at (created_at),
  
  CONSTRAINT fk_event_id FOREIGN KEY (event_id) 
    REFERENCES analytics_events_v2(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Pre-computed event dimensions for exploration queries';

-- ============================================================================
-- 2. JOURNEY STEPS TABLE
-- User navigation flow tracking
-- ============================================================================

CREATE TABLE IF NOT EXISTS analytics_journey_steps (
  id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT 'Step record ID',
  session_id VARCHAR(64) NOT NULL COMMENT 'User session identifier',
  user_id BIGINT UNSIGNED NULL COMMENT 'User ID (nullable for anonymous sessions)',
  
  -- Journey sequence
  step_number INT NOT NULL COMMENT 'Sequence number (1, 2, 3, ...)',
  source_page VARCHAR(500) NULL COMMENT 'Previous page path',
  destination_page VARCHAR(500) NOT NULL COMMENT 'Current page path',
  event_name VARCHAR(100) NULL COMMENT 'Navigation event (e.g., page_view)',
  
  -- Timing information
  transition_timestamp_utc DATETIME NOT NULL COMMENT 'When transition occurred',
  time_on_previous_page_seconds INT NULL COMMENT 'Dwell time on source page',
  
  -- Context at time of transition
  device_type VARCHAR(20) NULL COMMENT 'mobile|tablet|desktop',
  tier VARCHAR(20) NOT NULL COMMENT 'free|pro|premium',
  
  -- Metadata
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation time',

  -- Indexes for journey queries
  INDEX idx_session_id (session_id),
  INDEX idx_user_id (user_id),
  INDEX idx_timestamp (transition_timestamp_utc),
  INDEX idx_session_step (session_id, step_number),
  INDEX idx_destination_page (destination_page),
  INDEX idx_tier_timestamp (tier, transition_timestamp_utc),
  
  CONSTRAINT fk_user_id_journey FOREIGN KEY (user_id)
    REFERENCES users(user_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='User navigation journey flow';

-- ============================================================================
-- 3. FUNNEL STEPS TABLE
-- Funnel conversion tracking for analysis
-- ============================================================================

CREATE TABLE IF NOT EXISTS analytics_funnel_steps (
  id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT 'Funnel step record ID',
  funnel_name VARCHAR(100) NOT NULL COMMENT 'Funnel identifier (e.g., auth_to_feature)',
  session_id VARCHAR(64) NOT NULL COMMENT 'User session',
  user_id BIGINT UNSIGNED NULL COMMENT 'User ID',
  
  -- Step tracking
  step_number INT NOT NULL COMMENT 'Step position in funnel (1, 2, 3, ...)',
  step_name VARCHAR(100) NOT NULL COMMENT 'Step identifier (e.g., auth, dashboard, feature)',
  event_name VARCHAR(100) NULL COMMENT 'Triggering event name',
  completed BOOLEAN NOT NULL DEFAULT 0 COMMENT 'Step completed?',
  
  -- Timing
  completed_at_utc DATETIME NULL COMMENT 'When step completed',
  step_timestamp_utc DATETIME NOT NULL COMMENT 'When step was first seen',
  time_since_previous_step_seconds INT NULL COMMENT 'Duration from prev step',
  
  -- Context
  tier VARCHAR(20) NOT NULL COMMENT 'free|pro|premium at time of step',
  device_type VARCHAR(20) NULL COMMENT 'mobile|tablet|desktop',
  
  -- Metadata
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

  -- Indexes for funnel analysis
  INDEX idx_funnel_name (funnel_name),
  INDEX idx_session_id (session_id),
  INDEX idx_user_id (user_id),
  INDEX idx_completed (completed),
  INDEX idx_funnel_session (funnel_name, session_id),
  INDEX idx_completed_at (completed_at_utc),
  INDEX idx_timestamp (step_timestamp_utc),
  INDEX idx_tier_timestamp (tier, completed_at_utc),
  
  CONSTRAINT fk_user_id_funnel FOREIGN KEY (user_id)
    REFERENCES users(user_id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Funnel step tracking for conversion analysis';

-- ============================================================================
-- 4. HOURLY ROLLUP TABLE
-- Pre-aggregated hourly event statistics for trends
-- ============================================================================

CREATE TABLE IF NOT EXISTS analytics_rollup_hourly (
  id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT 'Rollup record ID',
  
  -- Time and event identification
  date_hour DATETIME NOT NULL COMMENT 'Hour boundary (YYYY-MM-DD HH:00:00 UTC)',
  event_name VARCHAR(100) NOT NULL COMMENT 'Event type',
  event_category VARCHAR(50) NOT NULL COMMENT 'Event category',
  
  -- Total counts
  event_count BIGINT NOT NULL DEFAULT 0 COMMENT 'Total events in hour',
  unique_users INT NOT NULL DEFAULT 0 COMMENT 'Unique user_ids',
  unique_sessions INT NOT NULL DEFAULT 0 COMMENT 'Unique session_ids',
  avg_payload_size_bytes FLOAT NULL DEFAULT 0 COMMENT 'Average event payload size',
  
  -- Tier breakdown
  count_authenticated BIGINT NOT NULL DEFAULT 0 COMMENT 'Events with user_id',
  count_authenticated_unique_users INT NOT NULL DEFAULT 0 COMMENT 'Unique auth users',
  count_free_tier BIGINT NOT NULL DEFAULT 0 COMMENT 'Free tier events',
  count_pro_tier BIGINT NOT NULL DEFAULT 0 COMMENT 'Pro tier events',
  unique_free_tier_users INT NOT NULL DEFAULT 0 COMMENT 'Free tier users',
  unique_pro_tier_users INT NOT NULL DEFAULT 0 COMMENT 'Pro tier users',
  
  -- Device breakdown
  count_desktop BIGINT NOT NULL DEFAULT 0 COMMENT 'Desktop events',
  count_mobile BIGINT NOT NULL DEFAULT 0 COMMENT 'Mobile events',
  count_tablet BIGINT NOT NULL DEFAULT 0 COMMENT 'Tablet events',
  
  -- Geography (JSON for top countries)
  top_countries JSON NULL COMMENT '[{country: "US", count: 100}, ...]',
  
  -- Metadata
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Rollup calculation time',
  
  -- Indexes for rollup queries
  UNIQUE KEY uniq_hour_event (date_hour, event_name),
  INDEX idx_date_hour (date_hour),
  INDEX idx_event_name (event_name),
  INDEX idx_event_category (event_category),
  INDEX idx_category_hour (event_category, date_hour)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Hourly event aggregates for trend analysis';

-- ============================================================================
-- 5. FUNNEL CONFIGURATION TABLE
-- Define which funnels to track
-- ============================================================================

CREATE TABLE IF NOT EXISTS analytics_funnel_definitions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  funnel_name VARCHAR(100) NOT NULL UNIQUE COMMENT 'Funnel identifier',
  display_name VARCHAR(255) NOT NULL COMMENT 'Human-readable name',
  description TEXT NULL COMMENT 'Purpose and context',
  enabled BOOLEAN NOT NULL DEFAULT 1 COMMENT 'Track this funnel?',
  
  -- Step definitions (JSON array)
  steps JSON NOT NULL COMMENT '[{step: 1, name: "auth", eventName: "auth:login"}, ...]',
  
  -- Timing constraints
  max_time_between_steps_hours INT NOT NULL DEFAULT 24 COMMENT 'Max hours between steps',
  
  -- Metadata
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  
  INDEX idx_enabled (enabled)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Funnel definitions for tracking';

-- Seed initial funnel: Auth → Dashboard → Feature
INSERT INTO analytics_funnel_definitions (funnel_name, display_name, description, steps, max_time_between_steps_hours)
VALUES (
  'auth_to_feature',
  'Authentication to Feature Activation',
  'Core user journey: login → view dashboard → take feature action',
  JSON_ARRAY(
    JSON_OBJECT('step', 1, 'name', 'auth', 'eventName', 'auth:login_success'),
    JSON_OBJECT('step', 2, 'name', 'dashboard', 'eventName', 'navigation:dashboard_viewed'),
    JSON_OBJECT('step', 3, 'name', 'feature', 'eventName', 'feature:core_action')
  ),
  24
)
ON DUPLICATE KEY UPDATE display_name = VALUES(display_name);

-- ============================================================================
-- 6. DIMENSION EXTRACTION STATUS TABLE (optional, for monitoring)
-- Tracks which events have been processed for dimensions
-- ============================================================================

CREATE TABLE IF NOT EXISTS analytics_dimension_extraction_status (
  id INT AUTO_INCREMENT PRIMARY KEY,
  status_key VARCHAR(100) NOT NULL UNIQUE COMMENT 'Last processed event ID or timestamp',
  last_event_id BIGINT NOT NULL DEFAULT 0 COMMENT 'Highest event_id processed',
  last_processed_at DATETIME NOT NULL COMMENT 'Last extraction time',
  events_processed INT NOT NULL DEFAULT 0 COMMENT 'Events in last batch',
  extraction_duration_ms INT NOT NULL DEFAULT 0 COMMENT 'Time taken (ms)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Dimension extraction job tracking';

-- ============================================================================
-- 7. STORED PROCEDURES FOR OPERATIONS
-- ============================================================================

-- Populate analytics_rollup_hourly for a given hour
DELIMITER $$
CREATE PROCEDURE IF NOT EXISTS sp_populate_hourly_rollup(
  IN p_date_hour DATETIME
)
LANGUAGE SQL
DETERMINISTIC
READS SQL DATA
BEGIN
  DECLARE v_date_hour_start DATETIME;
  DECLARE v_date_hour_end DATETIME;
  
  SET v_date_hour_start = DATE_FORMAT(p_date_hour, '%Y-%m-%d %H:00:00');
  SET v_date_hour_end = DATE_ADD(v_date_hour_start, INTERVAL 1 HOUR);
  
  -- Clear any existing data for this hour (in case of re-run)
  DELETE FROM analytics_rollup_hourly 
  WHERE date_hour = v_date_hour_start;
  
  -- Insert aggregated data
  INSERT INTO analytics_rollup_hourly (
    date_hour, event_name, event_category,
    event_count, unique_users, unique_sessions,
    count_authenticated, count_authenticated_unique_users,
    count_free_tier, count_pro_tier,
    unique_free_tier_users, unique_pro_tier_users,
    count_desktop, count_mobile, count_tablet
  )
  SELECT
    v_date_hour_start as date_hour,
    ed.event_name,
    ed.event_category,
    COUNT(*) as event_count,
    COUNT(DISTINCT ed.user_id) as unique_users,
    COUNT(DISTINCT ed.session_id) as unique_sessions,
    SUM(CASE WHEN ed.user_id IS NOT NULL THEN 1 ELSE 0 END) as count_authenticated,
    SUM(CASE WHEN ed.user_id IS NOT NULL THEN 1 ELSE 0 END) as count_authenticated_unique_users,
    SUM(CASE WHEN ed.tier = 'free' THEN 1 ELSE 0 END) as count_free_tier,
    SUM(CASE WHEN ed.tier = 'pro' THEN 1 ELSE 0 END) as count_pro_tier,
    COUNT(DISTINCT CASE WHEN ed.tier = 'free' THEN ed.user_id END) as unique_free_tier_users,
    COUNT(DISTINCT CASE WHEN ed.tier = 'pro' THEN ed.user_id END) as unique_pro_tier_users,
    SUM(CASE WHEN ed.device_type = 'desktop' THEN 1 ELSE 0 END) as count_desktop,
    SUM(CASE WHEN ed.device_type = 'mobile' THEN 1 ELSE 0 END) as count_mobile,
    SUM(CASE WHEN ed.device_type = 'tablet' THEN 1 ELSE 0 END) as count_tablet
  FROM analytics_event_dimensions ed
  WHERE ed.event_timestamp_utc >= v_date_hour_start
    AND ed.event_timestamp_utc < v_date_hour_end
  GROUP BY ed.event_name, ed.event_category;
END$$
DELIMITER ;

-- Check dimension extraction progress
DELIMITER $$
CREATE PROCEDURE IF NOT EXISTS sp_get_dimension_extraction_status()
LANGUAGE SQL
READS SQL DATA
BEGIN
  SELECT
    (SELECT COUNT(*) FROM analytics_events_v2) as total_events,
    (SELECT COUNT(*) FROM analytics_event_dimensions) as extracted_dimensions,
    (SELECT MAX(id) FROM analytics_events_v2) as latest_event_id,
    (SELECT IFNULL(MAX(last_event_id), 0) FROM analytics_dimension_extraction_status) as last_extracted_event_id,
    ROUND(100.0 * (SELECT IFNULL(MAX(last_event_id), 0) FROM analytics_dimension_extraction_status) / 
      (SELECT COUNT(*) FROM analytics_events_v2), 2) as extraction_progress_pct,
    (SELECT last_processed_at FROM analytics_dimension_extraction_status LIMIT 1) as last_extraction_time;
END$$
DELIMITER ;

-- ============================================================================
-- 8. VIEWS FOR ANALYTICS QUERIES
-- ============================================================================

-- Event popularity view (top events by count)
CREATE OR REPLACE VIEW analytics_event_popularity_7d AS
SELECT
  ed.event_name,
  ed.event_category,
  COUNT(*) as total_events,
  COUNT(DISTINCT ed.user_id) as unique_users,
  COUNT(DISTINCT ed.session_id) as unique_sessions,
  MAX(ed.event_timestamp_utc) as last_occurred,
  ROUND(100.0 * SUM(CASE WHEN ed.tier = 'pro' THEN 1 ELSE 0 END) / COUNT(*), 2) as pro_tier_pct
FROM analytics_event_dimensions ed
WHERE ed.event_timestamp_utc >= DATE_SUB(UTC_TIMESTAMP, INTERVAL 7 DAY)
GROUP BY ed.event_name, ed.event_category
ORDER BY total_events DESC;

-- Device distribution view
CREATE OR REPLACE VIEW analytics_device_distribution_30d AS
SELECT
  ed.device_type,
  ed.browser_name,
  ed.os_name,
  COUNT(*) as event_count,
  COUNT(DISTINCT ed.user_id) as unique_users,
  ROUND(100.0 * COUNT(*) / SUM(COUNT(*)) OVER(), 2) as pct_of_total
FROM analytics_event_dimensions ed
WHERE ed.event_timestamp_utc >= DATE_SUB(UTC_TIMESTAMP, INTERVAL 30 DAY)
GROUP BY ed.device_type, ed.browser_name, ed.os_name
ORDER BY event_count DESC;

-- Tier distribution view
CREATE OR REPLACE VIEW analytics_tier_distribution_7d AS
SELECT
  ed.tier,
  COUNT(*) as event_count,
  COUNT(DISTINCT ed.user_id) as unique_users,
  COUNT(DISTINCT ed.session_id) as unique_sessions,
  ROUND(100.0 * COUNT(*) / SUM(COUNT(*)) OVER(), 2) as pct_of_total,
  ed.event_name
FROM analytics_event_dimensions ed
WHERE ed.event_timestamp_utc >= DATE_SUB(UTC_TIMESTAMP, INTERVAL 7 DAY)
GROUP BY ed.tier, ed.event_name;

-- ============================================================================
-- MIGRATION COMPLETE
-- ============================================================================

-- Add marker record to indicate Phase 3 migration has been run
INSERT INTO analytics_event_rejections (event_name, rejection_reason, created_at)
VALUES ('system:migration', 'PHASE_3_SCHEMA_CREATED', UTC_TIMESTAMP)
ON DUPLICATE KEY UPDATE created_at = UTC_TIMESTAMP;
