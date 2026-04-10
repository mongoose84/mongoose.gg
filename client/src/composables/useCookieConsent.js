import { ref, computed } from 'vue'

/**
 * Cookie consent management composable
 * Handles localStorage-based consent tracking, cross-tab synchronization,
 * and consent expiry (183 days per CNIL guidelines)
 */

const CONSENT_KEY = 'mongoose_cookie_consent'
const CONSENT_DATE_KEY = 'mongoose_cookie_consent_date'
const CONSENT_EXPIRY_DAYS = 183

// Shared reactive state across all instances
const consentLevel = ref(localStorage.getItem(CONSENT_KEY) || null)
const consentDate = ref(localStorage.getItem(CONSENT_DATE_KEY) || null)

export function useCookieConsent() {
  // Computed properties
  const hasConsent = computed(() => consentLevel.value !== null)
  const isAccepted = computed(() => consentLevel.value === 'accepted')
  const isRejected = computed(() => consentLevel.value === 'rejected')
  const isExpired = computed(() => isConsentExpired())

  /**
   * Check if stored consent has expired (>183 days old)
   */
  function isConsentExpired() {
    if (!consentDate.value) return false

    try {
      const date = new Date(consentDate.value)
      if (Number.isNaN(date.getTime())) return true

      const now = new Date()
      const daysDiff = (now - date) / (1000 * 60 * 60 * 24)
      return daysDiff > CONSENT_EXPIRY_DAYS
    } catch {
      return true
    }
  }

  /**
   * Set consent level ('accepted' or 'rejected')
   * Stores both the consent level and the date for expiry tracking
   */
  function setConsent(level) {
    if (level !== 'accepted' && level !== 'rejected') {
      console.error('Invalid consent level:', level)
      return
    }

    const now = new Date().toISOString()
    consentLevel.value = level
    consentDate.value = now

    localStorage.setItem(CONSENT_KEY, level)
    localStorage.setItem(CONSENT_DATE_KEY, now)

    // Dispatch custom event for other parts of the app to react to consent change
    window.dispatchEvent(
      new CustomEvent('consentChanged', { detail: { level, date: now } })
    )
  }

  /**
   * Reset consent and show banner again
   */
  function resetConsent() {
    consentLevel.value = null
    consentDate.value = null

    localStorage.removeItem(CONSENT_KEY)
    localStorage.removeItem(CONSENT_DATE_KEY)

    window.dispatchEvent(
      new CustomEvent('consentChanged', { detail: { level: null, date: null } })
    )
  }

  /**
   * Get current consent level
   * Returns 'accepted', 'rejected', or null if not decided
   * Automatically resets if expired
   */
  function getConsent() {
    if (isExpired.value) {
      resetConsent()
      return null
    }
    return consentLevel.value
  }

  /**
   * Listen to storage events for cross-tab synchronization
   * When another tab changes consent, update this tab's state
   */
  function setupCrossTabSync() {
    const handleStorageChange = (event) => {
      if (event.key === CONSENT_KEY) {
        consentLevel.value = event.newValue
      } else if (event.key === CONSENT_DATE_KEY) {
        consentDate.value = event.newValue
      }

      // If both keys are cleared, it means consent was reset
      if (event.newValue === null && (event.key === CONSENT_KEY || event.key === CONSENT_DATE_KEY)) {
        if (!localStorage.getItem(CONSENT_KEY)) {
          consentLevel.value = null
          consentDate.value = null
        }
      }
    }

    window.addEventListener('storage', handleStorageChange)

    return () => {
      window.removeEventListener('storage', handleStorageChange)
    }
  }

  /**
   * Check if banner should be shown
   * Shows if:
   * - No consent decision made yet
   * - Consent has expired
   */
  function shouldShowBanner() {
    return !hasConsent.value || isExpired.value
  }

  return {
    // State
    consentLevel,
    hasConsent,
    isAccepted,
    isRejected,
    isExpired,

    // Methods
    setConsent,
    resetConsent,
    getConsent,
    isConsentExpired,
    setupCrossTabSync,
    shouldShowBanner
  }
}
