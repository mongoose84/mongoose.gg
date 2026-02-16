import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import WinrateChart from '@/components/solo/WinrateChart.vue'
import { winrateConfig } from '@/utils/chartConfigs.js'

vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('WinrateChart', () => {
  const mockData = [
    { timestamp: '2024-01-01T00:00:00Z', winRate: 52.5, wins: 21, losses: 19, gameIndex: 1, isWin: true },
    { timestamp: '2024-01-02T00:00:00Z', winRate: 53.0, wins: 22, losses: 19, gameIndex: 2, isWin: true }
  ]

  const mountComponent = (props = {}) => {
    return mount(WinrateChart, {
      props: { data: [], ...props }
    })
  }

  describe('Rendering', () => {
    it('renders chart when data is provided', () => {
      const wrapper = mountComponent({ data: mockData })
      expect(wrapper.find('[data-testid="winrate-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').text()).toContain('No winrate data available')
    })
  })

  describe('Config Integration', () => {
    it('passes correct config with overallWinRate annotation', () => {
      const wrapper = mountComponent({ data: mockData, overallWinRate: 55.3 })
      const trendLineChart = wrapper.findComponent({ name: 'TrendLineChart' })

      const config = trendLineChart.props('config')
      expect(config.dataKey).toBe('winRate')
      expect(config.label).toBe('Winrate %')
      expect(config.annotations).toHaveLength(1)
      expect(config.annotations[0].value).toBe(55.3)
      expect(config.annotations[0].label).toContain('55.3%')
    })

    it('has no annotations when overallWinRate is null', () => {
      const wrapper = mountComponent({ data: mockData, overallWinRate: null })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.annotations).toHaveLength(0)
    })

    it('generates config matching winrateConfig function', () => {
      const wrapper = mountComponent({ data: mockData, overallWinRate: 50 })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const expected = winrateConfig({ overallWinRate: 50 })

      expect(config.dataKey).toBe(expected.dataKey)
      expect(config.label).toBe(expected.label)
      expect(config.yAxis.min).toBe(expected.yAxis.min)
      expect(config.yAxis.max).toBe(expected.yAxis.max)
      expect(typeof config.yAxis.formatter).toBe('function')
    })
  })
})
