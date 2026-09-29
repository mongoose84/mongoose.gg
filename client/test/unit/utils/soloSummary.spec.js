import { describe, it, expect } from 'vitest'
import {
  buildBackfillProgress,
  buildDeathZonesCaption,
  buildDeathZonesTitle,
  describeDeathMap,
  zoneLabel,
  zoneTimingNote,
  buildFocusEvidence,
  buildFocusFinding,
  buildFocusFix,
  buildFocusStripCaption,
  focusMarkLabel,
  buildChampionLpCaption,
  buildChampionLpTitle,
  buildClimbEmpty,
  buildClimbHeadline,
  buildClimbStats,
  buildClimbTitle,
  buildLeftOutNote,
  buildRankLine,
  describeChampionLp,
  describeClimb,
  formatChampionValue,
  formatLp,
  ladderDivision,
  rankName,
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
import { climbResponse, focusFixture, statTrendsResponse, winFactorsResponse, winRateClimbResponse } from '@test/helpers/soloFixtures'

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

describe('soloSummary climb', () => {
  it('writes LP with a real minus and ±0', () => {
    expect(formatLp(148)).toBe('+148 LP')
    expect(formatLp(-20)).toBe(`${MINUS}20 LP`)
    expect(formatLp(0)).toBe('±0 LP')
  })

  it('names divisions on the ladder', () => {
    expect(rankName('EMERALD', 'II')).toBe('Emerald II')
    expect(rankName('GRANDMASTER', null)).toBe('Grandmaster')
    expect(ladderDivision(2200)).toBe('Emerald II')
    expect(ladderDivision(0)).toBe('Iron IV')
    expect(ladderDivision(3100)).toBe('Master')
  })

  describe('buildClimbHeadline (FR 5–6)', () => {
    it('counts LP in LP mode', () => {
      expect(buildClimbHeadline(climbResponse())).toBe('+64 LP over your last 20 matches')
      expect(buildClimbHeadline(climbResponse({ range: 'season' }))).toBe('+64 LP this season')
    })

    it('counts wins in win-rate mode', () => {
      expect(buildClimbHeadline(winRateClimbResponse())).toBe('12 wins in your last 20')
      expect(buildClimbHeadline(winRateClimbResponse({ range: 'season', matches: 64 }))).toBe('12 wins in 64 matches this season')
      expect(buildClimbHeadline(winRateClimbResponse({ wins: 1 }))).toBe('1 win in your last 20')
    })

    it('says when there are no wins', () => {
      expect(buildClimbHeadline(winRateClimbResponse({ wins: 0 }))).toBe('No wins in your last 20')
    })

    it('is null without matches', () => {
      expect(buildClimbHeadline(climbResponse({ matches: 0 }))).toBeNull()
      expect(buildClimbHeadline(null)).toBeNull()
    })
  })

  it('builds the rank line for one ranked queue (FR 8)', () => {
    expect(buildRankLine(climbResponse())).toEqual({ text: 'Emerald II · 34 LP · Solo/Duo', tierKey: 'emerald' })
    expect(buildRankLine(winRateClimbResponse())).toBeNull()
  })

  describe('buildClimbTitle (FR 11–12)', () => {
    it('names the divisions in LP mode', () => {
      expect(buildClimbTitle(climbResponse())).toBe('From Emerald III to Emerald II')
      const held = climbResponse()
      held.lp = { ...held.lp, start: { tier: 'EMERALD', division: 'II', lp: 10 } }
      expect(buildClimbTitle(held)).toBe('Holding Emerald II')
    })

    it('compares win rates in win-rate mode, holding under 3 points', () => {
      expect(buildClimbTitle(winRateClimbResponse())).toBe('Win rate up from 50% to 70%')
      expect(buildClimbTitle(winRateClimbResponse({ winRate: { was: 60, now: 52, points: [] } }))).toBe('Win rate down from 60% to 52%')
      expect(buildClimbTitle(winRateClimbResponse({ winRate: { was: 52, now: 54, points: [] } }))).toBe('Win rate held at 54%')
      expect(buildClimbTitle(winRateClimbResponse({ winRate: null }))).toBeNull()
    })
  })

  it('lists win rate, LP per match and the record', () => {
    expect(buildClimbStats(climbResponse()).map((s) => s.value)).toEqual(['60%', '+3.2', '12–8'])
    expect(buildClimbStats(winRateClimbResponse()).map((s) => s.key)).toEqual(['winRate', 'record'])
  })

  it('asks for 20 matches before the win-rate line', () => {
    expect(buildClimbEmpty(winRateClimbResponse({ matches: 14, winRate: null })).title).toBe('Play 6 more matches to see your climb')
    expect(buildClimbEmpty(climbResponse())).toBeNull()
  })

  it('describes the climb in words', () => {
    expect(describeClimb(climbResponse())).toBe(
      'LP, last 20 matches: from Emerald III 70 LP to Emerald II 34 LP, +64 LP. '
        + 'Promoted to Emerald II at match 2. Demoted to Emerald III at match 4. Promoted to Emerald II at match 20. '
        + `Biggest drop ${MINUS}60 LP over 3 losses, ending at match 4.`
    )
    expect(describeClimb(winRateClimbResponse())).toBe('Win rate over 10 matches, last 20 matches: from 50% to 70%.')
  })

  describe('LP per champion (FR 24–25)', () => {
    it('titles the card after the top champion, or the lowest when none gained', () => {
      expect(buildChampionLpTitle(climbResponse())).toBe('Ahri earned most of your climb')
      const losing = climbResponse({ champions: [{ championName: 'Ahri', value: -5 }, { championName: 'Zed', value: -30 }] })
      expect(buildChampionLpTitle(losing)).toBe('Zed cost you the most LP')
      expect(buildChampionLpTitle(winRateClimbResponse())).toBe('Ahri won you the most matches')
      const losingWins = winRateClimbResponse({ champions: [{ championName: 'Ahri', value: 0 }, { championName: 'Zed', value: -2 }] })
      expect(buildChampionLpTitle(losingWins)).toBe('Zed cost you the most matches')
    })

    it('captions and formats by mode', () => {
      expect(buildChampionLpCaption(climbResponse())).toBe('LP won or lost per champion')
      expect(buildChampionLpCaption(winRateClimbResponse())).toBe('Wins minus losses per champion')
      expect(formatChampionValue(58, 'lp')).toBe('+58 LP')
      expect(formatChampionValue(-3, 'winRate')).toBe(`${MINUS}3`)
      expect(formatChampionValue(0, 'winRate')).toBe('±0')
    })

    it('names up to three champions left out', () => {
      expect(buildLeftOutNote(['Orianna'])).toBe('Left out with fewer than 3 matches: Orianna.')
      expect(buildLeftOutNote(['Orianna', 'Zed', 'Lux'])).toBe('Left out with fewer than 3 matches: Orianna, Zed and Lux.')
      expect(buildLeftOutNote(['Orianna', 'Zed', 'Lux', 'Akali', 'Viktor'])).toBe('Left out with fewer than 3 matches: Orianna, Zed, Lux and 2 more.')
      expect(buildLeftOutNote([])).toBeNull()
    })

    it('describes a row in words', () => {
      expect(describeChampionLp(climbResponse().champions[0], 'lp')).toBe('Ahri: 9 matches, 6 wins, +58 LP.')
      expect(describeChampionLp(winRateClimbResponse().champions[1], 'winRate')).toBe(`Syndra: 5 matches, 1 win, ${MINUS}3 net wins.`)
    })
  })
})

describe('soloSummary focus (FR 19–20, FR 29)', () => {
  it('finds the one stat slipping, or the biggest lever', () => {
    expect(buildFocusFinding(focusFixture(), 'slipping')).toBe('Vision is the one stat slipping')
    expect(buildFocusFinding(focusFixture({ stat: 'deaths', factor: 'lowDeaths' }), 'steady')).toBe('Deaths is your biggest lever')
  })

  it('gives the evidence from the player’s own matches', () => {
    expect(buildFocusEvidence(focusFixture()))
      .toBe('Down from 0.9 to 0.7 per minute. You win 63% of matches at 0.9+ vision per minute, and 44% below it.')
    expect(buildFocusEvidence(focusFixture({ stat: 'goldLeadAt15', factor: 'aheadAt15', mark: null, was: 120, now: 340 })))
      .toBe("Up from +120 to +340 gold. You win 63% of matches at ahead at 15 minutes, and 44% when you're behind.")
    expect(buildFocusEvidence(focusFixture({ stat: 'deaths', factor: 'lowDeaths', mark: null, was: 5, now: 5 })))
      .toBe('Holding at 5.0 per match. You win 63% of matches at 4 or fewer deaths, and 44% with more.')
  })

  it('skips the change without a was', () => {
    expect(buildFocusEvidence(focusFixture({ was: null }))).toBe('You win 63% of matches at 0.9+ vision per minute, and 44% below it.')
  })

  it('picks the fix, with the support and jungle variants from the mark', () => {
    expect(buildFocusFix(focusFixture())).toEqual({ icon: 'eye', text: 'Buy a control ward on every back.' })
    expect(buildFocusFix(focusFixture({ mark: 2 })).text).toBe('Place a control ward before every dragon.')
    expect(buildFocusFix(focusFixture({ stat: 'csPerMin', factor: 'cs', mark: 7 })).text).toBe('Last-hit every cannon minion.')
    expect(buildFocusFix(focusFixture({ stat: 'csPerMin', factor: 'cs', mark: 5.5 })).text).toBe('Finish your full first clear before the first gank.')
    expect(buildFocusFix(focusFixture({ stat: 'dragonParticipation', factor: 'dragons' })))
      .toEqual({ icon: 'castle', text: 'Move to the river 30 seconds before dragon spawns.' })
    expect(buildFocusFix(null)).toBeNull()
  })

  it('counts hits against the matches where the mark applied', () => {
    expect(buildFocusStripCaption(focusFixture())).toBe('Hit the mark in 4 of 19 matches')
  })

  it('names the mark in sentence case', () => {
    expect(focusMarkLabel(focusFixture({ factor: 'aheadAt15' }))).toBe('Ahead at 15 minutes')
    expect(focusMarkLabel(focusFixture({ factor: 'cs', mark: 5.5 }))).toBe('5.5+ CS per minute')
  })
})

describe('soloSummary death zones (FR 33, 37, 39)', () => {
  it('names zones and says where deaths cost the most', () => {
    expect(zoneLabel('riverBot')).toBe('Bot river')
    expect(buildDeathZonesTitle([{ key: 'baronPit', deaths: 9, lostObjectives: 3 }])).toBe('Deaths in the Baron pit cost you the most objectives')
    expect(buildDeathZonesTitle([
      { key: 'riverBot', deaths: 9, lostObjectives: 2 },
      { key: 'topLaneEnemy', deaths: 20, lostObjectives: 1 }
    ])).toBe('You die most in their half of top lane')
    expect(buildDeathZonesTitle([])).toBeNull()
  })

  it('captions the range and the mirroring', () => {
    expect(buildDeathZonesCaption({ deaths: 40, matches: 64, range: 'season' }))
      .toBe('40 deaths over 64 matches this season. Red-side matches are mirrored, so your base is always bottom left.')
  })

  it('writes the timing note by phase', () => {
    expect(zoneTimingNote({ phase: 'late', count: 27 })).toBe('27 of them after 25 minutes')
    expect(zoneTimingNote({ phase: 'early', count: 9 })).toBe('Mostly before 14 minutes')
    expect(zoneTimingNote({ phase: 'mid', count: 9 })).toBe('Mostly between 14 and 25 minutes')
    expect(zoneTimingNote({ phase: null, count: 4 })).toBe('Spread over the match')
  })

  it('describes the map in words', () => {
    expect(describeDeathMap({ range: 'last20', matches: 20, zones: [{ key: 'riverBot', deaths: 6, lostObjectives: 1 }] }))
      .toBe('Death map, last 20 matches: Bot river, 6 deaths, 1 objective lost.')
  })

  it('writes the backfill progress for each status', () => {
    expect(buildBackfillProgress({ status: 'running', done: 12, total: 50 })).toMatchObject({
      title: 'Adding detail to your older matches · 12 of 50',
      line: 'Your death map appears when this finishes. You can keep using the rest of the page.',
      determinate: true
    })
    expect(buildBackfillProgress({ status: 'waiting', done: 12, total: 50 }).line).toBe("Waiting on Riot's servers. We'll continue automatically.")
    expect(buildBackfillProgress({ status: 'queued', done: 0, total: 0 })).toMatchObject({
      title: 'Adding detail to your older matches', line: 'Queued behind your match sync.', determinate: false
    })
    expect(buildBackfillProgress(null)).toBeNull()
  })
})
