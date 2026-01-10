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

