-- Migration: Add is_lp_estimated column to participants table
-- This column tracks whether LP data was estimated by LpEstimationService (true)
-- or is actual data from the Riot API (false).
--
-- The column is NOT NULL DEFAULT FALSE to ensure:
-- 1. Existing rows get FALSE (actual LP data or no LP data)
-- 2. New rows default to FALSE unless explicitly set
-- 3. GetBoolean() reads won't throw on NULL values

-- Add the column if it doesn't exist
-- MySQL doesn't have IF NOT EXISTS for ADD COLUMN, so we use a procedure
DELIMITER //

DROP PROCEDURE IF EXISTS add_is_lp_estimated_column//

CREATE PROCEDURE add_is_lp_estimated_column()
BEGIN
    -- Check if column already exists
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = DATABASE() 
        AND TABLE_NAME = 'participants' 
        AND COLUMN_NAME = 'is_lp_estimated'
    ) THEN
        ALTER TABLE participants 
            ADD COLUMN is_lp_estimated BOOLEAN NOT NULL DEFAULT FALSE 
            AFTER rank_after;
    ELSE
        -- Column exists, ensure it's NOT NULL DEFAULT FALSE
        ALTER TABLE participants 
            MODIFY COLUMN is_lp_estimated BOOLEAN NOT NULL DEFAULT FALSE;
    END IF;
END//

DELIMITER ;

CALL add_is_lp_estimated_column();

DROP PROCEDURE IF EXISTS add_is_lp_estimated_column;

