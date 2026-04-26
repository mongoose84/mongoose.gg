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

  describe('terrible range (< 40)', () => {
    it('returns winrate-terrible for 0', () => {
      expect(getWinRateColorClass(0)).toBe('winrate-terrible')
    })

    it('returns winrate-terrible for 30', () => {
      expect(getWinRateColorClass(30)).toBe('winrate-terrible')
    })

    it('returns winrate-terrible for 39.99', () => {
      expect(getWinRateColorClass(39.99)).toBe('winrate-terrible')
    })
  })

  describe('bad range (40–44.99)', () => {
    it('returns winrate-bad for 40', () => {
      expect(getWinRateColorClass(40)).toBe('winrate-bad')
    })

    it('returns winrate-bad for 42', () => {
      expect(getWinRateColorClass(42)).toBe('winrate-bad')
    })

    it('returns winrate-bad for 44.99', () => {
      expect(getWinRateColorClass(44.99)).toBe('winrate-bad')
    })
  })

  describe('poor range (45–47.99)', () => {
    it('returns winrate-poor for 45', () => {
      expect(getWinRateColorClass(45)).toBe('winrate-poor')
    })

    it('returns winrate-poor for 46', () => {
      expect(getWinRateColorClass(46)).toBe('winrate-poor')
    })

    it('returns winrate-poor for 47.99', () => {
      expect(getWinRateColorClass(47.99)).toBe('winrate-poor')
    })
  })

  describe('average range (48–51.99)', () => {
    it('returns winrate-average for 48', () => {
      expect(getWinRateColorClass(48)).toBe('winrate-average')
    })

    it('returns winrate-average for 50', () => {
      expect(getWinRateColorClass(50)).toBe('winrate-average')
    })

    it('returns winrate-average for 51.99', () => {
      expect(getWinRateColorClass(51.99)).toBe('winrate-average')
    })
  })

  describe('good range (52–54.99)', () => {
    it('returns winrate-good for 52', () => {
      expect(getWinRateColorClass(52)).toBe('winrate-good')
    })

    it('returns winrate-good for 53', () => {
      expect(getWinRateColorClass(53)).toBe('winrate-good')
    })

    it('returns winrate-good for 54.99', () => {
      expect(getWinRateColorClass(54.99)).toBe('winrate-good')
    })

    it('returns winrate-good for 55', () => {
      expect(getWinRateColorClass(55)).toBe('winrate-good')
    })
  })

  describe('great range (> 55)', () => {
    it('returns winrate-great for 55.01', () => {
      expect(getWinRateColorClass(55.01)).toBe('winrate-great')
    })

    it('returns winrate-great for 60', () => {
      expect(getWinRateColorClass(60)).toBe('winrate-great')
    })

    it('returns winrate-great for 100', () => {
      expect(getWinRateColorClass(100)).toBe('winrate-great')
    })
  })
})
