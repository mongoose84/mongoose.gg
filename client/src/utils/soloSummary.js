/**
 * Copy and small display rules for the Solo page (features/solo-trends.spec.md).
 * The server sends numbers and verdicts; every sentence and number format lives here.
 */

const MINUS = '−'

/** FR 15, FR 18, FR 23: verdicts, the focus and the win-factor card need 20 matches. */
export const MIN_MATCHES = 20

/** FR 23: the win-factor card needs at least two rows. */
export const MIN_FACTOR_ROWS = 2

/** FR 27: "after a loss" this many points below "after a win" counts as tilt. */
const TILT_GAP_POINTS = 10

export const QUEUE_OPTIONS = [
  { value: 'ranked_solo', label: 'Solo/Duo' },
  { value: 'ranked_flex', label: 'Flex' },
  { value: 'all', label: 'All queues' }
]

export const RANGE_OPTIONS = [
  { value: 'last20', label: 'Last 20' },
  { value: 'last50', label: 'Last 50' },
  { value: 'season', label: 'Season' }
]

/**
 * The six match-deciding stats (FR 13), in the order the server sends them.
 * `format` is how the number is written, `unit` what follows it.
 */
export const STATS = {
  deaths: {
    label: 'Deaths',
    format: 'decimal',
    unit: 'per match',
    improving: 'Fewer deaths did most of it',
    slipping: "You're dying more"
  },
  goldLeadAt15: {
    label: 'Gold lead at 15',
    format: 'signed',
    unit: 'gold',
    improving: 'A bigger lead at 15 did most of it',
    slipping: 'Your lead at 15 is slipping'
  },
  dragonParticipation: {
    label: 'Dragon participation',
    format: 'percent',
    unit: '',
    improving: 'More dragon fights did most of it',
    slipping: 'Dragon participation is slipping'
  },
  visionPerMin: {
    label: 'Vision',
    format: 'decimal',
    unit: 'per minute',
    improving: 'Better vision did most of it',
    slipping: 'Vision is slipping'
  },
  csPerMin: {
    label: 'CS',
    format: 'decimal',
    unit: 'per minute',
    improving: 'Better farming did most of it',
    slipping: 'CS is slipping'
  },
  killParticipation: {
    label: 'Kill participation',
    format: 'percent',
    unit: '',
    improving: 'More kill participation did most of it',
    slipping: 'Kill participation is slipping'
  }
}

const VERDICTS = {
  improving: { word: 'Improving', tone: 'up' },
  slipping: { word: 'Slipping', tone: 'down' },
  steady: { word: 'Steady', tone: 'neutral' }
}

// ───────────────────────── Numbers (FR 30) ─────────────────────────

function withMinus(text) {
  return text.replace('-', MINUS)
}

function thousands(value) {
  return Math.abs(value).toLocaleString('en-US', { maximumFractionDigits: 0 })
}

/** A signed whole number with a real minus and thousands separators: +1,240, −650, 0. */
export function formatSigned(value) {
  const rounded = Math.round(value)
  if (rounded === 0) return '0'
  return `${rounded > 0 ? '+' : MINUS}${thousands(rounded)}`
}

/** A stat's number without its unit ("4.1", "+320", "62%"); "—" when missing. */
export function formatStatNumber(key, value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '—'
  switch (STATS[key]?.format) {
    case 'signed':
      return formatSigned(value)
    case 'percent':
      return `${Math.round(value)}%`
    default:
      return withMinus(value.toFixed(1))
  }
}

/** A stat's number with its unit ("4.1 per match", "+320 gold", "62%"). */
export function formatStatValue(key, value) {
  const number = formatStatNumber(key, value)
  const unit = STATS[key]?.unit
  return unit && number !== '—' ? `${number} ${unit}` : number
}

export function statLabel(key) {
  return STATS[key]?.label ?? key
}

// ───────────────────────── Ranges and queues ─────────────────────────

export function queueLabel(queue) {
  return QUEUE_OPTIONS.find((o) => o.value === queue)?.label ?? 'All queues'
}

function matchWord(count) {
  return count === 1 ? 'match' : 'matches'
}

/** "last 20 matches" / "this season": the range as the page counted it. */
export function rangeText(range, matches) {
  if (range === 'season') return 'this season'
  return `last ${matches} ${matchWord(matches)}`
}

/**
 * The page headline until the climb card (5c) brings wins and LP:
 * "Your last 20 matches", "Your 64 matches this season". Null with no matches.
 */
