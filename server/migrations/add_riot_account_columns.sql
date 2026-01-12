-- Migration: Add missing columns to riot_accounts table
-- Run this if you have an existing riot_accounts table without game_name, tag_line, etc.

-- Check if columns exist first and add if missing
-- MySQL doesn't support IF NOT EXISTS for columns, so these may fail if columns exist

-- Add game_name column
ALTER TABLE riot_accounts ADD COLUMN game_name VARCHAR(100) NOT NULL DEFAULT '' AFTER user_id;

-- Add tag_line column  
ALTER TABLE riot_accounts ADD COLUMN tag_line VARCHAR(10) NOT NULL DEFAULT '' AFTER game_name;

-- Add is_primary column
ALTER TABLE riot_accounts ADD COLUMN is_primary BOOLEAN DEFAULT FALSE AFTER region;

-- Add sync_status column
ALTER TABLE riot_accounts ADD COLUMN sync_status ENUM('pending', 'syncing', 'completed', 'failed') DEFAULT 'pending' AFTER is_primary;

-- Add last_sync_at column
ALTER TABLE riot_accounts ADD COLUMN last_sync_at TIMESTAMP NULL AFTER sync_status;

-- Add indexes for new columns
ALTER TABLE riot_accounts ADD INDEX idx_game_name_tag (game_name, tag_line);
ALTER TABLE riot_accounts ADD INDEX idx_user_primary_created (user_id, is_primary, created_at);
ALTER TABLE riot_accounts ADD INDEX idx_sync_status (sync_status);

-- Update existing records to populate game_name and tag_line from summoner_name if possible
-- This assumes summoner_name format is "GameName#TagLine"
-- UPDATE riot_accounts SET 
--     game_name = SUBSTRING_INDEX(summoner_name, '#', 1),
--     tag_line = SUBSTRING_INDEX(summoner_name, '#', -1)
-- WHERE game_name = '' AND summoner_name LIKE '%#%';

