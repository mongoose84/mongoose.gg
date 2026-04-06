import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('@/services/apiConfig', () => ({
  getBaseApi: () => 'http://localhost:5000/api/v2'
}))

vi.mock('@/services/accountContext', () => ({
  getAccountParam: () => 'all',
  appendAccountParam: (params) => params.append('accountId', 'all')
}))

const mockApiRequest = vi.fn()
vi.mock('@/services/apiClient', () => ({
  apiRequest: (...args) => mockApiRequest(...args),
  parseResponse: vi.fn(async (response, _errMsg) => response._data)
}))

import * as apiClient from '@/services/apiClient'

describe('matchesApi', () => {
  let matchesApi

  beforeEach(async () => {
    vi.clearAllMocks()
    vi.resetModules()
    mockApiRequest.mockResolvedValue({ status: 200, _data: { matches: [], totalMatches: 0 } })
    matchesApi = await import('@/services/matchesApi')
  })

  describe('getMatchList', () => {
    it('calls the correct endpoint for a user', async () => {
      await matchesApi.getMatchList(42)
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/matches/42'),
        { method: 'GET' }
      )
    })

    it('appends queueType when not "all"', async () => {
      await matchesApi.getMatchList(42, 'ranked_solo')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('queueType=ranked_solo')
    })

    it('omits queueType when value is "all"', async () => {
      await matchesApi.getMatchList(42, 'all')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).not.toContain('queueType')
    })

    it('returns null when response status is 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await matchesApi.getMatchList(42)
      expect(result).toBeNull()
    })
  })

  describe('getMatchDetails', () => {
    it('calls the correct endpoint for a match', async () => {
      mockApiRequest.mockResolvedValue({ status: 200, _data: { match: {} } })
      await matchesApi.getMatchDetails('EUW1_12345')
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/matches/EUW1_12345/details'),
        { method: 'GET' }
      )
    })

    it('appends accountId when provided and not "all"', async () => {
      mockApiRequest.mockResolvedValue({ status: 200, _data: {} })
      await matchesApi.getMatchDetails('EUW1_12345', 'acc_test')
      const endpoint = mockApiRequest.mock.calls[0][0]
      expect(endpoint).toContain('accountId=acc_test')
    })

    it('returns null on 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await matchesApi.getMatchDetails('EUW1_12345')
      expect(result).toBeNull()
    })
  })

  describe('getMatchNarrative', () => {
    it('calls the correct endpoint for a match', async () => {
      mockApiRequest.mockResolvedValue({ status: 200, _data: { laneMatchups: [] } })
      await matchesApi.getMatchNarrative('EUW1_12345')
      expect(mockApiRequest).toHaveBeenCalledWith(
        expect.stringContaining('/matches/EUW1_12345/narrative'),
        { method: 'GET' }
      )
    })

    it('returns null on 404', async () => {
      mockApiRequest.mockResolvedValue({ status: 404 })
      const result = await matchesApi.getMatchNarrative('EUW1_12345')
      expect(result).toBeNull()
    })
  })
})
