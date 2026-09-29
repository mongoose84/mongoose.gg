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
