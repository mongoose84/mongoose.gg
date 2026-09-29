import { describe, it, expect } from 'vitest'
import {
  buildNoMatchesEmpty,
  buildPatternCards,
  buildSecondLine,
  buildSoloHeadline,
  buildStatTrendsCaption,
  buildStatTrendsTitle,
  buildWinFactorsEmpty,
  buildWinFactorsTitle,
  describeStatTrend,
  describeWinFactor,
  factorLabel,
  formatSigned,
  formatStatNumber,
  formatStatValue,
  statVerdict
} from '@/utils/soloSummary'
import { statTrendsResponse, winFactorsResponse } from '@test/helpers/soloFixtures'

const MINUS = '−'

describe('soloSummary', () => {
  describe('number formats (FR 30)', () => {
    it('writes signed numbers with a real minus and thousands separators', () => {
      expect(formatSigned(1240)).toBe('+1,240')
      expect(formatSigned(-650)).toBe(`${MINUS}650`)
      expect(formatSigned(0.4)).toBe('0')
    })

    it('writes each stat in its own format', () => {
      expect(formatStatNumber('deaths', 4.13)).toBe('4.1')
      expect(formatStatNumber('goldLeadAt15', -320)).toBe(`${MINUS}320`)
      expect(formatStatNumber('dragonParticipation', 61.6)).toBe('62%')
      expect(formatStatNumber('visionPerMin', 0.75)).toBe('0.8')
      expect(formatStatNumber('csPerMin', null)).toBe('—')
    })

    it('adds the unit', () => {
      expect(formatStatValue('deaths', 4.1)).toBe('4.1 per match')
      expect(formatStatValue('goldLeadAt15', 340)).toBe('+340 gold')
      expect(formatStatValue('killParticipation', 55)).toBe('55%')
      expect(formatStatValue('visionPerMin', null)).toBe('—')
    })
  })

  describe('headline', () => {
    it('counts matches in the range', () => {
      expect(buildSoloHeadline('last20', 20)).toBe('Your last 20 matches')
      expect(buildSoloHeadline('last50', 1)).toBe('Your last 1 match')
      expect(buildSoloHeadline('season', 64)).toBe('Your 64 matches this season')
      expect(buildSoloHeadline('last20', 0)).toBeNull()
    })
  })

  describe('buildSecondLine (FR 7)', () => {
    it('names the Improving stat with the largest normalised change', () => {
      expect(buildSecondLine(statTrendsResponse().stats))
        .toBe('Fewer deaths did most of it: 4.1 per match, down from 5.6.')
    })

    it('falls back to the Slipping stat', () => {
      const stats = statTrendsResponse().stats.filter((s) => s.verdict !== 'improving')
      expect(buildSecondLine(stats)).toBe('Vision is slipping: 0.7 per minute, down from 0.9.')
    })

    it('is null when every stat is Steady or has no verdict', () => {
      const stats = statTrendsResponse().stats.map((s) => ({ ...s, verdict: s.key === 'deaths' ? null : 'steady' }))
      expect(buildSecondLine(stats)).toBeNull()
    })
  })

  describe('statVerdict (FR 15)', () => {
    it('shows the direction of the number and the meaning of the change', () => {
      expect(statVerdict({ verdict: 'improving', was: 5.6, now: 4.1 })).toEqual({ word: 'Improving', tone: 'up', arrow: '▼' })
      expect(statVerdict({ verdict: 'slipping', was: 0.9, now: 0.7 })).toEqual({ word: 'Slipping', tone: 'down', arrow: '▼' })
      expect(statVerdict({ verdict: 'steady', was: 5, now: 5.2 })).toEqual({ word: 'Steady', tone: 'neutral', arrow: '' })
    })

    it('asks for 20 matches without a verdict', () => {
      expect(statVerdict({ verdict: null, was: null, now: 4 }).word).toBe('Needs 20 matches')
    })
  })

  describe('buildStatTrendsTitle (FR 17)', () => {
    const withVerdicts = (...verdicts) => verdicts.map((verdict, i) => ({ key: `s${i}`, verdict }))

    it('counts improved stats among those with a verdict', () => {
      expect(buildStatTrendsTitle(withVerdicts('improving', 'slipping', 'steady', null))).toBe('1 of 3 match-deciding stats improved')
    })

    it('counts slipped stats when none improved', () => {
      expect(buildStatTrendsTitle(withVerdicts('slipping', 'slipping', 'steady'))).toBe('2 of 3 match-deciding stats slipped')
    })

    it('says steady when every verdict is Steady', () => {
      expect(buildStatTrendsTitle(withVerdicts('steady', 'steady'))).toBe('Your match-deciding stats held steady')
    })

    it('is null without verdicts', () => {
      expect(buildStatTrendsTitle(withVerdicts(null, null))).toBeNull()
    })
  })

  it('captions the range', () => {
    expect(buildStatTrendsCaption('last50', 50)).toBe('10-match average, last 50 matches')
    expect(buildStatTrendsCaption('season', 80)).toBe('10-match average, this season')
  })

  it('describes a tile in words', () => {
    const deaths = statTrendsResponse().stats[0]
    expect(describeStatTrend(deaths, 'last20', 20))
      .toBe('Deaths, last 20 matches: 4.1 per match, was 5.6, improving, your season average 4.9.')
  })

  describe('win factors (FR 21–23)', () => {
    it('labels vision and CS with the role mark when there is one', () => {
      expect(factorLabel({ key: 'vision', mark: 2 })).toBe('2.0+ vision per minute')
      expect(factorLabel({ key: 'vision', mark: null })).toBe('Vision mark for your role')
      expect(factorLabel({ key: 'cs', mark: 7 })).toBe('7+ CS per minute')
      expect(factorLabel({ key: 'cs', mark: 5.5 })).toBe('5.5+ CS per minute')
      expect(factorLabel({ key: 'aheadAt15' })).toBe('Ahead at 15 minutes')
    })

    it('titles the card after the top row', () => {
      expect(buildWinFactorsTitle(winFactorsResponse().factors)).toBe('Your gold at 15 decides your matches most')
      expect(buildWinFactorsTitle([{ key: 'lowDeaths' }])).toBe('Your deaths decide your matches most')
      expect(buildWinFactorsTitle([])).toBeNull()
    })

    it('describes a row in words', () => {
      expect(describeWinFactor(winFactorsResponse().factors[0]))
        .toBe('Ahead at 15 minutes: you win 71% of 12 matches when you hit it, and 34% of 8 when you miss it.')
    })

    it('asks for more matches under 20', () => {
      expect(buildWinFactorsEmpty(14, []).title).toBe('Play 6 more matches to see what decides your matches')
      expect(buildWinFactorsEmpty(19, []).title).toBe('Play 1 more match to see what decides your matches')
    })

    it('explains when fewer than two rows can be shown', () => {
      expect(buildWinFactorsEmpty(30, [{ key: 'cs' }]).title).toBe('Your matches split too unevenly to compare yet')
      expect(buildWinFactorsEmpty(30, winFactorsResponse().factors)).toBeNull()
    })
  })

  describe('buildNoMatchesEmpty', () => {
    it('offers all queues when the queue filter causes it', () => {
      expect(buildNoMatchesEmpty('ranked_flex')).toMatchObject({ title: 'No Flex matches yet', showAllQueues: true })
      expect(buildNoMatchesEmpty('all')).toMatchObject({ title: 'No matches yet', showAllQueues: false })
    })
  })

  describe('buildPatternCards (FR 26–28)', () => {
    it('builds the cards whose rules are met, in page order', () => {
      const cards = buildPatternCards(winFactorsResponse().patterns, 'last20', 20)

      expect(cards.map((c) => c.key)).toEqual(['session', 'afterLoss'])
    })

    it('titles the session drop-off after the match before the weak group', () => {
      const [session] = buildPatternCards(winFactorsResponse().patterns, 'last20', 20)

      expect(session).toMatchObject({ kind: 'pattern', title: 'You drop off after your third match', weakKey: '4plus' })
      expect(session.groups.map((g) => g.label)).toEqual(['1st', '2nd', '4th+'])
      expect(session.caption).toBe('Win rate by match of the session, last 20 matches')
    })

    it('calls a session without a weak spot a strength', () => {
      const patterns = { session: { groups: [{ key: '1', winRate: 60 }, { key: '2', winRate: 58 }], weak: null } }
      expect(buildPatternCards(patterns, 'season', 40)[0]).toMatchObject({ kind: 'strength', title: 'Your win rate holds through a session' })
    })

    it('flags tilt at 10 points below after a win', () => {
      const tilted = { afterLoss: { afterWin: { winRate: 57 }, afterLoss: { winRate: 47 } } }
      const fine = { afterLoss: { afterWin: { winRate: 57 }, afterLoss: { winRate: 48 } } }

      expect(buildPatternCards(tilted, 'last20', 20)[0]).toMatchObject({ kind: 'pattern', title: 'Losses carry into your next match', weakKey: 'afterLoss' })
      expect(buildPatternCards(fine, 'last20', 20)[0]).toMatchObject({ kind: 'strength', title: "A loss doesn't tilt you", weakKey: null })
    })

    it('names the match length that slips', () => {
      const length = (weak) => ({ length: { groups: [{ key: 'under25', winRate: 60 }, { key: 'over35', winRate: 30 }], weak } })

      expect(buildPatternCards(length('over35'), 'last20', 20)[0]).toMatchObject({ kind: 'trend', title: 'Long matches slip away from you' })
      expect(buildPatternCards(length('under25'), 'last20', 20)[0].title).toBe('Short matches get away from you')
      expect(buildPatternCards(length(null), 'last20', 20)[0]).toMatchObject({ kind: 'strength', title: 'You win at every match length' })
    })

    it('is empty without patterns', () => {
      expect(buildPatternCards({ session: null, afterLoss: null, length: null }, 'last20', 20)).toEqual([])
      expect(buildPatternCards(null, 'last20', 20)).toEqual([])
    })
  })
})
