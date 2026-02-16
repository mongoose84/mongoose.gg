import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import CsPerMinuteChart from '@/components/solo/CsPerMinuteChart.vue'
import { csPerMinuteConfig } from '@/utils/chartConfigs.js'

vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('CsPerMinuteChart', () => {
  const mockData = [
    { timestamp: '2024-01-01T00:00:00Z', csPerMinute: 7.2, totalCs: 216, gameDurationMinutes: 30, championName: 'Ahri', gameIndex: 1, role: 'MID' },
    { timestamp: '2024-01-02T00:00:00Z', csPerMinute: 6.8, totalCs: 204, gameDurationMinutes: 30, championName: 'Zed', gameIndex: 2, role: 'MID' }
  ]

  const mountComponent = (props = {}) => {
    return mount(CsPerMinuteChart, {
      props: { data: [], ...props }
    })
  }

  describe('Rendering', () => {
    it('renders chart when data is provided', () => {
      const wrapper = mountComponent({ data: mockData })
      expect(wrapper.find('[data-testid="cs-per-minute-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').text()).toContain('No CS per minute data available')
    })
  })

  describe('Config Integration', () => {
    it('passes correct base config', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      expect(config.dataKey).toBe('csPerMinute')
      expect(config.label).toBe('CS/min')
    })

    it('includes roleTarget annotation when provided', () => {
      const wrapper = mountComponent({ data: mockData, roleTarget: 7.0 })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      expect(config.annotations).toHaveLength(1)
      expect(config.annotations[0].value).toBe(7.0)
      expect(config.annotations[0].label).toContain('7.0 CS/min')
    })

    it('has no annotations when roleTarget is null', () => {
      const wrapper = mountComponent({ data: mockData, roleTarget: null })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.annotations).toHaveLength(0)
    })

    it('generates config matching csPerMinuteConfig function', () => {
      const wrapper = mountComponent({ data: mockData, roleTarget: 6.5 })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const expected = csPerMinuteConfig({ roleTarget: 6.5 })

      expect(config.dataKey).toBe(expected.dataKey)
      expect(config.label).toBe(expected.label)
      expect(config.yAxis.min).toBe(expected.yAxis.min)
      expect(typeof config.yAxis.formatter).toBe('function')
      expect(config.annotations).toEqual(expected.annotations)
    })
  })
})
