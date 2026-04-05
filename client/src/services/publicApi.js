/**
 * Public, unauthenticated API service
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
