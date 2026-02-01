/**
 * Centralized API client with global error handling for session expiry.
 * All API calls should use this client to ensure consistent session handling.
 */

import { getBaseApi } from './apiConfig'

const API_BASE = getBaseApi()

// Session expiry callback - will be set by the auth store
let onSessionExpired = null

/**
 * Set the callback to be called when a session expires.
 * This should be called once during app initialization.
 * @param {Function} callback - Function to call when session expires
 */
export function setSessionExpiredCallback(callback) {
  onSessionExpired = callback
}

/**
 * Error codes that indicate session-related issues
 */
export const AUTH_ERROR_CODES = {
  SESSION_EXPIRED: 'SESSION_EXPIRED',
  NOT_AUTHENTICATED: 'NOT_AUTHENTICATED',
  FORBIDDEN: 'FORBIDDEN',
  INVALID_CREDENTIALS: 'INVALID_CREDENTIALS',
  ACCOUNT_DEACTIVATED: 'ACCOUNT_DEACTIVATED'
}

/**
 * Check if an error code indicates the session has expired
 * @param {string} code - Error code from API response
 * @returns {boolean}
 */
export function isSessionExpiredError(code) {
  return code === AUTH_ERROR_CODES.SESSION_EXPIRED
}

/**
 * Check if an error code indicates the user is not authenticated
 * @param {string} code - Error code from API response
 * @returns {boolean}
 */
export function isNotAuthenticatedError(code) {
  return code === AUTH_ERROR_CODES.NOT_AUTHENTICATED
}

/**
 * Make an API request with global session expiry handling.
 * @param {string} endpoint - API endpoint (without base URL)
 * @param {Object} options - Fetch options
 * @param {Object} config - Additional config
 * @param {boolean} config.skipSessionCheck - Skip session expiry handling (for auth endpoints)
 * @returns {Promise<Response>}
 */
export async function apiRequest(endpoint, options = {}, config = {}) {
  const url = endpoint.startsWith('http') ? endpoint : `${API_BASE}${endpoint}`
  
  const response = await fetch(url, {
    ...options,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...options.headers
    }
  })

  // Handle session expiry globally (unless skipped)
  if (!config.skipSessionCheck && response.status === 401) {
    let data = {}
    try {
      data = await response.clone().json()
    } catch {
      // Response might not be JSON
    }

    // Trigger session expired for both SESSION_EXPIRED and NOT_AUTHENTICATED
    // The authStore will check if user was previously logged in before showing the banner
    if ((data.code === AUTH_ERROR_CODES.SESSION_EXPIRED ||
         data.code === AUTH_ERROR_CODES.NOT_AUTHENTICATED) && onSessionExpired) {
      onSessionExpired(data)
    }
  }

  return response
}

/**
 * Make a GET request
 * @param {string} endpoint - API endpoint
 * @param {Object} config - Additional config
 * @returns {Promise<Response>}
 */
export function get(endpoint, config = {}) {
  return apiRequest(endpoint, { method: 'GET' }, config)
}

/**
 * Make a POST request
 * @param {string} endpoint - API endpoint
 * @param {Object} body - Request body
 * @param {Object} config - Additional config
 * @returns {Promise<Response>}
 */
export function post(endpoint, body, config = {}) {
  return apiRequest(endpoint, {
    method: 'POST',
    body: body ? JSON.stringify(body) : undefined
  }, config)
}

/**
 * Make a DELETE request
 * @param {string} endpoint - API endpoint
 * @param {Object} config - Additional config
 * @returns {Promise<Response>}
 */
export function del(endpoint, config = {}) {
  return apiRequest(endpoint, { method: 'DELETE' }, config)
}

/**
 * Parse API response and throw error if not ok
 * @param {Response} response - Fetch response
 * @param {string} defaultError - Default error message
 * @returns {Promise<Object>}
 */
export async function parseResponse(response, defaultError = 'Request failed') {
  let data = {}
  try {
    data = await response.json()
  } catch {
    // Response might not be JSON
  }

  if (!response.ok) {
    const error = new Error(data.error || defaultError)
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

