import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ImpactStats from '@/components/matches/ImpactStats.vue'

describe('ImpactStats.vue', () => {
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
    gameDurationSec: 1800
  }

  const createWrapper = (matchOverrides = {}) =>
    mount(ImpactStats, { props: { match: { ...baseMatch, ...matchOverrides } } })

  it('renders section title "Personal Impact"', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.section-title').text()).toBe('Personal Impact')
  })

  it('renders the impact stats grid', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.impact-grid').exists()).toBe(true)
  })

  describe('Non-support metrics', () => {
    it('renders 3 impact stats for non-support roles', () => {
      const wrapper = createWrapper({ role: 'TOP' })
      expect(wrapper.findAll('.impact-item')).toHaveLength(3)
    })

    it('renders 3 impact stats for MIDDLE role', () => {
      const wrapper = createWrapper({ role: 'MIDDLE' })
      expect(wrapper.findAll('.impact-item')).toHaveLength(3)
    })

    it('renders 3 impact stats for BOTTOM role', () => {
      const wrapper = createWrapper({ role: 'BOTTOM' })
      expect(wrapper.findAll('.impact-item')).toHaveLength(3)
    })

    it('includes Dmg/Gold efficiency stat for non-support roles', () => {
      const wrapper = createWrapper({ role: 'BOTTOM' })
      const labels = wrapper.findAll('.impact-label').map(l => l.text())
      expect(labels).toContain('Dmg/Gold')
    })

    it('applies positive sentiment when goldDiffAt15 is +500 or more', () => {
      const wrapper = createWrapper({ goldDiffAt15: 600 })
      const goldItem = wrapper.findAll('.impact-item').find(item =>
        item.find('.impact-label').text().includes('Gold')
      )
      expect(goldItem.classes()).toContain('positive')
    })

    it('applies negative sentiment when goldDiffAt15 is -500 or worse', () => {
      const wrapper = createWrapper({ goldDiffAt15: -600 })
      const goldItem = wrapper.findAll('.impact-item').find(item =>
        item.find('.impact-label').text().includes('Gold')
      )
      expect(goldItem.classes()).toContain('negative')
    })

    it('applies neutral sentiment when goldDiffAt15 is between -500 and +500', () => {
      const wrapper = createWrapper({ goldDiffAt15: 200 })
      const goldItem = wrapper.findAll('.impact-item').find(item =>
        item.find('.impact-label').text().includes('Gold')
      )
      expect(goldItem.classes()).toContain('neutral')
    })

    it('shows N/A value when goldDiffAt15 is null', () => {
      const wrapper = createWrapper({ goldDiffAt15: null })
      const goldItem = wrapper.findAll('.impact-item').find(item =>
        item.find('.impact-label').text().includes('Gold')
      )
      expect(goldItem.find('.impact-value').text()).toBe('N/A')
    })

    it('shows formatted positive gold diff value', () => {
      const wrapper = createWrapper({ goldDiffAt15: 800 })
      const goldItem = wrapper.findAll('.impact-item').find(item =>
        item.find('.impact-label').text().includes('Gold')
      )
      expect(goldItem.find('.impact-value').text()).toContain('+')
    })

    it('shows formatted negative gold diff value', () => {
      const wrapper = createWrapper({ goldDiffAt15: -800 })
      const goldItem = wrapper.findAll('.impact-item').find(item =>
        item.find('.impact-label').text().includes('Gold')
      )
      expect(goldItem.find('.impact-value').text()).toContain('-')
    })
  })

  describe('Support metrics', () => {
    const supportMatch = { ...baseMatch, role: 'UTILITY' }

    it('renders 3 impact stats for UTILITY role', () => {
      const wrapper = mount(ImpactStats, { props: { match: supportMatch } })
      expect(wrapper.findAll('.impact-item')).toHaveLength(3)
    })

    it('renders 3 impact stats for SUPPORT role', () => {
      const wrapper = mount(ImpactStats, { props: { match: { ...baseMatch, role: 'SUPPORT' } } })
      expect(wrapper.findAll('.impact-item')).toHaveLength(3)
    })

    it('includes Vision/min stat for support', () => {
      const wrapper = mount(ImpactStats, { props: { match: supportMatch } })
      const labels = wrapper.findAll('.impact-label').map(l => l.text())
      expect(labels).toContain('Vision/min')
    })

    it('includes Kill Part. stat for support', () => {
      const wrapper = mount(ImpactStats, { props: { match: supportMatch } })
      const labels = wrapper.findAll('.impact-label').map(l => l.text())
      expect(labels).toContain('Kill Part.')
    })

    it('shows positive vision sentiment when visionScore/min >= 2.5', () => {
      // 60 vision / 30 min = 2.0 per minute, so use visionScore=90 for gameDurationSec=1800
      const wrapper = mount(ImpactStats, {
        props: { match: { ...supportMatch, visionScore: 90, gameDurationSec: 1800 } }
      })
      const visionItem = wrapper.findAll('.impact-item').find(item =>
        item.find('.impact-label').text() === 'Vision/min'
      )
      expect(visionItem.classes()).toContain('positive')
    })
  })
})
