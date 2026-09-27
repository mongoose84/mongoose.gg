/**
 * Pure helpers for the Champion Select page: role order, the default role, matchup splits,
 * opponent search and the hero sentence for the selected pick.
 */
import { formatRoleWithAdc } from './formatters'

/** Lane order players expect in champ select */
const ROLE_ORDER = ['TOP', 'JUNGLE', 'MIDDLE', 'BOTTOM', 'UTILITY']

/** A matchup counts once you have met the champion this many times in lane */
export const MIN_LANE_MATCHES = 3

/** Picks with fewer matches than this are called a lean, not a rule */
export const SMALL_SAMPLE_MATCHES = 5

const MAX_MATCHUPS_PER_SIDE = 4
const MAX_SEARCH_RESULTS = 8

export function roleLabel(role) {
  return formatRoleWithAdc(role) || 'Fill'
}

/** Role groups sorted Top → Support, unknown roles last */
export function sortRoleGroups(mainChampions) {
  if (!Array.isArray(mainChampions)) return []
  const rank = (role) => {
    const index = ROLE_ORDER.indexOf(role)
    return index === -1 ? ROLE_ORDER.length : index
  }
  return [...mainChampions]
    .filter((group) => group?.champions?.length)
    .sort((a, b) => rank(a.role) - rank(b.role))
}

/** The role with the most matches across its top champions */
export function pickDefaultRole(roleGroups) {
  let bestRole = null
  let bestMatches = -1
  for (const group of roleGroups || []) {
    const matches = (group.champions || []).reduce((sum, c) => sum + (c.gamesPlayed || 0), 0)
    if (matches > bestMatches) {
      bestMatches = matches
      bestRole = group.role
    }
  }
  return bestRole
}

function winRate(wins, matches) {
  return matches > 0 ? (wins / matches) * 100 : 0
}

/** Formats a record with an en dash, as players write it: "4–1" */
export function formatRecord(wins, losses) {
  return `${wins}–${losses}`
}

/**
 * Lane matchups for one champion in one role: opponents met at least MIN_LANE_MATCHES times in
 * lane, split into strong (win rate above 50%, best first) and weak (below 50%, worst first).
 */
export function splitMatchups(matchups, championId, role) {
  const entry = (matchups || []).find((m) => m.championId === championId && m.role === role)
  if (!entry?.opponents) return { strong: [], weak: [] }

  const opponents = entry.opponents
    .map((o) => {
      const matches = o.inLaneWins + o.inLaneLosses
      return {
        championId: o.opponentChampionId,
        championName: o.opponentChampionName,
        wins: o.inLaneWins,
        losses: o.inLaneLosses,
        matches,
        winRate: winRate(o.inLaneWins, matches)
      }
    })
    .filter((o) => o.matches >= MIN_LANE_MATCHES)

  return {
    strong: opponents
      .filter((o) => o.winRate > 50)
      .sort((a, b) => b.winRate - a.winRate || b.matches - a.matches)
      .slice(0, MAX_MATCHUPS_PER_SIDE),
    weak: opponents
      .filter((o) => o.winRate < 50)
      .sort((a, b) => a.winRate - b.winRate || b.matches - a.matches)
      .slice(0, MAX_MATCHUPS_PER_SIDE)
  }
}

/**
 * Your champions in one role that have faced an enemy champion whose name contains the query.
 * Needs at least two characters. Most-played matchups first.
 */
export function searchMatchups(matchups, query, role) {
  const needle = (query || '').trim().toLowerCase()
  if (needle.length < 2) return []

  const results = []
  for (const matchup of matchups || []) {
    if (role && matchup.role !== role) continue
    for (const o of matchup.opponents || []) {
      if (!o.opponentChampionName?.toLowerCase().includes(needle)) continue
      const laneMatches = o.inLaneWins + o.inLaneLosses
      const wins = o.inLaneWins + o.outOfLaneWins
      const losses = o.inLaneLosses + o.outOfLaneLosses
      const matches = wins + losses
      if (matches === 0) continue
      results.push({
        key: `${matchup.championId}-${matchup.role}-${o.opponentChampionId}`,
        championName: matchup.championName,
        opponentName: o.opponentChampionName,
        laneWins: o.inLaneWins,
        laneLosses: o.inLaneLosses,
        laneMatches,
        wins,
        losses,
        matches,
        winRate: winRate(wins, matches)
      })
    }
  }

  return results
    .sort((a, b) => b.laneMatches - a.laneMatches || b.matches - a.matches)
    .slice(0, MAX_SEARCH_RESULTS)
}

/** Hero headline for the selected pick: "Ahri is your best pick for Mid" */
export function buildPickHeadline(champion, index, role) {
  if (!champion) return 'Your champion picks'
  const place = index === 0 ? 'best' : `#${index + 1}`
  return `${champion.championName} is your ${place} pick for ${roleLabel(role)}`
}

/** Finding → evidence: win rate and sample, then the strongest and weakest lane matchup */
export function buildPickText(champion, matchupSplit) {
  if (!champion) return null
  const matches = champion.gamesPlayed
  const matchWord = matches === 1 ? 'match' : 'matches'
  const parts = [
    `You win ${Math.round(champion.winRate)}% over ${matches} ${matchWord} with a ${champion.avgKda.toFixed(1)} KDA.`
  ]

  const best = matchupSplit?.strong?.[0]
  const worst = matchupSplit?.weak?.[0]
  if (best && worst) {
    parts.push(`Strongest into ${best.championName} (${formatRecord(best.wins, best.losses)} in lane), weakest into ${worst.championName} (${formatRecord(worst.wins, worst.losses)}).`)
  } else if (best) {
    parts.push(`Strongest into ${best.championName} (${formatRecord(best.wins, best.losses)} in lane).`)
  } else if (worst) {
    parts.push(`Weakest into ${worst.championName} (${formatRecord(worst.wins, worst.losses)} in lane).`)
  }

  if (matches < SMALL_SAMPLE_MATCHES) {
    parts.push('Only a few matches so far, so treat this as a lean.')
  }

  return parts.join(' ')
}

/** Two glass chips: win rate and CS per minute */
export function buildPickChips(champion) {
  if (!champion) return []
  const chips = [`${Math.round(champion.winRate)}% win rate`]
  if (champion.avgCsPerMin > 0) {
    chips.push(`${champion.avgCsPerMin.toFixed(1)} CS per minute`)
  }
  return chips
}
