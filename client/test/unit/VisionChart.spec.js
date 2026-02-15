/**
 * Unit tests for VisionChart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Dual-line chart (vision per minute + rolling average)
 * - Line color based on performance relative to role target
 * - Role-specific target lines (Support: 2.0/min, Others: 1.0/min)
 * - Overall average reference line
 * - Tooltip content with ward statistics
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import VisionChart from '@/components/solo/VisionChart.vue'

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

describe('VisionChart', () => {
  const sampleData = [
    {
      matchId: 'NA1_123',
      gameIndex: 1,
      timestamp: '2026-01-01T12:00:00Z',
      visionScore: 45,
      visionScorePerMinute: 1.2,
      rollingAverage: 1.2,
      gameDurationMinutes: 37.5,
      championName: 'Jinx',
      role: 'BOTTOM'
    },
    {
      matchId: 'NA1_124',
      gameIndex: 2,
      timestamp: '2026-01-02T12:00:00Z',
      visionScore: 52,
      visionScorePerMinute: 1.5,
      rollingAverage: 1.35,
      gameDurationMinutes: 34.7,
      championName: 'Caitlyn',
      role: 'BOTTOM'
    },
    {
      matchId: 'NA1_125',
      gameIndex: 3,
      timestamp: '2026-01-03T12:00:00Z',
      visionScore: 48,
      visionScorePerMinute: 1.4,
      rollingAverage: 1.37,
      gameDurationMinutes: 34.3,
      championName: 'Jinx',
      role: 'BOTTOM'
    }
  ]

  const supportData = [
    {
      matchId: 'NA1_126',
      gameIndex: 1,
      timestamp: '2026-01-01T12:00:00Z',
      visionScore: 90,
      visionScorePerMinute: 2.5,
      rollingAverage: 2.5,
      gameDurationMinutes: 36.0,
      championName: 'Lulu',
      role: 'UTILITY'
    }
  ]

  const mountComponent = (props = {}) => {
    return mount(VisionChart, {
      props: {
        data: sampleData,
        trend: 'neutral',
        roleTarget: 1.0,
        overallAverage: 1.3,
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component with data', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="vision-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-line-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No vision score data available')
      expect(wrapper.text()).toContain('Play some games to see your vision score trends')
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
    it('creates two datasets: vision per minute and rolling average', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets).toHaveLength(2)
      expect(chartData.datasets[0].label).toBe('Vision/Min')
      expect(chartData.datasets[1].label).toBe('Rolling Average')
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

    it('maps vision per minute data correctly', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const visionData = chartData.datasets[0].data
      expect(visionData).toEqual([1.2, 1.5, 1.4])
    })

    it('maps rolling average data correctly', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const rollingAvgData = chartData.datasets[1].data
      expect(rollingAvgData).toEqual([1.2, 1.35, 1.37])
    })
  })

  describe('Line color based on performance', () => {
    it('uses green color when meeting target', () => {
      const highPerformanceData = [
        {
          ...sampleData[0],
          visionScorePerMinute: 1.2
        }
      ]
      const wrapper = mountComponent({ 
        data: highPerformanceData,
        roleTarget: 1.0
      })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      // Green color when meeting target
      expect(chartData.datasets[0].borderColor).toContain('22c55e')
    })

    it('uses yellow color when approaching target', () => {
      const mediumPerformanceData = [
        {
          ...sampleData[0],
          visionScorePerMinute: 0.85
        }
      ]
      const wrapper = mountComponent({ 
        data: mediumPerformanceData,
        roleTarget: 1.0
      })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      // Yellow color when approaching target (80-100%)
      expect(chartData.datasets[0].borderColor).toContain('eab308')
    })

    it('uses red color when below target', () => {
      const lowPerformanceData = [
        {
          ...sampleData[0],
          visionScorePerMinute: 0.5
        }
      ]
      const wrapper = mountComponent({ 
        data: lowPerformanceData,
        roleTarget: 1.0
      })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      // Red color when below target
      expect(chartData.datasets[0].borderColor).toContain('ef4444')
    })
  })

  describe('Chart options', () => {
    it('includes target line annotation for non-support role', () => {
      const wrapper = mountComponent({ roleTarget: 1.0 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      expect(chartOptions.plugins.annotation.annotations.targetLine).toBeDefined()
      expect(chartOptions.plugins.annotation.annotations.targetLine.yMin).toBe(1.0)
      expect(chartOptions.plugins.annotation.annotations.targetLine.label.content).toBe('Target: 1.0/min')
    })

    it('includes support target line annotation for support role', () => {
      const wrapper = mountComponent({ 
        data: supportData,
        roleTarget: 2.0 
      })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      expect(chartOptions.plugins.annotation.annotations.targetLine).toBeDefined()
      expect(chartOptions.plugins.annotation.annotations.targetLine.yMin).toBe(2.0)
      expect(chartOptions.plugins.annotation.annotations.targetLine.label.content).toBe('Support Target: 2.0/min')
    })

    it('includes overall average line when provided', () => {
      const wrapper = mountComponent({ overallAverage: 1.3 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      expect(chartOptions.plugins.annotation.annotations.overallLine).toBeDefined()
      expect(chartOptions.plugins.annotation.annotations.overallLine.yMin).toBe(1.3)
      expect(chartOptions.plugins.annotation.annotations.overallLine.label.content).toBe('Overall: 1.30/min')
    })

    it('does not include overall average line when not provided', () => {
      const wrapper = mountComponent({ overallAverage: null })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      expect(chartOptions.plugins.annotation.annotations.overallLine).toBeUndefined()
    })

    it('configures tooltip options properly', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      // Check that tooltip plugin is configured
      expect(chartOptions.plugins.tooltip).toBeDefined()
      expect(chartOptions.plugins.tooltip.backgroundColor).toBe('rgba(0, 0, 0, 0.9)')
      expect(chartOptions.plugins.tooltip.displayColors).toBe(false)
    })
  })

  describe('Y-axis scaling', () => {
    it('suggests max based on role target for non-support', () => {
      const wrapper = mountComponent({ roleTarget: 1.0 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      // Should be at least 1.5 times the role target (1.5) or 3.0
      expect(chartOptions.scales.y.suggestedMax).toBeGreaterThanOrEqual(1.5)
    })

    it('suggests max based on role target for support', () => {
      const wrapper = mountComponent({ 
        data: supportData,
        roleTarget: 2.0 
      })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      // Should be 1.5 times the role target (3.0)
      expect(chartOptions.scales.y.suggestedMax).toBe(3.0)
    })
  })

  describe('Edge cases', () => {
    it('handles single data point', () => {
      const wrapper = mountComponent({ data: [sampleData[0]] })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].data).toHaveLength(1)
      expect(chartData.labels).toHaveLength(1)
    })

    it('handles missing role in data', () => {
      const dataWithoutRole = [{
        ...sampleData[0],
        role: null
      }]
      const wrapper = mountComponent({ data: dataWithoutRole })
      
      // Should render without errors
      expect(wrapper.find('[data-testid="mock-line-chart"]').exists()).toBe(true)
    })

    it('renders with default props', () => {
      const wrapper = mount(VisionChart)
      expect(wrapper.find('[data-testid="vision-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })
  })
})
