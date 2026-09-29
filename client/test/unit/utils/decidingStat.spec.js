import { describe, it, expect } from 'vitest'
import {
  formatStatValue,
  getMeterScale,
  buildValueText,
  buildMeterProps,
  buildFinding,
  buildFix,
  buildUsualNote,
  STAT_LABELS,
  STAT_BETTER,
  STAT_FIX_ICONS
} from '@/utils/decidingStat'

function makeDecidingStat(stat, outcome, value, usual, usualMatches = 20) {
  return {
    outcome,
    stat,
    meters: [{ stat, value, usual, score: outcome === 'strength' ? 1.5 : -1.5 }],
    fix: null,
    usualMatches
  }
}

describe('decidingStat', () => {
  describe('formatStatValue', () => {
    it('formats gold signed with a real minus', () => {
      expect(formatStatValue('goldLeadAt10', 1240)).toBe('+1,240')
      expect(formatStatValue('goldLeadAt10', -850)).toBe('−850')
    })

    it('formats CS and deaths as plain whole numbers', () => {
      expect(formatStatValue('csAt10', 84)).toBe('84')
      expect(formatStatValue('deathsBefore10', 3)).toBe('3')
    })

    it('formats kill participation as a whole percentage', () => {
      expect(formatStatValue('killParticipation', 70.6)).toBe('71%')
    })

    it('formats vision to one decimal', () => {
      expect(formatStatValue('visionPerMin', 0.6)).toBe('0.6')
      expect(formatStatValue('visionPerMin', 1)).toBe('1.0')
    })
  })

  describe('getMeterScale', () => {
    it('keeps gold at ±2,000 within range', () => {
      expect(getMeterScale('goldLeadAt10', 1240, 180)).toEqual({ min: -2000, max: 2000 })
    })

    it('widens gold to the next 1,000 above the larger magnitude', () => {
      expect(getMeterScale('goldLeadAt10', 2500, 100)).toEqual({ min: -3000, max: 3000 })
      // Exactly on a 1,000 boundary still widens to the next band above it
      expect(getMeterScale('goldLeadAt10', 3000, 100)).toEqual({ min: -4000, max: 4000 })
    })

    it('keeps CS at 0–100 within range', () => {
      expect(getMeterScale('csAt10', 84, 71)).toEqual({ min: 0, max: 100 })
    })

    it('widens CS to the next 20 above the larger value', () => {
      expect(getMeterScale('csAt10', 110, 71)).toEqual({ min: 0, max: 120 })
    })

    it('scales deaths to max(4, value, ceil(usual))', () => {
      expect(getMeterScale('deathsBefore10', 0, 0.4)).toEqual({ min: 0, max: 4 })
      expect(getMeterScale('deathsBefore10', 5, 1)).toEqual({ min: 0, max: 5 })
    })

    it('keeps kill participation at 0–100', () => {
      expect(getMeterScale('killParticipation', 71, 58)).toEqual({ min: 0, max: 100 })
    })

    it('scales vision to max(3, ceil(max(value, usual)))', () => {
      expect(getMeterScale('visionPerMin', 1, 0.9)).toEqual({ min: 0, max: 3 })
      expect(getMeterScale('visionPerMin', 4.2, 1)).toEqual({ min: 0, max: 5 })
    })
  })

  describe('buildValueText', () => {
    it('states both numbers for gold, signed', () => {
      expect(buildValueText('goldLeadAt10', 1240, 180)).toBe('+1,240 gold, your usual is +180')
    })

    it('pluralizes deaths correctly', () => {
      expect(buildValueText('deathsBefore10', 1, 0.4)).toBe('1 death before 10, your usual is 0.4')
      expect(buildValueText('deathsBefore10', 3, 0.4)).toBe('3 deaths before 10, your usual is 0.4')
    })

    it('spells out percent for kill participation', () => {
      expect(buildValueText('killParticipation', 71, 58)).toBe('71 percent, your usual is 58 percent')
    })

    it('states vision per minute to one decimal', () => {
      expect(buildValueText('visionPerMin', 0.6, 0.9)).toBe('0.6 per minute, your usual is 0.9 per minute')
    })
  })

  describe('buildMeterProps', () => {
    it('builds the full BaseUsualMeter prop set for a meter', () => {
      const props = buildMeterProps({ stat: 'goldLeadAt10', value: 1240, usual: 180, score: 2.4 })
      expect(props).toEqual({
        stat: 'goldLeadAt10',
        label: STAT_LABELS.goldLeadAt10,
        value: '+1,240',
        now: 1240,
        min: -2000,
        max: 2000,
        usual: 180,
        better: STAT_BETTER.goldLeadAt10,
        valueText: '+1,240 gold, your usual is +180'
      })
    })
  })

  describe('buildFinding', () => {
    it('returns the "none" line when nothing stood out', () => {
      expect(buildFinding({ outcome: 'none', stat: null, meters: [] }))
        .toBe('A match like your usual: nothing stood out')
    })

    it('returns null-safe empty string without a decidingStat', () => {
      expect(buildFinding(null)).toBe('')
    })

    it('goldLeadAt10 strength and shortfall', () => {
      expect(buildFinding(makeDecidingStat('goldLeadAt10', 'strength', 1240, 180)))
        .toBe('You won your lane by 1,240 gold at 10')
      expect(buildFinding(makeDecidingStat('goldLeadAt10', 'shortfall', -850, 180)))
        .toBe('You were 850 gold behind your lane at 10')
    })

    it('csAt10 strength and shortfall', () => {
      expect(buildFinding(makeDecidingStat('csAt10', 'strength', 84, 71)))
        .toBe('84 CS at 10, 13 more than usual')
      expect(buildFinding(makeDecidingStat('csAt10', 'shortfall', 58, 71)))
        .toBe('58 CS at 10, 13 fewer than usual')
    })

    it('deathsBefore10 strength with zero deaths', () => {
      expect(buildFinding(makeDecidingStat('deathsBefore10', 'strength', 0, 1.4)))
        .toBe('No early deaths to hold you back')
    })

    it('deathsBefore10 strength with one or more deaths pluralizes death(s)', () => {
      expect(buildFinding(makeDecidingStat('deathsBefore10', 'strength', 1, 1.4)))
        .toBe('Only 1 death before 10, fewer than usual')
      expect(buildFinding(makeDecidingStat('deathsBefore10', 'strength', 2, 3.4)))
        .toBe('Only 2 deaths before 10, fewer than usual')
    })

    it('deathsBefore10 shortfall', () => {
      expect(buildFinding(makeDecidingStat('deathsBefore10', 'shortfall', 3, 0.4)))
        .toBe('3 deaths before 10 put you behind early')
    })

    it('killParticipation strength and shortfall', () => {
      expect(buildFinding(makeDecidingStat('killParticipation', 'strength', 71, 58)))
        .toBe("You were in 71% of your team's kills")
      expect(buildFinding(makeDecidingStat('killParticipation', 'shortfall', 20, 58)))
        .toBe("You were in only 20% of your team's kills")
    })

    it('visionPerMin strength and shortfall', () => {
      expect(buildFinding(makeDecidingStat('visionPerMin', 'strength', 1.2, 0.9)))
        .toBe('Your vision was well above usual at 1.2 per minute')
      expect(buildFinding(makeDecidingStat('visionPerMin', 'shortfall', 0.6, 0.9)))
        .toBe('Your vision dropped to 0.6 per minute')
    })
  })

  describe('buildFix', () => {
    it('returns null without a fix', () => {
      expect(buildFix(null, 'MIDDLE')).toBeNull()
    })

    it('goldLeadAt10', () => {
      expect(buildFix({ stat: 'goldLeadAt10', value: -850, usual: 180, score: -1.2 }, 'MIDDLE')).toEqual({
        icon: 'coins',
        text: 'Trade when your wave is pushing into them, not before.'
      })
    })

    it('csAt10 for a non-Jungle role', () => {
      expect(buildFix({ stat: 'csAt10' }, 'MIDDLE')).toEqual({
        icon: 'wheat',
        text: 'Last-hit every cannon minion until 10 minutes.'
      })
    })

    it('csAt10 for Jungle', () => {
      expect(buildFix({ stat: 'csAt10' }, 'JUNGLE')).toEqual({
        icon: 'wheat',
        text: 'Finish your full first clear before the first gank.'
      })
    })

    it('deathsBefore10', () => {
      expect(buildFix({ stat: 'deathsBefore10' }, 'TOP')).toEqual({
        icon: 'skull',
        text: "Back off when you can't see their jungler before 10."
      })
    })

    it('killParticipation', () => {
      expect(buildFix({ stat: 'killParticipation' }, 'TOP')).toEqual({
        icon: 'handshake',
        text: 'Move with your team for the first dragon fight.'
      })
    })

    it('visionPerMin for Support', () => {
      expect(buildFix({ stat: 'visionPerMin' }, 'UTILITY')).toEqual({
        icon: 'eye',
        text: 'Place a control ward before every dragon.'
      })
    })

    it('visionPerMin for other roles', () => {
      expect(buildFix({ stat: 'visionPerMin' }, 'MIDDLE')).toEqual({
        icon: 'eye',
        text: 'Buy a control ward on your first back.'
      })
    })

    it('exposes an icon per stat from the vocabulary', () => {
      expect(STAT_FIX_ICONS).toEqual({
        goldLeadAt10: 'coins',
        csAt10: 'wheat',
        deathsBefore10: 'skull',
        killParticipation: 'handshake',
        visionPerMin: 'eye'
      })
    })
  })

  describe('buildUsualNote', () => {
    it('states the sample size and role, following the app\'s role names', () => {
      expect(buildUsualNote({ usualMatches: 20 }, 'MIDDLE')).toBe('Your usual · last 20 matches as Mid')
      expect(buildUsualNote({ usualMatches: 20 }, 'JUNGLE')).toBe('Your usual · last 20 matches as Jungle')
      expect(buildUsualNote({ usualMatches: 5 }, 'UTILITY')).toBe('Your usual · last 5 matches as Support')
    })

    it('returns empty string without a decidingStat', () => {
      expect(buildUsualNote(null, 'MIDDLE')).toBe('')
    })
  })
})
