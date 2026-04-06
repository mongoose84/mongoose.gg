import { describe, it, expect, vi, beforeEach } from 'vitest'

const mockPost = vi.fn()
const mockParseResponse = vi.fn(async (response) => response._data)

vi.mock('@/services/apiClient', () => ({
  post: (...args) => mockPost(...args),
  parseResponse: (...args) => mockParseResponse(...args)
}))

vi.mock('@/services/apiConfig', () => ({
  isDevelopment: false
}))

// Stub browser APIs
Object.defineProperty(global, 'navigator', {
  value: {
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120',
    brave: undefined
  },
  writable: true
})

describe('feedbackApi', () => {
  let feedbackApi

  beforeEach(async () => {
    vi.clearAllMocks()
    vi.resetModules()
    mockPost.mockResolvedValue({ status: 200, _data: { success: true, message: 'Thank you!' } })
    mockParseResponse.mockResolvedValue({ success: true, message: 'Thank you!' })
    feedbackApi = await import('@/services/feedbackApi')
  })

  describe('submitFeedback', () => {
    it('calls POST /feedback with correct payload fields', async () => {
      await feedbackApi.submitFeedback({
        type: 'bug',
        summary: 'Something broke',
        details: 'Details here',
        route: '/solo/dashboard'
      })

      expect(mockPost).toHaveBeenCalledWith(
        '/feedback',
        expect.objectContaining({
          type: 'bug',
          summary: 'Something broke',
          details: 'Details here',
          route: '/solo/dashboard'
        })
      )
    })

    it('includes browser, os, and environment in payload', async () => {
      await feedbackApi.submitFeedback({
        type: 'feature',
        summary: 'New feature',
        details: null,
        route: '/overview'
      })

      const payload = mockPost.mock.calls[0][1]
      expect(payload).toHaveProperty('browser')
      expect(payload).toHaveProperty('os')
      expect(payload).toHaveProperty('environment')
    })

    it('sets details to null when not provided', async () => {
      await feedbackApi.submitFeedback({
        type: 'feature',
        summary: 'Idea',
        route: '/overview'
      })

      const payload = mockPost.mock.calls[0][1]
      expect(payload.details).toBeNull()
    })

    it('returns parsed response from parseResponse', async () => {
      const result = await feedbackApi.submitFeedback({
        type: 'bug',
        summary: 'Bug',
        details: 'info',
        route: '/'
      })

      expect(result).toEqual({ success: true, message: 'Thank you!' })
    })

    it('sets environment to "production" when not development', async () => {
      await feedbackApi.submitFeedback({ type: 'bug', summary: 'x', route: '/' })
      const payload = mockPost.mock.calls[0][1]
      expect(payload.environment).toBe('production')
    })
  })
})
