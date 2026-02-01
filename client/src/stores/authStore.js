import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as authApi from '../services/authApi'
import { setSessionExpiredCallback } from '../services/apiClient'

export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref(null)
  const isLoading = ref(false)
  const isInitialized = ref(false)
  const error = ref(null)
  const isLinkingAccount = ref(false)

  // Session expiry state
  const sessionExpired = ref(false)
  const sessionExpiredMessage = ref('')

  // Getters
  const isAuthenticated = computed(() => !!user.value)
  const isVerified = computed(() => user.value?.emailVerified ?? false)
  const username = computed(() => user.value?.username ?? '')
  const email = computed(() => user.value?.email ?? '')
  const tier = computed(() => user.value?.tier ?? 'free')
  const userId = computed(() => user.value?.userId ?? null)

  // Riot account getters
  const riotAccounts = computed(() => user.value?.riotAccounts ?? [])
  const hasLinkedAccount = computed(() => riotAccounts.value.length > 0)
  const primaryRiotAccount = computed(() => riotAccounts.value.find(a => a.isPrimary) ?? riotAccounts.value[0] ?? null)

  // Actions
  async function initialize() {
    if (isInitialized.value) return

    isLoading.value = true
    error.value = null

    try {
      // Skip session check during initialization - user wasn't previously authenticated
      // in this browser session, so we don't want to show session expired banner
      const userData = await authApi.getCurrentUser({ skipSessionCheck: true })
      user.value = userData
    } catch (e) {
      // Not authenticated is not an error state
      user.value = null
    } finally {
      isLoading.value = false
      isInitialized.value = true
    }
  }

  async function login({ username: uname, password, rememberMe = false }) {
    isLoading.value = true
    error.value = null

    try {
      // Call login API first
      await authApi.login({ username: uname, password, rememberMe })
      // After login, fetch full user data (session is fresh, skip session check)
      const userData = await authApi.getCurrentUser({ skipSessionCheck: true })
      user.value = userData
      return { success: true, emailVerified: userData?.emailVerified }
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      isLoading.value = false
    }
  }

  async function register({ username: uname, email: em, password }) {
    isLoading.value = true
    error.value = null

    try {
      // Call register API first
      await authApi.register({ username: uname, email: em, password })
      // After registration, user is logged in but not verified (session is fresh, skip session check)
      const userData = await authApi.getCurrentUser({ skipSessionCheck: true })
      user.value = userData
      return { success: true, needsVerification: true }
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      isLoading.value = false
    }
  }

  async function verify(code) {
    isLoading.value = true
    error.value = null

    try {
      await authApi.verify(code)
      // Refresh user data to get updated emailVerified status (session is fresh, skip session check)
      const userData = await authApi.getCurrentUser({ skipSessionCheck: true })
      user.value = userData
      return { success: true }
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      isLoading.value = false
    }
  }

  async function logout() {
    isLoading.value = true
    error.value = null
    
    try {
      await authApi.logout()
      user.value = null
    } catch (e) {
      error.value = e.message
      // Clear user anyway on logout failure
      user.value = null
      throw e
    } finally {
      isLoading.value = false
    }
  }

  function clearError() {
    error.value = null
  }

  /**
   * Handle session expiry - called by the API client when a SESSION_EXPIRED or NOT_AUTHENTICATED error is received.
   * This clears the user state and shows the session expired banner.
   * Only triggers if the user was previously authenticated (to avoid showing on initial page load).
   * @param {Object} errorData - Error data from the API response
   */
  function handleSessionExpired(errorData) {
    // Only handle if user was previously authenticated (or banner already showing)
    if (user.value || sessionExpired.value) {
      user.value = null
      sessionExpired.value = true
      sessionExpiredMessage.value = errorData?.error || 'Your session has expired. Please log in again.'
    }
  }

  /**
   * Clear the session expired state (after user acknowledges or logs in again)
   */
  function clearSessionExpired() {
    sessionExpired.value = false
    sessionExpiredMessage.value = ''
  }

  /**
   * Initialize the session expiry callback.
   * This should be called once during app initialization.
   */
  function initializeSessionHandler() {
    setSessionExpiredCallback(handleSessionExpired)
  }

  /**
   * Refresh user data from the server.
   * Note: This does NOT skip session check, so if the session has expired,
   * the session expired banner will be shown.
   */
  async function refreshUser() {
    try {
      // Don't skip session check - if session expired, show the banner
      const userData = await authApi.getCurrentUser()
      user.value = userData
    } catch (e) {
      // Silent fail - user might be logged out
      console.error('Failed to refresh user:', e)
    }
  }

  /**
   * Link a Riot account to the current user
   */
  async function linkRiotAccount({ gameName, tagLine, region }) {
    isLinkingAccount.value = true
    error.value = null

    try {
      const linkedAccount = await authApi.linkRiotAccount({ gameName, tagLine, region })
      // Refresh user data to get updated riot accounts list
      await refreshUser()
      return { success: true, account: linkedAccount }
    } catch (e) {
      error.value = e.message
      throw e
    } finally {
      isLinkingAccount.value = false
    }
  }

  /**
   * Unlink a Riot account from the current user
   */
  async function unlinkRiotAccount(puuid) {
    error.value = null

    try {
      await authApi.unlinkRiotAccount(puuid)
      // Refresh user data to get updated riot accounts list
      await refreshUser()
      return { success: true }
    } catch (e) {
      error.value = e.message
      throw e
    }
  }

  /**
   * Trigger a sync for a Riot account
   */
  async function triggerSync(puuid) {
    error.value = null

    try {
      const result = await authApi.triggerRiotAccountSync(puuid)
      // Refresh user data to get updated sync status
      await refreshUser()
      return result
    } catch (e) {
      error.value = e.message
      throw e
    }
  }

  return {
    // State
    user,
    isLoading,
    isInitialized,
    isLinkingAccount,
    error,
    sessionExpired,
    sessionExpiredMessage,
    // Getters
    isAuthenticated,
    isVerified,
    username,
    email,
    tier,
    userId,
    riotAccounts,
    hasLinkedAccount,
    primaryRiotAccount,
    // Actions
    initialize,
    login,
    register,
    verify,
    logout,
    clearError,
    clearSessionExpired,
    initializeSessionHandler,
    refreshUser,
    linkRiotAccount,
    unlinkRiotAccount,
    triggerSync
  }
})

