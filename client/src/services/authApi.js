/**
 * Auth API service for user authentication endpoints
 * All endpoints use cookie-based session authentication
 */

import { getBaseApi } from './apiConfig'
import { apiRequest, parseResponse } from './apiClient'

const API_BASE = getBaseApi()

		/**
		 * Get public aggregate stats for the landing page
		 * @returns {Promise<{ totalMatches: number, activePlayers: number }>}
		 */
		export async function getPublicStats() {
	  const response = await fetch(`${API_BASE}/public/stats`, {
	    method: 'GET'
	  })

	  let data
	  try {
	    data = await response.json()
	  } catch {
	    data = {}
	  }

	  if (!response.ok) {
	    const error = new Error(data.error || 'Failed to load public stats')
	    error.status = response.status
	    error.code = data.code
	    throw error
	  }

	  return data
	}

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

// ============ Overview API ============

/**
 * Get overview dashboard data for a user
 * @param {number} userId - User ID
 * @returns {Promise<Object>} Overview data including playerHeader, rankSnapshot, lastMatch, activeGoals, suggestedActions
 */
export async function getOverview(userId) {
  const response = await apiRequest(`/overview/${userId}`, { method: 'GET' })

  if (response.status === 404) {
    return null // No linked Riot accounts
  }

  return parseResponse(response, 'Failed to get overview data')
}

// ============ Solo Dashboard API ============

/**
 * Get solo dashboard data for a user
 * @param {number} userId - User ID
 * @param {string} queueType - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @returns {Promise<Object>} Solo dashboard data
 */
export async function getSoloDashboard(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }

  const endpoint = `/solo/dashboard/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No match data found
  }

  return parseResponse(response, 'Failed to get solo dashboard')
}

/**
 * Get champion select data (champion recommendations based on performance)
 * @param {number} userId - User ID
 * @param {string} queueType - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @returns {Promise<ChampionSelectResponse | null>} Champion select data or null if no data found
 *
 * @typedef {Object} ChampionSelectResponse
 * @property {MainChampionRoleGroup[]} mainChampions - Champion recommendations grouped by role
 * @property {number} gamesPlayed - Total games played in the time range
 * @property {number} winRate - Overall win rate percentage
 *
 * @typedef {Object} MainChampionRoleGroup
 * @property {string} role - The role (TOP, JUNGLE, MIDDLE, BOTTOM, UTILITY)
 * @property {MainChampionEntry[]} champions - Top champions for this role
 *
 * @typedef {Object} MainChampionEntry
 * @property {string} championName - Champion name
 * @property {number} championId - Champion ID
 * @property {string} role - Role played
 * @property {number} winRate - Win rate percentage
 * @property {number} gamesPlayed - Games played with this champion
 * @property {number} wins - Number of wins
 * @property {number} losses - Number of losses
 * @property {number} mScore - Mongoose score (composite rating)
 */
export async function getChampionSelectData(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }

  const endpoint = `/champion-select/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No match data found
  }

  return parseResponse(response, 'Failed to get champion select data')
}

/**
 * Get match activity data for heatmap (daily match counts for past 6 months)
 * @param {number} userId - User ID
 * @returns {Promise<Object>} Match activity data with dailyMatchCounts, startDate, endDate, totalMatches
 */
export async function getMatchActivity(userId) {
  const response = await apiRequest(`/solo/activity/${userId}`, { method: 'GET' })

  if (response.status === 404) {
    return null // No match data found
  }

  return parseResponse(response, 'Failed to get match activity')
}

// ============ Trends API ============

/**
 * Get winrate trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @param {number} [limit] - Maximum number of most recent games to return (null for all with downsampling)
 * @returns {Promise<Object>} Winrate trend data with winrateTrend array
 */
export async function getWinrateTrend(userId, queueType = 'all', timeRange, limit) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  if (limit) {
    params.append('limit', limit.toString())
  }

  const endpoint = `/trends/winrate/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get winrate trend')
}

/**
 * Get gold at 15 minutes trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @param {number} [limit] - Maximum number of most recent games to return (null for all with downsampling)
 * @returns {Promise<Object>} Gold at 15 trend data with goldAt15Trend array
 */
export async function getGoldAt15Trend(userId, queueType = 'all', timeRange, limit) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  if (limit) {
    params.append('limit', limit.toString())
  }

  const endpoint = `/trends/gold-at-15/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get gold at 15 trend')
}

