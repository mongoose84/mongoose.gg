/**
 * Auth API service for user authentication endpoints
 * All endpoints use cookie-based session authentication
 */

import { getBaseApi } from './apiConfig'
import { apiRequest, parseResponse } from './apiClient'

export { getAccountParam } from './accountContext'

const API_BASE = getBaseApi()

/**
 * Register a new user
 * @param {Object} params - Registration params
 * @param {string} params.username - Unique username (3-50 chars)
 * @param {string} params.email - User email
 * @param {string} params.password - User password
 * @returns {Promise<Object>} User data on success
 */
export async function register({ username, email, password }) {
  const response = await fetch(`${API_BASE}/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ username, email, password })
  })

  const data = await response.json()
  
  if (!response.ok) {
    const error = new Error(data.error || 'Registration failed')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Login user
 * @param {Object} params - Login params
 * @param {string} params.username - Username or email
 * @param {string} params.password - User password
 * @param {string} params.consentLevel - Cookie consent level ('accepted' or 'rejected')
 * @returns {Promise<Object>} User data on success
 */
export async function login({ username, password, consentLevel = 'accepted' }) {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ username, password, consentLevel })
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Login failed')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Logout current user
 * @returns {Promise<void>}
 */
export async function logout() {
  const response = await fetch(`${API_BASE}/auth/logout`, {
    method: 'POST',
    credentials: 'include'
  })

  if (!response.ok) {
    const data = await response.json().catch(() => ({}))
    throw new Error(data.error || 'Logout failed')
  }
}

/**
 * Delete current user's account permanently
 * Requires password confirmation
 * @param {string} password - User's current password for confirmation
 * @returns {Promise<Object>} Success response
 */
export async function deleteAccount(password) {
  const response = await apiRequest('/auth/account', {
    method: 'DELETE',
    body: JSON.stringify({ password })
  })

  return parseResponse(response, 'Failed to delete account')
}

/**
 * Verify user email with 6-digit code
 * @param {string} code - 6-digit verification code
 * @returns {Promise<Object>} Verification result
 */
export async function verify(code) {
  const response = await fetch(`${API_BASE}/auth/verify`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ code })
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Verification failed')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Resend verification email
 * @returns {Promise<Object>} Result with success status
 */
export async function resendVerification() {
  const response = await fetch(`${API_BASE}/auth/resend-verification`, {
    method: 'POST',
    credentials: 'include'
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to resend verification code')
    error.status = response.status
    error.code = data.code
    error.waitSeconds = data.waitSeconds
    throw error
  }

  return data
}

/**
 * Get current authenticated user
 * @param {Object} options - Options
 * @param {boolean} options.skipSessionCheck - Skip session expiry handling (default: false)
 *   Set to true during initialization when user wasn't previously authenticated.
 *   Set to false (default) during refreshUser() so session expiry banner can appear.
 * @returns {Promise<Object|null>} User data or null if not authenticated
 */
export async function getCurrentUser({ skipSessionCheck = false } = {}) {
  const response = await apiRequest('/users/me', { method: 'GET' }, { skipSessionCheck })

  if (response.status === 401) {
    return null
  }

  if (!response.ok) {
    const data = await response.json().catch(() => ({}))
    throw new Error(data.error || 'Failed to get user')
  }

  return response.json()
}

/**
 * Update the current user's persisted icon preference.
 * @param {number|null} userIconId - League profile icon ID, or null to clear.
 * @returns {Promise<Object>} Success payload
 */
export async function updateUserIcon(userIconId) {
  const response = await apiRequest('/users/me/icon', {
    method: 'PUT',
    body: JSON.stringify({ userIconId })
  })

  return parseResponse(response, 'Failed to update user icon')
}

// ============ Riot Account API ============

/**
 * Link a Riot account to the current user
 * @param {Object} params - Link params
 * @param {string} params.gameName - Riot game name (e.g., "Faker")
 * @param {string} params.tagLine - Riot tag line (e.g., "KR1")
 * @param {string} params.region - Region code (e.g., "euw1", "na1", "kr")
 * @returns {Promise<Object>} Linked account data
 */
export async function linkRiotAccount({ gameName, tagLine, region }) {
  const response = await apiRequest('/users/me/riot-accounts', {
    method: 'POST',
    body: JSON.stringify({ gameName, tagLine, region })
  })

  if (!response.ok) {
    const data = await response.json().catch(() => ({}))
    const error = new Error(data.error || 'Failed to link Riot account')
    error.status = response.status
    error.code = data.code
    if (typeof data.currentLimit === 'number') {
      error.currentLimit = data.currentLimit
    }
    if (typeof data.tier === 'string') {
      error.tier = data.tier
    }
    throw error
  }

  return response.json()
}

/**
 * Unlink a Riot account from the current user
 * @param {string} puuid - The PUUID of the account to unlink
 * @returns {Promise<void>}
 */
export async function unlinkRiotAccount(puuid) {
  const response = await apiRequest(`/users/me/riot-accounts/${puuid}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    const data = await response.json().catch(() => ({}))
    const error = new Error(data.error || 'Failed to unlink Riot account')
    error.status = response.status
    error.code = data.code
    throw error
  }
}

