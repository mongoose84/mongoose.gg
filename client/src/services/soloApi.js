/**
 * Solo and overview analytics API service
 */

import { apiRequest, parseResponse } from './apiClient'
import { appendAccountParam } from './accountContext'

/**
 * Get overview dashboard data for a user
 * @param {number} userId - User ID
 * @returns {Promise<Object|null>} Overview data including playerHeader, lastMatch, mostPlayedChampion, championPool, sessionStats, survivalStats
 */
export async function getOverview(userId) {
  const params = new URLSearchParams()
  appendAccountParam(params)

  const endpoint = `/overview/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get overview data')
}

/**
 * Get solo dashboard data for a user
 * @param {number} userId - User ID
 * @param {string} queueType - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @returns {Promise<Object|null>} Solo dashboard data
 */
export async function getSoloDashboard(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  appendAccountParam(params)

  const endpoint = `/solo/dashboard/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get solo dashboard')
}

/**
 * Get champion select data (champion recommendations based on performance)
 * @param {number} userId - User ID
 * @param {string} queueType - Optional queue filter (all, ranked_solo, ranked_flex, normal, aram)
 * @param {string} [timeRange] - Optional time range (1w, 1m, 3m, 6m, current_season, last_season)
 * @returns {Promise<Object|null>} Champion select data or null if no data found
 */
export async function getChampionSelectData(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  appendAccountParam(params)

  const endpoint = `/champion-select/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get champion select data')
}

/**
 * Get match activity data for the overview heatmap
 * @param {number} userId - User ID
 * @returns {Promise<Object|null>} Match activity data
 */
export async function getMatchActivity(userId) {
  const params = new URLSearchParams()
  appendAccountParam(params)

  const endpoint = `/solo/activity/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get match activity')
}

/**
 * Query string shared by the Solo trend endpoints (features/solo-trends.spec.md).
 * A missing queue lets the server pick the default (Solo/Duo, else Flex, else all queues).
 */
function soloTrendsQuery(queueType, range) {
  const params = new URLSearchParams()
  if (queueType) params.append('queueType', queueType)
  if (range) params.append('range', range)
  appendAccountParam(params)
  return params.toString()
}

/**
 * Get the six match-deciding stat trends and the "Your focus" pick
 * @param {number} userId - User ID
 * @param {string|null} [queueType] - ranked_solo, ranked_flex or all; null for the server's default
 * @param {string} [range] - last20, last50 or season
 * @returns {Promise<{ matches: number, queueType: string, range: string, stats: Array, focus: Object|null }|null>}
 *   null when no Riot account is linked
 */
export async function getSoloStatTrends(userId, queueType = null, range = 'last20') {
  const response = await apiRequest(`/solo/stat-trends/${userId}?${soloTrendsQuery(queueType, range)}`, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get stat trends')
}

/**
 * Get win factors (hit vs missed win rates) and the session, after-a-loss and match-length patterns
 * @param {number} userId - User ID
 * @param {string|null} [queueType] - ranked_solo, ranked_flex or all; null for the server's default
 * @param {string} [range] - last20, last50 or season
 * @returns {Promise<{ matches: number, queueType: string, range: string, factors: Array, patterns: Object }|null>}
 *   null when no Riot account is linked
 */
export async function getSoloWinFactors(userId, queueType = null, range = 'last20') {
  const response = await apiRequest(`/solo/win-factors/${userId}?${soloTrendsQuery(queueType, range)}`, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get win factors')
}

/**
 * Get death position data for the danger zone heatmap
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @param {string} [side] - Optional side filter (all, blue, red)
 * @returns {Promise<Object|null>} Death positions data or null if no data found
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
  appendAccountParam(params)

  const endpoint = `/solo/death-positions/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get death positions')
}

/**
 * Get champion matchups data for a user
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @returns {Promise<Object|null>} Champion matchup data or null if no data found
 */
export async function getChampionMatchups(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  appendAccountParam(params)

  const endpoint = `/solo/matchups/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get champion matchups')
}
