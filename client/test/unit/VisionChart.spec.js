/**
 * Unit tests for VisionChart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Single smooth line chart (rolling average only, per-game data in tooltip)
 * - Line color based on performance relative to role target
 * - Role-specific target lines (Support: 2.0/min, Others: 1.0/min)
 * - No overall average line (removed to reduce clutter)
 * - Tooltip callbacks with detailed per-game stats
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import VisionChart from '@/components/solo/VisionChart.vue'

// Mock Chart.js and vue-chartjs to avoid canvas rendering issues in tests
vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-line-chart"></div>'
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
    it('creates single dataset with rolling average', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets).toHaveLength(1)
      expect(chartData.datasets[0].label).toBe('Vision Score')
    })

    it('formats x-axis labels as dates', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.labels).toHaveLength(3)
      expect(chartData.labels[0]).toBe('Jan 1')
      expect(chartData.labels[1]).toBe('Jan 2')
      expect(chartData.labels[2]).toBe('Jan 3')
    })

    it('maps rolling average data correctly', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      const rollingAvgData = chartData.datasets[0].data
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
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      // Green color when meeting target
      expect(chartData.datasets[0].borderColor).toBe('#22c55e')
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
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      // Yellow color when approaching target (80-100%)
      expect(chartData.datasets[0].borderColor).toBe('#eab308')
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
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      // Red color when below target
      expect(chartData.datasets[0].borderColor).toBe('#ef4444')
    })
  })

  describe('Chart options', () => {
    it('includes target line annotation for non-support role', () => {
      const wrapper = mountComponent({ roleTarget: 1.0 })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartOptions = lineChart.props('options')

      expect(chartOptions.plugins.annotation.annotations.targetLine).toBeDefined()
      expect(chartOptions.plugins.annotation.annotations.targetLine.yMin).toBe(1.0)
      expect(chartOptions.plugins.annotation.annotations.targetLine.label.content).toBe('Target: 1.0/min')
    })

    it('includes support target line annotation for support role', () => {
      const wrapper = mountComponent({ 
        data: supportData,
        roleTarget: 2.0 
      })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartOptions = lineChart.props('options')

      expect(chartOptions.plugins.annotation.annotations.targetLine).toBeDefined()
      expect(chartOptions.plugins.annotation.annotations.targetLine.yMin).toBe(2.0)
      expect(chartOptions.plugins.annotation.annotations.targetLine.label.content).toBe('Support Target: 2.0/min')
    })

    it('does not include overall average line (removed to reduce clutter)', () => {
      const wrapper = mountComponent({ overallAverage: 1.3 })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartOptions = lineChart.props('options')

      expect(chartOptions.plugins.annotation.annotations.overallLine).toBeUndefined()
    })

    it('configures tooltip without filter (single dataset)', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartOptions = lineChart.props('options')

      // Check that tooltip plugin is configured
      expect(chartOptions.plugins.tooltip).toBeDefined()
      expect(chartOptions.plugins.tooltip.backgroundColor).toBe('rgba(0, 0, 0, 0.9)')
      expect(chartOptions.plugins.tooltip.displayColors).toBe(false)
      
      // No filter needed with single dataset
      expect(chartOptions.plugins.tooltip.filter).toBeUndefined()
      
      // Verify callbacks exist
      expect(chartOptions.plugins.tooltip.callbacks).toBeDefined()
      expect(chartOptions.plugins.tooltip.callbacks.title).toBeTypeOf('function')
      expect(chartOptions.plugins.tooltip.callbacks.label).toBeTypeOf('function')
    })

    it('tooltip title callback returns game info', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartOptions = lineChart.props('options')

      const titleCallback = chartOptions.plugins.tooltip.callbacks.title
      const mockTooltipItems = [{ dataIndex: 0 }]
      
      const result = titleCallback(mockTooltipItems)
      expect(result).toBe('Game 1 - Jinx')
    })

    it('tooltip label callback returns detailed stats', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartOptions = lineChart.props('options')

      const labelCallback = chartOptions.plugins.tooltip.callbacks.label
      const mockContext = { dataIndex: 0 }
      
      const result = labelCallback(mockContext)
      expect(result).toBeInstanceOf(Array)
      expect(result.length).toBeGreaterThan(0)
      
      // Check for expected fields
      const resultString = result.join(' ')
      expect(resultString).toContain('Vision/Min: 1.20')
      expect(resultString).toContain('Rolling Avg: 1.20')
      expect(resultString).toContain('Vision Score: 45')
      expect(resultString).toContain('Game Duration: 37.5 min')
      expect(resultString).toContain('Role: BOTTOM')
      expect(resultString).toContain('Date:')
    })
  })

  describe('Y-axis scaling', () => {
    it('suggests max based on role target for non-support', () => {
      const wrapper = mountComponent({ roleTarget: 1.0 })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartOptions = lineChart.props('options')

      // Should be at least 1.5 times the role target (1.5) or 3.0
      expect(chartOptions.scales.y.suggestedMax).toBeGreaterThanOrEqual(1.5)
    })

    it('suggests max based on role target for support', () => {
      const wrapper = mountComponent({ 
        data: supportData,
        roleTarget: 2.0 
      })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartOptions = lineChart.props('options')

      // Should be 1.5 times the role target (3.0)
      expect(chartOptions.scales.y.suggestedMax).toBe(3.0)
    })
  })

  describe('Edge cases', () => {
    it('handles single data point', () => {
      const wrapper = mountComponent({ data: [sampleData[0]] })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

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
