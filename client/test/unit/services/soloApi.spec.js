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

describe('soloApi', () => {
  let soloApi

  beforeEach(async () => {
    vi.clearAllMocks()
    vi.resetModules()
    mockApiRequest.mockResolvedValue({ status: 200, _data: {} })
    soloApi = await import('@/services/soloApi')
  })

  describe('getOverview', () => {
    it('calls the correct endpoint for a user', async () => {
      await soloApi.getOverview(7)
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/overview/7'),
        { method: 'GET' }
      )
    })

    it('returns null on 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await soloApi.getOverview(7)
      expect(result).toBeNull()
    })

    it('appends accountId via appendAccountParam', async () => {
      await soloApi.getOverview(7)
      expect(mockAppendAccountParam).toHaveBeenCalled()
    })
  })

  describe('getSoloDashboard', () => {
    it('calls the correct endpoint for a user', async () => {
      await soloApi.getSoloDashboard(7)
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/solo/dashboard/7'),
        { method: 'GET' }
      )
    })

    it('appends queueType when not "all"', async () => {
      await soloApi.getSoloDashboard(7, 'ranked_solo')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('queueType=ranked_solo')
    })

    it('omits queueType param when value is "all"', async () => {
      await soloApi.getSoloDashboard(7, 'all')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).not.toContain('queueType=all')
    })

    it('appends timeRange when provided', async () => {
      await soloApi.getSoloDashboard(7, 'all', '1m')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('timeRange=1m')
    })

    it('returns null on 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await soloApi.getSoloDashboard(7)
      expect(result).toBeNull()
    })
  })

  describe('getChampionSelectData', () => {
    it('calls the correct endpoint for a user', async () => {
      await soloApi.getChampionSelectData(7)
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/champion-select/7'),
        { method: 'GET' }
      )
    })

    it('appends queueType when not "all"', async () => {
      await soloApi.getChampionSelectData(7, 'ranked_flex')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('queueType=ranked_flex')
    })

    it('appends timeRange when provided', async () => {
      await soloApi.getChampionSelectData(7, 'all', 'current_season')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('timeRange=current_season')
    })

    it('returns null on 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await soloApi.getChampionSelectData(7)
      expect(result).toBeNull()
    })
  })
})
