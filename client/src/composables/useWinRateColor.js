/**
 * Shared composable for win rate color styling
 * Returns a CSS class based on win rate value
 *
 * Thresholds match the Win Rate Color System in the UI/UX spec (Section 25):
 *   < 40  → winrate-terrible
 *   40–45 → winrate-bad
 *   45–48 → winrate-poor
 *   48–52 → winrate-average
 *   52–55 → winrate-good
 *   ≥ 55  → winrate-great
 */

/**
 * Get the CSS class for coloring a win rate value
 * @param {number|null|undefined} value - The win rate percentage (0-100)
 * @returns {string} CSS class name for the color
 */
export function getWinRateColorClass(value) {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return 'winrate-neutral'
  }
  if (value < 40) return 'winrate-terrible'
  if (value < 45) return 'winrate-bad'
  if (value < 48) return 'winrate-poor'
  if (value < 52) return 'winrate-average'
  if (value < 55) return 'winrate-good'
  return 'winrate-great'
}

