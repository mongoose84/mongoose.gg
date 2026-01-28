import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import * as analyticsApi from '@/services/analyticsApi';

// Mock fetch globally
const mockFetch = vi.fn();
global.fetch = mockFetch;

// Mock apiConfig
vi.mock('@/services/apiConfig', () => ({
  getBaseApi: () => 'http://localhost:5000/api/v2'
}));

describe('analyticsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockResolvedValue({ ok: true });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('getSessionId', () => {
    it('returns a non-empty session ID', () => {
      const sessionId = analyticsApi.getSessionId();
      expect(sessionId).toBeTruthy();
      expect(typeof sessionId).toBe('string');
      expect(sessionId.length).toBeGreaterThan(0);
    });

    it('returns the same session ID on multiple calls', () => {
      const id1 = analyticsApi.getSessionId();
      const id2 = analyticsApi.getSessionId();
      expect(id1).toBe(id2);
    });
  });

  describe('track', () => {
    it('sends event to analytics endpoint', async () => {
      await analyticsApi.track('test:event', { foo: 'bar' });

      expect(mockFetch).toHaveBeenCalledTimes(1);
      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/v2/analytics',
        expect.objectContaining({
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include'
        })
      );

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.eventName).toBe('test:event');
      expect(body.payload).toEqual({ foo: 'bar' });
      expect(body.sessionId).toBe(analyticsApi.getSessionId());
    });

    it('handles null payload', async () => {
      await analyticsApi.track('test:event');

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.payload).toBeNull();
    });

    it('silently fails on network error', async () => {
      mockFetch.mockRejectedValue(new Error('Network error'));
      
      // Should not throw
      await expect(analyticsApi.track('test:event')).resolves.toBeUndefined();
    });
  });

  describe('trackBatch', () => {
    it('sends multiple events in batch', async () => {
      const events = [
        { eventName: 'event1', payload: { a: 1 } },
        { eventName: 'event2', payload: { b: 2 } }
      ];

      await analyticsApi.trackBatch(events);

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/v2/analytics/batch',
        expect.any(Object)
      );

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.events).toHaveLength(2);
      expect(body.events[0].eventName).toBe('event1');
      expect(body.events[1].eventName).toBe('event2');
    });

    it('does nothing for empty events array', async () => {
      await analyticsApi.trackBatch([]);
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('does nothing for null events', async () => {
      await analyticsApi.trackBatch(null);
      expect(mockFetch).not.toHaveBeenCalled();
    });
  });

  describe('convenience methods', () => {
    it('trackPageView sends page:view event', async () => {
      await analyticsApi.trackPageView('/app/matches', '/app/overview');

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.eventName).toBe('page:view');
      expect(body.payload).toEqual({ path: '/app/matches', referrer: '/app/overview' });
    });

    it('trackAuth sends auth event with action', async () => {
      await analyticsApi.trackAuth('login', true, { method: 'email' });

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.eventName).toBe('auth:login');
      expect(body.payload).toEqual({ success: true, method: 'email' });
    });

    it('trackFilterChange sends filter:change event', async () => {
      await analyticsApi.trackFilterChange('queue', 'ranked_solo');

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.eventName).toBe('filter:change');
      expect(body.payload).toEqual({ filterType: 'queue', value: 'ranked_solo' });
    });
  });

  describe('match details analytics', () => {
    it('trackMatchSelect sends match:select event', async () => {
      await analyticsApi.trackMatchSelect('EUW1_12345', 2, 'ranked_solo');

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.eventName).toBe('match:select');
      expect(body.payload).toEqual({ matchId: 'EUW1_12345', matchIndex: 2, queueType: 'ranked_solo' });
    });

    it('trackMatchDetailsView sends match:details_view event', async () => {
      await analyticsApi.trackMatchDetailsView('EUW1_12345', 'MIDDLE', true);

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.eventName).toBe('match:details_view');
      expect(body.payload).toEqual({ matchId: 'EUW1_12345', role: 'MIDDLE', win: true });
    });

    it('trackSectionToggle sends match:section_toggle event', async () => {
      await analyticsApi.trackSectionToggle('personal_stats', true);

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.eventName).toBe('match:section_toggle');
      expect(body.payload).toEqual({ section: 'personal_stats', expanded: true });
    });

    it('trackLaneExpand sends match:lane_expand event', async () => {
      await analyticsApi.trackLaneExpand('TOP', false, 'ally');

      const body = JSON.parse(mockFetch.mock.calls[0][1].body);
      expect(body.eventName).toBe('match:lane_expand');
      expect(body.payload).toEqual({ role: 'TOP', isUserRole: false, laneWinner: 'ally' });
    });
  });
});

