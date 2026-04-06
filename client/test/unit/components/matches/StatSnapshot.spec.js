import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import StatSnapshot from '@/components/matches/StatSnapshot.vue'

vi.mock('@/utils/formatters', () => ({
  formatNumber: (n) => (n != null ? n.toLocaleString() : '0')
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

  it('shows a non-zero stat count in the section header', () => {
    const wrapper = createWrapper()
    const count = parseInt(wrapper.find('.stat-count').text())
    expect(count).toBeGreaterThan(0)
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

  it('renders CS/min stat', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('CS/min')
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