export function buildSoloHeadline(range, matches) {
  if (!matches) return null
  if (range === 'season') return `Your ${matches} ${matchWord(matches)} this season`
  return `Your last ${matches} ${matchWord(matches)}`
}

// ───────────────────────── Stat trends (FR 13–17) ─────────────────────────

/** Verdict word, tone (up / down / neutral) and the arrow for the number's direction (FR 15). */
export function statVerdict(stat) {
  const verdict = VERDICTS[stat?.verdict]
  if (!verdict) return { word: `Needs ${MIN_MATCHES} matches`, tone: 'neutral', arrow: '' }

  let arrow = ''
  if (stat.verdict !== 'steady' && stat.now !== stat.was) {
    arrow = stat.now > stat.was ? '▲' : '▼'
  }
  return { ...verdict, arrow }
}

export function benchmarkLabel(benchmark) {
  if (!benchmark) return null
  return benchmark.kind === 'season' ? 'Your season average' : 'Rank average'
}

/** FR 17: the card title from the stats that have a verdict; null when none has one. */
export function buildStatTrendsTitle(stats) {
  const judged = (stats ?? []).filter((s) => s.verdict)
  if (!judged.length) return null

  const improved = judged.filter((s) => s.verdict === 'improving').length
  const slipped = judged.filter((s) => s.verdict === 'slipping').length
  if (improved) return `${improved} of ${judged.length} match-deciding stats improved`
  if (slipped) return `${slipped} of ${judged.length} match-deciding stats slipped`
  return 'Your match-deciding stats held steady'
}

export function buildStatTrendsCaption(range, matches) {
  return `10-match average, ${rangeText(range, matches)}`
}

/**
 * FR 7: the line under the headline. The Improving stat with the largest normalised change,
 * else the Slipping one; null when every stat is Steady or has no verdict.
 * "Fewer deaths did most of it: 4.1 per match, down from 5.6."
 */
export function buildSecondLine(stats) {
  const byChange = (verdict) => (stats ?? [])
    .filter((s) => s.verdict === verdict && STATS[s.key])
    .sort((a, b) => Math.abs(b.normalizedChange ?? 0) - Math.abs(a.normalizedChange ?? 0))[0]

  const improving = byChange('improving')
  const stat = improving ?? byChange('slipping')
  if (!stat) return null

  const reason = improving ? STATS[stat.key].improving : STATS[stat.key].slipping
  const direction = stat.now >= stat.was ? 'up' : 'down'
  return `${reason}: ${formatStatValue(stat.key, stat.now)}, ${direction} from ${formatStatNumber(stat.key, stat.was)}.`
}

/** The tile's text alternative: range, the change and the benchmark in words. */
export function describeStatTrend(stat, range, matches) {
  const label = statLabel(stat.key)
  const now = formatStatValue(stat.key, stat.now)
  const parts = [`${label}, ${rangeText(range, matches)}: ${now}`]
  if (stat.was !== null && stat.was !== undefined) parts.push(`was ${formatStatNumber(stat.key, stat.was)}`)
  parts.push(statVerdict(stat).word.toLowerCase())
  const benchmark = benchmarkLabel(stat.benchmark)
  if (benchmark) parts.push(`${benchmark.toLowerCase()} ${formatStatNumber(stat.key, stat.benchmark.value)}`)
  return `${parts.join(', ')}.`
}

// ───────────────────────── Win factors (FR 21–23) ─────────────────────────

const FACTORS = {
  aheadAt15: { label: 'Ahead at 15 minutes', phrase: 'gold at 15' },
  lowDeaths: { label: '4 or fewer deaths', phrase: 'deaths', plural: true },
  dragons: { label: 'In on 2 or more dragons', phrase: 'dragon presence' },
  vision: { label: (mark) => (mark ? `${mark.toFixed(1)}+ vision per minute` : 'Vision mark for your role'), phrase: 'vision' },
  cs: { label: (mark) => (mark ? `${mark % 1 ? mark.toFixed(1) : mark}+ CS per minute` : 'CS mark for your role'), phrase: 'farming' }
}

/** FR 21: the row label; vision and CS show the role's mark only when the range is one role. */
export function factorLabel(factor) {
  const label = FACTORS[factor.key]?.label
  if (typeof label === 'function') return label(factor.mark)
  return label ?? factor.key
}

/** FR 23: "Your gold at 15 decides your matches most", from the top row. */
export function buildWinFactorsTitle(factors) {
  const top = FACTORS[factors?.[0]?.key]
  if (!top) return null
  return `Your ${top.phrase} ${top.plural ? 'decide' : 'decides'} your matches most`
}

