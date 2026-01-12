/**
 * Shared utility for consistent gamer color mapping across all components.
 * Gamer names are sorted alphabetically to ensure consistent colors.
 */

// Color palette for gamers
const GAMER_COLORS = [
  'var(--color-primary)',      // Purple - First gamer (alphabetically)
  'var(--color-success)',      // Green - Second gamer
  '#f59e0b',                   // Amber - Third gamer
  '#ec4899',                   // Pink - Fourth gamer
  '#06b6d4',                   // Cyan - Fifth gamer
];

/**
 * Sort gamer names alphabetically for consistent ordering.
 * @param {string[]} names - Array of gamer names
 * @returns {string[]} - Sorted array of unique gamer names
 */
export function sortGamerNames(names) {
  const uniqueNames = [...new Set(names)];
  return uniqueNames.sort((a, b) => a.localeCompare(b));
}

/**
 * Get color for a gamer based on their position in the sorted list.
 * @param {string} gamerName - The gamer's name
 * @param {string[]} sortedNames - The alphabetically sorted list of gamer names
 * @returns {string} - CSS color value
 */
export function getGamerColor(gamerName, sortedNames) {
  const index = sortedNames.indexOf(gamerName);
  return GAMER_COLORS[index] ?? 'var(--color-text)';
}

/**
 * Create a color getter function for a list of gamer names.
 * @param {string[]} names - Array of gamer names (will be sorted)
 * @returns {{ sortedNames: string[], getColor: (name: string) => string }}
 */
export function createGamerColorMap(names) {
  const sortedNames = sortGamerNames(names);
  return {
    sortedNames,
    getColor: (name) => getGamerColor(name, sortedNames),
  };
}

export { GAMER_COLORS };

