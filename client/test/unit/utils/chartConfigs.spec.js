import { describe, it, expect } from 'vitest'
import {
  ACCOUNT_COLORS,
  ACCOUNT_DASH_PATTERNS,
  winrateConfig,
  deathsConfig,
  dragonParticipationConfig
} from '@/utils/chartConfigs'

describe('chartConfigs', () => {
  describe('ACCOUNT_COLORS', () => {
    it('exports an array of color strings', () => {
      expect(Array.isArray(ACCOUNT_COLORS)).toBe(true)
      expect(ACCOUNT_COLORS.length).toBeGreaterThan(0)
      ACCOUNT_COLORS.forEach((c) => expect(typeof c).toBe('string'))
    })
  })

  describe('ACCOUNT_DASH_PATTERNS', () => {
    it('exports an array of dash pattern arrays', () => {
      expect(Array.isArray(ACCOUNT_DASH_PATTERNS)).toBe(true)
      ACCOUNT_DASH_PATTERNS.forEach((p) => expect(Array.isArray(p)).toBe(true))
    })
  })

  describe('winrateConfig', () => {
    it('returns correct dataKey and label', () => {
      const config = winrateConfig()
      expect(config.dataKey).toBe('winRate')
      expect(config.label).toBe('Winrate %')
    })

    it('color function returns green for high winrate (>= 52)', () => {
      const config = winrateConfig()
      const data = [{ winRate: 55 }]
      expect(config.color(data)).toBe('#22c55e')
    })

    it('color function returns red for low winrate (< 48)', () => {
      const config = winrateConfig()
      const data = [{ winRate: 44 }]
      expect(config.color(data)).toBe('#ef4444')
    })

    it('color function returns purple for neutral winrate', () => {
      const config = winrateConfig()
      const data = [{ winRate: 50 }]
      expect(config.color(data)).toBe('#6d28d9')
    })

    it('color function returns purple for empty data', () => {
      const config = winrateConfig()
      expect(config.color([])).toBe('#6d28d9')
      expect(config.color(null)).toBe('#6d28d9')
    })

    it('yAxis has correct range and formatter', () => {
      const config = winrateConfig()
      expect(config.yAxis.min).toBe(0)
      expect(config.yAxis.max).toBe(100)
      expect(config.yAxis.formatter(50)).toBe('50%')
    })

    it('includes annotation when overallWinRate is provided', () => {
      const config = winrateConfig({ overallWinRate: 51.5 })
      expect(config.annotations).toHaveLength(1)
      expect(config.annotations[0].value).toBe(51.5)
    })

    it('has empty annotations when overallWinRate is not provided', () => {
      const config = winrateConfig()
      expect(config.annotations).toHaveLength(0)
    })

    it('has empty annotations when overallWinRate is null', () => {
      const config = winrateConfig({ overallWinRate: null })
      expect(config.annotations).toHaveLength(0)
    })

    it('tooltip title returns game index label', () => {
      const config = winrateConfig()
      expect(config.tooltip.title({ gameIndex: 5 })).toBe('Game 5')
    })

    it('tooltip label includes winrate, record, and game result', () => {
      const config = winrateConfig()
      const lines = config.tooltip.label({ winRate: 52.3, wins: 5, losses: 4, gameIndex: 10, isWin: true })
      expect(lines).toContain('Winrate: 52.3%')
      expect(lines).toContain('Record: 5-4')
      expect(lines.some((l) => l.includes('Win'))).toBe(true)
    })
  })

  describe('deathsConfig', () => {
    it('returns correct dataKey and label', () => {
      const config = deathsConfig()
      expect(config.dataKey).toBe('rollingAverage')
      expect(config.label).toBe('Deaths')
    })

    it('yAxis min is 0', () => {
      const config = deathsConfig()
      expect(config.yAxis.min).toBe(0)
    })

    it('includes annotation when overallAverage is provided', () => {
      const config = deathsConfig({ overallAverage: 4.2 })
      expect(config.annotations).toHaveLength(1)
      expect(config.annotations[0].value).toBe(4.2)
    })

    it('has empty annotations when overallAverage is not provided', () => {
      const config = deathsConfig()
      expect(config.annotations).toHaveLength(0)
    })

    it('passes trend option through to color config', () => {
      const config = deathsConfig({ trend: 'positive' })
      expect(config.color).toEqual({ type: 'trend', trend: 'positive' })
    })

    it('defaults trend to neutral', () => {
      const config = deathsConfig()
      expect(config.color.trend).toBe('neutral')
    })

    it('tooltip label filters out null lines (no role)', () => {
      const config = deathsConfig()
      const lines = config.tooltip.label({
        gameIndex: 1,
        championName: 'Jinx',
        deaths: 3,
        rollingAverage: 3.5,
        role: null,
        timestamp: new Date('2025-01-01').getTime()
      })
      const hasNull = lines.some((l) => l === null)
      expect(hasNull).toBe(false)
    })
  })

  describe('dragonParticipationConfig', () => {
    it('returns correct dataKey and label', () => {
      const config = dragonParticipationConfig()
      expect(config.dataKey).toBe('rollingAverage')
      expect(config.label).toBe('Dragon Participation')
    })

    it('yAxis min is 0 and max is 100', () => {
      const config = dragonParticipationConfig()
      expect(config.yAxis.min).toBe(0)
      expect(config.yAxis.max).toBe(100)
    })

    it('always includes the 70% target annotation', () => {
      const config = dragonParticipationConfig()
      const target = config.annotations.find((a) => a.value === 70)
      expect(target).toBeDefined()
      expect(target.label).toBe('Target: 70%')
    })

    it('includes overall annotation when overallAverage is provided', () => {
      const config = dragonParticipationConfig({ overallAverage: 65.0 })
      const overall = config.annotations.find((a) => a.value === 65.0)
      expect(overall).toBeDefined()
    })

    it('only has the target annotation when overallAverage is null', () => {
      const config = dragonParticipationConfig({ overallAverage: null })
      expect(config.annotations).toHaveLength(1)
    })
  })
})
