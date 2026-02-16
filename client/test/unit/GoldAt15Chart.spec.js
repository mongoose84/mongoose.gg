import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import GoldAt15Chart from '@/components/solo/GoldAt15Chart.vue'
import { goldAt15Config } from '@/utils/chartConfigs.js'

vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('GoldAt15Chart', () => {
  const mockData = [
    { timestamp: '2024-01-01T00:00:00Z', playerGold: 5200, opponentGold: 4800, goldDifferential: 400, championName: 'Ahri', gameIndex: 1, role: 'MID' },
    { timestamp: '2024-01-02T00:00:00Z', playerGold: 4900, opponentGold: 5100, goldDifferential: -200, championName: 'Zed', gameIndex: 2, role: 'MID' }
  ]

  const mountComponent = (props = {}) => {
    return mount(GoldAt15Chart, {
      props: { data: [], ...props }
    })
  }

  describe('Rendering', () => {
    it('renders chart when data is provided', () => {
      const wrapper = mountComponent({ data: mockData })
      expect(wrapper.find('[data-testid="gold-at-15-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').text()).toContain('No gold at 15 data available')
    })
  })

  describe('Config Integration', () => {
    it('passes correct config with additionalDatasets for opponent gold', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      expect(config.dataKey).toBe('playerGold')
      expect(config.label).toBe('Your Gold')
      expect(config.showLegend).toBe(true)
      expect(config.additionalDatasets).toHaveLength(1)
      expect(config.additionalDatasets[0].dataKey).toBe('opponentGold')
      expect(config.additionalDatasets[0].label).toBe('Opponent Gold')
    })

    it('has empty annotations array', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.annotations).toEqual([])
    })

    it('generates config matching goldAt15Config function', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const expected = goldAt15Config()

      expect(config.dataKey).toBe(expected.dataKey)
      expect(config.label).toBe(expected.label)
      expect(config.showLegend).toBe(expected.showLegend)
      expect(typeof config.yAxis.formatter).toBe('function')
      expect(config.additionalDatasets).toEqual(expected.additionalDatasets)
      expect(config.annotations).toEqual(expected.annotations)
    })
  })
})
