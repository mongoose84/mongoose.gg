import { describe, it, expect } from 'vitest'
import { getWinRateColorClass } from '@/composables/useWinRateColor'

describe('getWinRateColorClass', () => {
  describe('null / undefined / NaN inputs', () => {
    it('returns winrate-neutral for null', () => {
      expect(getWinRateColorClass(null)).toBe('winrate-neutral')
    })

    it('returns winrate-neutral for undefined', () => {
      expect(getWinRateColorClass(undefined)).toBe('winrate-neutral')
    })

    it('returns winrate-neutral for NaN', () => {
      expect(getWinRateColorClass(NaN)).toBe('winrate-neutral')
    })
  })

  describe('red range (< 47)', () => {
    it('returns winrate-red for 0', () => {
      expect(getWinRateColorClass(0)).toBe('winrate-red')
    })

    it('returns winrate-red for 46', () => {
      expect(getWinRateColorClass(46)).toBe('winrate-red')
    })

    it('returns winrate-red for 46.99', () => {
      expect(getWinRateColorClass(46.99)).toBe('winrate-red')
    })
  })

  describe('red-orange range (47–48.99)', () => {
    it('returns winrate-redorange for 47', () => {
      expect(getWinRateColorClass(47)).toBe('winrate-redorange')
    })

    it('returns winrate-redorange for 48', () => {
      expect(getWinRateColorClass(48)).toBe('winrate-redorange')
    })

    it('returns winrate-redorange for 48.99', () => {
      expect(getWinRateColorClass(48.99)).toBe('winrate-redorange')
    })
  })

  describe('orange range (49–50.99)', () => {
    it('returns winrate-orange for 49', () => {
      expect(getWinRateColorClass(49)).toBe('winrate-orange')
    })

    it('returns winrate-orange for 50', () => {
      expect(getWinRateColorClass(50)).toBe('winrate-orange')
    })

    it('returns winrate-orange for 50.99', () => {
      expect(getWinRateColorClass(50.99)).toBe('winrate-orange')
    })
  })

  describe('yellow range (51–51.99)', () => {
    it('returns winrate-yellow for 51', () => {
      expect(getWinRateColorClass(51)).toBe('winrate-yellow')
    })

    it('returns winrate-yellow for 51.99', () => {
      expect(getWinRateColorClass(51.99)).toBe('winrate-yellow')
    })
  })

  describe('yellow-green range (52–52.99)', () => {
    it('returns winrate-yellowgreen for 52', () => {
      expect(getWinRateColorClass(52)).toBe('winrate-yellowgreen')
    })

    it('returns winrate-yellowgreen for 52.99', () => {
      expect(getWinRateColorClass(52.99)).toBe('winrate-yellowgreen')
    })
  })

  describe('green range (>= 53)', () => {
    it('returns winrate-green for 53', () => {
      expect(getWinRateColorClass(53)).toBe('winrate-green')
    })

    it('returns winrate-green for 60', () => {
      expect(getWinRateColorClass(60)).toBe('winrate-green')
    })

    it('returns winrate-green for 100', () => {
      expect(getWinRateColorClass(100)).toBe('winrate-green')
    })
  })
})
