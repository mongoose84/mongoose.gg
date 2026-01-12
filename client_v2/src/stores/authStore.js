import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as authApi from '../services/authApi'

export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref(null)
  const isLoading = ref(false)
  const isInitialized = ref(false)
  const error = ref(null)
  const isLinkingAccount = ref(false)

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
      const userData = await authApi.getCurrentUser()
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
      // After login, fetch full user data
      const userData = await authApi.getCurrentUser()
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
      // After registration, user is logged in but not verified
      const userData = await authApi.getCurrentUser()
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
      // Refresh user data to get updated emailVerified status
      const userData = await authApi.getCurrentUser()
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
   * Refresh user data from the server
   */
  async function refreshUser() {
    try {
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
    refreshUser,
    linkRiotAccount,
    unlinkRiotAccount,
    triggerSync
  }
})

