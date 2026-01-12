/**
 * Auth API service for user authentication endpoints
 * All endpoints use cookie-based session authentication
 */

import { getBaseApi } from './apiConfig'

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
 * @param {boolean} params.rememberMe - Keep session for 30 days
 * @returns {Promise<Object>} User data on success
 */
export async function login({ username, password, rememberMe = false }) {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ username, password, rememberMe })
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
 * Get current authenticated user
 * @returns {Promise<Object|null>} User data or null if not authenticated
 */
export async function getCurrentUser() {
  const response = await fetch(`${API_BASE}/users/me`, {
    method: 'GET',
    credentials: 'include'
  })

  if (response.status === 401) {
    return null
  }

  if (!response.ok) {
    const data = await response.json().catch(() => ({}))
    throw new Error(data.error || 'Failed to get user')
  }

  return response.json()
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
  const response = await fetch(`${API_BASE}/users/me/riot-accounts`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ gameName, tagLine, region })
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to link Riot account')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Unlink a Riot account from the current user
 * @param {string} puuid - The PUUID of the account to unlink
 * @returns {Promise<void>}
 */
export async function unlinkRiotAccount(puuid) {
  const response = await fetch(`${API_BASE}/users/me/riot-accounts/${puuid}`, {
    method: 'DELETE',
    credentials: 'include'
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
 * Trigger a sync for a Riot account
 * @param {string} puuid - The PUUID of the account to sync
 * @returns {Promise<Object>} Sync status
 */
export async function triggerRiotAccountSync(puuid) {
  const response = await fetch(`${API_BASE}/users/me/riot-accounts/${puuid}/sync`, {
    method: 'POST',
    credentials: 'include'
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to trigger sync')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Get sync status for a Riot account
 * @param {string} puuid - The PUUID of the account
 * @returns {Promise<Object>} Sync status data
 */
export async function getRiotAccountSyncStatus(puuid) {
  const response = await fetch(`${API_BASE}/users/me/riot-accounts/${puuid}/sync-status`, {
    method: 'GET',
    credentials: 'include'
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get sync status')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}
