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

  it('says the stats were close to average when nothing stands out', () => {
    // damage per gold 1.0, damage share 20%, damage per death 6,000: all in the normal band
    const wrapper = createWrapper({ damageDealt: 12000, damageShare: 20 })
    expect(wrapper.find('.section-title').text()).toBe('Your stats were close to your average')
  })

  it('counts stats above and below the average in the title', () => {
    // damage per gold 20000 / 12000 = 1.67 → up; damage share 25 → up; nothing down
    const wrapper = createWrapper({ damageShare: 25 })
    expect(wrapper.find('.section-title').text()).toBe('3 of your stats beat your average')
    const mixed = createWrapper({ damageDealt: 8000, goldEarned: 12000, damageShare: 10 })
    expect(mixed.find('.section-title').text()).toMatch(/below/)
  })

  it('renders the stats grid', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.stats-grid').exists()).toBe(true)
  })

  it('shows exactly 10 metrics', () => {
    const wrapper = createWrapper()
    expect(wrapper.findAll('.stat-item')).toHaveLength(10)
  })

  it('renders KDA Ratio stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('KDA ratio')
  })

  it('renders Kill Participation stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Kill participation')
  })

  it('renders Damage Dealt stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Damage dealt')
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

  it('renders Damage per gold stat at position 3', () => {
    const wrapper = createWrapper()
    const items = wrapper.findAll('.stat-item')
    expect(items[2].find('.stat-label').text()).toBe('Damage per gold')
  })

  it('renders Damage per gold value as ratio with 2 decimals', () => {
    // damageDealt=20000, goldEarned=12000 → 1.67
    const wrapper = createWrapper()
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Damage per gold')
    expect(item.find('.stat-value').text()).toBe('1.67')
  })

  it('applies up trend on damage per gold when ratio >= 1.5', () => {
    // damageDealt=20000, goldEarned=12000 → 1.67 >= 1.5
    const wrapper = createWrapper()
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Damage per gold')
    expect(item.classes()).toContain('up')
  })

  it('applies down trend on damage per gold when ratio < 0.8', () => {
    const wrapper = createWrapper({ damageDealt: 5000, goldEarned: 12000 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Damage per gold')
    expect(item.classes()).toContain('down')
  })

  it('shows no damage per gold trend for support', () => {
    const wrapper = createWrapper({ role: 'UTILITY', damageDealt: 30000, goldEarned: 12000 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Damage per gold')
    expect(item.classes()).not.toContain('up')
  })

  it('renders damage per death at position 10 for non-support', () => {
    const wrapper = createWrapper()
    const items = wrapper.findAll('.stat-item')
    expect(items[9].find('.stat-label').text()).toBe('Damage per death')
  })

  it('renders damage per death value correctly', () => {
    // damageDealt=20000, deaths=2 → 10000
    const wrapper = createWrapper()
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Damage per death')
    expect(item.find('.stat-value').text()).toContain('10,000')
  })

  it('applies up trend on damage per death when >= 8000', () => {
    const wrapper = createWrapper({ damageDealt: 20000, deaths: 2 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Damage per death')
    expect(item.classes()).toContain('up')
  })

  it('applies down trend on damage per death when < 3000', () => {
    const wrapper = createWrapper({ damageDealt: 4000, deaths: 2 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Damage per death')
    expect(item.classes()).toContain('down')
  })

  it('uses deaths=1 floor for damage per death when deaths is 0', () => {
    const wrapper = createWrapper({ damageDealt: 20000, deaths: 0 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Damage per death')
    expect(item.find('.stat-value').text()).toContain('20,000')
  })

  it('renders vision per minute at position 10 for support', () => {
    const wrapper = createWrapper({ role: 'UTILITY' })
    const items = wrapper.findAll('.stat-item')
    expect(items[9].find('.stat-label').text()).toBe('Vision per minute')
  })

  it('renders vision per minute value correctly for support', () => {
    // visionScore=25, gameDurationSec=1800 → 25/30 = 0.8
    const wrapper = createWrapper({ role: 'UTILITY', visionScore: 75, gameDurationSec: 1800 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Vision per minute')
    expect(item.find('.stat-value').text()).toBe('2.5')
  })

  it('applies up trend on vision per minute when >= 2.5', () => {
    const wrapper = createWrapper({ role: 'SUPPORT', visionScore: 80, gameDurationSec: 1800 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Vision per minute')
    expect(item.classes()).toContain('up')
  })

  it('applies down trend on vision per minute when < 1.5', () => {
    const wrapper = createWrapper({ role: 'UTILITY', visionScore: 20, gameDurationSec: 1800 })
    const item = wrapper.findAll('.stat-item').find(i => i.find('.stat-label').text() === 'Vision per minute')
    expect(item.classes()).toContain('down')
  })

  it('calculates KDA as sum of kills+assists when deaths is 0', () => {
    const wrapper = createWrapper({ kills: 10, deaths: 0, assists: 5 })
    const kdaItem = wrapper.findAll('.stat-item').find(i =>
      i.find('.stat-label').text() === 'KDA ratio'
    )
    expect(kdaItem.find('.stat-value').text()).toBe('15.00')
  })

  it('calculates KDA correctly with deaths', () => {
    const wrapper = createWrapper({ kills: 6, deaths: 2, assists: 4 })
    // (6 + 4) / 2 = 5.00
    const kdaItem = wrapper.findAll('.stat-item').find(i =>
      i.find('.stat-label').text() === 'KDA ratio'
    )
    expect(kdaItem.find('.stat-value').text()).toBe('5.00')
  })

  describe('No baseline', () => {
    it('shows no comparison text when baseline is null', () => {
      const wrapper = createWrapper()
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA ratio'
      )
      expect(kdaItem.find('.stat-comparison').exists()).toBe(false)
    })

    it('shows no trend arrow when baseline is null', () => {
      const wrapper = createWrapper()
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA ratio'
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
        i.find('.stat-label').text() === 'KDA ratio'
      )
      expect(kdaItem.find('.stat-comparison').exists()).toBe(true)
    })

    it('applies up trend class when value is above baseline', () => {
      const wrapper = createWrapper({ kills: 10, deaths: 2, assists: 4 }, baseline)
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA ratio'
      )
      expect(kdaItem.classes()).toContain('up')
    })

    it('applies down trend class when value is below baseline', () => {
      // KDA of 1.0 vs baseline 3.0 — well below threshold
      const wrapper = createWrapper({ kills: 1, deaths: 3, assists: 2 }, baseline)
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA ratio'
      )
      expect(kdaItem.classes()).toContain('down')
    })

    it('shows no trend class when value is near baseline', () => {
      // KDA of ~3.0 matching the baseline
      const wrapper = createWrapper({ kills: 4, deaths: 2, assists: 2 }, baseline)
      const kdaItem = wrapper.findAll('.stat-item').find(i =>
        i.find('.stat-label').text() === 'KDA ratio'
      )
      expect(kdaItem.classes()).not.toContain('up')
      expect(kdaItem.classes()).not.toContain('down')
    })
  })
})
