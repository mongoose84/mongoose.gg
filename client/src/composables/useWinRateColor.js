/**
 * Shared composable for win rate color styling
 * Returns a CSS class based on win rate value
 * 
 * Color gradient: red < 47, green > 53, with intermediate warm colors
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
  if (value < 47) return 'winrate-red'
  if (value < 49) return 'winrate-redorange'
  if (value < 51) return 'winrate-orange'
  if (value < 52) return 'winrate-yellow'
  if (value < 53) return 'winrate-yellowgreen'
  return 'winrate-green'
}

/**
 * CSS styles for win rate colors (to be used with :deep() or in global styles)
 * These are the color values for reference:
 * 
 * .winrate-red { color: #ef4444; }
 * .winrate-redorange { color: #f97316; }
 * .winrate-orange { color: #fdba74; }
 * .winrate-yellow { color: #eab308; }
 * .winrate-yellowgreen { color: #84cc16; }
 * .winrate-green { color: #22c55e; }
 * .winrate-neutral { color: var(--color-text); }
 */

