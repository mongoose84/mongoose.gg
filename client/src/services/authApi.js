/**
 * Auth API service for user authentication endpoints
 * All endpoints use cookie-based session authentication
 */

import { getBaseApi } from './apiConfig'

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

// ============ Overview API ============

/**
 * Get overview dashboard data for a user
 * @param {number} userId - User ID
 * @returns {Promise<Object>} Overview data including playerHeader, rankSnapshot, lastMatch, activeGoals, suggestedActions
 */
export async function getOverview(userId) {
  const url = `${API_BASE}/overview/${userId}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (response.status === 404) {
    return null // No linked Riot accounts
  }

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get overview data')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
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

  const url = `${API_BASE}/solo/dashboard/${userId}${params.toString() ? '?' + params.toString() : ''}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (response.status === 404) {
    return null // No match data found
  }

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get solo dashboard')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Get champion select data (champion recommendations based on performance)
 * @param {number} userId - User ID
 * @param {string} queueType - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @returns {Promise<Object>} Champion select data
 */
export async function getChampionSelectData(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }

  const url = `${API_BASE}/champion-select/${userId}${params.toString() ? '?' + params.toString() : ''}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (response.status === 404) {
    return null // No match data found
  }

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get champion select data')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Get match activity data for heatmap (daily match counts for past 6 months)
 * @param {number} userId - User ID
 * @returns {Promise<Object>} Match activity data with dailyMatchCounts, startDate, endDate, totalMatches
 */
export async function getMatchActivity(userId) {
  const url = `${API_BASE}/solo/activity/${userId}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (response.status === 404) {
    return null // No match data found
  }

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get match activity')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

// ============ Trends API ============

/**
 * Get winrate trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @returns {Promise<Object>} Winrate trend data with winrateTrend array
 */
export async function getWinrateTrend(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }

  const url = `${API_BASE}/trends/winrate/${userId}${params.toString() ? '?' + params.toString() : ''}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (response.status === 404) {
    return null // No data found
  }

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get winrate trend')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
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

  const url = `${API_BASE}/solo/matchups/${userId}${params.toString() ? '?' + params.toString() : ''}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (response.status === 404) {
    return null // No data found
  }

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get champion matchups')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
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

  const url = `${API_BASE}/matches/${userId}${params.toString() ? '?' + params.toString() : ''}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (response.status === 404) {
    return null // No match data found
  }

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get match list')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}

/**
 * Get match narrative (lane matchups) for a specific match
 * @param {string} matchId - The match ID
 * @param {string} puuid - The user's PUUID
 * @returns {Promise<{ matchId: string, laneMatchups: Array } | null>}
 */
export async function getMatchNarrative(matchId, puuid) {
  const params = new URLSearchParams({ puuid })
  const url = `${API_BASE}/matches/${matchId}/narrative?${params.toString()}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  const data = await response.json()

  if (!response.ok) {
    const error = new Error(data.error || 'Failed to get match narrative')
    error.status = response.status
    error.code = data.code
    throw error
  }

  return data
}