export function describeWinFactor(factor) {
  return `${factorLabel(factor)}: you win ${factor.hitWinRate}% of ${factor.hitMatches} ${matchWord(factor.hitMatches)} when you hit it, `
    + `and ${factor.missWinRate}% of ${factor.missMatches} when you miss it.`
}

// ───────────────────────── Empty states (UI/UX: Behavior) ─────────────────────────

/** No matches in scope: the title and whether the queue filter causes it. */
export function buildNoMatchesEmpty(queue) {
  const byQueue = queue && queue !== 'all'
  return {
    title: byQueue ? `No ${queueLabel(queue)} matches yet` : 'No matches yet',
    description: byQueue
      ? `Your ${queueLabel(queue)} matches show up here after you play one and sync.`
      : 'Play a match or sync now, and your trends show up here within a few minutes.',
    showAllQueues: byQueue
  }
}

/** The win-factor card below its minimum (FR 23); null when the card can show. */
export function buildWinFactorsEmpty(matches, factors) {
  if (matches < MIN_MATCHES) {
    const more = MIN_MATCHES - matches
    return {
      title: `Play ${more} more ${matchWord(more)} to see what decides your matches`,
      description: 'Win factors compare the matches where you hit a mark with the ones where you missed it.'
    }
  }
  if ((factors?.length ?? 0) < MIN_FACTOR_ROWS) {
    return {
      title: 'Your matches split too unevenly to compare yet',
      description: 'Each mark needs 5 matches where you hit it and 5 where you missed it.'
    }
  }
  return null
}

// ───────────────────────── Patterns (FR 26–28) ─────────────────────────

const SESSION_LABELS = { 1: '1st', 2: '2nd', 3: '3rd', '4plus': '4th+' }
const SESSION_BEFORE_WEAK = { 2: 'first', 3: 'second', '4plus': 'third' }
const LENGTH_LABELS = { under25: 'Under 25 min', '25to35': '25–35 min', over35: 'Over 35 min' }
const LENGTH_WEAK_TITLES = {
  under25: 'Short matches get away from you',
  '25to35': 'Mid-length matches get away from you',
  over35: 'Long matches slip away from you'
}

function sessionCard(pattern, range, matches) {
  let title = 'Your win rate holds through a session'
  if (pattern.weak === '1') title = 'Your first match of a session is your weakest'
  else if (pattern.weak) title = `You drop off after your ${SESSION_BEFORE_WEAK[pattern.weak]} match`

  return {
    key: 'session',
    kind: pattern.weak ? 'pattern' : 'strength',
    title,
    caption: `Win rate by match of the session, ${rangeText(range, matches)}`,
    measure: 'Win rate by match of the session',
    groups: pattern.groups.map((g) => ({ key: g.key, label: SESSION_LABELS[g.key] ?? g.key, value: g.winRate })),
    weakKey: pattern.weak
  }
}

function afterLossCard(pattern, range, matches) {
  const tilted = pattern.afterLoss.winRate <= pattern.afterWin.winRate - TILT_GAP_POINTS
  return {
    key: 'afterLoss',
    kind: tilted ? 'pattern' : 'strength',
    title: tilted ? 'Losses carry into your next match' : "A loss doesn't tilt you",
    caption: `Win rate of the next match in a session, ${rangeText(range, matches)}`,
    measure: 'Win rate of the next match',
    groups: [
      { key: 'afterWin', label: 'After a win', value: pattern.afterWin.winRate },
      { key: 'afterLoss', label: 'After a loss', value: pattern.afterLoss.winRate }
    ],
    weakKey: tilted ? 'afterLoss' : null
  }
}

function lengthCard(pattern, range, matches) {
  return {
    key: 'length',
    kind: pattern.weak ? 'trend' : 'strength',
    title: pattern.weak ? LENGTH_WEAK_TITLES[pattern.weak] : 'You win at every match length',
    caption: `Win rate by match length, ${rangeText(range, matches)}`,
    measure: 'Win rate by match length',
    groups: pattern.groups.map((g) => ({ key: g.key, label: LENGTH_LABELS[g.key] ?? g.key, value: g.winRate })),
    weakKey: pattern.weak
  }
}

/** The pattern cards whose rules are met, in page order; empty when none are. */
export function buildPatternCards(patterns, range, matches) {
  if (!patterns) return []
  return [
    patterns.session ? sessionCard(patterns.session, range, matches) : null,
    patterns.afterLoss ? afterLossCard(patterns.afterLoss, range, matches) : null,
    patterns.length ? lengthCard(patterns.length, range, matches) : null
  ].filter(Boolean)
}
