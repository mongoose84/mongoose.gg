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
 * The page headline: "12 wins in your last 20".
 * Remakes are left out of the count. Returns null when there is nothing to summarise.
 */
export function buildMatchesHeadline(matches) {
  const counted = countedMatches(matches)
  if (!counted.length) return null
  const wins = counted.filter((m) => m.win).length
  if (counted.length === 1) return wins ? 'You won your last match' : 'You lost your last match'
  if (!wins) return `No wins in your last ${counted.length}`
  return `${wins} ${wins === 1 ? 'win' : 'wins'} in your last ${counted.length}`
}

/** The FormStrip shows at most this many matches, the same range as the list */
export const FORM_MAX_MATCHES = 20

/**
 * Results for the FormStrip, oldest first: 'win', 'loss' or 'remake'.
 * The list comes newest first, so the newest match ends up on the right.
 */
export function formResults(matches) {
  return (matches || [])
    .slice(0, FORM_MAX_MATCHES)
    .map((m) => (isRemake(m) ? 'remake' : m.win ? 'win' : 'loss'))
    .reverse()
}

/** Start-time groups for the "win rate by start time" chart, in the player's local time */
export const START_TIME_GROUPS = [
  { key: 'morning', label: 'Morning', phrase: 'in the morning', from: 5, to: 12 },
  { key: 'afternoon', label: 'Afternoon', phrase: 'in the afternoon', from: 12, to: 18 },
  { key: 'evening', label: 'Evening', phrase: 'in the evening', from: 18, to: 23 },
  { key: 'late', label: 'After 11pm', phrase: 'after 11pm', from: 23, to: 5 }
]

/** Groups with fewer matches are left out rather than show a 0% or 100% from one match */
export const START_TIME_MIN_MATCHES = 3

/** A group is the weak spot when its win rate is this many points below the other groups together */
export const START_TIME_WEAK_GAP = 15

function startTimeGroupKey(timestamp) {
  const hour = new Date(timestamp).getHours()
  const group = START_TIME_GROUPS.find((g) => (g.from < g.to
    ? hour >= g.from && hour < g.to
    : hour >= g.from || hour < g.to))
  return group.key
}

/**
 * Win rate by start time from `gameStartTime` (epoch ms), in the browser's time zone.
 * Remakes and groups under START_TIME_MIN_MATCHES are left out.
 * Returns { groups: [{ key, label, matches, wins, winRate }], weakKey, title } or null when
 * fewer than two groups have enough matches to compare. `title` names the weak spot
 * ("You lose most after 11pm") and is null when nothing stands out.
 */
export function buildStartTimeChart(matches) {
  const counts = new Map()
  for (const match of countedMatches(matches)) {
    if (typeof match.gameStartTime !== 'number') continue
    const key = startTimeGroupKey(match.gameStartTime)
    const entry = counts.get(key) || { matches: 0, wins: 0 }
    entry.matches++
    if (match.win) entry.wins++
    counts.set(key, entry)
  }

  const groups = START_TIME_GROUPS
    .filter((g) => (counts.get(g.key)?.matches ?? 0) >= START_TIME_MIN_MATCHES)
    .map((g) => {
      const { matches: played, wins } = counts.get(g.key)
      return { key: g.key, label: g.label, matches: played, wins, winRate: Math.round((wins / played) * 100) }
    })

  if (groups.length < 2) return null

  const lowest = Math.min(...groups.map((g) => g.winRate))
  const lowestGroups = groups.filter((g) => g.winRate === lowest)
  let weakKey = null
  if (lowestGroups.length === 1) {
    const weak = lowestGroups[0]
    const others = groups.filter((g) => g !== weak)
    const otherWins = others.reduce((sum, g) => sum + g.wins, 0)
    const otherMatches = others.reduce((sum, g) => sum + g.matches, 0)
    const otherRate = (otherWins / otherMatches) * 100
    if (otherRate - (weak.wins / weak.matches) * 100 >= START_TIME_WEAK_GAP) weakKey = weak.key
  }

  const phrase = START_TIME_GROUPS.find((g) => g.key === weakKey)?.phrase
  return { groups, weakKey, title: phrase ? `You lose most ${phrase}` : null }
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

/** A lane is won or lost at this many gold ahead or behind at 10 minutes; less is even */
export const LANE_EVEN_GOLD = 300

/** ±this much gold at 10 fills half of a LaneBar */
export const LANE_BAR_FULL_GOLD = 1500

/**
 * Your side's gold difference at 10 minutes in a lane matchup. Like the server's lane winner,
 * falls back to the opponent's difference, inverted. Null when neither side has one.
 */
export function laneGoldDiffAt10(matchup) {
  const own = matchup?.allyParticipant?.goldDiffAt10
  if (typeof own === 'number') return own
  const theirs = matchup?.enemyParticipant?.goldDiffAt10
  return typeof theirs === 'number' ? -theirs : null
}

/**
 * What a LaneBar draws for a gold difference: the result ('won', 'lost', 'even', or 'unknown'
 * when there is no difference), the widths of the behind and ahead halves, the signed number
 * and a sentence for assistive tech.
 */
export function laneBar(diff) {
  if (typeof diff !== 'number' || Number.isNaN(diff)) {
    return { result: 'unknown', behindWidth: '0%', aheadWidth: '0%', diffText: '—', description: 'no gold recorded at 10 minutes' }
  }
  const amount = Math.abs(Math.round(diff)).toLocaleString('en-US')
  if (Math.abs(diff) < LANE_EVEN_GOLD) {
    return { result: 'even', behindWidth: '0%', aheadWidth: '0%', diffText: formatSigned(diff), description: `even lane at 10 minutes (${formatSigned(diff)} gold)` }
  }
  const width = `${Math.round(Math.min(Math.abs(diff) / LANE_BAR_FULL_GOLD, 1) * 100)}%`
  return diff > 0
    ? { result: 'won', behindWidth: '0%', aheadWidth: width, diffText: formatSigned(diff), description: `won lane by ${amount} gold at 10 minutes` }
    : { result: 'lost', behindWidth: width, aheadWidth: '0%', diffText: formatSigned(diff), description: `lost lane by ${amount} gold at 10 minutes` }
}
