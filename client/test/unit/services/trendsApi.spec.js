import { describe, it, expect, vi, beforeEach } from 'vitest'

const mockApiRequest = vi.fn()
const mockAppendAccountParam = vi.fn((params) => params.append('accountId', 'all'))

vi.mock('@/services/apiClient', () => ({
  apiRequest: (...args) => mockApiRequest(...args),
  parseResponse: vi.fn(async (response) => response._data)
}))

vi.mock('@/services/accountContext', () => ({
  appendAccountParam: (...args) => mockAppendAccountParam(...args)
}))

describe('trendsApi', () => {
  let trendsApi

  beforeEach(async () => {
    vi.clearAllMocks()
    vi.resetModules()
    mockApiRequest.mockResolvedValue({ status: 200, _data: { points: [] } })
    trendsApi = await import('@/services/trendsApi')
  })

  describe('getWinrateTrend', () => {
    it('calls /trends/winrate/:userId', async () => {
      await trendsApi.getWinrateTrend(5)
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/trends/winrate/5'),
        { method: 'GET' }
      )
    })

    it('appends queueType when not "all"', async () => {
      await trendsApi.getWinrateTrend(5, 'ranked_solo')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('queueType=ranked_solo')
    })

    it('appends timeRange when provided', async () => {
      await trendsApi.getWinrateTrend(5, 'all', '3m')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('timeRange=3m')
    })

    it('appends limit when provided', async () => {
      await trendsApi.getWinrateTrend(5, 'all', undefined, 20)
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('limit=20')
    })

    it('returns null on 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await trendsApi.getWinrateTrend(5)
      expect(result).toBeNull()
    })
  })

  describe('getGoldAt15Trend', () => {
    it('calls /trends/gold-at-15/:userId', async () => {
      await trendsApi.getGoldAt15Trend(5)
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/trends/gold-at-15/5'),
        { method: 'GET' }
      )
    })

    it('appends queueType when not "all"', async () => {
      await trendsApi.getGoldAt15Trend(5, 'ranked_flex')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('queueType=ranked_flex')
    })

    it('returns null on 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await trendsApi.getGoldAt15Trend(5)
      expect(result).toBeNull()
    })
  })

  describe('getCsPerMinuteTrend', () => {
    it('calls /trends/cs-per-minute/:userId', async () => {
      await trendsApi.getCsPerMinuteTrend(5)
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/trends/cs-per-minute/5'),
        { method: 'GET' }
      )
    })

    it('appends limit when provided', async () => {
      await trendsApi.getCsPerMinuteTrend(5, 'all', undefined, 50)
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('limit=50')
    })

    it('returns null on 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await trendsApi.getCsPerMinuteTrend(5)
      expect(result).toBeNull()
    })
  })
})
