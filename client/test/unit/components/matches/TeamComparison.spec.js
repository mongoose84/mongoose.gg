import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import TeamComparison from '@/components/matches/TeamComparison.vue'

vi.mock('@/utils/formatters', () => ({
  formatNumber: (n) => (n != null ? String(n) : '0'),
  formatGoldDiff: (n) => (n >= 0 ? `+${n}` : `${n}`)
}))

describe('TeamComparison.vue', () => {
  const baseMatch = {
    teamKills: 30,
    enemyTeamKills: 25,
    teamTotalDamage: 150000,
    enemyTeamTotalDamage: 120000,
    teamGoldLeadAt15: 1200,
    teamDragons: 3,
    enemyTeamDragons: 1,
    teamBarons: 1,
    enemyTeamBarons: 0,
    teamTowers: 8,
    enemyTeamTowers: 4
  }

  const createWrapper = (matchOverrides = {}) =>
    mount(TeamComparison, { props: { match: { ...baseMatch, ...matchOverrides } } })

  it('renders the team comparison container', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.team-comparison').exists()).toBe(true)
  })

  it('renders section title "Team Summary"', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.section-title').text()).toBe('Team Summary')
  })

  it('renders the comparison grid', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.comparison-grid').exists()).toBe(true)
  })

  describe('Damage bar', () => {
    it('shows damage row when both damage values are positive', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('.damage-row').exists()).toBe(true)
    })

    it('shows damage bar when only team damage is non-zero', () => {
      const wrapper = createWrapper({ teamTotalDamage: 5000, enemyTeamTotalDamage: 0 })
      expect(wrapper.find('.damage-row').exists()).toBe(true)
    })

    it('hides damage row when both damage values are 0', () => {
      const wrapper = createWrapper({ teamTotalDamage: 0, enemyTeamTotalDamage: 0 })
      expect(wrapper.find('.damage-row').exists()).toBe(false)
    })

    it('hides damage row when damage values are null', () => {
      const wrapper = createWrapper({ teamTotalDamage: null, enemyTeamTotalDamage: null })
      expect(wrapper.find('.damage-row').exists()).toBe(false)
    })
  })

  describe('Gold lead at 15', () => {
    it('shows team gold lead when team is positive', () => {
      const wrapper = createWrapper({ teamGoldLeadAt15: 800 })
      const cells = wrapper.findAll('.value-cell')
      expect(cells[0].classes()).toContain('positive')
    })

    it('shows enemy gold lead when team is negative', () => {
      const wrapper = createWrapper({ teamGoldLeadAt15: -800 })
      const cells = wrapper.findAll('.value-cell')
      expect(cells[1].classes()).toContain('positive')
    })

    it('applies empty class to both gold cells when lead is exactly 0', () => {
      const wrapper = createWrapper({ teamGoldLeadAt15: 0 })
      const cells = wrapper.findAll('.value-cell')
      expect(cells[0].classes()).toContain('empty')
      expect(cells[1].classes()).toContain('empty')
    })

    it('applies empty class to both gold cells when gold lead is null', () => {
      const wrapper = createWrapper({ teamGoldLeadAt15: null })
      const cells = wrapper.findAll('.value-cell')
      expect(cells[0].classes()).toContain('empty')
      expect(cells[1].classes()).toContain('empty')
    })
  })

  describe('Objective counts', () => {
    it('renders correct team dragon count', () => {
      const wrapper = createWrapper({ teamDragons: 3 })
      const counts = wrapper.findAll('.obj-count')
      expect(counts[0].text()).toBe('3')
    })

    it('renders correct team baron count', () => {
      const wrapper = createWrapper({ teamBarons: 1 })
      const counts = wrapper.findAll('.obj-count')
      expect(counts[1].text()).toBe('1')
    })

    it('renders correct team tower count', () => {
      const wrapper = createWrapper({ teamTowers: 8 })
      const counts = wrapper.findAll('.obj-count')
      expect(counts[2].text()).toBe('8')
    })

    it('renders correct enemy dragon count', () => {
      const wrapper = createWrapper({ enemyTeamDragons: 2 })
      const counts = wrapper.findAll('.obj-count')
      expect(counts[3].text()).toBe('2')
    })

    it('renders correct enemy baron count', () => {
      const wrapper = createWrapper({ enemyTeamBarons: 1 })
      const counts = wrapper.findAll('.obj-count')
      expect(counts[4].text()).toBe('1')
    })

    it('renders correct enemy tower count', () => {
      const wrapper = createWrapper({ enemyTeamTowers: 4 })
      const counts = wrapper.findAll('.obj-count')
      expect(counts[5].text()).toBe('4')
    })
  })
})
