/**
 * Feedback API service
 * Handles submission of bug reports and feature requests
 */

import { post, parseResponse } from './apiClient'
import { isDevelopment } from './apiConfig'

/**
 * Get browser information from user agent
 * @returns {string} Browser name and version
 */
function getBrowserInfo() {
  const ua = navigator.userAgent
  
  // Check for common browsers
  if (ua.includes('Firefox/')) {
    const match = ua.match(/Firefox\/(\d+)/)
    return match ? `Firefox ${match[1]}` : 'Firefox'
  }
  if (ua.includes('Edg/')) {
    const match = ua.match(/Edg\/(\d+)/)
    return match ? `Edge ${match[1]}` : 'Edge'
  }
  if (ua.includes('Chrome/')) {
    const match = ua.match(/Chrome\/(\d+)/)
    return match ? `Chrome ${match[1]}` : 'Chrome'
  }
  if (ua.includes('Safari/') && !ua.includes('Chrome')) {
    const match = ua.match(/Version\/(\d+)/)
    return match ? `Safari ${match[1]}` : 'Safari'
  }
  
  return 'Unknown'
}

/**
 * Get OS information from user agent
 * @returns {string} Operating system name
 */
function getOsInfo() {
  const ua = navigator.userAgent
  
  if (ua.includes('Windows NT 10')) return 'Windows 10/11'
  if (ua.includes('Windows')) return 'Windows'
  if (ua.includes('Mac OS X')) return 'macOS'
  if (ua.includes('Linux')) return 'Linux'
  if (ua.includes('Android')) return 'Android'
  if (ua.includes('iPhone') || ua.includes('iPad')) return 'iOS'
  
  return 'Unknown'
}

/**
 * Get current environment
 * @returns {string} Environment name
 */
function getEnvironment() {
  return isDevelopment ? 'development' : 'production'
}

/**
 * Submit feedback (bug report or feature request)
 * @param {Object} feedback - Feedback data
 * @param {string} feedback.type - 'bug' or 'feature'
 * @param {string} feedback.summary - Short summary/title
 * @param {string} feedback.details - Detailed description
 * @param {string} feedback.route - Current route/page
 * @returns {Promise<Object>} Response with success and message
 */
export async function submitFeedback({ type, summary, details, route }) {
  const payload = {
    type,
    summary,
    details: details || null,
    route,
    environment: getEnvironment(),
    browser: getBrowserInfo(),
    os: getOsInfo()
  }
  
  const response = await post('/feedback', payload)
  return parseResponse(response, 'Failed to submit feedback')
}

