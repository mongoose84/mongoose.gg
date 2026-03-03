/**
 * Centralized utility for League of Legends asset URLs and data.
 * Provides champion icons, role icons, and other League-specific helpers.
 */

/**
 * Current Data Dragon CDN version for champion assets.
 * Update this when a new League patch is released.
 */
export const DATA_DRAGON_VERSION = '16.1.1'

/**
 * Base URLs for external CDNs
 */
const DATA_DRAGON_CDN = 'https://ddragon.leagueoflegends.com/cdn'
const COMMUNITY_DRAGON_CDN = 'https://raw.communitydragon.org/latest'

/**
 * Normalizes a champion name for use in Data Dragon URLs.
 * Removes spaces and special characters (e.g., "Cho'Gath" -> "ChoGath").
 * @param {string} name - The champion name
 * @returns {string} Normalized champion name for URL usage
 */
export function normalizeChampionName(name) {
  if (!name) return ''
  // Remove spaces, punctuation, etc. (e.g., "Cho'Gath" -> "ChoGath", "Lee Sin" -> "LeeSin")
  return name.replace(/[^A-Za-z0-9]/g, '')
}

/**
 * Generates a Data Dragon CDN URL for a champion icon.
 * @param {string} championName - The champion name (e.g., "Cho'Gath", "Lee Sin")
 * @returns {string} The full URL to the champion icon image
 */
export function getChampionIconUrl(championName) {
  const normalized = normalizeChampionName(championName)
  return `${DATA_DRAGON_CDN}/${DATA_DRAGON_VERSION}/img/champion/${normalized}.png`
}

/**
 * Generates a Data Dragon splash URL for champion mural artwork.
 * @param {string} championName - The champion name (e.g., "Cho'Gath", "Lee Sin")
 * @returns {string} The full URL to the default champion splash image
 */
export function getChampionSplashUrl(championName) {
  const normalized = normalizeChampionName(championName)
  return normalized
    ? `https://ddragon.leagueoflegends.com/cdn/img/champion/splash/${normalized}_0.jpg`
    : ''
}

/**
 * Role icon mapping for Community Dragon URLs.
 * Maps Riot API role names to URL-friendly names.
 */
const ROLE_URL_MAP = {
  TOP: 'top',
  JUNGLE: 'jungle',
  MIDDLE: 'middle',
  BOTTOM: 'bottom',
  UTILITY: 'utility'
}

/**
 * Generates a Community Dragon CDN URL for a role icon.
 * @param {string} role - The role identifier (e.g., 'TOP', 'JUNGLE', 'MIDDLE')
 * @returns {string} The full URL to the role icon image
 */
export function getRoleIconUrl(role) {
  const roleName = ROLE_URL_MAP[role] || 'fill'
  return `${COMMUNITY_DRAGON_CDN}/plugins/rcp-fe-lol-clash/global/default/assets/images/position-selector/positions/icon-position-${roleName}.png`
}

/**
 * Generates a Data Dragon CDN URL for a profile icon.
 * @param {number} profileIconId - The profile icon ID
 * @returns {string} The full URL to the profile icon image
 */
export function getProfileIconUrl(profileIconId) {
  return `${DATA_DRAGON_CDN}/${DATA_DRAGON_VERSION}/img/profileicon/${profileIconId}.png`
}

/**
 * Generates a Data Dragon CDN URL for an item icon.
 * @param {number} itemId - The item ID
 * @returns {string} The full URL to the item icon image
 */
export function getItemIconUrl(itemId) {
  return `${DATA_DRAGON_CDN}/${DATA_DRAGON_VERSION}/img/item/${itemId}.png`
}

/**
 * Generates a Data Dragon CDN URL for a summoner spell icon.
 * @param {string} spellName - The spell name (e.g., "Flash", "Ignite")
 * @returns {string} The full URL to the summoner spell icon image
 */
export function getSummonerSpellIconUrl(spellName) {
  return `${DATA_DRAGON_CDN}/${DATA_DRAGON_VERSION}/img/spell/Summoner${spellName}.png`
}

/**
 * Mapping of Riot region codes to short display labels.
 */
export const REGION_LABELS = {
  euw1: 'EUW', eun1: 'EUNE', na1: 'NA', kr: 'KR', jp1: 'JP',
  br1: 'BR', la1: 'LAN', la2: 'LAS', oc1: 'OCE', tr1: 'TR',
  ru: 'RU', ph2: 'PH', sg2: 'SG', th2: 'TH', tw2: 'TW', vn2: 'VN'
}

/**
 * Converts a Riot region code to a short display label.
 * @param {string} region - The region code (e.g., 'euw1', 'na1')
 * @returns {string} The display label (e.g., 'EUW', 'NA')
 */
export function formatRegion(region) {
  if (!region) return ''
  return REGION_LABELS[region.toLowerCase()] || region.toUpperCase()
}

