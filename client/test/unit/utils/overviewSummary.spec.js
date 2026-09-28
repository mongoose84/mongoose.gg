import { describe, it, expect } from 'vitest'
import {
  formatRankLabel,
  buildHeroHeadline,
  buildHeroText,
  buildHeroChips,
  buildPlayerLine,
  buildSurvivalInsight,
  buildTodaySummary,
  isWinResult
} from '@/utils/overviewSummary'

function survival(overrides = {}) {
  return {
    avgDeathsPerGame: 5.2,
    winRateLowDeaths: 0.64,
    winRateHighDeaths: 0.38,
    gamesLowDeaths: 9,
    gamesHighDeaths: 8,
    lowDeathThreshold: 4,
    highDeathThreshold: 7,
    totalGames: 20,
    ...overrides
  }
}

describe('overviewSummary', () => {
  describe('formatRankLabel', () => {
    it('formats tier and division in title case', () => {
      expect(formatRankLabel('GOLD II')).toBe('Gold II')
      expect(formatRankLabel('MASTER')).toBe('Master')
    })

    it('returns null without a rank', () => {
      expect(formatRankLabel(null)).toBeNull()
    })
  })

  describe('buildHeroHeadline', () => {
    it('counts wins out of this week\'s matches', () => {
      expect(buildHeroHeadline({ gamesThisWeek: 12, winsThisWeek: 7 })).toBe("You've won 7 of 12 matches this week")
    })

    it('handles a single match', () => {
      expect(buildHeroHeadline({ gamesThisWeek: 1, winsThisWeek: 1 })).toBe('You won your one match this week')
      expect(buildHeroHeadline({ gamesThisWeek: 1, winsThisWeek: 0 })).toBe('You lost your one match this week')
    })

    it('says so when there are no matches this week', () => {
      expect(buildHeroHeadline({ gamesThisWeek: 0, winsThisWeek: 0 })).toBe('No matches this week yet')
      expect(buildHeroHeadline(null)).toBe('No matches this week yet')
    })

    it('stays within ten words', () => {
      expect(buildHeroHeadline({ gamesThisWeek: 120, winsThisWeek: 64 }).split(' ').length).toBeLessThanOrEqual(10)
    })
  })

  describe('buildHeroText', () => {
    it('states the deaths finding when the win-rate gap is large', () => {
      expect(buildHeroText({ gamesThisWeek: 5 }, survival()))
        .toBe('You win 64% of matches with 4 or fewer deaths and 38% with more than 7.')
    })

    it('falls back to average deaths when the gap is small', () => {
      expect(buildHeroText({ gamesThisWeek: 5 }, survival({ winRateHighDeaths: 0.55 })))
        .toBe('You average 5.2 deaths per match over your last 20 matches.')
    })

    it('falls back to average deaths when a bucket has no win rate', () => {
      expect(buildHeroText({ gamesThisWeek: 5 }, survival({ winRateHighDeaths: null, totalGames: 1 })))
        .toBe('You average 5.2 deaths per match over your last 1 match.')
    })

    it('tells a player without matches what happens next', () => {
      expect(buildHeroText({ gamesThisWeek: 0 }, null)).toBe('Play a match and your Overview updates within a few minutes.')
    })

    it('returns null when there is nothing to add', () => {
      expect(buildHeroText({ gamesThisWeek: 3 }, survival({ totalGames: 0 }))).toBeNull()
    })
  })

  describe('buildHeroChips', () => {
    it('shows matches and win rate this week', () => {
      expect(buildHeroChips({ gamesThisWeek: 12, winsThisWeek: 7 })).toEqual(['12 matches this week', '58% win rate'])
      expect(buildHeroChips({ gamesThisWeek: 1, winsThisWeek: 1 })).toEqual(['1 match this week', '100% win rate'])
    })

    it('shows no chips without matches', () => {
      expect(buildHeroChips({ gamesThisWeek: 0 })).toEqual([])
      expect(buildHeroChips(null)).toEqual([])
    })
  })

  describe('buildPlayerLine', () => {
    it('joins Riot ID, champion main, rank and LP', () => {
      expect(buildPlayerLine({ summonerName: 'Faker#KR1', rank: 'GOLD II', lp: 45 }, 'Ahri'))
        .toBe('Faker#KR1 · Ahri main · Gold II · 45 LP')
    })

    it('leaves out missing parts', () => {
      expect(buildPlayerLine({ summonerName: 'Faker#KR1', rank: null, lp: 45 }, '')).toBe('Faker#KR1')
    })

    it('returns null without a player header', () => {
      expect(buildPlayerLine(null, 'Ahri')).toBeNull()
    })
  })

  describe('buildSurvivalInsight', () => {
    it('is a Pattern when the player dies more than the low-death range', () => {
      const insight = buildSurvivalInsight(survival())
      expect(insight.kind).toBe('pattern')
      expect(insight.title).toBe('You win 64% of matches with 4 or fewer deaths')
      expect(insight.text).toContain('With more than 7 it drops to 38%.')
      expect(insight.text).toContain('You average 5.2 per match over your last 20 matches')
    })

    it('is a Strength when the player already stays in the low-death range', () => {
      const insight = buildSurvivalInsight(survival({ avgDeathsPerGame: 3.4 }))
      expect(insight.kind).toBe('strength')
      expect(insight.text).toContain('inside that range')
    })

    it('returns null without a clear finding', () => {
      expect(buildSurvivalInsight(null)).toBeNull()
      expect(buildSurvivalInsight(survival({ totalGames: 0 }))).toBeNull()
      expect(buildSurvivalInsight(survival({ winRateLowDeaths: null }))).toBeNull()
      expect(buildSurvivalInsight(survival({ gamesHighDeaths: 0 }))).toBeNull()
      expect(buildSurvivalInsight(survival({ winRateHighDeaths: 0.55 }))).toBeNull()
    })
  })

  describe('buildTodaySummary', () => {
    it('counts today\'s matches, wins and losses', () => {
      expect(buildTodaySummary({ gamesToday: 3, winsToday: 2, lossesToday: 1 })).toBe('3 matches today · 2 wins, 1 loss')
      expect(buildTodaySummary({ gamesToday: 1, winsToday: 1, lossesToday: 0 })).toBe('1 match today · 1 win, 0 losses')
    })

    it('says so when there are no matches today', () => {
      expect(buildTodaySummary({ gamesToday: 0 })).toBe('No matches today yet')
      expect(buildTodaySummary(null)).toBe('No matches today yet')
    })
  })

  describe('isWinResult', () => {
    it('accepts Victory and Win in any case', () => {
      expect(isWinResult('Victory')).toBe(true)
      expect(isWinResult('win')).toBe(true)
      expect(isWinResult('Defeat')).toBe(false)
      expect(isWinResult(null)).toBe(false)
    })
  })
})
