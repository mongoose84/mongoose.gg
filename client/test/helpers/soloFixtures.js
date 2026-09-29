/**
 * Solo page API responses (features/solo-trends.spec.md, API Contracts) for unit tests.
 */

function stat(key, overrides = {}) {
  return {
    key,
    values: [5, 4, null, 6],
    rolling: [{ index: 1, value: 4.5 }, { index: 3, value: 5 }],
    was: 5,
    now: 5,
    count: 3,
    verdict: 'steady',
    normalizedChange: 0,
    benchmark: { kind: 'season', value: 4.9 },
    ...overrides
  }
}

export function statTrendsResponse(overrides = {}) {
  return {
    matches: 20,
    queueType: 'ranked_solo',
    range: 'last20',
    stats: [
      stat('deaths', { was: 5.6, now: 4.1, verdict: 'improving', normalizedChange: 3 }),
      stat('goldLeadAt15', { was: 120, now: 340, verdict: 'improving', normalizedChange: 1.47 }),
      stat('dragonParticipation', { was: 50, now: 52 }),
      stat('visionPerMin', { was: 0.9, now: 0.7, verdict: 'slipping', normalizedChange: -2 }),
      stat('csPerMin', { was: 6.8, now: 6.9 }),
      stat('killParticipation', { was: 55, now: 56 })
    ],
    focus: null,
    ...overrides
  }
}

export function winFactorsResponse(overrides = {}) {
  return {
    matches: 20,
    queueType: 'ranked_solo',
    range: 'last20',
    factors: [
      { key: 'aheadAt15', hitWinRate: 71, missWinRate: 34, hitMatches: 12, missMatches: 8, gap: 37, mark: null },
      { key: 'vision', hitWinRate: 60, missWinRate: 44, hitMatches: 10, missMatches: 10, gap: 16, mark: 0.9 },
      { key: 'lowDeaths', hitWinRate: 40, missWinRate: 55, hitMatches: 11, missMatches: 9, gap: -15, mark: null }
    ],
    patterns: {
      session: {
        groups: [
          { key: '1', matches: 8, winRate: 62 },
          { key: '2', matches: 6, winRate: 67 },
          { key: '4plus', matches: 4, winRate: 25 }
        ],
        weak: '4plus'
      },
      afterLoss: { afterWin: { pairs: 7, winRate: 57 }, afterLoss: { pairs: 6, winRate: 33 } },
      length: null
    },
    ...overrides
  }
}

export function climbResponse(overrides = {}) {
  return {
    matches: 20,
    queueType: 'ranked_solo',
    range: 'last20',
    mode: 'lp',
    wins: 12,
    losses: 8,
    lp: {
      net: 64,
      start: { tier: 'EMERALD', division: 'III', lp: 70 },
      end: { tier: 'EMERALD', division: 'II', lp: 34 },
      points: [{ index: 0, ladder: 2190 }, { index: 1, ladder: 2210 }, { index: 3, ladder: 2150 }, { index: 19, ladder: 2234 }],
      events: [
        { index: 1, kind: 'promotion', tier: 'EMERALD', division: 'II' },
        { index: 3, kind: 'demotion', tier: 'EMERALD', division: 'III' },
        { index: 19, kind: 'promotion', tier: 'EMERALD', division: 'II' }
      ],
      biggestDrop: { index: 3, lp: -60, losses: 3 }
    },
    winRate: null,
    champions: [
      { championId: 103, championName: 'Ahri', matches: 9, wins: 6, value: 58 },
      { championId: 134, championName: 'Syndra', matches: 5, wins: 2, value: -18 }
    ],
    championsLeftOut: ['Orianna'],
    rank: { tier: 'EMERALD', division: 'II', lp: 34 },
    ...overrides
  }
}

export function winRateClimbResponse(overrides = {}) {
  return climbResponse({
    queueType: 'all',
    mode: 'winRate',
    lp: null,
    winRate: { was: 50, now: 70, points: [{ index: 9, rate: 50 }, { index: 19, rate: 70 }] },
    champions: [
      { championId: 103, championName: 'Ahri', matches: 9, wins: 6, value: 3 },
      { championId: 134, championName: 'Syndra', matches: 5, wins: 1, value: -3 }
    ],
    rank: null,
    ...overrides
  })
}

export function focusFixture(overrides = {}) {
  return {
    stat: 'visionPerMin',
    factor: 'vision',
    mark: 0.9,
    was: 0.9,
    now: 0.7,
    hitWinRate: 63,
    missWinRate: 44,
    last20: ['hit', 'miss', null, ...Array(17).fill('miss')].map((r, i) => (i >= 17 ? 'hit' : r)),
    hits: 4,
    ...overrides
  }
}

const breakdown = (phase, how, cost) => ({ phase, how, cost })

export function deathZonesResponse(overrides = {}) {
  return {
    matches: 50,
    queueType: 'ranked_solo',
    range: 'last50',
    deaths: 236,
    ready: true,
    zones: [
      { key: 'jungleEnemyBot', deaths: 38, lostObjectives: 14, costly: true, anchor: { u: 0.74, v: 0.39 }, timing: { phase: 'late', count: 27 } },
      { key: 'midLaneYours', deaths: 44, lostObjectives: 2, costly: false, anchor: { u: 0.4, v: 0.4 }, timing: { phase: null, count: 20 } }
    ],
    breakdowns: {
      all: breakdown({ early: 88, mid: 84, late: 64 }, { ganked: 71, alone: 97, teamfight: 68, other: 0 }, { dragon: 26, tower: 23, baron: 9, herald: 0 }),
      byZone: {
        jungleEnemyBot: breakdown({ early: 4, mid: 7, late: 27 }, { ganked: 0, alone: 22, teamfight: 14, other: 2 }, { dragon: 9, tower: 2, baron: 3, herald: 0 }),
        midLaneYours: breakdown({ early: 30, mid: 10, late: 4 }, { ganked: 20, alone: 10, teamfight: 4, other: 10 }, { dragon: 1, tower: 1, baron: 0, herald: 0 })
      }
    },
    backfill: null,
    ...overrides
  }
}
