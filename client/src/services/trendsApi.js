/**
 * Trend analytics API service
 */

import { apiRequest, parseResponse } from './apiClient'
import { appendAccountParam } from './accountContext'

function buildTrendParams(queueType = 'all', timeRange, limit) {
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
  appendAccountParam(params)
  return params
}

async function getTrendResponse(endpoint, errorMessage) {
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, errorMessage)
}

/**
 * Get winrate trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @param {number} [limit] - Maximum number of most recent games to return
 * @returns {Promise<Object|null>} Winrate trend data or null if no data is found
 */
export async function getWinrateTrend(userId, queueType = 'all', timeRange, limit) {
  const params = buildTrendParams(queueType, timeRange, limit)
  return getTrendResponse(
    `/trends/winrate/${userId}${params.toString() ? '?' + params.toString() : ''}`,
    'Failed to get winrate trend'
  )
}

/**
 * Get gold at 15 trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @param {number} [limit] - Maximum number of most recent games to return
 * @returns {Promise<Object|null>} Gold-at-15 trend data or null if no data is found
 */
export async function getGoldAt15Trend(userId, queueType = 'all', timeRange, limit) {
  const params = buildTrendParams(queueType, timeRange, limit)
  return getTrendResponse(
    `/trends/gold-at-15/${userId}${params.toString() ? '?' + params.toString() : ''}`,
    'Failed to get gold at 15 trend'
  )
}

/**
 * Get CS per minute trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @param {number} [limit] - Maximum number of most recent games to return
 * @returns {Promise<Object|null>} CS-per-minute trend data or null if no data is found
 */
export async function getCsPerMinuteTrend(userId, queueType = 'all', timeRange, limit) {
  const params = buildTrendParams(queueType, timeRange, limit)
  return getTrendResponse(
    `/trends/cs-per-minute/${userId}${params.toString() ? '?' + params.toString() : ''}`,
    'Failed to get CS per minute trend'
  )
}

/**
 * Get deaths trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @param {number} [limit] - Maximum number of most recent games to return
 * @returns {Promise<Object|null>} Deaths trend data or null if no data is found
 */
export async function getDeathsTrend(userId, queueType = 'all', timeRange, limit) {
  const params = buildTrendParams(queueType, timeRange, limit)
  return getTrendResponse(
    `/trends/deaths/${userId}${params.toString() ? '?' + params.toString() : ''}`,
    'Failed to get deaths trend'
  )
}

/**
 * Get dragon participation trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @param {number} [limit] - Maximum number of most recent games to return
 * @returns {Promise<Object|null>} Dragon participation trend data or null if no data is found
 */
export async function getDragonParticipationTrend(userId, queueType = 'all', timeRange, limit) {
  const params = buildTrendParams(queueType, timeRange, limit)
  return getTrendResponse(
    `/trends/dragon-participation/${userId}${params.toString() ? '?' + params.toString() : ''}`,
    'Failed to get dragon participation trend'
  )
}

/**
 * Get vision score trend data for chart display
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @param {number} [limit] - Maximum number of most recent games to return
 * @returns {Promise<Object|null>} Vision score trend data or null if no data is found
 */
export async function getVisionScoreTrend(userId, queueType = 'all', timeRange, limit) {
  const params = buildTrendParams(queueType, timeRange, limit)
  return getTrendResponse(
    `/trends/vision-score/${userId}${params.toString() ? '?' + params.toString() : ''}`,
    'Failed to get vision score trend'
  )
}
