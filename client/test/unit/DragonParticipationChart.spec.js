/**
 * Unit tests for DragonParticipationChart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Single smooth line chart (rolling average only, per-game data in tooltip)
 * - Line color based on trend (improving/worsening/neutral)
 * - Target line at 70% (research-based win correlation threshold)
 * - No overall average line (removed to reduce clutter)
 * - Tooltip content with per-game participation details
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import DragonParticipationChart from '@/components/solo/DragonParticipationChart.vue'

// Mock Chart.js and vue-chartjs to avoid canvas rendering issues in tests
vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-line-chart" :data-chart-data="JSON.stringify(data)" :data-chart-options="JSON.stringify(options)"></div>'
  }
}))

vi.mock('chart.js', () => ({
  Chart: { register: vi.fn() },
  CategoryScale: {},
  LinearScale: {},
  PointElement: {},
  LineElement: {},
  Title: {},
  Tooltip: {},
  Legend: {},
  Filler: {}
}))

vi.mock('chartjs-plugin-annotation', () => ({
  default: {}
}))

describe('DragonParticipationChart', () => {
  const sampleData = [
    {
      matchId: 'NA1_123',
      gameIndex: 1,
      timestamp: '2026-01-01T12:00:00Z',
      teamDragons: 3,
      dragonsParticipated: 2,
      participationRate: 66.7,
      rollingAverage: 66.7,
      championName: 'Jinx',
      role: 'ADC'
    },
    {
      matchId: 'NA1_124',
      gameIndex: 2,
      timestamp: '2026-01-02T12:00:00Z',
      teamDragons: 2,
      dragonsParticipated: 2,
      participationRate: 100.0,
      rollingAverage: 83.3,
      championName: 'Caitlyn',
      role: 'ADC'
    },
    {
      matchId: 'NA1_125',
      gameIndex: 3,
      timestamp: '2026-01-03T12:00:00Z',
      teamDragons: 4,
      dragonsParticipated: 3,
      participationRate: 75.0,
      rollingAverage: 80.6,
      championName: 'Jinx',
      role: 'ADC'
    }
  ]

  const mountComponent = (props = {}) => {
    return mount(DragonParticipationChart, {
      props: {
        data: sampleData,
        trend: 'neutral',
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component with data', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="dragon-participation-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-line-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No dragon participation data available')
      expect(wrapper.text()).toContain('Play some games to see your dragon participation trends')
    })

    it('shows empty state when data is null', () => {
      const wrapper = mountComponent({ data: null })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })

    it('shows empty state when data is undefined', () => {
      const wrapper = mountComponent({ data: undefined })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })
  })

  describe('Chart data structure', () => {
    it('creates single dataset with rolling average', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets).toHaveLength(1)
      expect(chartData.datasets[0].label).toBe('Dragon Participation')
    })

    it('formats x-axis labels as dates', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.labels).toHaveLength(3)
      expect(chartData.labels[0]).toBe('Jan 1')
      expect(chartData.labels[1]).toBe('Jan 2')
      expect(chartData.labels[2]).toBe('Jan 3')
    })

    it('maps rolling average data correctly', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const rollingAvgData = chartData.datasets[0].data
      expect(rollingAvgData).toEqual([66.7, 83.3, 80.6])
    })
  })

  describe('Trend-based line coloring', () => {
    it('uses green color when trend is improving', () => {
      const wrapper = mountComponent({ trend: 'improving' })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const lineColor = chartData.datasets[0].borderColor
      expect(lineColor).toBe('#22c55e') // Green
    })

    it('uses red color when trend is worsening', () => {
      const wrapper = mountComponent({ trend: 'worsening' })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const lineColor = chartData.datasets[0].borderColor
      expect(lineColor).toBe('#ef4444') // Red
    })

    it('uses purple color when trend is neutral', () => {
      const wrapper = mountComponent({ trend: 'neutral' })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const lineColor = chartData.datasets[0].borderColor
      expect(lineColor).toBe('#6d28d9') // Purple
    })
  })

  describe('Annotation lines', () => {
    it('includes target line at 70% (research-based threshold)', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation).toBeDefined()
      expect(options.plugins.annotation.annotations.targetLine).toBeDefined()
      expect(options.plugins.annotation.annotations.targetLine.yMin).toBe(70)
      expect(options.plugins.annotation.annotations.targetLine.yMax).toBe(70)
      expect(options.plugins.annotation.annotations.targetLine.label.content).toBe('Target: 70%')
    })

    it('does not include overall average line (removed to reduce clutter)', () => {
      const wrapper = mountComponent({ overallAverage: 65.5 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation.annotations.overallLine).toBeUndefined()
    })
  })

  describe('Chart options', () => {
    it('sets Y-axis scale from 0 to 100', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.scales.y.min).toBe(0)
      expect(options.scales.y.max).toBe(100)
    })

    it('enables responsive mode', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.responsive).toBe(true)
      expect(options.maintainAspectRatio).toBe(false)
    })

    it('hides legend (single dataset needs no legend)', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.legend.display).toBe(false)
    })
  })

  describe('Tooltip configuration', () => {
    it('configures tooltip without filter (single dataset)', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.tooltip).toBeDefined()
      expect(options.plugins.tooltip.displayColors).toBe(false)
      // No filter needed with single dataset
    })

    it('configures tooltip with title and label callbacks', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      // Note: Callback functions cannot be tested via JSON.parse
      // as functions don't serialize. The component implements these inline.
      expect(options.plugins.tooltip.callbacks).toBeDefined()
      expect(options.plugins.tooltip.padding).toBe(12)
    })

    it('configures tooltip styling correctly', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.tooltip.backgroundColor).toBe('rgba(0, 0, 0, 0.9)')
      expect(options.plugins.tooltip.titleColor).toBe('#ffffff')
      expect(options.plugins.tooltip.bodyColor).toBe('#ffffff')
      expect(options.plugins.tooltip.borderWidth).toBe(1)
    })

    it('configures Y-axis with percentage formatting', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.scales.y.ticks).toBeDefined()
      expect(options.scales.y.ticks.stepSize).toBe(10)
      // Note: The callback function for percentage formatting cannot be tested via JSON.parse
    })
  })

  describe('Props validation', () => {
    it('accepts data prop as array', () => {
      const wrapper = mountComponent({ data: sampleData })
      expect(wrapper.props('data')).toEqual(sampleData)
    })

    it('accepts overallAverage prop as number', () => {
      const wrapper = mountComponent({ overallAverage: 65.5 })
      expect(wrapper.props('overallAverage')).toBe(65.5)
    })

    it('accepts trend prop as string', () => {
      const wrapper = mountComponent({ trend: 'improving' })
      expect(wrapper.props('trend')).toBe('improving')
    })

    it('defaults data to empty array', () => {
      const wrapper = mount(DragonParticipationChart)
      expect(wrapper.props('data')).toEqual([])
    })

    it('defaults overallAverage to null', () => {
      const wrapper = mount(DragonParticipationChart)
      expect(wrapper.props('overallAverage')).toBeNull()
    })

    it('defaults trend to neutral', () => {
      const wrapper = mount(DragonParticipationChart)
      expect(wrapper.props('trend')).toBe('neutral')
    })
  })
})
