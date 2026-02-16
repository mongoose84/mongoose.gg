import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import VisionChart from '@/components/solo/VisionChart.vue'
import { visionScoreConfig } from '@/utils/chartConfigs.js'

vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('VisionChart', () => {
  const mockData = [
    { timestamp: '2024-01-01T00:00:00Z', visionScorePerMinute: 1.2, rollingAverage: 1.1, visionScore: 24, gameDurationMinutes: 20, championName: 'Ahri', gameIndex: 1, role: 'MID' },
    { timestamp: '2024-01-02T00:00:00Z', visionScorePerMinute: 1.4, rollingAverage: 1.15, visionScore: 28, gameDurationMinutes: 20, championName: 'Lux', gameIndex: 2, role: 'SUPPORT' }
  ]

  const mountComponent = (props = {}) => {
    return mount(VisionChart, {
      props: { data: [], ...props }
    })
  }

  describe('Rendering', () => {
    it('renders chart when data is provided', () => {
      const wrapper = mountComponent({ data: mockData })
      expect(wrapper.find('[data-testid="vision-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').text()).toContain('No vision score data available')
    })
  })

  describe('Config Integration', () => {
    it('includes roleTarget annotation with default 1.0', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      expect(config.dataKey).toBe('rollingAverage')
      expect(config.label).toBe('Vision Score')
      const targetAnnotation = config.annotations.find(a => a.value === 1.0)
      expect(targetAnnotation).toBeTruthy()
      expect(targetAnnotation.label).toBe('Target: 1.0/min')
    })

    it('uses support target label when roleTarget >= 2.0', () => {
      const wrapper = mountComponent({ data: mockData, roleTarget: 2.0 })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      const targetAnnotation = config.annotations.find(a => a.value === 2.0)
      expect(targetAnnotation).toBeTruthy()
      expect(targetAnnotation.label).toBe('Support Target: 2.0/min')
    })

    it('includes overallAverage annotation when provided', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: 1.15 })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      expect(config.annotations).toHaveLength(2)
      const overallAnnotation = config.annotations.find(a => a.value === 1.15)
      expect(overallAnnotation).toBeTruthy()
      expect(overallAnnotation.label).toContain('1.15')
    })

    it('has only target annotation when overallAverage is null', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: null })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.annotations).toHaveLength(1)
    })

    it('generates config matching visionScoreConfig function', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: 1.2, roleTarget: 2.0, trend: 'improving' })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const expected = visionScoreConfig({ overallAverage: 1.2, roleTarget: 2.0, trend: 'improving' })

      expect(config.dataKey).toBe(expected.dataKey)
      expect(config.label).toBe(expected.label)
      expect(config.yAxis.min).toBe(expected.yAxis.min)
      expect(config.yAxis.suggestedMax).toBe(expected.yAxis.suggestedMax)
      expect(typeof config.yAxis.formatter).toBe('function')
      expect(config.annotations).toEqual(expected.annotations)
    })
  })
})
