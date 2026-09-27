/**
 * Copy for the Overview page, built from the existing OverviewResponse fields
 * (sessionStats, survivalStats, playerHeader, mostPlayedChampion).
 * Follows the design-system voice: "you", finding → evidence → fix, whole-number
 * percentages, per-match stats with one decimal, "match" never "game".
 */

// Win-rate gap (low-death vs high-death matches) that counts as a real finding
const DEATH_GAP_THRESHOLD = 0.15

function plural(count, singular, pluralForm = `${singular}es`) {
  return `${count} ${count === 1 ? singular : pluralForm}`
}

function matches(count) {
  return plural(count, 'match')
}

function percent(ratio) {
  return Math.round(ratio * 100)
}

/**
 * "GOLD II" → "Gold II", "MASTER" → "Master"
 */
export function formatRankLabel(rank) {
  if (!rank) return null
  const [tier, division] = rank.trim().split(/\s+/)
  const formattedTier = tier.charAt(0).toUpperCase() + tier.slice(1).toLowerCase()
  return division ? `${formattedTier} ${division}` : formattedTier
}

/**
 * The page headline: one sentence about this week.
 */
export function buildHeroHeadline(sessionStats) {
  const played = sessionStats?.gamesThisWeek ?? 0
  const wins = sessionStats?.winsThisWeek ?? 0

  if (played === 0) return 'No matches this week yet'
  if (played === 1) return wins === 1 ? 'You won your one match this week' : 'You lost your one match this week'
  return `You've won ${wins} of ${played} matches this week`
}

/**
 * One supporting sentence under the headline, from the survival stats when they tell something.
 */
export function buildHeroText(sessionStats, survivalStats) {
  const s = survivalStats
  if (s && s.totalGames > 0) {
    const hasBothRates = s.winRateLowDeaths != null && s.winRateHighDeaths != null
    if (hasBothRates && s.winRateLowDeaths - s.winRateHighDeaths >= DEATH_GAP_THRESHOLD) {
      return `You win ${percent(s.winRateLowDeaths)}% of matches with ${s.lowDeathThreshold} or fewer deaths `
        + `and ${percent(s.winRateHighDeaths)}% with ${s.highDeathThreshold} or more.`
    }
    return `You average ${s.avgDeathsPerGame.toFixed(1)} deaths per match over your last ${matches(s.totalGames)}.`
  }

  if ((sessionStats?.gamesThisWeek ?? 0) === 0) {
    return 'Play a match and your Overview updates within a few minutes.'
  }
  return null
}

/**
 * Up to two glass chips for the hero: matches and win rate this week.
 */
export function buildHeroChips(sessionStats) {
  const played = sessionStats?.gamesThisWeek ?? 0
  if (played === 0) return []

  const wins = sessionStats?.winsThisWeek ?? 0
  return [`${matches(played)} this week`, `${Math.round((wins / played) * 100)}% win rate`]
}

/**
 * Riot ID · champion main · rank · LP
 */
export function buildPlayerLine(playerHeader, championName) {
  if (!playerHeader) return null
  const rank = formatRankLabel(playerHeader.rank)

  return [
    playerHeader.summonerName,
    championName ? `${championName} main` : null,
    rank,
    rank && playerHeader.lp != null ? `${playerHeader.lp} LP` : null
  ].filter(Boolean).join(' · ')
}

/**
 * The deaths finding as an insight, or null when the numbers don't show one.
 * Strength when the player already stays in the low-death range, Pattern when not.
 */
export function buildSurvivalInsight(survivalStats) {
  const s = survivalStats
  if (!s || s.totalGames === 0) return null
  if (s.winRateLowDeaths == null || s.winRateHighDeaths == null) return null
  if (s.gamesLowDeaths === 0 || s.gamesHighDeaths === 0) return null
  if (s.winRateLowDeaths - s.winRateHighDeaths < DEATH_GAP_THRESHOLD) return null

  const low = percent(s.winRateLowDeaths)
  const high = percent(s.winRateHighDeaths)
  const avg = s.avgDeathsPerGame.toFixed(1)

  if (s.avgDeathsPerGame <= s.lowDeathThreshold) {
    return {
      kind: 'strength',
      title: `You win ${low}% of matches with ${s.lowDeathThreshold} or fewer deaths`,
      text: `You average ${avg} deaths per match over your last ${matches(s.totalGames)}, inside that range. `
        + `With ${s.highDeathThreshold} or more deaths you win ${high}%.`
    }
  }

  return {
    kind: 'pattern',
    title: `You win ${low}% of matches with ${s.lowDeathThreshold} or fewer deaths`,
    text: `With ${s.highDeathThreshold} or more it drops to ${high}%. You average ${avg} per match `
      + `over your last ${matches(s.totalGames)}, so one less death a match is the quickest way up.`
  }
}

/**
 * The line above the match row in "Today's matches".
 */
export function buildTodaySummary(sessionStats) {
  const played = sessionStats?.gamesToday ?? 0
  if (played === 0) return 'No matches today yet'

  const wins = sessionStats?.winsToday ?? 0
  const losses = sessionStats?.lossesToday ?? 0
  return `${matches(played)} today · ${plural(wins, 'win', 'wins')}, ${plural(losses, 'loss')}`
}

/**
 * Whether a match result string from the API is a win.
 */
export function isWinResult(result) {
  const r = (result || '').toLowerCase()
  return r === 'victory' || r === 'win'
}
