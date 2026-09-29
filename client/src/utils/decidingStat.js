/**
 * Copy, formatting and meter-scale rules for the "What decided it" card
 * (Phase 4c of design-migration.plan.md). Kept pure so it can be unit tested.
 * Voice: "you", finding first, sentence case, no emoji, real minus for signed numbers.
 */
import { formatSigned } from './matchesSummary'
import { formatRole } from './formatters'

/** FR1 table order — also the tie-break order the backend uses. */
export const STAT_ORDER = ['goldLeadAt10', 'csAt10', 'deathsBefore10', 'killParticipation', 'visionPerMin']

export const STAT_LABELS = {
  goldLeadAt10: 'Gold lead at 10',
  csAt10: 'CS at 10',
  deathsBefore10: 'Deaths before 10',
  killParticipation: 'Kill participation',
  visionPerMin: 'Vision per minute'
}

/** Which direction is better for each stat — deaths is the only "lower is better" one. */
export const STAT_BETTER = {
  goldLeadAt10: 'higher',
  csAt10: 'higher',
  deathsBefore10: 'lower',
  killParticipation: 'higher',
  visionPerMin: 'higher'
}

/** Lucide icon per stat's fix, from the design-system vocabulary. */
export const STAT_FIX_ICONS = {
  goldLeadAt10: 'coins',
  csAt10: 'wheat',
  deathsBefore10: 'skull',
  killParticipation: 'handshake',
  visionPerMin: 'eye'
}

const JUNGLE_ROLE = 'JUNGLE'
const SUPPORT_ROLES = new Set(['UTILITY', 'SUPPORT'])

/**
 * A stat's value, formatted per the voice rules: gold signed with a real minus, CS and deaths as
 * plain whole numbers, kill participation as a whole percentage, vision to one decimal.
 */
export function formatStatValue(stat, value) {
  switch (stat) {
    case 'goldLeadAt10':
      return formatSigned(value, 0)
    case 'csAt10':
    case 'deathsBefore10':
      return String(Math.round(value))
    case 'killParticipation':
      return `${Math.round(value)}%`
    case 'visionPerMin':
      return value.toFixed(1)
    default:
      return String(value)
  }
}

function nextMultipleAbove(value, step) {
  return (Math.floor(value / step) + 1) * step
}

/**
 * The meter's min/max, clamped to the value (FR "Meter scale"). Gold is centred on zero with a
 * negative min; the others start at zero and widen only when the value or usual exceeds the base
 * range.
 */
export function getMeterScale(stat, value, usual) {
  switch (stat) {
    case 'goldLeadAt10': {
      const bound = Math.max(Math.abs(value), Math.abs(usual))
      const max = bound > 2000 ? nextMultipleAbove(bound, 1000) : 2000
      return { min: -max, max }
    }
    case 'csAt10': {
      const bound = Math.max(value, usual)
      const max = bound > 100 ? nextMultipleAbove(bound, 20) : 100
      return { min: 0, max }
    }
    case 'deathsBefore10':
      return { min: 0, max: Math.max(4, value, Math.ceil(usual)) }
    case 'killParticipation':
      return { min: 0, max: 100 }
    case 'visionPerMin':
      return { min: 0, max: Math.max(3, Math.ceil(Math.max(value, usual))) }
    default:
      return { min: 0, max: Math.max(value, usual, 1) }
  }
}

/** aria-valuetext stating both numbers, e.g. "+1,240 gold, your usual is +180". */
export function buildValueText(stat, value, usual) {
  switch (stat) {
    case 'goldLeadAt10':
      return `${formatSigned(value, 0)} gold, your usual is ${formatSigned(usual, 0)}`
    case 'csAt10':
      return `${Math.round(value)} CS at 10, your usual is ${Math.round(usual)}`
    case 'deathsBefore10': {
      const count = Math.round(value)
      return `${count} ${count === 1 ? 'death' : 'deaths'} before 10, your usual is ${usual.toFixed(1)}`
    }
    case 'killParticipation':
      return `${Math.round(value)} percent, your usual is ${Math.round(usual)} percent`
    case 'visionPerMin':
      return `${value.toFixed(1)} per minute, your usual is ${usual.toFixed(1)} per minute`
    default:
      return `${value}, your usual is ${usual}`
  }
}

