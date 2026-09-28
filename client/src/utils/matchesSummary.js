/**
 * Copy and small rules for the Matches page, kept pure so they can be unit tested.
 * Voice: "you", finding first, sentence case, no emoji (design system).
 */

/** Matches shorter than this ended in a remake: neither a win nor a loss */
export const REMAKE_MAX_SECONDS = 300

export function isRemake(match) {
  return typeof match?.gameDurationSec === 'number' && match.gameDurationSec < REMAKE_MAX_SECONDS
}

export function matchKda(match) {
  if (!match) return null
  return `${match.kills}/${match.deaths}/${match.assists}`
}

/** Riot ID (Name#TAG) of the account a match was played on, for Overall mode */
export function matchRiotId(match) {
  if (!match?.accountGameName) return null
  return match.accountTagLine ? `${match.accountGameName}#${match.accountTagLine}` : match.accountGameName
}

/**
 * A signed number with a real minus sign and thousands separators: formatSigned(1.24, 1) → "+1.2",
 * formatSigned(-1250) → "−1,250". A value that rounds to zero shows as "+0".
 */
export function formatSigned(value, decimals = 0) {
  if (typeof value !== 'number' || Number.isNaN(value)) return '—'
  const rounded = Number(value.toFixed(decimals))
  const magnitude = Math.abs(rounded).toLocaleString('en-US', {
    maximumFractionDigits: decimals
  })
  return rounded < 0 ? `−${magnitude}` : `+${magnitude}`
}

function countedMatches(matches) {
  return (matches || []).filter((m) => !isRemake(m))
}

/**
 * The page headline: "You won 12 of your last 20 matches".
 * Returns null when there is nothing to summarise.
 */
export function buildMatchesHeadline(matches) {
  const counted = countedMatches(matches)
  if (!counted.length) return null
  const wins = counted.filter((m) => m.win).length
  if (counted.length === 1) return wins ? 'You won your last match' : 'You lost your last match'
  return `You won ${wins} of your last ${counted.length} matches`
}

/** Current streak from the newest match: { win, length } or null */
export function currentStreak(matches) {
  const counted = countedMatches(matches)
  if (!counted.length) return null
  const win = counted[0].win
  let length = 0
  for (const match of counted) {
    if (match.win !== win) break
    length++
  }
  return { win, length }
}

/** Most played champion in the list with its record, ties broken by the newest match */
export function mostPlayedChampion(matches) {
  const counted = countedMatches(matches)
  const byChampion = new Map()
  for (const match of counted) {
    const entry = byChampion.get(match.championName) || { championName: match.championName, matches: 0, wins: 0 }
    entry.matches++
    if (match.win) entry.wins++
    byChampion.set(match.championName, entry)
  }
  let best = null
  for (const entry of byChampion.values()) {
    if (!best || entry.matches > best.matches) best = entry
  }
  return best
}

/**
 * The line under the headline: the streak (when it is 3 or more) and the most played
 * champion with its record (when played more than once).
 */
export function buildMatchesSubline(matches) {
  const parts = []

  const streak = currentStreak(matches)
  if (streak && streak.length >= 3) {
    parts.push(streak.win
      ? `You're on a ${streak.length}-match win streak.`
      : `You've lost your last ${streak.length} matches; a short break often helps.`)
  }

  const main = mostPlayedChampion(matches)
  if (main && main.matches > 1) {
    parts.push(`${main.championName} is your most played, with ${main.wins} ${main.wins === 1 ? 'win' : 'wins'} in ${main.matches} matches.`)
  }

  return parts.join(' ') || null
}
