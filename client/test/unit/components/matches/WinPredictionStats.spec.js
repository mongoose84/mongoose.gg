import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import WinPredictionStats from '@/components/matches/WinPredictionStats.vue'

describe('WinPredictionStats.vue', () => {
  const baseMatch = {
    role: 'MIDDLE',
    kills: 5,
    deaths: 3,
    assists: 8,
    killParticipation: 45,
    damageShare: 30,
    damageDealt: 20000,
    goldEarned: 12000,
    visionScore: 25,
    goldDiffAt15: 500,
    gameDurationSec: 1800,
    csPerMin: 7.5,
    deathsPre10: 1,
    teamDragons: 3,
    dragonsParticipated: 2
  }

  const baseBaseline = {
    role: 'MIDDLE',
    gamesCount: 10,
    avgDeaths: 4.0,
    avgCsPerMin: 7.0,
    avgVisionScore: 25.0,
    avgGameDurationSec: 1800,
    avgKills: 5.0,
    avgAssists: 7.0,
    avgKda: 2.5,
    avgCreepScore: 130,
    avgGoldEarned: 11000,
    avgGoldPerMin: 366,
    avgDamageDealt: 18000,
    avgDamageTaken: 22000,
    avgKillParticipation: 50,
    winRate: 0.52
  }

  const createWrapper = (matchOverrides = {}, baseline = null) =>
    mount(WinPredictionStats, {
      props: { match: { ...baseMatch, ...matchOverrides }, baseline }
    })

  describe('structure', () => {
    it('renders with correct data-testid on root', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="win-prediction-stats"]').exists()).toBe(true)
    })

    it('states how many match-deciding stats went your way in the title', () => {
      // deaths 3 vs 4.0 → neutral; gold +500 → positive; dragons 2/3 → positive; CS 7.5 vs 7.0 → neutral
      const wrapper = createWrapper({}, baseBaseline)
      expect(wrapper.find('.section-title').text()).toBe('2 of 6 match-deciding stats went your way')
    })

    it('says none went your way when only needs-work stats remain', () => {
      const wrapper = createWrapper({ goldDiffAt15: -800, teamDragons: 3, dragonsParticipated: 0, deathsPre10: 3 })
      expect(wrapper.find('.section-title').text()).toBe('None of the 6 match-deciding stats went your way')
    })

    it('says the stats were close to usual when nothing stands out', () => {
      const wrapper = createWrapper({ goldDiffAt15: 100, teamDragons: 0, deathsPre10: 1 })
      expect(wrapper.find('.section-title').text()).toBe('Your match-deciding stats were close to usual')
    })

    it('marks good and needs-work descriptions with purple and orange text', () => {
      const good = createWrapper({ goldDiffAt15: 800 }).find('[data-testid="kpi-tile-gold15"] .kpi-description')
      const bad = createWrapper({ goldDiffAt15: -800 }).find('[data-testid="kpi-tile-gold15"] .kpi-description')
      expect(good.classes()).toContain('mp-up')
      expect(good.text()).toContain('▲')
      expect(bad.classes()).toContain('mp-down')
      expect(bad.text()).toContain('▼')
    })

    it('renders 6 KPI tiles', () => {
      const wrapper = createWrapper()
      expect(wrapper.findAll('.kpi-tile')).toHaveLength(6)
    })

    it('renders all 6 data-testid tile attributes', () => {
      const wrapper = createWrapper()
      const testIds = ['kpi-tile-deaths', 'kpi-tile-gold15', 'kpi-tile-dragon', 'kpi-tile-cspm', 'kpi-tile-vision', 'kpi-tile-deaths-pre10']
      testIds.forEach(id => {
        expect(wrapper.find(`[data-testid="${id}"]`).exists()).toBe(true)
      })
    })
  })

  describe('Deaths tile', () => {
    it('shows neutral sentiment with no baseline', () => {
      const wrapper = createWrapper({ deaths: 10 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths"]').classes()).toContain('neutral')
    })

    it('shows positive sentiment when deaths < avgDeaths - 1', () => {
      const wrapper = createWrapper({ deaths: 2 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-deaths"]').classes()).toContain('positive')
    })

    it('shows negative sentiment when deaths > avgDeaths + 1', () => {
      const wrapper = createWrapper({ deaths: 6 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-deaths"]').classes()).toContain('negative')
    })

    it('shows neutral sentiment when deaths within ±1 of avg', () => {
      const wrapper = createWrapper({ deaths: 4 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-deaths"]').classes()).toContain('neutral')
    })

    it('shows comparison text when baseline provided', () => {
      const wrapper = createWrapper({ deaths: 3 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-deaths"]').find('.kpi-description').text()).toContain('vs your average')
    })

    it('shows no comparison text without baseline', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="kpi-tile-deaths"]').find('.kpi-description').exists()).toBe(false)
    })

    it('rounds floating-point deaths delta to 1 decimal place', () => {
      // 7 - 9.9 === -2.9000000000000004 in IEEE 754; must render as −2.9 (real minus)
      const wrapper = createWrapper({ deaths: 7 }, { ...baseBaseline, avgDeaths: 9.9 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths"]').find('.kpi-description').text()).toContain('−2.9 vs your average')
    })

    it('renders +0 instead of −0 when delta rounds to negative zero', () => {
      // deaths=4, avgDeaths=4.049 → rawDiff ≈ -0.049 → toFixed(1) → "-0.0" → Number → -0
      const wrapper = createWrapper({ deaths: 4 }, { ...baseBaseline, avgDeaths: 4.049 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths"]').find('.kpi-description').text()).toContain('+0 vs your average')
    })
  })

  describe('Gold @15 tile', () => {
    it('shows positive sentiment when goldDiffAt15 >= 500', () => {
      const wrapper = createWrapper({ goldDiffAt15: 600 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').classes()).toContain('positive')
    })

    it('shows negative sentiment when goldDiffAt15 <= -500', () => {
      const wrapper = createWrapper({ goldDiffAt15: -600 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').classes()).toContain('negative')
    })

    it('shows neutral sentiment when goldDiffAt15 is between -500 and 500', () => {
      const wrapper = createWrapper({ goldDiffAt15: 200 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').classes()).toContain('neutral')
    })

    it('shows a dash when goldDiffAt15 is null', () => {
      const wrapper = createWrapper({ goldDiffAt15: null })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').find('.kpi-value').text()).toBe('—')
    })

    it('shows "Ended before 15 minutes" when goldDiffAt15 is null and game < 15m', () => {
      const wrapper = createWrapper({ goldDiffAt15: null, gameDurationSec: 600 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').find('.kpi-description').text()).toContain('Ended before 15 minutes')
    })

    it('shows "Not recorded" when goldDiffAt15 is null and game >= 15m', () => {
      const wrapper = createWrapper({ goldDiffAt15: null, gameDurationSec: 1800 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').find('.kpi-description').text()).toContain('Not recorded')
    })

    it('shows "Won lane" when goldDiffAt15 >= 500', () => {
      const wrapper = createWrapper({ goldDiffAt15: 800 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').find('.kpi-description').text()).toContain('Won lane')
    })

    it('shows "Lost lane" when goldDiffAt15 <= -500', () => {
      const wrapper = createWrapper({ goldDiffAt15: -800 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').find('.kpi-description').text()).toContain('Lost lane')
    })

    it('shows "Even lane" when goldDiffAt15 is between -499 and 499', () => {
      const wrapper = createWrapper({ goldDiffAt15: 100 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').find('.kpi-description').text()).toContain('Even lane')
    })

    it('shows + prefix for positive gold diff', () => {
      const wrapper = createWrapper({ goldDiffAt15: 800 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').find('.kpi-value').text()).toContain('+')
    })

    it('shows a real minus for negative gold diff', () => {
      const wrapper = createWrapper({ goldDiffAt15: -800 })
      expect(wrapper.find('[data-testid="kpi-tile-gold15"]').find('.kpi-value').text()).toBe('−800')
    })
  })

  describe('Dragon Participation tile', () => {
    it('shows "No dragons" value when teamDragons is 0', () => {
      const wrapper = createWrapper({ teamDragons: 0, dragonsParticipated: 0 })
      expect(wrapper.find('[data-testid="kpi-tile-dragon"]').find('.kpi-value').text()).toBe('No dragons')
    })

    it('shows neutral sentiment when teamDragons is 0', () => {
      const wrapper = createWrapper({ teamDragons: 0, dragonsParticipated: 0 })
      expect(wrapper.find('[data-testid="kpi-tile-dragon"]').classes()).toContain('neutral')
    })

    it('shows "No dragons" only once when teamDragons is 0', () => {
      const wrapper = createWrapper({ teamDragons: 0, dragonsParticipated: 0 })
      const tileText = wrapper.find('[data-testid="kpi-tile-dragon"]').text()
      const occurrences = tileText.split('No dragons').length - 1
      expect(occurrences).toBe(1)
    })

    it('shows positive sentiment when participation >= 67%', () => {
      const wrapper = createWrapper({ teamDragons: 3, dragonsParticipated: 2 })
      expect(wrapper.find('[data-testid="kpi-tile-dragon"]').classes()).toContain('positive')
    })

    it('shows negative sentiment when 0 participation and teamDragons > 0', () => {
      const wrapper = createWrapper({ teamDragons: 4, dragonsParticipated: 0 })
      expect(wrapper.find('[data-testid="kpi-tile-dragon"]').classes()).toContain('negative')
    })

    it('shows formatted participation value', () => {
      const wrapper = createWrapper({ teamDragons: 4, dragonsParticipated: 3 })
      expect(wrapper.find('[data-testid="kpi-tile-dragon"]').find('.kpi-value').text()).toBe('3/4 (75%)')
    })

    it('shows "High involvement" description for high participation', () => {
      const wrapper = createWrapper({ teamDragons: 3, dragonsParticipated: 2 })
      expect(wrapper.find('[data-testid="kpi-tile-dragon"]').find('.kpi-description').text()).toContain('High involvement')
    })

    it('shows "Low involvement" description when 0 participation and teamDragons > 0', () => {
      const wrapper = createWrapper({ teamDragons: 4, dragonsParticipated: 0 })
      expect(wrapper.find('[data-testid="kpi-tile-dragon"]').find('.kpi-description').text()).toContain('Low involvement')
    })
  })

  describe('CS/min tile', () => {
    it('shows kpi-value with 1 decimal', () => {
      const wrapper = createWrapper({ csPerMin: 7.53 })
      expect(wrapper.find('[data-testid="kpi-tile-cspm"]').find('.kpi-value').text()).toBe('7.5')
    })

    it('shows positive sentiment when csPerMin > avgCsPerMin + 0.5', () => {
      const wrapper = createWrapper({ csPerMin: 8.0 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-cspm"]').classes()).toContain('positive')
    })

    it('shows negative sentiment when csPerMin < avgCsPerMin - 0.5', () => {
      const wrapper = createWrapper({ csPerMin: 6.0 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-cspm"]').classes()).toContain('negative')
    })

    it('shows neutral sentiment for UTILITY role even with baseline', () => {
      const wrapper = createWrapper({ role: 'UTILITY', csPerMin: 9.0 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-cspm"]').classes()).toContain('neutral')
    })

    it('shows no comparison for SUPPORT role', () => {
      const wrapper = createWrapper({ role: 'SUPPORT', csPerMin: 9.0 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-cspm"]').find('.kpi-description').exists()).toBe(false)
    })

    it('shows comparison text for non-support with baseline', () => {
      const wrapper = createWrapper({ csPerMin: 8.0 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-cspm"]').find('.kpi-description').text()).toContain('vs your average')
    })

    it('shows no comparison without baseline', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="kpi-tile-cspm"]').find('.kpi-description').exists()).toBe(false)
    })
  })

  describe('Vision Score tile', () => {
    it('shows the vision score value', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="kpi-tile-vision"]').find('.kpi-value').text()).toBe('25')
    })

    it('shows positive sentiment when visionScore > expected by > 15%', () => {
      // expected = 25 * (1800/1800) = 25, diff = 30-25=5, pct=20% > 15%
      const wrapper = createWrapper({ visionScore: 30 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-vision"]').classes()).toContain('positive')
    })

    it('shows negative sentiment when visionScore < expected by > 15%', () => {
      // expected = 25, diff = 18-25=-7, pct=-28% < -15%
      const wrapper = createWrapper({ visionScore: 18 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-vision"]').classes()).toContain('negative')
    })

    it('shows neutral sentiment without baseline', () => {
      const wrapper = createWrapper({ visionScore: 5 })
      expect(wrapper.find('[data-testid="kpi-tile-vision"]').classes()).toContain('neutral')
    })

    it('shows comparison text with baseline', () => {
      const wrapper = createWrapper({ visionScore: 30 }, baseBaseline)
      expect(wrapper.find('[data-testid="kpi-tile-vision"]').find('.kpi-description').text()).toContain('vs your average')
    })

    it('shows no comparison without baseline', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="kpi-tile-vision"]').find('.kpi-description').exists()).toBe(false)
    })
  })

  describe('Deaths <10m tile', () => {
    it('shows the deathsPre10 value', () => {
      const wrapper = createWrapper({ deathsPre10: 2 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths-pre10"]').find('.kpi-value').text()).toBe('2')
    })

    it('shows positive sentiment when deathsPre10 is 0', () => {
      const wrapper = createWrapper({ deathsPre10: 0 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths-pre10"]').classes()).toContain('positive')
    })

    it('shows negative sentiment when deathsPre10 >= 2', () => {
      const wrapper = createWrapper({ deathsPre10: 2 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths-pre10"]').classes()).toContain('negative')
    })

    it('shows neutral sentiment when deathsPre10 is 1', () => {
      const wrapper = createWrapper({ deathsPre10: 1 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths-pre10"]').classes()).toContain('neutral')
    })

    it('shows "Safe early game" description when deathsPre10 is 0', () => {
      const wrapper = createWrapper({ deathsPre10: 0 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths-pre10"]').find('.kpi-description').text()).toContain('Safe early game')
    })

    it('shows "Risky early game" description when deathsPre10 >= 2', () => {
      const wrapper = createWrapper({ deathsPre10: 3 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths-pre10"]').find('.kpi-description').text()).toContain('Risky early game')
    })

    it('shows no description when deathsPre10 is 1', () => {
      const wrapper = createWrapper({ deathsPre10: 1 })
      expect(wrapper.find('[data-testid="kpi-tile-deaths-pre10"]').find('.kpi-description').exists()).toBe(false)
    })
  })
})
