import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import DpmChart from '@/components/solo/DpmChart.vue'
import { dpmConfig } from '@/utils/chartConfigs.js'

vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('DpmChart', () => {
  const mockData = [
    {
      timestamp: '2024-01-01T00:00:00Z',
      damagePerMinute: 850.5,
      totalDamageDealt: 25000,
      gameDurationMinutes: 29.4,
      championName: 'Ahri',
      gameIndex: 1,
      role: 'MID'
    },
    {
      timestamp: '2024-01-02T00:00:00Z',
      damagePerMinute: 920.0,
      totalDamageDealt: 28000,
      gameDurationMinutes: 30.4,
      championName: 'Zed',
      gameIndex: 2,
      role: 'MID'
    }
  ]

  const mountComponent = (props = {}) => {
    return mount(DpmChart, {
      props: { data: [], ...props }
    })
  }

  describe('Rendering', () => {
    it('renders chart when data is provided', () => {
      const wrapper = mountComponent({ data: mockData })
      expect(wrapper.find('[data-testid="dpm-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').text()).toContain('No damage per minute data available')
    })

    it('passes chartMode to TrendLineChart', () => {
      const wrapper = mountComponent({ data: mockData, chartMode: 'per-account' })
      expect(wrapper.findComponent({ name: 'TrendLineChart' }).props('chartMode')).toBe('per-account')
    })

    it('passes accounts to TrendLineChart', () => {
      const accounts = [{ gameName: 'Faker#KR1', color: '#7c3aed' }]
      const wrapper = mountComponent({ data: mockData, accounts })
      expect(wrapper.findComponent({ name: 'TrendLineChart' }).props('accounts')).toEqual(accounts)
    })
  })

  describe('Config Integration', () => {
    it('uses damagePerMinute as dataKey', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.dataKey).toBe('damagePerMinute')
    })

    it('includes overallAverage annotation when provided', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: 875.0 })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.annotations).toHaveLength(1)
      expect(config.annotations[0].value).toBe(875.0)
      expect(config.annotations[0].label).toContain('875')
    })

    it('has no annotations when overallAverage is null', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: null })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.annotations).toHaveLength(0)
    })

    it('passes trend to color config', () => {
      const wrapper = mountComponent({ data: mockData, trend: 'worsening' })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.color).toEqual({ type: 'trend', trend: 'worsening' })
    })

    it('defaults to neutral trend', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.color).toEqual({ type: 'trend', trend: 'neutral' })
    })

    it('generates config matching dpmConfig function', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: 900.0, trend: 'improving' })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const expected = dpmConfig({ overallAverage: 900.0, trend: 'improving' })

      expect(config.dataKey).toBe(expected.dataKey)
      expect(config.label).toBe(expected.label)
      expect(config.yAxis.min).toBe(expected.yAxis.min)
      expect(typeof config.yAxis.formatter).toBe('function')
      expect(config.annotations).toEqual(expected.annotations)
    })
  })

  describe('Tooltip', () => {
    it('tooltip title includes game index and champion name', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const title = config.tooltip.title(mockData[0])
      expect(title).toContain('Game 1')
      expect(title).toContain('Ahri')
    })

    it('tooltip label includes DPM value', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const label = config.tooltip.label(mockData[0])
      expect(label.some(l => l.includes('DPM:'))).toBe(true)
    })

    it('tooltip label includes game duration', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const label = config.tooltip.label(mockData[0])
      expect(label.some(l => l.includes('Game Duration:'))).toBe(true)
    })

    it('tooltip label includes formatted date', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const label = config.tooltip.label(mockData[0])
      expect(label.some(l => l.includes('Date:'))).toBe(true)
    })

    it('tooltip label includes role when present', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const label = config.tooltip.label(mockData[0])
      expect(label.some(l => l.includes('Role: MID'))).toBe(true)
    })

    it('tooltip label omits role when null', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const pointWithoutRole = { ...mockData[0], role: null }
      const label = config.tooltip.label(pointWithoutRole)
      expect(label.some(l => l && l.includes('Role:'))).toBe(false)
    })
  })
})
