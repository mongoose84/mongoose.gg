import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import DragonParticipationChart from '@/components/solo/DragonParticipationChart.vue'
import { dragonParticipationConfig } from '@/utils/chartConfigs.js'

vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('DragonParticipationChart', () => {
  const mockData = [
    { timestamp: '2024-01-01T00:00:00Z', participationRate: 75.0, rollingAverage: 72.5, teamDragons: 3, dragonsParticipated: 2, championName: 'Ahri', gameIndex: 1, role: 'MID' },
    { timestamp: '2024-01-02T00:00:00Z', participationRate: 80.0, rollingAverage: 74.0, teamDragons: 4, dragonsParticipated: 3, championName: 'Zed', gameIndex: 2, role: 'MID' }
  ]

  const mountComponent = (props = {}) => {
    return mount(DragonParticipationChart, {
      props: { data: [], ...props }
    })
  }

  describe('Rendering', () => {
    it('renders chart when data is provided', () => {
      const wrapper = mountComponent({ data: mockData })
      expect(wrapper.find('[data-testid="dragon-participation-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').text()).toContain('No dragon participation data available')
    })
  })

  describe('Config Integration', () => {
    it('always includes 70% target annotation', () => {
      const wrapper = mountComponent({ data: mockData })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      expect(config.dataKey).toBe('rollingAverage')
      expect(config.label).toBe('Dragon Participation')
      const targetAnnotation = config.annotations.find(a => a.value === 70)
      expect(targetAnnotation).toBeTruthy()
      expect(targetAnnotation.label).toBe('Target: 70%')
    })

    it('includes overallAverage annotation when provided', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: 68.5 })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')

      expect(config.annotations).toHaveLength(2)
      const overallAnnotation = config.annotations.find(a => a.value === 68.5)
      expect(overallAnnotation).toBeTruthy()
      expect(overallAnnotation.label).toContain('68.5%')
    })

    it('has only target annotation when overallAverage is null', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: null })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.annotations).toHaveLength(1)
      expect(config.annotations[0].value).toBe(70)
    })

    it('passes trend to color config', () => {
      const wrapper = mountComponent({ data: mockData, trend: 'worsening' })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      expect(config.color).toEqual({ type: 'trend', trend: 'worsening' })
    })

    it('generates config matching dragonParticipationConfig function', () => {
      const wrapper = mountComponent({ data: mockData, overallAverage: 72.0, trend: 'improving' })
      const config = wrapper.findComponent({ name: 'TrendLineChart' }).props('config')
      const expected = dragonParticipationConfig({ overallAverage: 72.0, trend: 'improving' })

      expect(config.dataKey).toBe(expected.dataKey)
      expect(config.label).toBe(expected.label)
      expect(config.yAxis.min).toBe(expected.yAxis.min)
      expect(config.yAxis.max).toBe(expected.yAxis.max)
      expect(typeof config.yAxis.formatter).toBe('function')
      expect(config.annotations).toEqual(expected.annotations)
    })
  })
})