/**
 * Get CS per minute trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @param {number} [limit] - Maximum number of most recent games to return (null for all with downsampling)
 * @returns {Promise<Object>} CS per minute trend data with csPerMinuteTrend array
 */
export async function getCsPerMinuteTrend(userId, queueType = 'all', timeRange, limit) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  if (limit) {
    params.append('limit', limit.toString())
  }

  const endpoint = `/trends/cs-per-minute/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get CS per minute trend')
}

/**
 * Get deaths over time trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @param {number} [limit] - Maximum number of most recent games to return (null for all with downsampling)
 * @returns {Promise<Object>} Deaths trend data with deathsTrend array and summary statistics
 */
export async function getDeathsTrend(userId, queueType = 'all', timeRange, limit) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  if (limit) {
    params.append('limit', limit.toString())
  }

  const endpoint = `/trends/deaths/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get deaths trend')
}

/**
 * Get dragon participation trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @param {number} [limit] - Maximum number of most recent games to return (null for all with downsampling)
 * @returns {Promise<Object>} Dragon participation trend data with dragonParticipationTrend array and summary statistics
 */
export async function getDragonParticipationTrend(userId, queueType = 'all', timeRange, limit) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  if (limit) {
    params.append('limit', limit.toString())
  }

  const endpoint = `/trends/dragon-participation/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get dragon participation trend')
}

/**
 * Get vision score trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @param {number} [limit] - Maximum number of most recent games to return (null for all with downsampling)
 * @returns {Promise<Object>} Vision score trend data with visionScoreTrend array and summary statistics
 */
export async function getVisionScoreTrend(userId, queueType = 'all', timeRange, limit) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  if (limit) {
    params.append('limit', limit.toString())
  }

  const endpoint = `/trends/vision-score/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get vision score trend')
}

/**
 * Get radar chart performance profile data
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @returns {Promise<Object|null>} Radar profile data with axes and gamesAnalyzed, or null if no data found
 */
export async function getRadarChart(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }

  const endpoint = `/solo/radar-chart/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get radar chart')
}

/**
 * Get death position data for danger zone heatmap
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @param {string} [side] - Optional side filter (all, blue, red)
 * @returns {Promise<Object|null>} Death positions data with deaths array and phase summary, or null if no data found
 */
export async function getDeathPositions(userId, queueType = 'all', timeRange, side = 'all') {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  if (side && side !== 'all') {
    params.append('side', side)
  }

  const endpoint = `/solo/death-positions/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get death positions')
}

// ============ Champion Matchups API ============

/**
 * Get champion matchups data for a user
 * Returns top 5 champions with opponent matchup details
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @returns {Promise<Object>} Champion matchups data with matchups array
 */
export async function getChampionMatchups(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }

  const endpoint = `/solo/matchups/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No data found
  }

  return parseResponse(response, 'Failed to get champion matchups')
}

/**
 * Get match list with trend badges and role baselines
 * @param {number} userId - The user ID
 * @param {string} queueType - Queue filter (ranked_solo, ranked_flex, normal, aram, all)
 * @returns {Promise<{ matches: Array, baselinesByRole: Object, queueType: string, totalMatches: number } | null>}
 */
export async function getMatchList(userId, queueType = 'all') {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }

  const endpoint = `/matches/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // No match data found
  }

  return parseResponse(response, 'Failed to get match list')
}

/**
 * Get full match details for a single match (on-demand)
 * Called when user selects a match from the list
 * @param {string} matchId - The match ID
 * @param {string} puuid - The user's PUUID
 * @returns {Promise<{ match: Object, baseline: Object | null } | null>}
 */
export async function getMatchDetails(matchId, puuid) {
  const params = new URLSearchParams({ puuid })
  const endpoint = `/matches/${matchId}/details?${params.toString()}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null // Match not found
  }

  return parseResponse(response, 'Failed to get match details')
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

/**
 * Get match narrative (lane matchups) for a specific match
 * @param {string} matchId - The match ID
 * @param {string} puuid - The user's PUUID
 * @returns {Promise<{ matchId: string, laneMatchups: Array } | null>}
 */
export async function getMatchNarrative(matchId, puuid) {
  const params = new URLSearchParams({ puuid })
  const endpoint = `/matches/${matchId}/narrative?${params.toString()}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  return parseResponse(response, 'Failed to get match narrative')
}
