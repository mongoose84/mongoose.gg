import { describe, it, expect } from 'vitest'
import {
  isRemake,
  matchKda,
  matchRiotId,
  formatSigned,
  buildMatchesHeadline,
  buildMatchesSubline,
  currentStreak,
  mostPlayedChampion
} from '@/utils/matchesSummary'

const match = (win, championName = 'Ahri', gameDurationSec = 1800) => ({ win, championName, gameDurationSec })

describe('matchesSummary', () => {
  describe('isRemake', () => {
    it('treats matches under five minutes as remakes', () => {
      expect(isRemake({ gameDurationSec: 299 })).toBe(true)
      expect(isRemake({ gameDurationSec: 300 })).toBe(false)
      expect(isRemake({})).toBe(false)
      expect(isRemake(null)).toBe(false)
    })
  })

  describe('matchKda and matchRiotId', () => {
    it('formats K / D / A', () => {
      expect(matchKda({ kills: 7, deaths: 2, assists: 9 })).toBe('7/2/9')
      expect(matchKda(null)).toBeNull()
    })

    it('builds the Riot ID only when the match carries an account', () => {
      expect(matchRiotId({ accountGameName: 'Faker', accountTagLine: 'EUW' })).toBe('Faker#EUW')
      expect(matchRiotId({ accountGameName: 'Faker', accountTagLine: null })).toBe('Faker')
      expect(matchRiotId({ accountGameName: null })).toBeNull()
    })
  })

  describe('formatSigned', () => {
    it('uses a real minus and a plus sign', () => {
      expect(formatSigned(1.24, 1)).toBe('+1.2')
      expect(formatSigned(-2.9000000000000004, 1)).toBe('−2.9')
      expect(formatSigned(-1250)).toBe('−1,250')
    })

    it('never shows a negative zero', () => {
      expect(formatSigned(-0.04, 1)).toBe('+0')
    })

    it('shows a dash for missing values', () => {
      expect(formatSigned(null)).toBe('—')
      expect(formatSigned(Number.NaN)).toBe('—')
    })
  })

  describe('buildMatchesHeadline', () => {
    it('counts wins over the matches in the list, leaving remakes out', () => {
      const matches = [match(true), match(false), match(true), match(false, 'Ahri', 200)]
      expect(buildMatchesHeadline(matches)).toBe('You won 2 of your last 3 matches')
    })

    it('reads naturally for a single match', () => {
      expect(buildMatchesHeadline([match(true)])).toBe('You won your last match')
      expect(buildMatchesHeadline([match(false)])).toBe('You lost your last match')
    })

    it('returns null with nothing to summarise', () => {
      expect(buildMatchesHeadline([])).toBeNull()
      expect(buildMatchesHeadline(null)).toBeNull()
    })
  })

  describe('currentStreak', () => {
    it('counts the run from the newest match', () => {
      expect(currentStreak([match(true), match(true), match(false), match(true)])).toEqual({ win: true, length: 2 })
      expect(currentStreak([match(false), match(false, 'Ahri', 120), match(false)])).toEqual({ win: false, length: 2 })
      expect(currentStreak([])).toBeNull()
    })
  })

  describe('mostPlayedChampion', () => {
    it('returns the champion with the most matches and its wins', () => {
      const matches = [match(true, 'Ahri'), match(false, 'Zed'), match(true, 'Ahri'), match(false, 'Ahri')]
      expect(mostPlayedChampion(matches)).toEqual({ championName: 'Ahri', matches: 3, wins: 2 })
    })

    it('keeps the newest champion on a tie', () => {
      expect(mostPlayedChampion([match(true, 'Zed'), match(true, 'Ahri')]).championName).toBe('Zed')
    })
  })

  describe('buildMatchesSubline', () => {
    it('mentions a win streak of three or more and the most played champion', () => {
      const matches = [match(true), match(true), match(true), match(false, 'Zed')]
      expect(buildMatchesSubline(matches))
        .toBe("You're on a 3-match win streak. Ahri is your most played, with 3 wins in 3 matches.")
    })

    it('suggests a break after three losses without scolding', () => {
      const matches = [match(false, 'Zed'), match(false, 'Ahri'), match(false, 'Lux')]
      expect(buildMatchesSubline(matches)).toBe("You've lost your last 3 matches; a short break often helps.")
    })

    it('leaves out a champion played only once', () => {
      expect(buildMatchesSubline([match(true, 'Zed'), match(false, 'Ahri')])).toBeNull()
    })

    it('uses the singular for one win', () => {
      expect(buildMatchesSubline([match(false), match(true)])).toBe('Ahri is your most played, with 1 win in 2 matches.')
    })
  })
})
