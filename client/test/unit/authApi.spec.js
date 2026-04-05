import { describe, it, expect, vi, beforeEach } from 'vitest'
import * as authApi from '@/services/authApi'
import * as soloApi from '@/services/soloApi'
import * as matchesApi from '@/services/matchesApi'
import * as trendsApi from '@/services/trendsApi'
import { apiRequest, parseResponse } from '@/services/apiClient'

vi.mock('@/services/apiConfig', () => ({
  getBaseApi: () => 'http://localhost:5000/api/v2'
}))

vi.mock('@/services/apiClient', () => ({
  apiRequest: vi.fn(),
  parseResponse: vi.fn()
}))

describe('API service account parameter handling', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    apiRequest.mockResolvedValue({ status: 200, ok: true })
    parseResponse.mockResolvedValue({})
  })

  it('getAccountParam maps overall to all', () => {
    localStorage.setItem('mongoose_active_account', 'overall')

    expect(authApi.getAccountParam()).toBe('all')
  })

  it('getAccountParam resets stale raw PUUID to all and clears localStorage', () => {
    localStorage.setItem('mongoose_active_account', 'puuid-123')

    expect(authApi.getAccountParam()).toBe('all')
    expect(localStorage.getItem('mongoose_active_account')).toBeNull()
  })

  it('getAccountParam passes through valid acc_ token', () => {
    localStorage.setItem('mongoose_active_account', 'acc_abcdef1234567890')

    expect(authApi.getAccountParam()).toBe('acc_abcdef1234567890')
  })

  it('getSoloDashboard includes account query parameter', async () => {
    localStorage.setItem('mongoose_active_account', 'overall')

    await soloApi.getSoloDashboard(42, 'ranked_solo', '1m')

    expect(apiRequest).toHaveBeenCalledWith(
      '/solo/dashboard/42?queueType=ranked_solo&timeRange=1m&accountId=all',
      { method: 'GET' }
    )
  })

  it('getOverview includes account query parameter from active acc_ token', async () => {
    localStorage.setItem('mongoose_active_account', 'acc_xyz123')

    await soloApi.getOverview(42)

    expect(apiRequest).toHaveBeenCalledWith(
      '/overview/42?accountId=acc_xyz123',
      { method: 'GET' }
    )
  })

  it('getMatchList includes queue and account query parameters', async () => {
    localStorage.setItem('mongoose_active_account', 'acc_xyz123')

    await matchesApi.getMatchList(42, 'aram')

    expect(apiRequest).toHaveBeenCalledWith(
      '/matches/42?queueType=aram&accountId=acc_xyz123',
      { method: 'GET' }
    )
  })

  it('getChampionSelectData includes account query parameter', async () => {
    localStorage.setItem('mongoose_active_account', 'overall')

    await soloApi.getChampionSelectData(42, 'all', 'current_season')

    expect(apiRequest).toHaveBeenCalledWith(
      '/champion-select/42?timeRange=current_season&accountId=all',
      { method: 'GET' }
    )
  })

  it('getMatchDetails includes explicit accountId query parameter', async () => {
    await matchesApi.getMatchDetails('NA1_12345', 'acc_xyz123')

    expect(apiRequest).toHaveBeenCalledWith(
      '/matches/NA1_12345/details?accountId=acc_xyz123',
      { method: 'GET' }
    )
  })

  it('getMatchDetails omits accountId when overall mode, letting backend default to primary', async () => {
    localStorage.setItem('mongoose_active_account', 'overall')

    await matchesApi.getMatchDetails('NA1_12345')

    expect(apiRequest).toHaveBeenCalledWith(
      '/matches/NA1_12345/details',
      { method: 'GET' }
    )
  })

  it('getMatchNarrative omits accountId when overall mode, letting backend default to primary', async () => {
    localStorage.setItem('mongoose_active_account', 'overall')

    await matchesApi.getMatchNarrative('NA1_12345')

    expect(apiRequest).toHaveBeenCalledWith(
      '/matches/NA1_12345/narrative',
      { method: 'GET' }
    )
  })

  it('returns null on 404 responses for data endpoints', async () => {
    localStorage.setItem('mongoose_active_account', 'overall')
    apiRequest.mockResolvedValueOnce({ status: 404, ok: false })

    const result = await trendsApi.getWinrateTrend(42, 'all', '1m', 20)

    expect(result).toBeNull()
    expect(parseResponse).not.toHaveBeenCalled()
  })
})
