/**
 * Solo and overview analytics API service
 */

import { apiRequest, parseResponse } from './apiClient'
import { appendAccountParam } from './accountContext'

/**
 * Get overview dashboard data for a user
 * @param {number} userId - User ID
 * @returns {Promise<Object|null>} Overview data including playerHeader, rankSnapshot, lastMatch, activeGoals, suggestedActions
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
 * Get radar chart performance profile data
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @returns {Promise<Object|null>} Radar profile data or null if no data found
 */
export async function getRadarChart(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  appendAccountParam(params)

  const endpoint = `/solo/radar-chart/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get radar chart')
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
