import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'

const ACTIVE_ACCOUNT_KEY = 'mongoose_active_account'

const mockGetItem = vi.fn()
const mockSetItem = vi.fn()
const mockRemoveItem = vi.fn()

Object.defineProperty(global, 'localStorage', {
  value: {
    getItem: mockGetItem,
    setItem: mockSetItem,
    removeItem: mockRemoveItem,
    clear: vi.fn()
  },
  writable: true
})

describe('accountContext', () => {
  let accountContext

  beforeEach(async () => {
    vi.clearAllMocks()
    vi.resetModules()
    mockGetItem.mockReturnValue(null)
    accountContext = await import('@/services/accountContext')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('getAccountParam', () => {
    it('returns "all" when localStorage is empty (defaults to overall)', () => {
      mockGetItem.mockReturnValue(null)
      expect(accountContext.getAccountParam()).toBe('all')
    })

    it('returns "all" when stored value is "overall"', () => {
      mockGetItem.mockReturnValue('overall')
      expect(accountContext.getAccountParam()).toBe('all')
    })

    it('returns "all" when stored value is "all"', () => {
      mockGetItem.mockReturnValue('all')
      expect(accountContext.getAccountParam()).toBe('all')
    })

    it('returns opaque accountId when stored value starts with "acc_"', () => {
      mockGetItem.mockReturnValue('acc_abc123')
      expect(accountContext.getAccountParam()).toBe('acc_abc123')
    })

    it('removes and returns "all" when stored value is a raw PUUID (no acc_ prefix)', () => {
      mockGetItem.mockReturnValue('rawpuuid-without-prefix')
      const result = accountContext.getAccountParam()
      expect(result).toBe('all')
      expect(mockRemoveItem).toHaveBeenCalledWith(ACTIVE_ACCOUNT_KEY)
    })
  })

  describe('appendAccountParam', () => {
    it('appends accountId=all when in overall mode', () => {
      mockGetItem.mockReturnValue(null)
      const params = new URLSearchParams()
      accountContext.appendAccountParam(params)
      expect(params.get('accountId')).toBe('all')
    })

    it('appends opaque accountId when in specific account mode', () => {
      mockGetItem.mockReturnValue('acc_xyz')
      const params = new URLSearchParams()
      accountContext.appendAccountParam(params)
      expect(params.get('accountId')).toBe('acc_xyz')
    })
  })
})
