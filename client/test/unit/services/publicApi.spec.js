import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

vi.mock('@/services/apiConfig', () => ({
  getBaseApi: () => 'http://localhost:5000/api/v2'
}))

const mockFetch = vi.fn()
global.fetch = mockFetch

describe('publicApi', () => {
  let publicApi

  beforeEach(async () => {
    vi.resetModules()
    vi.clearAllMocks()
    publicApi = await import('@/services/publicApi')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('getPublicStats', () => {
    it('returns parsed data on success', async () => {
      const payload = { totalMatches: 5000, activePlayers: 120 }
      mockFetch.mockResolvedValue({
        ok: true,
        json: () => Promise.resolve(payload)
      })

      const result = await publicApi.getPublicStats()

      expect(result).toEqual(payload)
      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/v2/public/stats',
        { method: 'GET' }
      )
    })

    it('throws an error when response is not ok', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 500,
        json: () => Promise.resolve({ error: 'Internal Server Error', code: 'SERVER_ERROR' })
      })

      await expect(publicApi.getPublicStats()).rejects.toThrow('Internal Server Error')
    })

    it('throws with status attached to error', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 503,
        json: () => Promise.resolve({ error: 'Unavailable', code: 'UNAVAILABLE' })
      })

      let thrownError
      try {
        await publicApi.getPublicStats()
      } catch (err) {
        thrownError = err
      }

      expect(thrownError.status).toBe(503)
      expect(thrownError.code).toBe('UNAVAILABLE')
    })

    it('handles non-JSON response gracefully and still throws on non-ok', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 502,
        json: () => Promise.reject(new Error('not json'))
      })

      await expect(publicApi.getPublicStats()).rejects.toThrow()
    })

    it('falls back to default message when error field is missing', async () => {
      mockFetch.mockResolvedValue({
        ok: false,
        status: 500,
        json: () => Promise.resolve({})
      })

      await expect(publicApi.getPublicStats()).rejects.toThrow('Failed to load public stats')
    })
  })
})
