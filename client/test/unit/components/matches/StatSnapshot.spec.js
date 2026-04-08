import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import StatSnapshot from '@/components/matches/StatSnapshot.vue'

vi.mock('@/utils/formatters', () => ({
  formatNumber: (n) => (n != null ? n.toLocaleString('en-US') : '0')
}))

describe('StatSnapshot.vue', () => {
  const baseMatch = {
    role: 'MIDDLE',
    kills: 5,
    deaths: 2,
    assists: 8,
    killParticipation: 50,
    damageDealt: 20000,
    damageShare: 25,
    damageTaken: 10000,
    creepScore: 180,
    csPerMin: 6.0,
    goldEarned: 12000,
    goldPerMin: 400,
    visionScore: 25,
    deathsPre10: 0,
    goldDiffAt15: 200,
    gameDurationSec: 1800
  }

  const createWrapper = (matchOverrides = {}, baseline = null) =>
    mount(StatSnapshot, { props: { match: { ...baseMatch, ...matchOverrides }, baseline } })

  it('renders section title "Personal Stats"', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.section-title').text()).toBe('Personal Stats')
  })

  it('renders the stats grid', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.stats-grid').exists()).toBe(true)
  })

  it('shows exactly 10 metrics', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.stat-count').text()).toBe('10 metrics')
  })

  it('renders KDA Ratio stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('KDA Ratio')
  })

  it('renders Kill Participation stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Kill Participation')
  })

  it('renders Damage Dealt stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Damage Dealt')
  })

  it('does not render CS/min stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).not.toContain('CS/min')
  })

  it('does not render Vision Score stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).not.toContain('Vision Score')
  })

  it('renders Dmg/Gold stat at position 3', () => {
    const wrapper = createWrapper()
    const items = wrapper.findAll('.stat-item')
    expect(items[2].find('.stat-label').text()).toBe('Dmg/Gold')
  })

  it('renders Dmg/Gold value as ratio with 2 decimals', () => {
    // damageDealt=20000, goldEarned=12000 → 1.67
    const wrapper = createWrapper()
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Dmg/Gold')
    expect(item.find('.stat-value').text()).toBe('1.67')
  })

  it('applies up trend on Dmg/Gold when ratio >= 1.5', () => {
    // damageDealt=20000, goldEarned=12000 → 1.67 >= 1.5
    const wrapper = createWrapper()
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Dmg/Gold')
    expect(item.classes()).toContain('up')
  })

  it('applies down trend on Dmg/Gold when ratio < 0.8', () => {
    const wrapper = createWrapper({ damageDealt: 5000, goldEarned: 12000 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Dmg/Gold')
    expect(item.classes()).toContain('down')
  })

  it('shows no Dmg/Gold trend for support', () => {
    const wrapper = createWrapper({ role: 'UTILITY', damageDealt: 30000, goldEarned: 12000 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Dmg/Gold')
    expect(item.classes()).not.toContain('up')
  })

  it('renders Dmg/Death at position 10 for non-support', () => {
    const wrapper = createWrapper()
    const items = wrapper.findAll('.stat-item')
    expect(items[9].find('.stat-label').text()).toBe('Dmg/Death')
  })

  it('renders Dmg/Death value correctly', () => {
    // damageDealt=20000, deaths=2 → 10000
    const wrapper = createWrapper()
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Dmg/Death')
    expect(item.find('.stat-value').text()).toContain('10,000')
  })

  it('applies up trend on Dmg/Death when >= 8000', () => {
    const wrapper = createWrapper({ damageDealt: 20000, deaths: 2 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Dmg/Death')
    expect(item.classes()).toContain('up')
  })

  it('applies down trend on Dmg/Death when < 3000', () => {
    const wrapper = createWrapper({ damageDealt: 4000, deaths: 2 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Dmg/Death')
    expect(item.classes()).toContain('down')
  })

  it('uses deaths=1 floor for Dmg/Death when deaths is 0', () => {
    const wrapper = createWrapper({ damageDealt: 20000, deaths: 0 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Dmg/Death')
    expect(item.find('.stat-value').text()).toContain('20,000')
  })

  it('renders Vision/min at position 10 for support', () => {
    const wrapper = createWrapper({ role: 'UTILITY' })
    const items = wrapper.findAll('.stat-item')
    expect(items[9].find('.stat-label').text()).toBe('Vision/min')
  })

  it('renders Vision/min value correctly for support', () => {
    // visionScore=25, gameDurationSec=1800 → 25/30 = 0.8
    const wrapper = createWrapper({ role: 'UTILITY', visionScore: 75, gameDurationSec: 1800 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Vision/min')
    expect(item.find('.stat-value').text()).toBe('2.5')
  })

  it('applies up trend on Vision/min when >= 2.5', () => {
    const wrapper = createWrapper({ role: 'SUPPORT', visionScore: 80, gameDurationSec: 1800 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Vision/min')
    expect(item.classes()).toContain('up')
  })

  it('applies down trend on Vision/min when < 1.5', () => {
    const wrapper = createWrapper({ role: 'UTILITY', visionScore: 20, gameDurationSec: 1800 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Vision/min')
    expect(item.classes()).toContain('down')
  })

  it('calculates KDA as sum of kills+assists when deaths is 0', () => {
    const wrapper = createWrapper({ kills: 10, deaths: 0, assists: 5 })
    const kdaItem = wrapper.findAll('.stat-item').find(i =>
      i.find('.stat-label').text() === 'KDA Ratio'
    )
    expect(kdaItem.find('.stat-value').text()).toBe('15.00')
  })

  it('calculates KDA correctly with deaths', () => {
    const wrapper = createWrapper({ kills: 6, deaths: 2, assists: 4 })
    // (6 + 4) / 2 = 5.00
    const kdaItem = wrapper.findAll('.stat-item').find(i =>
      i.find('.stat-label').text() === 'KDA Ratio'
    )
    expect(kdaItem.find('.stat-value').text()).toBe('5.00')
  })

  describe('No baseline', () => {
    it('shows no comparison text when baseline is null', () => {
      const wrapper = createWrapper()
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA Ratio'
      )
      expect(kdaItem.find('.stat-comparison').exists()).toBe(false)
    })

    it('shows no trend arrow when baseline is null', () => {
      const wrapper = createWrapper()
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA Ratio'
      )
      expect(kdaItem.find('.trend-arrow').exists()).toBe(false)
    })
  })

  describe('With baseline', () => {
    const baseline = {
      gamesCount: 10,
      role: 'MIDDLE',
      avgKda: 3.0,
      avgKillParticipation: 40,
      avgDamageDealt: 18000,
      avgDamageTaken: 9000,
      avgCreepScore: 170,
      avgCsPerMin: 5.5,
      avgGoldEarned: 11000,
      avgGoldPerMin: 380,
      avgVisionScore: 20,
      avgGameDurationSec: 1800
    }

    it('shows comparison text when value is significantly above baseline', () => {
      // KDA of 7.0 vs baseline 3.0 — well above threshold
      const wrapper = createWrapper({ kills: 10, deaths: 2, assists: 4 }, baseline)
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA Ratio'
      )
      expect(kdaItem.find('.stat-comparison').exists()).toBe(true)
    })

    it('applies up trend class when value is above baseline', () => {
      const wrapper = createWrapper({ kills: 10, deaths: 2, assists: 4 }, baseline)
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA Ratio'
      )
      expect(kdaItem.classes()).toContain('up')
    })

    it('applies down trend class when value is below baseline', () => {
      // KDA of 1.0 vs baseline 3.0 — well below threshold
      const wrapper = createWrapper({ kills: 1, deaths: 3, assists: 2 }, baseline)
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA Ratio'
      )
      expect(kdaItem.classes()).toContain('down')
    })

    it('shows no trend class when value is near baseline', () => {
      // KDA of ~3.0 matching the baseline
      const wrapper = createWrapper({ kills: 4, deaths: 2, assists: 2 }, baseline)
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA Ratio'
      )
      expect(kdaItem.classes()).not.toContain('up')
      expect(kdaItem.classes()).not.toContain('down')
    })
  })
})
