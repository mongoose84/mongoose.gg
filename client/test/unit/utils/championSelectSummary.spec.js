import { describe, it, expect } from 'vitest'
import {
  sortRoleGroups,
  pickDefaultRole,
  splitMatchups,
  searchMatchups,
  buildPickHeadline,
  buildPickText,
  buildPickChips,
  formatRecord,
  roleLabel
} from '@/utils/championSelectSummary'

const ahri = { championId: 103, championName: 'Ahri', role: 'MIDDLE', winRate: 63.6, gamesPlayed: 22, mScore: 71, avgKda: 4.12, avgCsPerMin: 7.84 }

function opponent(id, name, inLaneWins, inLaneLosses, outOfLaneWins = 0, outOfLaneLosses = 0) {
  return { opponentChampionId: id, opponentChampionName: name, inLaneWins, inLaneLosses, outOfLaneWins, outOfLaneLosses }
}

const matchups = [
  {
    championId: 103,
    championName: 'Ahri',
    role: 'MIDDLE',
    opponents: [
      opponent(238, 'Zed', 4, 1),
      opponent(7, 'LeBlanc', 3, 0),
      opponent(134, 'Syndra', 1, 3),
      opponent(61, 'Orianna', 2, 2),
      opponent(84, 'Akali', 1, 1, 2, 0),
      opponent(91, 'Talon', 0, 0, 1, 1)
    ]
  },
  { championId: 103, championName: 'Ahri', role: 'TOP', opponents: [opponent(238, 'Zed', 0, 3)] },
  { championId: 112, championName: 'Viktor', role: 'MIDDLE', opponents: [opponent(238, 'Zed', 5, 5, 1, 0)] }
]

describe('championSelectSummary', () => {
  describe('sortRoleGroups', () => {
    it('orders roles top to support, unknown roles last, and drops empty roles', () => {
      const groups = [
        { role: 'UTILITY', champions: [ahri] },
        { role: 'UNKNOWN', champions: [ahri] },
        { role: 'TOP', champions: [ahri] },
        { role: 'JUNGLE', champions: [] },
        { role: 'MIDDLE', champions: [ahri] }
      ]
      expect(sortRoleGroups(groups).map((g) => g.role)).toEqual(['TOP', 'MIDDLE', 'UTILITY', 'UNKNOWN'])
    })

    it('returns an empty list without data', () => {
      expect(sortRoleGroups(undefined)).toEqual([])
    })
  })

  it('picks the role with the most matches as the default', () => {
    const groups = [
      { role: 'TOP', champions: [{ gamesPlayed: 10 }] },
      { role: 'MIDDLE', champions: [{ gamesPlayed: 8 }, { gamesPlayed: 5 }] }
    ]
    expect(pickDefaultRole(groups)).toBe('MIDDLE')
    expect(pickDefaultRole([])).toBeNull()
  })

  describe('splitMatchups', () => {
    it('keeps opponents met 3+ times in lane and splits them into strong and weak', () => {
      const { strong, weak } = splitMatchups(matchups, 103, 'MIDDLE')
      expect(strong.map((o) => o.championName)).toEqual(['LeBlanc', 'Zed'])
      expect(strong[1]).toMatchObject({ wins: 4, losses: 1, matches: 5, winRate: 80 })
      expect(weak.map((o) => o.championName)).toEqual(['Syndra'])
    })

    it('uses the matchups for the given role only', () => {
      const { strong, weak } = splitMatchups(matchups, 103, 'TOP')
      expect(strong).toEqual([])
      expect(weak.map((o) => o.championName)).toEqual(['Zed'])
    })

    it('returns empty lists when the champion has no matchups', () => {
      expect(splitMatchups(matchups, 999, 'MIDDLE')).toEqual({ strong: [], weak: [] })
      expect(splitMatchups(null, 103, 'MIDDLE')).toEqual({ strong: [], weak: [] })
    })
  })

  describe('searchMatchups', () => {
    it('needs at least two characters', () => {
      expect(searchMatchups(matchups, 'z', 'MIDDLE')).toEqual([])
    })

    it('finds your champions in the role that faced the enemy, most lane matches first', () => {
      const results = searchMatchups(matchups, ' ZE ', 'MIDDLE')
      expect(results.map((r) => r.championName)).toEqual(['Viktor', 'Ahri'])
      expect(results[0]).toMatchObject({ laneWins: 5, laneLosses: 5, wins: 6, losses: 5, matches: 11 })
    })

    it('includes out-of-lane meetings and skips opponents never met', () => {
      const results = searchMatchups(matchups, 'ta', 'MIDDLE')
      expect(results.map((r) => r.opponentName)).toEqual(['Talon'])
      expect(results[0]).toMatchObject({ laneMatches: 0, wins: 1, losses: 1, winRate: 50 })
    })
  })

  describe('hero copy', () => {
    it('names the best pick and later picks by place', () => {
      expect(buildPickHeadline(ahri, 0, 'MIDDLE')).toBe('Ahri is your best pick for Mid')
      expect(buildPickHeadline(ahri, 2, 'BOTTOM')).toBe('Ahri is your #3 pick for ADC')
      expect(buildPickHeadline(null, 0, 'MIDDLE')).toBe('Your champion picks')
    })

    it('states the win rate, then the strongest and weakest lane matchup', () => {
      const split = splitMatchups(matchups, 103, 'MIDDLE')
      expect(buildPickText(ahri, split)).toBe(
        'You win 64% over 22 matches with a 4.1 KDA. Strongest into LeBlanc (3–0 in lane), weakest into Syndra (1–3).'
      )
    })

    it('calls a small sample a lean', () => {
      const text = buildPickText({ ...ahri, gamesPlayed: 1 }, { strong: [], weak: [] })
      expect(text).toBe('You win 64% over 1 match with a 4.1 KDA. Only a few matches so far, so treat this as a lean.')
    })

    it('shows win rate and CS per minute as chips', () => {
      expect(buildPickChips(ahri)).toEqual(['64% win rate', '7.8 CS per minute'])
      expect(buildPickChips({ ...ahri, avgCsPerMin: 0 })).toEqual(['64% win rate'])
    })
  })

  it('formats records with an en dash and roles as players say them', () => {
    expect(formatRecord(4, 1)).toBe('4–1')
    expect(roleLabel('UTILITY')).toBe('Support')
    expect(roleLabel(null)).toBe('Fill')
  })
})
