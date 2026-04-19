/**
 * Analytics API service for tracking user behavior
 * Events are sent to the backend and stored for Grafana dashboards
 */

import { getBaseApi } from './apiConfig'

const API_BASE = getBaseApi()

// Generate a unique session ID for grouping events
// Fallback for older browsers or non-secure contexts where crypto may be unavailable
const SESSION_ID = (() => {
  try {
    return globalThis.crypto?.randomUUID?.() ??
      `${Date.now()}-${Math.random().toString(36).substring(2, 11)}`
  } catch {
    return `${Date.now()}-${Math.random().toString(36).substring(2, 11)}`
  }
})()

/**
 * Returns the stable session ID for this browser session.
 * @returns {string}
 */
export function getSessionId() {
  return SESSION_ID
}

/**
 * Track a single analytics event
 * @param {string} eventName - Event name (e.g., 'page:view', 'click:nav', 'auth:login')
 * @param {Object} [payload] - Optional event-specific data
 * @returns {Promise<void>} Resolves when event is recorded (fire-and-forget)
 */
export async function track(eventName, payload = null) {
  try {
    await fetch(`${API_BASE}/analytics`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({
        eventName,
        payload,
        sessionId: SESSION_ID
      })
    })
  } catch {
    // Silently fail - analytics should never break the user experience
    console.debug('[Analytics] Failed to track event:', eventName)
  }
}

// ============ Convenience methods for common events ============

/**
 * Track page view
 * @param {string} path - Current route path
 * @param {string} [referrer] - Previous route path
 */
export function trackPageView(path, referrer = null) {
  track('page:view', { path, referrer })
}

/**
 * Track authentication events
 * @param {'login' | 'logout' | 'register'} action - Auth action
 * @param {boolean} success - Whether the action succeeded
 * @param {Object} [extra] - Additional data
 */
export function trackAuth(action, success, extra = {}) {
  track(`auth:${action}`, { success, ...extra })
}

/**
 * Track filter changes (queue type, time range)
 * @param {'queue' | 'time'} filterType - Type of filter changed
 * @param {string} value - New filter value
 */
export function trackFilterChange(filterType, value) {
  track('filter:change', { filterType, value })
}

// ============ Match Details Analytics ============

/**
 * Track match selection in the matches list
 * @param {string} matchId - The selected match ID
 * @param {number} matchIndex - Position in the list (0-based)
 * @param {string} queueType - Current queue filter
 */
export function trackMatchSelect(matchId, matchIndex, queueType) {
  track('match:select', { matchId, matchIndex, queueType })
}

/**
 * Track match details view (when details panel loads)
 * @param {string} matchId - The match ID being viewed
 * @param {string} role - User's role in the match
 * @param {boolean} win - Whether the user won
 */
export function trackMatchDetailsView(matchId, role, win) {
  track('match:details_view', { matchId, role, win })
}

/**
 * Track lane matchup expansion in Match Narrative
 * @param {string} role - The role that was expanded (TOP, JUNGLE, etc.)
 * @param {boolean} isUserRole - Whether this is the user's own role
 * @param {string} laneWinner - Who won the lane (ally, enemy, even)
 */
export function trackLaneExpand(role, isUserRole, laneWinner) {
  track('match:lane_expand', { role, isUserRole, laneWinner })
}

/**
 * Track section toggle in match details
 * @param {string} section - The section identifier
 * @param {boolean} expanded - Whether the section was expanded
 */
export function trackSectionToggle(section, expanded) {
  track('match:section_toggle', { section, expanded })
}

/**
 * Send multiple events in a single batch request
 * @param {Array<{eventName: string, payload?: Object}>} events - Events to send
 * @returns {Promise<void>}
 */
export async function trackBatch(events) {
  if (!events || events.length === 0) return
  try {
    await fetch(`${API_BASE}/analytics/batch`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ events, sessionId: SESSION_ID })
    })
  } catch {
    console.debug('[Analytics] Failed to track batch events')
  }
}

