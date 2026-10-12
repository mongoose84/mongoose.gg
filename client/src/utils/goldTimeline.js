/**
 * Copy and chart props for the open match's gold-lead chart (design-migration.plan.md, 4d),
 * kept pure so they can be unit tested. The title states the takeaway; the metric is the subtitle.
 */
import { formatSigned } from './matchesSummary'

// Leads inside this are "even": the swing of one kill and a wave
const CLOSE = 1000

// A lead this big that changes hands is the story of the match
const DECISIVE = 2000

const MOMENTS = {
  dragon: { letter: 'D', name: 'dragon' },
  baron: { letter: 'B', name: 'Baron' },
  herald: { letter: 'H', name: 'Herald' },
  tower: { letter: 'T', name: 'tower' }
}

/** "+3,200" or "−1,500" with "gold" left to the context */
export function formatLead(lead) {
  return formatSigned(lead)
}

function extremes(points) {
  let max = points[0]
  let min = points[0]
  for (const p of points) {
    if (p.lead > max.lead) max = p
    if (p.lead < min.lead) min = p
  }
  return { max, min, last: points[points.length - 1] }
}

/**
 * The takeaway title, from the shape of the lead and the result:
 * a comeback, a lead that slipped, a game led (or trailed) throughout, else where it ended.
 */
export function buildGoldTitle(timeline, win) {
  const { max, min, last } = extremes(timeline.points)

  if (win && min.lead <= -DECISIVE) return `Came back from ${formatLead(min.lead)} at ${min.minute} min`
  if (!win && max.lead >= DECISIVE) return `Lost a ${formatLead(max.lead)} lead from ${max.minute} min`
  if (min.lead > -CLOSE && max.lead >= CLOSE) return `Ahead all match, up to ${formatLead(max.lead)}`
  if (max.lead < CLOSE && min.lead <= -CLOSE) return `Behind all match, down to ${formatLead(min.lead)}`
  if (Math.abs(last.lead) < CLOSE) return 'Even gold all match'
  return `Ended ${formatLead(last.lead)} gold`
}

/** The metric, and how often it was measured */
export function buildGoldSubtitle(timeline) {
  return timeline.source === 'checkpoints'
    ? 'Your team\'s gold lead, every 5 minutes'
    : 'Your team\'s gold lead, minute by minute'
}

/** Text alternative: the range, where it ended, the high and the low */
export function describeGoldTimeline(timeline) {
  const { max, min, last } = extremes(timeline.points)
  return `Your team's gold lead over ${last.minute} minutes: ended ${formatLead(last.lead)}, ` +
    `highest ${formatLead(max.lead)} at ${max.minute} min, lowest ${formatLead(min.lead)} at ${min.minute} min.`
}

/** Props for BaseLineChart: x is the minute, an "Even" guide, the swing as the one orange point */
export function buildGoldChart(timeline) {
  const points = timeline.points.map((p) => ({ x: p.minute, y: p.lead }))
  const lastMinute = points[points.length - 1].x
  const leadAt = new Map(points.map((p) => [p.x, p.y]))

  const markers = []
  const swing = timeline.swing
  if (swing && leadAt.has(swing.minute)) {
    const arrow = swing.change < 0 ? '▼' : '▲'
    markers.push({ x: swing.minute, y: leadAt.get(swing.minute), label: `${arrow} ${formatLead(swing.change)}`, tone: 'warn' })
  }

  const moments = (timeline.moments ?? [])
    .filter((m) => MOMENTS[m.type])
    .map((m) => ({
      x: Math.min(m.timestampSec / 60, lastMinute),
      letter: MOMENTS[m.type].letter,
      tone: m.ours ? 'positive' : 'warn'
    }))

  return {
    points,
    length: lastMinute + 1,
    guides: [{ y: 0, label: 'Even' }],
    markers,
    moments
  }
}

function clock(seconds) {
  const s = Math.max(0, Math.round(seconds))
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`
}

/** One line per objective for screen readers: "6:40 Your team took a dragon" */
export function describeMoments(timeline) {
  return (timeline.moments ?? [])
    .filter((m) => MOMENTS[m.type])
    .map((m) => `${clock(m.timestampSec)} ${m.ours ? 'Your team' : 'The enemy'} took a ${MOMENTS[m.type].name}`)
}

/** The lead every 5 minutes and at the end, for the screen-reader list */
export function describeLeads(timeline) {
  const last = timeline.points[timeline.points.length - 1]
  return timeline.points
    .filter((p) => p.minute % 5 === 0 || p === last)
    .filter((p) => p.minute > 0)
    .map((p) => `${p.minute} min: ${formatLead(p.lead)}`)
}

/** The swing as a caption, so the orange point has words too */
export function buildSwingCaption(timeline) {
  const swing = timeline.swing
  if (!swing) return null
  const direction = swing.change < 0 ? 'Biggest swing against you' : 'Biggest swing for you'
  return `${direction}: ${formatLead(swing.change)} gold from ${swing.fromMinute} to ${swing.minute} min`
}
