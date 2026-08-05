-- ============================================================================
-- MIGRATION: Riot Sign-On (RSO) identity
-- Date: 2026-08-05
-- Purpose: Allow users to authenticate with their Riot account. The PUUID
--          returned by RSO uniquely identifies the user that owns the login,
--          independent of the M:M user_riot_accounts link table.
-- ============================================================================

ALTER TABLE users
    ADD COLUMN riot_puuid VARCHAR(78) NULL AFTER mollie_customer_id,
    ADD UNIQUE KEY idx_riot_puuid (riot_puuid);
