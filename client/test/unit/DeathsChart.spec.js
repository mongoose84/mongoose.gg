import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import DeathsChart from '@/components/solo/DeathsChart.vue'
import { deathsConfig } from '@/utils/chartConfigs.js'

vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('DeathsChart', () => {
  const mockData = [
    { timestamp: '2024-01-01T00:00:00Z', deaths: 4, rollingAverage: 4.5, championName: 'Ahri', gameIndex: 1, role: 'MID' },
    { timestamp: '2024-01-02T00:00:00Z', deaths: 3, rollingAverage: 4.2, championName: 'Zed', gameIndex: 2, role: 'MID' }
  ]

  const mountComponent = (props = {}) => {
    return mount(DeathsChart, {
      props: { data: [], ...props }
    })
  }

  describe('Rendering', () => {
    it('renders chart when data is provided', () => {
      const wrapper = mountComponent({ data: mockData })
      expect(wrapper.find('[data-testid="deaths-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').text()).toContain('No deaths data available')
    })
  })

  describe('Config Integration', () => {
    it('passes correct config with overallAverage annotation', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: 4.3 })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      expect(config.dataKey).toBe('rollingAverage')
      expect(config.label).toBe('Deaths')
      expect(config.annotations).toHaveLength(1)
      expect(config.annotations[0].value).toBe(4.3)
      expect(config.annotations[0].label).toContain('4.3')
    })

    it('has no annotations when overallAverage is null', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: null })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.annotations).toHaveLength(0)
    })

    it('passes trend to color config', () => {
      const wrapper = mountComponent({ data: mockData, trend: 'improving' })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.color).toEqual({ type: 'trend', trend: 'improving' })
    })

    it('defaults trend to neutral', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.color).toEqual({ type: 'trend', trend: 'neutral' })
    })

    it('generates config matching deathsConfig function', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: 5.0, trend: 'worsening' })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const expected = deathsConfig({ overallAverage: 5.0, trend: 'worsening' })

      expect(config.dataKey).toBe(expected.dataKey)
      expect(config.label).toBe(expected.label)
      expect(config.yAxis.min).toBe(expected.yAxis.min)
      expect(typeof config.yAxis.formatter).toBe('function')
      expect(config.color).toEqual(expected.color)
    })
  })
})
