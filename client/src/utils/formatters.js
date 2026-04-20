/**
 * Centralized utility functions for formatting data across the application.
 * Import these functions instead of duplicating formatting logic in components.
 */

// ============================================================================
// Role Formatting
// ============================================================================

/**
 * Format a League of Legends role to a display-friendly name
 * @param {string} role - The role identifier (e.g., 'TOP', 'JUNGLE', 'MIDDLE')
 * @returns {string} The formatted role name
 */
export function formatRole(role) {
  if (!role) return ''
  const roleMap = {
    TOP: 'Top',
    JUNGLE: 'Jungle',
    MIDDLE: 'Mid',
    MID: 'Mid',
    BOTTOM: 'Bot',
    ADC: 'Bot',
    UTILITY: 'Support',
    SUPPORT: 'Support',
    NONE: '',
    UNKNOWN: '',
    FILL: 'Fill',
    ARAM: 'ARAM'
  }
  const upperRole = role.toUpperCase()
  return upperRole in roleMap ? roleMap[upperRole] : role
}

/**
 * Format a role for display with ADC label instead of Bot
 * @param {string} role - The role identifier
 * @returns {string} The formatted role name with ADC for BOTTOM
 */
export function formatRoleWithAdc(role) {
  if (!role) return ''
  const roleMap = {
    TOP: 'Top',
    JUNGLE: 'Jungle',
    MIDDLE: 'Mid',
    MID: 'Mid',
    BOTTOM: 'ADC',
    ADC: 'ADC',
    UTILITY: 'Support',
    SUPPORT: 'Support',
    NONE: '',
    UNKNOWN: 'Fill',
    FILL: 'Fill',
    ARAM: 'ARAM'
  }
  const upperRole = role.toUpperCase()
  return upperRole in roleMap ? roleMap[upperRole] : role
}

// ============================================================================
// Time Formatting
// ============================================================================

/**
 * Format seconds into a duration string (e.g., "32:45")
 * @param {number} seconds - Duration in seconds
 * @returns {string} Formatted duration string
 */
export function formatDuration(seconds) {
  if (seconds === null || seconds === undefined) return '--'
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

/**
 * Format a timestamp to a relative time string (e.g., "5 min ago", "2 days ago")
 * @param {number} timestamp - Unix timestamp in milliseconds
 * @param {object} options - Formatting options
 * @param {boolean} options.short - Use short format (e.g., "5m" instead of "5 min")
 * @returns {string} Relative time string
 */
export function formatRelativeTime(timestamp, { short = false } = {}) {
  if (!timestamp) return ''

  const now = Date.now()
  const diffMs = now - timestamp
  const diffSec = Math.floor(diffMs / 1000)
  const diffMin = Math.floor(diffSec / 60)
  const diffHour = Math.floor(diffMin / 60)
  const diffDay = Math.floor(diffHour / 24)
  const diffWeek = Math.floor(diffDay / 7)
  const diffMonth = Math.floor(diffDay / 30)

  if (short) {
    if (diffMin < 1) return 'just now'
    if (diffMin < 60) return `${diffMin}m ago`
    if (diffHour < 24) return `${diffHour}h ago`
    if (diffDay < 7) return `${diffDay}d ago`
    if (diffWeek < 4) return `${diffWeek}w ago`
    return `${diffMonth}mo ago`
  }

  if (diffMin < 1) return 'just now'
  if (diffMin < 60) return `${diffMin} min ago`
  if (diffHour < 24) return `${diffHour} hour${diffHour > 1 ? 's' : ''} ago`
  if (diffDay < 7) return `${diffDay} day${diffDay > 1 ? 's' : ''} ago`
  if (diffWeek < 4) return `${diffWeek} week${diffWeek > 1 ? 's' : ''} ago`
  return `${diffMonth} month${diffMonth > 1 ? 's' : ''} ago`
}

/**
 * Format a date for display (e.g., "Jan 15")
 * @param {number|Date} date - Date object or timestamp
 * @returns {string} Formatted date string
 */
export function formatDate(date) {
  const d = date instanceof Date ? date : new Date(date)
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

// ============================================================================
// Number Formatting
// ============================================================================

/**
 * Format a large number with K suffix (e.g., 1500 -> "1.5k")
 * @param {number} num - The number to format
 * @returns {string} Formatted number string
 */
export function formatNumber(num) {
  if (num === null || num === undefined) return '0'
  if (num >= 1000) return (num / 1000).toFixed(1) + 'k'
  return num.toString()
}

/**
 * Format a win rate percentage
 * @param {number} value - Win rate value (0-100)
 * @returns {string} Formatted percentage string
 */
export function formatWinRate(value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  return `${value.toFixed(1)}%`
}

/**
 * Format a percentage value
 * @param {number} value - Percentage value
 * @param {number} decimals - Number of decimal places (default: 0)
 * @returns {string} Formatted percentage string
 */
export function formatPercent(value, decimals = 0) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  return `${value.toFixed(decimals)}%`
}

/**
 * Format LP per game with sign
 * @param {number} value - LP value
 * @returns {string} Formatted LP string with sign
 */
export function formatLpPerGame(value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  const rounded = value.toFixed(1)
  const sign = value > 0 ? '+' : ''
  return `${sign}${rounded}`
}

/**
 * Format a gold difference with sign and K suffix
 * @param {number} gold - Gold value
 * @param {object} options - Formatting options
 * @param {boolean} options.useLocale - Use locale formatting instead of K suffix (default: false)
 * @returns {string} Formatted gold string
 */
export function formatGoldDiff(gold, { useLocale = false } = {}) {
  if (gold === null || gold === undefined) return 'N/A'
  const sign = gold >= 0 ? '+' : ''
  if (useLocale) {
    return `${sign}${gold.toLocaleString('en-US')}`
  }
  if (Math.abs(gold) >= 1000) return sign + (gold / 1000).toFixed(1) + 'k'
  return sign + gold.toLocaleString('en-US')
}

/**
 * Format a CS difference with sign
 * @param {number} diff - CS difference value
 * @returns {string} Formatted CS string
 */
export function formatCsDiff(diff) {
  if (diff === null || diff === undefined) return 'N/A'
  const sign = diff >= 0 ? '+' : ''
  return `${sign}${diff} CS`
}

// ============================================================================
// KDA Formatting
// ============================================================================

/**
 * Format KDA as a string (e.g., "5/2/10")
 * @param {number} kills - Number of kills
 * @param {number} deaths - Number of deaths
 * @param {number} assists - Number of assists
 * @returns {string} Formatted KDA string
 */
export function formatKda(kills, deaths, assists) {
  return `${kills}/${deaths}/${assists}`
}

/**
 * Format KDA from a participant object
 * @param {object} participant - Participant object with kills, deaths, assists
 * @returns {string} Formatted KDA string
 */
export function formatKdaFromParticipant(participant) {
  return `${participant.kills}/${participant.deaths}/${participant.assists}`
}

/**
 * Calculate KDA ratio
 * @param {number} kills - Number of kills
 * @param {number} deaths - Number of deaths
 * @param {number} assists - Number of assists
 * @returns {number} KDA ratio
 */
export function calculateKdaRatio(kills, deaths, assists) {
  return deaths === 0 ? kills + assists : (kills + assists) / deaths
}

