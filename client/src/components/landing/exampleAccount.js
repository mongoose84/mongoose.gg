/**
 * Example account shown in the landing page previews (always labelled "Example").
 * Previews use the real design-system components with this data, never screenshots.
 */

export const exampleAccount = {
  riotId: 'Example#EUW',
  rank: 'Emerald II',
  lp: 64,
  main: {
    name: 'Ahri',
    headline: 'Your Ahri laning is elite. Your late-game discipline is not.',
    sub: 'Most of your Ahri losses come from deaths after 25 minutes.',
    stats: [
      { value: '64%', label: 'win rate' },
      { value: '22', label: 'matches' },
      { value: '4.1', label: 'KDA' }
    ]
  },
  scores: [
    { label: 'Laning', value: 82, delta: 5 },
    { label: 'Teamfighting', value: 76, delta: 2 },
    { label: 'Discipline', value: 58, delta: -7 }
  ],
  insight: {
    title: 'Sylas is costing you LP.',
    body: '41% win rate over 9 matches. Ban him, or pick Viktor into him.'
  },
  readiness: {
    value: 71,
    verdict: 'Good to go',
    advice: 'One more match, then take a break. Your deaths rise after the third match in a row.'
  },
  pick: {
    opponent: 'Sylas',
    champion: 'Viktor',
    wins: 6,
    matches: 8,
    winRate: 75,
    alternatives: [
      { name: 'Ahri', winRate: 52 },
      { name: 'Syndra', winRate: 44 }
    ]
  },
  matches: [
    { champion: 'Ahri', win: true, lp: 19, meta: 'Ranked Solo · 27 min · 2h ago' },
    {
      champion: 'Syndra',
      win: false,
      lp: -17,
      meta: 'Ranked Solo · 31 min · 3h ago',
      fixLead: 'Three deaths after 25 minutes',
      fix: 'turned a lead into a loss. Group with your team before Baron.'
    }
  ],
  deathsTrend: [6.1, 6.4, 5.9, 6.2, 6.0, 5.8, 6.1, 5.7, 5.9, 5.6, 5.8, 5.5, 5.6, 5.4, 5.6, 5.3, 5.4, 5.2, 5.3, 5.2],
  champions: [
    { name: 'Ahri', winRate: 64, matches: 22, kda: '4.1', tag: 'Best laning', focus: '60% 30%' },
    { name: 'Syndra', winRate: 57, matches: 18, kda: '3.3', tag: 'Most damage', focus: '55% 25%' },
    { name: 'Viktor', winRate: 61, matches: 13, kda: '3.8', tag: 'Best scaling', focus: '50% 30%' }
  ]
}

/** Signed LP with a real minus sign: +19 LP, −17 LP */
export function formatSignedLp(lp) {
  return lp < 0 ? `−${Math.abs(lp)} LP` : `+${lp} LP`
}
