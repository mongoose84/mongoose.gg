-- Migration: Create user_riot_accounts junction table for M:M relationship
-- This allows multiple users to link the same Riot account

-- Step 1: Create the junction table
CREATE TABLE IF NOT EXISTS user_riot_accounts (
    user_id BIGINT UNSIGNED NOT NULL,
    puuid VARCHAR(78) NOT NULL,
    is_primary BOOLEAN DEFAULT FALSE,
    linked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, puuid),
    KEY idx_puuid (puuid),
    KEY idx_user_primary (user_id, is_primary)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Step 2: Migrate existing data from riot_accounts to junction table
INSERT IGNORE INTO user_riot_accounts (user_id, puuid, is_primary, linked_at)
SELECT user_id, puuid, is_primary, created_at
FROM riot_accounts
WHERE user_id IS NOT NULL;

-- Step 3: Drop the foreign key constraint first
ALTER TABLE riot_accounts DROP FOREIGN KEY fk_riot_accounts_user;

-- Step 4: Drop indexes that reference user_id
ALTER TABLE riot_accounts DROP INDEX idx_user_id;
ALTER TABLE riot_accounts DROP INDEX idx_user_primary_created;

-- Step 5: Drop the user_id and is_primary columns from riot_accounts
ALTER TABLE riot_accounts DROP COLUMN user_id;
ALTER TABLE riot_accounts DROP COLUMN is_primary;

-- Step 6: Add foreign key constraints to junction table
ALTER TABLE user_riot_accounts 
    ADD CONSTRAINT fk_user_riot_accounts_user FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    ADD CONSTRAINT fk_user_riot_accounts_riot FOREIGN KEY (puuid) REFERENCES riot_accounts(puuid) ON DELETE CASCADE;

