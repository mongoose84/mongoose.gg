import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as authApi from '../services/authApi'

export const useAuthStore = defineStore('auth', () => {
  // State
  const user = ref(null)
  const isLoading = ref(false)
  const isInitialized = ref(false)
  const error = ref(null)

  // Getters
  const isAuthenticated = computed(() => !!user.value)
  const isVerified = computed(() => user.value?.emailVerified ?? false)
  const username = computed(() => user.value?.username ?? '')
  const email = computed(() => user.value?.email ?? '')
  const tier = computed(() => user.value?.tier ?? 'free')
  const userId = computed(() => user.value?.userId ?? null)

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

  return {
    // State
    user,
    isLoading,
    isInitialized,
    error,
    // Getters
    isAuthenticated,
    isVerified,
    username,
    email,
    tier,
    userId,
    // Actions
    initialize,
    login,
    register,
    verify,
    logout,
    clearError
  }
})

