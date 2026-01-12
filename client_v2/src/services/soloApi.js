/**
 * Solo Dashboard API service for v2 endpoints
 * All endpoints use cookie-based session authentication
 */

import { getBaseApi } from './apiConfig'

const API_BASE = getBaseApi()

/**
 * Map frontend queue filter values to backend queueType values
 */
function mapQueueFilter(queueFilter) {
  const mapping = {
    'all_ranked': 'all',
    'ranked_solo': 'ranked_solo',
    'ranked_flex': 'ranked_flex',
    'normal': 'normal',
    'aram': 'aram'
  }
  return mapping[queueFilter] || 'all'
}

/**
 * Get solo dashboard data for a specific user
 * @param {number|string} userId - The user ID
 * @param {Object} options - Query options
 * @param {string} options.queueFilter - Queue filter: 'all_ranked', 'ranked_solo', 'ranked_flex', 'normal', 'aram'
 * @param {string} options.timePeriod - Time period: 'week', 'month', '3months', '6months'
 * @returns {Promise<Object>} Solo dashboard data
 */
export async function getSoloDashboard(userId, { queueFilter = 'all_ranked', timePeriod = 'month' } = {}) {
  const params = new URLSearchParams()
  if (queueFilter) params.set('queueType', mapQueueFilter(queueFilter))
  if (timePeriod) params.set('timePeriod', timePeriod)

  const url = `${API_BASE}/solo/dashboard/${userId}?${params.toString()}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (!response.ok) {
    const text = await response.text()
    let errorData = {}
    try {
      errorData = text ? JSON.parse(text) : {}
    } catch {
      // ignore parse error
    }
    const error = new Error(errorData.error || 'Failed to get solo dashboard')
    error.status = response.status
    error.code = errorData.code
    throw error
  }

  return response.json()
}

/**
 * Get champion matchups data for the authenticated user
 * @param {Object} options - Query options
 * @param {number} options.queueId - Optional queue ID filter (e.g., 420 for Ranked Solo)
 * @returns {Promise<Object>} Champion matchups data grouped by champion and role
 */
export async function getChampionMatchups({ queueId } = {}) {
  const params = new URLSearchParams()
  if (queueId) params.set('queueId', queueId.toString())

  const queryString = params.toString()
  const url = `${API_BASE}/solo/champion-matchups${queryString ? '?' + queryString : ''}`

  const response = await fetch(url, {
    method: 'GET',
    credentials: 'include'
  })

  if (!response.ok) {
    const text = await response.text()
    let errorData = {}
    try {
      errorData = text ? JSON.parse(text) : {}
    } catch {
      // ignore parse error
    }
    const error = new Error(errorData.error || 'Failed to get champion matchups')
    error.status = response.status
    error.code = errorData.code
    throw error
  }

  return response.json()
}

// Queue filter constants
export const QUEUE_FILTERS = {
  ALL_RANKED: 'all_ranked',
  RANKED_SOLO: 'ranked_solo',
  RANKED_FLEX: 'ranked_flex',
  NORMAL: 'normal',
  ARAM: 'aram'
}

// Queue ID constants for API calls
export const QUEUE_IDS = {
  RANKED_SOLO: 420,
  RANKED_FLEX: 440,
  NORMAL_DRAFT: 400,
  NORMAL_BLIND: 430,
  ARAM: 450
}

// Time period constants
export const TIME_PERIODS = {
  WEEK: 'week',
  MONTH: 'month',
  THREE_MONTHS: '3months',
  SIX_MONTHS: '6months'
}

