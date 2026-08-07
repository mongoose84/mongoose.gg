-- ============================================================================
-- MIGRATION: User Identity Providers (social sign-on)
-- Date: 2026-08-08
-- Purpose: Generic mapping from an external identity provider (Riot Sign-On,
--          Google Sign-On, and any future provider) to a local user, without
--          requiring a schema change per provider. (provider, provider_uid)
--          uniquely identifies the login; a user may have more than one linked
--          provider identity.
-- ============================================================================

CREATE TABLE IF NOT EXISTS user_identity_providers (
    id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT UNSIGNED NOT NULL,
    provider VARCHAR(32) NOT NULL,        -- 'riot', 'google', ...
    provider_uid VARCHAR(255) NOT NULL,   -- puuid / sub / provider-specific identity id
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY idx_provider_identity (provider, provider_uid),
    KEY idx_user_id (user_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
