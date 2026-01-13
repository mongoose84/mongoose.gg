import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useSyncWebSocket } from '@/composables/useSyncWebSocket';

// Mock WebSocket
class MockWebSocket {
  static CONNECTING = 0;
  static OPEN = 1;
  static CLOSING = 2;
  static CLOSED = 3;

  constructor(url) {
    this.url = url;
    this.readyState = MockWebSocket.CONNECTING;
    this.onopen = null;
    this.onclose = null;
    this.onmessage = null;
    this.onerror = null;
    this.sentMessages = [];
    
    // Store instance for test access
    MockWebSocket.lastInstance = this;
  }

  send(data) {
    this.sentMessages.push(JSON.parse(data));
  }

  close(code, reason) {
    this.readyState = MockWebSocket.CLOSED;
    if (this.onclose) {
      this.onclose({ code, reason });
    }
  }

  // Test helpers
  simulateOpen() {
    this.readyState = MockWebSocket.OPEN;
    if (this.onopen) {
      this.onopen({});
    }
  }

  simulateMessage(data) {
    if (this.onmessage) {
      this.onmessage({ data: JSON.stringify(data) });
    }
  }

  simulateError(error) {
    if (this.onerror) {
      this.onerror(error);
    }
  }

  simulateClose(code = 1000, reason = '') {
    this.readyState = MockWebSocket.CLOSED;
    if (this.onclose) {
      this.onclose({ code, reason });
    }
  }
}

// Mock Vue lifecycle hooks
vi.mock('vue', async () => {
  const actual = await vi.importActual('vue');
  return {
    ...actual,
    onMounted: vi.fn((cb) => cb()),
    onUnmounted: vi.fn()
  };
});

// Mock apiConfig
vi.mock('@/services/apiConfig', () => ({
  getHost: () => 'http://localhost:5164',
  isDevelopment: false
}));

