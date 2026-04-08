/**
 * API Service Mocking Helpers
 * 
 * This module provides reusable mock factories and utilities for testing
 * components that depend on API services. Use these helpers to create
 * consistent, maintainable mocks across the test suite.
 * 
 * USAGE PATTERNS:
 * 
 * 1. Mocking the entire API service (recommended for store tests):
 *    ```
 *    vi.mock('@/services/authApi', () => createAuthApiMock());
 *    import * as authApi from '@/services/authApi';
 *    // In tests: authApi.login.mockResolvedValue({ success: true });
 *    ```
 * 
 * 2. Mocking via store (recommended for component tests):
 *    ```
 *    vi.mock('@/stores/authStore', () => ({
 *      useAuthStore: () => createMockAuthStore()
 *    }));
 *    ```
 * 
 * 3. Custom mock responses:
 *    ```
 *    authApi.getCurrentUser.mockResolvedValue(createMockUser({ tier: 'pro' }));
 *    ```
 */

import { vi } from 'vitest';

// ============ Mock Data Factories ============

/**
 * Creates a mock user object with sensible defaults
 * @param {Object} overrides - Properties to override
 */
export function createMockUser(overrides = {}) {
  return {
    userId: 1,
    username: 'testuser',
    email: 'test@example.com',
    emailVerified: true,
    tier: 'free',
    riotAccounts: [],
    createdAt: new Date().toISOString(),
    ...overrides
  };
}

/**
 * Creates a mock Riot account object
 * @param {Object} overrides - Properties to override
 */
export function createMockRiotAccount(overrides = {}) {
  return {
    puuid: 'mock-puuid-123',
    gameName: 'TestPlayer',
    tagLine: 'EUW',
    region: 'euw1',
    isPrimary: true,
    tier: 'GOLD',
    rank: 'IV',
    leaguePoints: 50,
    lastSyncedAt: new Date().toISOString(),
    ...overrides
  };
}

/**
 * Creates a mock match object
 * @param {Object} overrides - Properties to override
 */
export function createMockMatch(overrides = {}) {
  return {
    matchId: 'EUW1_1234567890',
    queueId: 420,
    queueType: 'Ranked Solo',
    championId: 1,
    championName: 'Darius',
    championIconUrl: '/champions/Darius.png',
    role: 'TOP',
    win: true,
    kills: 5,
    deaths: 2,
    assists: 8,
    kda: 6.5,
    cs: 180,
    csPerMin: 6.0,
    gold: 12000,
    goldPerMin: 400,
    gameDurationSec: 1800,
    gameStartTime: Date.now() - 3600000,
    ...overrides
  };
}

// ============ API Mock Factories ============

/**
 * Creates a mock auth API with all functions stubbed
 * Use with vi.mock('@/services/authApi', () => createAuthApiMock())
 */
export function createAuthApiMock() {
  return {
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    verify: vi.fn(),
    resendVerificationEmail: vi.fn(),
    getCurrentUser: vi.fn(),
    requestPasswordReset: vi.fn(),
    resetPassword: vi.fn(),
    linkRiotAccount: vi.fn(),
    unlinkRiotAccount: vi.fn(),
    triggerRiotAccountSync: vi.fn(),
    setAccountPrimary: vi.fn(),
    updatePassword: vi.fn(),
    deleteAccount: vi.fn(),
    getAuthDevices: vi.fn(),
    logoutDevice: vi.fn()
  };
}

/**
 * Creates a mock analytics API with all functions stubbed
 * Use with vi.mock('@/services/analyticsApi', () => createAnalyticsApiMock())
 */
export function createAnalyticsApiMock() {
  return {
    track: vi.fn(),
    trackBatch: vi.fn(),
    trackPageView: vi.fn(),
    trackAuth: vi.fn(),
    trackNavClick: vi.fn(),
    trackFilterChange: vi.fn(),
    trackFeature: vi.fn(),
    trackUpgrade: vi.fn(),
    trackMatchSelect: vi.fn(),
    trackMatchDetailsView: vi.fn(),
    trackSectionToggle: vi.fn(),
    trackLaneExpand: vi.fn(),
    trackTeamComparisonView: vi.fn(),
    trackWinPredictionStatsView: vi.fn(),
    getSessionId: vi.fn().mockReturnValue('mock-session-id')
  };
}

/**
 * Creates a mock apiConfig
 * Use with vi.mock('@/services/apiConfig', () => createApiConfigMock())
 */
export function createApiConfigMock(baseUrl = 'http://localhost:5000/api/v2') {
  return {
    getHost: () => 'http://localhost:5000',
    getBaseApi: () => baseUrl,
    isDevelopment: true,
    apiVersionPath: '/api/v2'
  };
}

// ============ Store Mock Factories ============

/**
 * Creates a mock auth store object for component tests
 */
export function createMockAuthStore(overrides = {}) {
  return {
    // State
    user: null,
    isLoading: false,
    isInitialized: true,
    error: null,
    isLinkingAccount: false,
    // Getters
    isAuthenticated: false,
    isVerified: false,
    username: '',
    email: '',
    tier: 'free',
    userId: null,
    riotAccounts: [],
    hasLinkedAccount: false,
    primaryRiotAccount: null,
    // Actions (all stubbed)
    initialize: vi.fn(),
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    verify: vi.fn(),
    refreshUser: vi.fn(),
    linkRiotAccount: vi.fn(),
    unlinkRiotAccount: vi.fn(),
    clearError: vi.fn(),
    ...overrides
  };
}