/**
 * Set a linked Riot account as primary for the current user
 * @param {string} puuid - The PUUID of the account to set as primary
 * @returns {Promise<Object>} Success response
 */
export async function setPrimaryRiotAccount(puuid) {
  const response = await apiRequest(`/users/me/riot-accounts/${puuid}/primary`, {
    method: 'PUT'
  })

  return parseResponse(response, 'Failed to set primary Riot account')
}

/**
 * Trigger a sync for a Riot account
 * @param {string} puuid - The PUUID of the account to sync
 * @returns {Promise<Object>} Sync status
 */
export async function triggerRiotAccountSync(puuid) {
  const response = await apiRequest(`/users/me/riot-accounts/${puuid}/sync`, {
    method: 'POST'
  })

  return parseResponse(response, 'Failed to trigger sync')
}

/**
 * Get sync status for a Riot account
 * @param {string} puuid - The PUUID of the account
 * @returns {Promise<Object>} Sync status data
 */
export async function getRiotAccountSyncStatus(puuid) {
  const response = await apiRequest(`/users/me/riot-accounts/${puuid}/sync-status`, {
    method: 'GET'
  })

  return parseResponse(response, 'Failed to get sync status')
}

// ============ Password Management API ============

/**
 * Request a password reset code (forgot password)
 * Always returns 200 regardless of whether email exists (prevents email enumeration)
 * @param {string} email - User's email address
 * @returns {Promise<Object>} Success response
 */
export async function forgotPassword(email) {
  const response = await fetch(`${API_BASE}/auth/forgot-password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email })
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to send reset code')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Reset password using 6-digit code received via email
 * @param {Object} params - Reset params
 * @param {string} params.email - User's email address
 * @param {string} params.code - 6-digit reset code
 * @param {string} params.newPassword - New password (≥ 8 chars)
 * @returns {Promise<Object>} Success response
 */
export async function resetPassword({ email, code, newPassword }) {
  const response = await fetch(`${API_BASE}/auth/reset-password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, code, newPassword })
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to reset password')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Change password for the currently authenticated user
 * The server will invalidate the session after a successful change
 * @param {Object} params - Change password params
 * @param {string} params.currentPassword - User's current password
 * @param {string} params.newPassword - New password (≥ 8 chars)
 * @returns {Promise<Object>} Success response
 */
export async function changePassword({ currentPassword, newPassword }) {
  const response = await apiRequest('/auth/change-password', {
    method: 'POST',
    body: JSON.stringify({ currentPassword, newPassword })
  })

  return parseResponse(response, 'Failed to change password')
}
