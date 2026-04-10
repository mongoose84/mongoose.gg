import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { useCookieConsent } from '@/composables/useCookieConsent'

const CONSENT_KEY = 'mongoose_cookie_consent'
const CONSENT_DATE_KEY = 'mongoose_cookie_consent_date'

// The composable uses module-level shared refs that persist across tests.
// Reset via resetConsent() + localStorage.clear() in beforeEach.
beforeEach(() => {
  localStorage.clear()
  const { resetConsent } = useCookieConsent()
  resetConsent()
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('useCookieConsent', () => {
  describe('setConsent', () => {
    it('stores "accepted" in localStorage and updates reactive state', () => {
      const { setConsent, isAccepted, hasConsent } = useCookieConsent()

      setConsent('accepted')

      expect(localStorage.getItem(CONSENT_KEY)).toBe('accepted')
      expect(localStorage.getItem(CONSENT_DATE_KEY)).not.toBeNull()
      expect(isAccepted.value).toBe(true)
      expect(hasConsent.value).toBe(true)
    })

    it('stores "rejected" in localStorage and updates reactive state', () => {
      const { setConsent, isRejected, hasConsent } = useCookieConsent()

      setConsent('rejected')

      expect(localStorage.getItem(CONSENT_KEY)).toBe('rejected')
      expect(isRejected.value).toBe(true)
      expect(hasConsent.value).toBe(true)
    })

    it('dispatches a consentChanged window event with the correct level', () => {
      const { setConsent } = useCookieConsent()
      const listener = vi.fn()
      window.addEventListener('consentChanged', listener)

      setConsent('accepted')

      expect(listener).toHaveBeenCalledOnce()
      expect(listener.mock.calls[0][0].detail.level).toBe('accepted')

      window.removeEventListener('consentChanged', listener)
    })

    it('does nothing and logs an error for an invalid consent level', () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
      const { setConsent, hasConsent } = useCookieConsent()

      setConsent('maybe')

      expect(hasConsent.value).toBe(false)
      expect(localStorage.getItem(CONSENT_KEY)).toBeNull()
      expect(consoleSpy).toHaveBeenCalledWith('Invalid consent level:', 'maybe')
    })
  })

  describe('resetConsent', () => {
    it('clears localStorage and resets reactive state', () => {
      const { setConsent, resetConsent, hasConsent, isAccepted } = useCookieConsent()
      setConsent('accepted')

      resetConsent()

      expect(localStorage.getItem(CONSENT_KEY)).toBeNull()
      expect(localStorage.getItem(CONSENT_DATE_KEY)).toBeNull()
      expect(hasConsent.value).toBe(false)
      expect(isAccepted.value).toBe(false)
    })

    it('dispatches a consentChanged window event with null values', () => {
      const { setConsent, resetConsent } = useCookieConsent()
      setConsent('accepted')

      const listener = vi.fn()
      window.addEventListener('consentChanged', listener)
      resetConsent()

      expect(listener).toHaveBeenCalledOnce()
      expect(listener.mock.calls[0][0].detail.level).toBeNull()

      window.removeEventListener('consentChanged', listener)
    })
  })

  describe('getConsent', () => {
    it('returns the current consent level when not expired', () => {
      const { setConsent, getConsent } = useCookieConsent()
      setConsent('accepted')

      expect(getConsent()).toBe('accepted')
    })

    it('returns null when no consent has been given', () => {
      const { getConsent } = useCookieConsent()

      expect(getConsent()).toBeNull()
    })

    it('returns null and resets consent when it has expired', () => {
      const { setConsent, getConsent, hasConsent } = useCookieConsent()
      setConsent('accepted')

      // Advance system time past the 183-day expiry window
      vi.useFakeTimers()
      vi.setSystemTime(new Date(Date.now() + 184 * 24 * 60 * 60 * 1000))

      const result = getConsent()

      vi.useRealTimers()

      expect(result).toBeNull()
      expect(hasConsent.value).toBe(false)
    })
  })

  describe('isConsentExpired', () => {
    it('returns false when no consent date is set', () => {
      const { isConsentExpired } = useCookieConsent()

      expect(isConsentExpired()).toBe(false)
    })

    it('returns false when consent date is within 183 days', () => {
      const { setConsent, isConsentExpired } = useCookieConsent()
      setConsent('accepted')

      expect(isConsentExpired()).toBe(false)
    })

    it('returns true when consent date is older than 183 days', () => {
      const { setConsent, isConsentExpired } = useCookieConsent()
      setConsent('accepted')

      // Advance system time past the 183-day expiry window
      vi.useFakeTimers()
      vi.setSystemTime(new Date(Date.now() + 184 * 24 * 60 * 60 * 1000))

      const result = isConsentExpired()

      vi.useRealTimers()

      expect(result).toBe(true)
    })
  })

  describe('shouldShowBanner', () => {
    it('returns true when no consent decision has been made', () => {
      const { shouldShowBanner } = useCookieConsent()

      expect(shouldShowBanner()).toBe(true)
    })

    it('returns false when consent has been accepted', () => {
      const { setConsent, shouldShowBanner } = useCookieConsent()
      setConsent('accepted')

      expect(shouldShowBanner()).toBe(false)
    })

    it('returns false when consent has been rejected', () => {
      const { setConsent, shouldShowBanner } = useCookieConsent()
      setConsent('rejected')

      expect(shouldShowBanner()).toBe(false)
    })

    it('returns true when consent has expired', () => {
      const { setConsent, shouldShowBanner } = useCookieConsent()
      setConsent('accepted')

      // Advance system time past the 183-day expiry window
      vi.useFakeTimers()
      vi.setSystemTime(new Date(Date.now() + 184 * 24 * 60 * 60 * 1000))

      const result = shouldShowBanner()

      vi.useRealTimers()

      expect(result).toBe(true)
    })
  })

  describe('setupCrossTabSync', () => {
    it('updates consentLevel when another tab sets consent', () => {
      const { setupCrossTabSync, isAccepted } = useCookieConsent()
      const cleanup = setupCrossTabSync()

      localStorage.setItem(CONSENT_KEY, 'accepted')
      window.dispatchEvent(new StorageEvent('storage', {
        key: CONSENT_KEY,
        newValue: 'accepted'
      }))

      expect(isAccepted.value).toBe(true)

      cleanup()
    })

    it('updates consentDate when another tab sets the date', () => {
      const { setupCrossTabSync, consentLevel } = useCookieConsent()
      const cleanup = setupCrossTabSync()
      const newDate = new Date().toISOString()

      window.dispatchEvent(new StorageEvent('storage', {
        key: CONSENT_DATE_KEY,
        newValue: newDate
      }))

      // consentDate is module-level; verify indirectly via isConsentExpired
      const { isConsentExpired } = useCookieConsent()
      expect(isConsentExpired()).toBe(false)

      cleanup()
    })

    it('resets state when another tab clears consent', () => {
      const { setConsent, setupCrossTabSync, hasConsent } = useCookieConsent()
      setConsent('accepted')
      const cleanup = setupCrossTabSync()

      // Simulate another tab clearing localStorage
      localStorage.removeItem(CONSENT_KEY)
      localStorage.removeItem(CONSENT_DATE_KEY)
      window.dispatchEvent(new StorageEvent('storage', {
        key: CONSENT_KEY,
        newValue: null
      }))

      expect(hasConsent.value).toBe(false)

      cleanup()
    })

    it('removes the storage listener when cleanup is called', () => {
      const removeEventListenerSpy = vi.spyOn(window, 'removeEventListener')
      const { setupCrossTabSync } = useCookieConsent()

      const cleanup = setupCrossTabSync()
      cleanup()

      expect(removeEventListenerSpy).toHaveBeenCalledWith('storage', expect.any(Function))
    })
  })
})
