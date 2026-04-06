/**
 * Match history and details API service
 */

import { apiRequest, parseResponse } from './apiClient'
import { appendAccountParam, getAccountParam } from './accountContext'

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
  appendAccountParam(params)

  const endpoint = `/matches/${userId}${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get match list')
}

/**
 * Get full match details for a single match (on-demand)
 * @param {string} matchId - The match ID
 * @param {string} [accountId] - Opaque account identifier (acc_...). Defaults to active account context.
 * @returns {Promise<{ match: Object, baseline: Object | null } | null>}
 */
export async function getMatchDetails(matchId, accountId = getAccountParam()) {
  const params = new URLSearchParams()
  if (accountId && accountId !== 'all') {
    params.append('accountId', accountId)
  }

  const endpoint = `/matches/${matchId}/details${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get match details')
}

/**
 * Get match narrative (lane matchups) for a specific match
 * @param {string} matchId - The match ID
 * @param {string} [accountId] - Opaque account identifier (acc_...). Defaults to active account context.
 * @returns {Promise<{ matchId: string, laneMatchups: Array } | null>}
 */
export async function getMatchNarrative(matchId, accountId = getAccountParam()) {
  const params = new URLSearchParams()
  if (accountId && accountId !== 'all') {
    params.append('accountId', accountId)
  }

  const endpoint = `/matches/${matchId}/narrative${params.toString() ? '?' + params.toString() : ''}`
  const response = await apiRequest(endpoint, { method: 'GET' })

  if (response.status === 404) {
    return null
  }

  return parseResponse(response, 'Failed to get match narrative')
}