describe('useSyncWebSocket', () => {
  let originalWebSocket;

  beforeEach(() => {
    originalWebSocket = global.WebSocket;
    global.WebSocket = MockWebSocket;
    MockWebSocket.lastInstance = null;
    vi.useFakeTimers();
  });

  afterEach(() => {
    global.WebSocket = originalWebSocket;
    vi.useRealTimers();
    vi.clearAllMocks();
  });

  describe('Connection', () => {
    it('connects to WebSocket on mount', () => {
      useSyncWebSocket();
      
      expect(MockWebSocket.lastInstance).not.toBeNull();
      expect(MockWebSocket.lastInstance.url).toBe('ws://localhost:5164/ws/sync');
    });

    it('sets isConnected to true when connection opens', () => {
      const { isConnected } = useSyncWebSocket();
      
      expect(isConnected.value).toBe(false);
      
      MockWebSocket.lastInstance.simulateOpen();
      
      expect(isConnected.value).toBe(true);
    });

    it('sets isConnecting to true during connection', () => {
      const { isConnecting } = useSyncWebSocket();
      
      expect(isConnecting.value).toBe(true);
      
      MockWebSocket.lastInstance.simulateOpen();
      
      expect(isConnecting.value).toBe(false);
    });

    it('sets connectionError on error', () => {
      const { connectionError } = useSyncWebSocket();
      
      MockWebSocket.lastInstance.simulateError(new Error('Connection failed'));
      
      expect(connectionError.value).toBe('WebSocket connection error');
    });
  });

  describe('Reconnection', () => {
    it('attempts to reconnect on disconnect', () => {
      useSyncWebSocket();
      
      MockWebSocket.lastInstance.simulateOpen();
      MockWebSocket.lastInstance.simulateClose();
      
      // First reconnect attempt after 1 second
      vi.advanceTimersByTime(1000);
      
      expect(MockWebSocket.lastInstance).not.toBeNull();
    });

    it('uses exponential backoff for reconnection', () => {
      useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      MockWebSocket.lastInstance.simulateClose();

      // First attempt: 1 second
      vi.advanceTimersByTime(1000);
      const firstInstance = MockWebSocket.lastInstance;
      firstInstance.simulateClose();

      // Second attempt: 2 seconds
      vi.advanceTimersByTime(1500);
      expect(MockWebSocket.lastInstance).toBe(firstInstance); // Not yet

      vi.advanceTimersByTime(500);
      expect(MockWebSocket.lastInstance).not.toBe(firstInstance); // Now reconnected
    });
  });

  describe('Subscription', () => {
    it('sends subscribe message when subscribing to a puuid', () => {
      const { subscribe } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid-123');

      expect(MockWebSocket.lastInstance.sentMessages).toContainEqual({
        type: 'subscribe',
        puuid: 'test-puuid-123'
      });
    });

    it('creates progress entry when subscribing', () => {
      const { subscribe, syncProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid-123');

      expect(syncProgress.has('test-puuid-123')).toBe(true);
      expect(syncProgress.get('test-puuid-123').status).toBe('idle');
    });

    it('sends unsubscribe message when unsubscribing', () => {
      const { subscribe, unsubscribe } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid-123');
      unsubscribe('test-puuid-123');

      expect(MockWebSocket.lastInstance.sentMessages).toContainEqual({
        type: 'unsubscribe',
        puuid: 'test-puuid-123'
      });
    });

    it('removes progress entry when unsubscribing', () => {
      const { subscribe, unsubscribe, syncProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid-123');
      unsubscribe('test-puuid-123');

      expect(syncProgress.has('test-puuid-123')).toBe(false);
    });
  });

  describe('Message Handling', () => {
    it('updates progress on sync_progress message', () => {
      const { subscribe, syncProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid');

      MockWebSocket.lastInstance.simulateMessage({
        type: 'sync_progress',
        puuid: 'test-puuid',
        status: 'syncing',
        progress: 45,
        total: 100,
        matchId: 'EUW1_123456'
      });

      const progress = syncProgress.get('test-puuid');
      expect(progress.status).toBe('syncing');
      expect(progress.progress).toBe(45);
      expect(progress.total).toBe(100);
      expect(progress.matchId).toBe('EUW1_123456');
    });

    it('updates progress on sync_complete message', () => {
      const { subscribe, syncProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid');

      // First set some progress
      MockWebSocket.lastInstance.simulateMessage({
        type: 'sync_progress',
        puuid: 'test-puuid',
        progress: 50,
        total: 100
      });

      // Then complete
      MockWebSocket.lastInstance.simulateMessage({
        type: 'sync_complete',
        puuid: 'test-puuid',
        totalSynced: 100
      });

      const progress = syncProgress.get('test-puuid');
      expect(progress.status).toBe('completed');
      expect(progress.totalSynced).toBe(100);
      expect(progress.progress).toBe(100); // Filled to total
    });

    it('updates progress on sync_error message', () => {
      const { subscribe, syncProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid');

      MockWebSocket.lastInstance.simulateMessage({
        type: 'sync_error',
        puuid: 'test-puuid',
        error: 'Rate limited'
      });

      const progress = syncProgress.get('test-puuid');
      expect(progress.status).toBe('failed');
      expect(progress.error).toBe('Rate limited');
    });
  });

  describe('Helper Methods', () => {
    it('getProgress returns progress for subscribed puuid', () => {
      const { subscribe, getProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid');

      const progress = getProgress('test-puuid');
      expect(progress).not.toBeNull();
      expect(progress.status).toBe('idle');
    });

    it('getProgress returns null for unknown puuid', () => {
      const { getProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();

      expect(getProgress('unknown-puuid')).toBeNull();
    });

    it('isSyncing returns true when status is syncing', () => {
      const { subscribe, isSyncing, syncProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid');

      syncProgress.get('test-puuid').status = 'syncing';

      expect(isSyncing('test-puuid')).toBe(true);
    });

    it('resetProgress resets progress to initial state', () => {
      const { subscribe, resetProgress, syncProgress } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      subscribe('test-puuid');

      // Set some progress
      const progress = syncProgress.get('test-puuid');
      progress.status = 'syncing';
      progress.progress = 50;
      progress.total = 100;
      progress.error = 'Some error';

      resetProgress('test-puuid');

      expect(progress.status).toBe('idle');
      expect(progress.progress).toBe(0);
      expect(progress.total).toBe(0);
      expect(progress.error).toBeNull();
    });
  });

  describe('Disconnect', () => {
    it('closes WebSocket connection on disconnect', () => {
      const { disconnect } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();

      const closeSpy = vi.spyOn(MockWebSocket.lastInstance, 'close');
      disconnect();

      expect(closeSpy).toHaveBeenCalledWith(1000, 'Client disconnect');
    });

    it('sets isConnected to false on disconnect', () => {
      const { disconnect, isConnected } = useSyncWebSocket();

      MockWebSocket.lastInstance.simulateOpen();
      expect(isConnected.value).toBe(true);

      disconnect();

      expect(isConnected.value).toBe(false);
    });
  });
});

