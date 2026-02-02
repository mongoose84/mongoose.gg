import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useAnalysisStatus } from '@/composables/useAnalysisStatus';

// Mock the authStore
vi.mock('@/stores/authStore', () => ({
  useAuthStore: vi.fn()
}));

// Mock the useSyncWebSocket composable
vi.mock('@/composables/useSyncWebSocket', () => ({
  useSyncWebSocket: vi.fn()
}));

// Mock the authApi
vi.mock('@/services/authApi', () => ({
  triggerRiotAccountSync: vi.fn(),
  getRiotAccountSyncStatus: vi.fn()
}));

// Mock apiClient to prevent session callback issues
vi.mock('@/services/apiClient', () => ({
  setSessionExpiredCallback: vi.fn()
}));

import { useAuthStore } from '@/stores/authStore';
import { useSyncWebSocket } from '@/composables/useSyncWebSocket';
import { triggerRiotAccountSync, getRiotAccountSyncStatus } from '@/services/authApi';

describe('useAnalysisStatus', () => {
  let mockAuthStore;
  let mockSyncWebSocket;
  let mockSyncProgress;

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();

    // Setup mock sync progress as a Map
    mockSyncProgress = new Map();

    // Setup mock WebSocket composable
    mockSyncWebSocket = {
      syncProgress: mockSyncProgress,
      subscribe: vi.fn(),
      unsubscribe: vi.fn(),
      resetProgress: vi.fn(),
      isConnected: { value: true }
    };
    useSyncWebSocket.mockReturnValue(mockSyncWebSocket);

    // Setup mock auth store
    mockAuthStore = {
      primaryRiotAccount: {
        puuid: 'test-puuid-123',
        syncStatus: 'idle',
        lastSyncAt: '2026-02-01T12:00:00Z'
      }
    };
    useAuthStore.mockReturnValue(mockAuthStore);
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  describe('initial state', () => {
    it('returns status from stored account data when no WebSocket progress', () => {
      mockAuthStore.primaryRiotAccount.syncStatus = 'completed';
      const { status } = useAnalysisStatus();
      expect(status.value).toBe('completed');
    });

    it('returns idle when no stored status', () => {
      mockAuthStore.primaryRiotAccount.syncStatus = null;
      const { status } = useAnalysisStatus();
      expect(status.value).toBe('idle');
    });

    it('returns primaryPuuid from auth store', () => {
      const { primaryPuuid } = useAnalysisStatus();
      expect(primaryPuuid.value).toBe('test-puuid-123');
    });

    it('returns null primaryPuuid when no account linked', () => {
      mockAuthStore.primaryRiotAccount = null;
      const { primaryPuuid } = useAnalysisStatus();
      expect(primaryPuuid.value).toBeNull();
    });
  });

  describe('status computation', () => {
    it('prioritizes WebSocket status over stored status', () => {
      mockAuthStore.primaryRiotAccount.syncStatus = 'idle';
      mockSyncProgress.set('test-puuid-123', { status: 'syncing' });
      
      const { status } = useAnalysisStatus();
      expect(status.value).toBe('syncing');
    });

    it('isRunning is true when status is pending', () => {
      mockSyncProgress.set('test-puuid-123', { status: 'pending' });
      const { isRunning } = useAnalysisStatus();
      expect(isRunning.value).toBe(true);
    });

    it('isRunning is true when status is syncing', () => {
      mockSyncProgress.set('test-puuid-123', { status: 'syncing' });
      const { isRunning } = useAnalysisStatus();
      expect(isRunning.value).toBe(true);
    });

    it('isRunning is false when status is completed', () => {
      mockAuthStore.primaryRiotAccount.syncStatus = 'completed';
      const { isRunning } = useAnalysisStatus();
      expect(isRunning.value).toBe(false);
    });

    it('isRateLimited is true when WebSocket indicates rate limit', () => {
      mockSyncProgress.set('test-puuid-123', { status: 'syncing', isRateLimited: true });
      const { isRateLimited } = useAnalysisStatus();
      expect(isRateLimited.value).toBe(true);
    });

    it('hasFailed is true when status is failed', () => {
      mockSyncProgress.set('test-puuid-123', { status: 'failed' });
      const { hasFailed } = useAnalysisStatus();
      expect(hasFailed.value).toBe(true);
    });

    it('isUpToDate is true when completed with lastSyncAt', () => {
      mockAuthStore.primaryRiotAccount.syncStatus = 'completed';
      mockAuthStore.primaryRiotAccount.lastSyncAt = '2026-02-01T12:00:00Z';
      const { isUpToDate } = useAnalysisStatus();
      expect(isUpToDate.value).toBeTruthy();
    });

    it('isUpToDate is false when no lastSyncAt', () => {
      mockAuthStore.primaryRiotAccount.syncStatus = 'completed';
      mockAuthStore.primaryRiotAccount.lastSyncAt = null;
      const { isUpToDate } = useAnalysisStatus();
      expect(isUpToDate.value).toBeFalsy();
    });
  });

  describe('progress', () => {
    it('returns progress from WebSocket', () => {
      mockSyncProgress.set('test-puuid-123', {
        status: 'syncing',
        progress: 5,
        total: 10,
        matchId: 'match-123'
      });
      const { progress } = useAnalysisStatus();
      expect(progress.value).toEqual({
        current: 5,
        total: 10,
        matchId: 'match-123',
        totalSynced: null
      });
    });

    it('returns zero progress when no WebSocket data', () => {
      const { progress } = useAnalysisStatus();
      expect(progress.value).toEqual({
        current: 0,
        total: 0,
        matchId: null,
        totalSynced: null
      });
    });
  });

  describe('errorMessage', () => {
    it('returns error from WebSocket progress', () => {
      mockSyncProgress.set('test-puuid-123', { status: 'failed', error: 'API error' });
      const { errorMessage } = useAnalysisStatus();
      expect(errorMessage.value).toBe('API error');
    });

    it('returns null when no error', () => {
      const { errorMessage } = useAnalysisStatus();
      expect(errorMessage.value).toBeNull();
    });
  });

  describe('loadStatus', () => {
    it('calls getRiotAccountSyncStatus with puuid', async () => {
      getRiotAccountSyncStatus.mockResolvedValue({ status: 'completed' });

      const { loadStatus } = useAnalysisStatus();
      await loadStatus();

      expect(getRiotAccountSyncStatus).toHaveBeenCalledWith('test-puuid-123');
    });

    it('subscribes to WebSocket after loading', async () => {
      getRiotAccountSyncStatus.mockResolvedValue({ status: 'completed' });

      const { loadStatus } = useAnalysisStatus();
      await loadStatus();

      expect(mockSyncWebSocket.subscribe).toHaveBeenCalledWith('test-puuid-123');
    });

    it('sets isLoading during load', async () => {
      let resolvePromise;
      getRiotAccountSyncStatus.mockReturnValue(new Promise(r => { resolvePromise = r; }));

      const { loadStatus, isLoading } = useAnalysisStatus();
      const loadPromise = loadStatus();

      expect(isLoading.value).toBe(true);

      resolvePromise({ status: 'completed' });
      await loadPromise;

      expect(isLoading.value).toBe(false);
    });

    it('does nothing when no primary account', async () => {
      mockAuthStore.primaryRiotAccount = null;

      const { loadStatus } = useAnalysisStatus();
      await loadStatus();

      expect(getRiotAccountSyncStatus).not.toHaveBeenCalled();
    });

    it('sets error on failure', async () => {
      getRiotAccountSyncStatus.mockRejectedValue(new Error('Network error'));

      const { loadStatus, errorMessage } = useAnalysisStatus();
      await loadStatus();

      expect(errorMessage.value).toBe('Network error');
    });
  });

  describe('triggerAnalysis', () => {
    it('calls triggerRiotAccountSync with puuid', async () => {
      triggerRiotAccountSync.mockResolvedValue({});

      const { triggerAnalysis } = useAnalysisStatus();
      await triggerAnalysis();

      expect(triggerRiotAccountSync).toHaveBeenCalledWith('test-puuid-123');
    });

    it('resets progress before triggering', async () => {
      triggerRiotAccountSync.mockResolvedValue({});

      const { triggerAnalysis } = useAnalysisStatus();
      await triggerAnalysis();

      expect(mockSyncWebSocket.resetProgress).toHaveBeenCalledWith('test-puuid-123');
    });

    it('subscribes to WebSocket after triggering', async () => {
      triggerRiotAccountSync.mockResolvedValue({});

      const { triggerAnalysis } = useAnalysisStatus();
      await triggerAnalysis();

      expect(mockSyncWebSocket.subscribe).toHaveBeenCalledWith('test-puuid-123');
    });

    it('returns true on success', async () => {
      triggerRiotAccountSync.mockResolvedValue({});

      const { triggerAnalysis } = useAnalysisStatus();
      const result = await triggerAnalysis();

      expect(result).toBe(true);
    });

    it('returns false and sets error when no account', async () => {
      mockAuthStore.primaryRiotAccount = null;

      const { triggerAnalysis, errorMessage } = useAnalysisStatus();
      const result = await triggerAnalysis();

      expect(result).toBe(false);
      expect(errorMessage.value).toBe('No linked Riot account');
    });

    it('returns false on API error', async () => {
      triggerRiotAccountSync.mockRejectedValue(new Error('Sync failed'));

      const { triggerAnalysis, errorMessage } = useAnalysisStatus();
      const result = await triggerAnalysis();

      expect(result).toBe(false);
      expect(errorMessage.value).toBe('Sync failed');
    });
  });

  describe('clearError', () => {
    it('resets progress for the account', () => {
      const { clearError } = useAnalysisStatus();
      clearError();

      expect(mockSyncWebSocket.resetProgress).toHaveBeenCalledWith('test-puuid-123');
    });

    it('does not reset progress when no account', () => {
      mockAuthStore.primaryRiotAccount = null;

      const { clearError } = useAnalysisStatus();
      clearError();

      expect(mockSyncWebSocket.resetProgress).not.toHaveBeenCalled();
    });
  });

  describe('lastSyncAt', () => {
    it('returns lastSyncAt from account data', () => {
      mockAuthStore.primaryRiotAccount.lastSyncAt = '2026-02-01T15:30:00Z';
      const { lastSyncAt } = useAnalysisStatus();
      expect(lastSyncAt.value).toBe('2026-02-01T15:30:00Z');
    });

    it('returns null when no lastSyncAt', () => {
      mockAuthStore.primaryRiotAccount.lastSyncAt = null;
      const { lastSyncAt } = useAnalysisStatus();
      expect(lastSyncAt.value).toBeNull();
    });
  });
});

