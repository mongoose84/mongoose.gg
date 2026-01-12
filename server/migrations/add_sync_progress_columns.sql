-- Migration: Add sync progress columns to riot_accounts table
-- This supports fine-grained progress tracking during match synchronization

-- Add sync_progress and sync_total columns for progress tracking
ALTER TABLE riot_accounts
    ADD COLUMN sync_progress INT NOT NULL DEFAULT 0 AFTER sync_status,
    ADD COLUMN sync_total INT NOT NULL DEFAULT 0 AFTER sync_progress;