/** Full BaseUsualMeter prop set for one `decidingStat.meters[]` entry. */
export function buildMeterProps(meter) {
  const scale = getMeterScale(meter.stat, meter.value, meter.usual)
  return {
    stat: meter.stat,
    label: STAT_LABELS[meter.stat],
    value: formatStatValue(meter.stat, meter.value),
    now: meter.value,
    min: scale.min,
    max: scale.max,
    usual: meter.usual,
    better: STAT_BETTER[meter.stat],
    valueText: buildValueText(meter.stat, meter.value, meter.usual)
  }
}

/** The finding line for one stat and outcome, `{v}`/`{d}` filled in per the copy table. */
function findingLine(stat, isStrength, value, usual) {
  switch (stat) {
    case 'goldLeadAt10': {
      const magnitude = Math.round(Math.abs(value)).toLocaleString('en-US')
      return isStrength
        ? `You won your lane by ${magnitude} gold at 10`
        : `You were ${magnitude} gold behind your lane at 10`
    }
    case 'csAt10': {
      const v = formatStatValue('csAt10', value)
      const d = Math.round(Math.abs(value - usual))
      return isStrength ? `${v} CS at 10, ${d} more than usual` : `${v} CS at 10, ${d} fewer than usual`
    }
    case 'deathsBefore10': {
      const v = Math.round(value)
      if (isStrength) {
        return v === 0
          ? 'No early deaths to hold you back'
          : `Only ${v} ${v === 1 ? 'death' : 'deaths'} before 10, fewer than usual`
      }
      return `${v} deaths before 10 put you behind early`
    }
    case 'killParticipation': {
      const v = formatStatValue('killParticipation', value)
      return isStrength ? `You were in ${v} of your team's kills` : `You were in only ${v} of your team's kills`
    }
    case 'visionPerMin': {
      const v = formatStatValue('visionPerMin', value)
      return isStrength
        ? `Your vision was well above usual at ${v} per minute`
        : `Your vision dropped to ${v} per minute`
    }
    default:
      return ''
  }
}

/** The card's finding: the "none" line, or the deciding stat's strength/shortfall line. */
export function buildFinding(decidingStat) {
  if (!decidingStat) return ''
  if (decidingStat.outcome === 'none') return 'A match like your usual: nothing stood out'

  const meter = decidingStat.meters.find((m) => m.stat === decidingStat.stat)
  if (!meter) return ''
  return findingLine(decidingStat.stat, decidingStat.outcome === 'strength', meter.value, meter.usual)
}

/** The fix row's icon and copy, with the Jungle CS and Support vision variants. Null without a fix. */
export function buildFix(fix, role) {
  if (!fix) return null

  const isJungle = role === JUNGLE_ROLE
  const isSupport = SUPPORT_ROLES.has(role)
  let text

  switch (fix.stat) {
    case 'goldLeadAt10':
      text = 'Trade when your wave is pushing into them, not before.'
      break
    case 'csAt10':
      text = isJungle
        ? 'Finish your full first clear before the first gank.'
        : 'Last-hit every cannon minion until 10 minutes.'
      break
    case 'deathsBefore10':
      text = "Back off when you can't see their jungler before 10."
      break
    case 'killParticipation':
      text = 'Move with your team for the first dragon fight.'
      break
    case 'visionPerMin':
      text = isSupport ? 'Place a control ward before every dragon.' : 'Buy a control ward on your first back.'
      break
    default:
      text = ''
  }

  return { icon: STAT_FIX_ICONS[fix.stat], text }
}

/** "Your usual · last {n} matches as {Role}" — role names match the rest of the app. */
export function buildUsualNote(decidingStat, role) {
  if (!decidingStat) return ''
  return `Your usual · last ${decidingStat.usualMatches} matches as ${formatRole(role)}`
}
