import { describe, it, expect } from 'vitest'
import {
  isRemake,
  matchKda,
  matchRiotId,
  formatSigned,
  buildMatchesHeadline,
  buildMatchesSubline,
  currentStreak,
  mostPlayedChampion,
  formResults,
  buildStartTimeChart,
  laneGoldDiffAt10,
  laneBar,
  formatLpChange,
  lpChangeClass,
  formatRankAfter,
  sumLpChange
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
      expect(buildMatchesHeadline(matches)).toBe('2 wins in your last 3')
    })

    it('uses the singular for one win and says so when there are none', () => {
      expect(buildMatchesHeadline([match(true), match(false)])).toBe('1 win in your last 2')
      expect(buildMatchesHeadline([match(false), match(false)])).toBe('No wins in your last 2')
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

  describe('formResults', () => {
    it('turns the newest-first list into results oldest first', () => {
      const matches = [match(true), match(false, 'Ahri', 200), match(false)]
      expect(formResults(matches)).toEqual(['loss', 'remake', 'win'])
    })

    it('keeps the newest 20 matches', () => {
      const matches = [match(false), ...Array.from({ length: 24 }, () => match(true))]
      const results = formResults(matches)
      expect(results).toHaveLength(20)
      expect(results[19]).toBe('loss')
    })

    it('handles a missing list', () => {
      expect(formResults(null)).toEqual([])
    })
  })

  describe('buildStartTimeChart', () => {
    // Local time, so the groups don't depend on the test machine's time zone
    const at = (hour, win, gameDurationSec = 1800) => ({
      win,
      gameDurationSec,
      gameStartTime: new Date(2026, 8, 27, hour, 30).getTime()
    })

    it('groups by local start time and names the weak spot', () => {
      const matches = [
        at(14, true), at(15, true), at(16, false),
        at(19, true), at(20, true), at(21, true), at(22, false),
        at(23, false), at(0, false), at(1, false), at(2, true)
      ]
      const chart = buildStartTimeChart(matches)
      expect(chart.groups).toEqual([
        { key: 'afternoon', label: 'Afternoon', matches: 3, wins: 2, winRate: 67 },
        { key: 'evening', label: 'Evening', matches: 4, wins: 3, winRate: 75 },
        { key: 'late', label: 'After 11pm', matches: 4, wins: 1, winRate: 25 }
      ])
      expect(chart.weakKey).toBe('late')
      expect(chart.title).toBe('You lose most after 11pm')
    })

    it('leaves out groups under three matches and remakes', () => {
      const matches = [
        at(9, true), at(10, false),
        at(14, true), at(15, false), at(16, true), at(17, false, 200),
        at(19, true), at(20, false), at(21, true)
      ]
      const chart = buildStartTimeChart(matches)
      expect(chart.groups.map((g) => g.key)).toEqual(['afternoon', 'evening'])
      expect(chart.groups[0].matches).toBe(3)
    })

    it('omits the title when no group stands out', () => {
      const matches = [at(14, true), at(15, false), at(16, true), at(19, true), at(20, false), at(21, true)]
      const chart = buildStartTimeChart(matches)
      expect(chart.weakKey).toBeNull()
      expect(chart.title).toBeNull()
    })

    it('has no weak spot when two groups share the lowest win rate', () => {
      const matches = [
        at(9, false), at(10, false), at(11, true),
        at(14, false), at(15, false), at(16, true),
        at(19, true), at(20, true), at(21, true)
      ]
      expect(buildStartTimeChart(matches).weakKey).toBeNull()
    })

    it('returns null with fewer than two groups to compare', () => {
      expect(buildStartTimeChart([at(14, true), at(15, false), at(16, true)])).toBeNull()
      expect(buildStartTimeChart([])).toBeNull()
    })
  })

  describe('laneGoldDiffAt10', () => {
    it('uses your side and falls back to the opponent, inverted', () => {
      expect(laneGoldDiffAt10({ allyParticipant: { goldDiffAt10: 420 }, enemyParticipant: { goldDiffAt10: -420 } })).toBe(420)
      expect(laneGoldDiffAt10({ allyParticipant: { goldDiffAt10: null }, enemyParticipant: { goldDiffAt10: 650 } })).toBe(-650)
      expect(laneGoldDiffAt10({ allyParticipant: {}, enemyParticipant: {} })).toBeNull()
    })
  })

  describe('laneBar', () => {
    it('grows right when ahead, scaled to 1,500 gold for a full half', () => {
      expect(laneBar(750)).toEqual({
        result: 'won',
        behindWidth: '0%',
        aheadWidth: '50%',
        diffText: '+750',
        description: 'won lane by 750 gold at 10 minutes'
      })
    })

    it('grows left when behind and caps at a full half', () => {
      const bar = laneBar(-2400)
      expect(bar.result).toBe('lost')
      expect(bar.behindWidth).toBe('100%')
      expect(bar.aheadWidth).toBe('0%')
      expect(bar.diffText).toBe('−2,400')
      expect(bar.description).toBe('lost lane by 2,400 gold at 10 minutes')
    })

    it('draws no bar under 300 either way', () => {
      const bar = laneBar(-120)
      expect(bar.result).toBe('even')
      expect(bar.behindWidth).toBe('0%')
      expect(bar.aheadWidth).toBe('0%')
      expect(bar.diffText).toBe('−120')
      expect(laneBar(300).result).toBe('won')
    })

    it('shows a dash without a difference', () => {
      expect(laneBar(null)).toMatchObject({ result: 'unknown', diffText: '—' })
    })
  })

  describe('formatLpChange and lpChangeClass', () => {
    it('signs the change with a real minus and marks no change with ±', () => {
      expect(formatLpChange(19)).toBe('+19 LP')
      expect(formatLpChange(-17)).toBe('−17 LP')
      expect(formatLpChange(0)).toBe('±0 LP')
      expect(formatLpChange(null)).toBeNull()
    })

    it('colours gains purple, losses orange and nothing else', () => {
      expect(lpChangeClass(5)).toBe('mp-up')
      expect(lpChangeClass(-5)).toBe('mp-down')
      expect(lpChangeClass(0)).toBeNull()
      expect(lpChangeClass(undefined)).toBeNull()
    })
  })

  describe('formatRankAfter', () => {
    it('names the tier, division and LP', () => {
      expect(formatRankAfter('EMERALD', 'II', 64)).toEqual({ label: 'Emerald II · 64 LP', tierKey: 'emerald' })
    })

    it('leaves the division out for Master and above', () => {
      expect(formatRankAfter('GRANDMASTER', 'I', 412)).toEqual({ label: 'Grandmaster · 412 LP', tierKey: 'grandmaster' })
    })

    it('returns null without a known tier', () => {
      expect(formatRankAfter(null, 'II', 64)).toBeNull()
      expect(formatRankAfter('UNRANKED', 'I', 0)).toBeNull()
      expect(formatRankAfter('gold); background: red', 'I', 0)).toBeNull()
    })
  })

  describe('sumLpChange', () => {
    it('adds up the known changes and counts them', () => {
      const matches = [{ lpChange: 19 }, { lpChange: -17 }, { lpChange: null }, { lpChange: 21 }]
      expect(sumLpChange(matches)).toEqual({ total: 23, matches: 3 })
    })

    it('returns null with fewer than two known changes', () => {
      expect(sumLpChange([{ lpChange: 19 }, { lpChange: null }])).toBeNull()
      expect(sumLpChange(null)).toBeNull()
    })
  })
})
