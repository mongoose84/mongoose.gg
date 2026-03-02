import { describe, it, expect, vi, beforeEach } from 'vitest';
import * as authApi from '@/services/authApi';
import { apiRequest, parseResponse } from '@/services/apiClient';

vi.mock('@/services/apiConfig', () => ({
  getBaseApi: () => 'http://localhost:5000/api/v2'
}));

vi.mock('@/services/apiClient', () => ({
  apiRequest: vi.fn(),
  parseResponse: vi.fn()
}));

describe('authApi account parameter handling', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    apiRequest.mockResolvedValue({ status: 200, ok: true });
    parseResponse.mockResolvedValue({});
  });

  it('getAccountParam maps overall to all', () => {
    localStorage.setItem('mongoose_active_account', 'overall');

    expect(authApi.getAccountParam()).toBe('all');
  });

  it('getAccountParam returns stored puuid as-is', () => {
    localStorage.setItem('mongoose_active_account', 'puuid-123');

    expect(authApi.getAccountParam()).toBe('puuid-123');
  });

  it('getSoloDashboard includes account query parameter', async () => {
    localStorage.setItem('mongoose_active_account', 'overall');

    await authApi.getSoloDashboard(42, 'ranked_solo', '1m');

    expect(apiRequest).toHaveBeenCalledWith(
      '/solo/dashboard/42?queueType=ranked_solo&timeRange=1m&account=all',
      { method: 'GET' }
    );
  });

  it('getOverview includes account query parameter from active puuid', async () => {
    localStorage.setItem('mongoose_active_account', 'puuid-xyz');

    await authApi.getOverview(42);

    expect(apiRequest).toHaveBeenCalledWith(
      '/overview/42?account=puuid-xyz',
      { method: 'GET' }
    );
  });

  it('getMatchList includes queue and account query parameters', async () => {
    localStorage.setItem('mongoose_active_account', 'puuid-xyz');

    await authApi.getMatchList(42, 'aram');

    expect(apiRequest).toHaveBeenCalledWith(
      '/matches/42?queueType=aram&account=puuid-xyz',
      { method: 'GET' }
    );
  });

  it('getChampionSelectData includes account query parameter', async () => {
    localStorage.setItem('mongoose_active_account', 'overall');

    await authApi.getChampionSelectData(42, 'all', 'current_season');

    expect(apiRequest).toHaveBeenCalledWith(
      '/champion-select/42?timeRange=current_season&account=all',
      { method: 'GET' }
    );
  });

  it('returns null on 404 responses for data endpoints', async () => {
    localStorage.setItem('mongoose_active_account', 'overall');
    apiRequest.mockResolvedValueOnce({ status: 404, ok: false });

    const result = await authApi.getWinrateTrend(42, 'all', '1m', 20);

    expect(result).toBeNull();
    expect(parseResponse).not.toHaveBeenCalled();
  });
});
