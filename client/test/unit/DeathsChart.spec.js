/**
 * Unit tests for DeathsChart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Single smooth line chart (rolling average only, per-game data in tooltip)
 * - Line color based on trend (improving/worsening/neutral)
 * - Overall average reference line
 * - Tooltip callbacks (title, label)
 * - Dataset styling with fill, smooth tension, no visible points
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import DeathsChart from '@/components/solo/DeathsChart.vue'

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

describe('DeathsChart', () => {
  const sampleData = [
    {
      matchId: 'NA1_123',
      gameIndex: 1,
      timestamp: '2026-01-01T12:00:00Z',
      deaths: 5,
      rollingAverage: 5.0,
      championName: 'Jinx',
      role: 'ADC',
      gameDurationMinutes: 27.5
    },
    {
      matchId: 'NA1_124',
      gameIndex: 2,
      timestamp: '2026-01-02T12:00:00Z',
      deaths: 3,
      rollingAverage: 4.0,
      championName: 'Caitlyn',
      role: 'ADC',
      gameDurationMinutes: 25.2
    },
    {
      matchId: 'NA1_125',
      gameIndex: 3,
      timestamp: '2026-01-03T12:00:00Z',
      deaths: 4,
      rollingAverage: 4.0,
      championName: 'Jinx',
      role: 'ADC',
      gameDurationMinutes: 30.1
    }
  ]

  const mountComponent = (props = {}) => {
    return mount(DeathsChart, {
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
      expect(wrapper.find('[data-testid="deaths-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-line-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No deaths data available')
      expect(wrapper.text()).toContain('Play some games to see your death trends')
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
      expect(chartData.datasets[0].label).toBe('Deaths')
    })

    it('passes rolling average values to dataset', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].data).toEqual([5.0, 4.0, 4.0])
    })

    it('formats dates as labels', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.labels).toHaveLength(3)
      expect(chartData.labels[0]).toMatch(/Jan \d+/)
    })
  })

  describe('Line color based on trend', () => {
    it('uses green color when trend is improving', () => {
      const wrapper = mountComponent({ trend: 'improving' })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].borderColor).toBe('#22c55e') // Green
      expect(chartData.datasets[0].pointHoverBackgroundColor).toBe('#22c55e') // Green
    })

    it('uses red color when trend is worsening', () => {
      const wrapper = mountComponent({ trend: 'worsening' })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].borderColor).toBe('#ef4444') // Red
      expect(chartData.datasets[0].pointHoverBackgroundColor).toBe('#ef4444') // Red
    })

    it('uses purple color when trend is neutral', () => {
      const wrapper = mountComponent({ trend: 'neutral' })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].borderColor).toBe('#6d28d9') // Purple
      expect(chartData.datasets[0].pointHoverBackgroundColor).toBe('#6d28d9') // Purple
    })

    it('defaults to purple when trend is not provided', () => {
      const wrapper = mount(DeathsChart, {
        props: { data: sampleData }
      })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].borderColor).toBe('#6d28d9') // Purple
    })
  })

  describe('Overall average reference line', () => {
    it('includes annotation when overallAverage is provided', () => {
      const wrapper = mountComponent({ overallAverage: 5.2 })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      expect(options.plugins.annotation).toBeDefined()
      expect(options.plugins.annotation.annotations.overallLine).toBeDefined()
      expect(options.plugins.annotation.annotations.overallLine.yMin).toBe(5.2)
      expect(options.plugins.annotation.annotations.overallLine.yMax).toBe(5.2)
    })

    it('annotation label shows formatted overall average', () => {
      const wrapper = mountComponent({ overallAverage: 5.2 })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      expect(options.plugins.annotation.annotations.overallLine.label.content).toBe('Overall: 5.2')
    })

    it('does not include annotation when overallAverage is null', () => {
      const wrapper = mountComponent({ overallAverage: null })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      expect(options.plugins.annotation).toEqual({})
    })

    it('does not include annotation when overallAverage is not provided', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      expect(options.plugins.annotation).toEqual({})
    })
  })

  describe('Chart configuration', () => {
    it('hides legend (single dataset needs no legend)', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      expect(options.plugins.legend.display).toBe(false)
    })

    it('configures tooltip with proper callbacks', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      // Check that tooltip plugin is configured
      expect(options.plugins.tooltip).toBeDefined()
      expect(options.plugins.tooltip.backgroundColor).toBe('rgba(0, 0, 0, 0.9)')
      expect(options.plugins.tooltip.displayColors).toBe(false)
      
      // No filter needed with single dataset
      expect(options.plugins.tooltip.filter).toBeUndefined()
      
      // Verify callbacks exist
      expect(options.plugins.tooltip.callbacks).toBeDefined()
      expect(options.plugins.tooltip.callbacks.title).toBeTypeOf('function')
      expect(options.plugins.tooltip.callbacks.label).toBeTypeOf('function')
    })

    it('tooltip title callback returns game info', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      const titleCallback = options.plugins.tooltip.callbacks.title
      const mockTooltipItems = [{ dataIndex: 0 }]
      
      const result = titleCallback(mockTooltipItems)
      expect(result).toBe('Game 1 - Jinx')
    })

    it('tooltip label callback returns detailed stats', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      const labelCallback = options.plugins.tooltip.callbacks.label
      const mockContext = { dataIndex: 0 }
      
      const result = labelCallback(mockContext)
      expect(result).toBeInstanceOf(Array)
      expect(result.length).toBeGreaterThan(0)
      
      // Check for expected fields (DeathsChart shows: deaths, rolling avg, role, date)
      const resultString = result.join(' ')
      expect(resultString).toContain('Deaths: 5')
      expect(resultString).toContain('Rolling Avg: 5.0')
      expect(resultString).toContain('Role: ADC')
      expect(resultString).toContain('Date:')
    })



    it('calculates max Y value with padding', () => {
      const highDeathsData = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          deaths: 12,
          rollingAverage: 10.5,
          championName: 'Yasuo',
          role: 'MID',
          gameDurationMinutes: 35.0
        }
      ]
      const wrapper = mountComponent({ data: highDeathsData })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      // Max should be ceiling of 12 * 1.2 = 14.4 -> 15
      expect(options.scales.y.max).toBe(15)
    })
  })

  describe('Tooltip configuration', () => {
    it('handles data without role field', () => {
      const dataWithoutRole = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          deaths: 5,
          rollingAverage: 5.0,
          championName: 'Jinx',
          role: null,
          gameDurationMinutes: 27.5
        }
      ]
      const wrapper = mountComponent({ data: dataWithoutRole })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      
      // Should render without errors
      expect(chart.exists()).toBe(true)
    })

    it('tooltip label callback handles missing role', () => {
      const dataWithoutRole = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          deaths: 5,
          rollingAverage: 5.0,
          championName: 'Jinx',
          role: null,
          gameDurationMinutes: 27.5
        }
      ]
      const wrapper = mountComponent({ data: dataWithoutRole })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      const labelCallback = options.plugins.tooltip.callbacks.label
      const mockContext = { dataIndex: 0 }
      
      const result = labelCallback(mockContext)
      const resultString = result.join(' ')
      
      // Should not include role line when role is null
      expect(resultString).not.toContain('Role:')
    })
  })

  describe('Dataset styling', () => {
    it('styles dataset as smooth filled line with no visible points', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      const dataset = chartData.datasets[0]
      expect(dataset.pointRadius).toBe(0) // No visible points
      expect(dataset.pointHoverRadius).toBe(6) // Hover reveals point
      expect(dataset.tension).toBe(0.3) // Smooth line
      expect(dataset.fill).toBe(true) // Filled area
      expect(dataset.borderWidth).toBe(2) // Thicker line
    })

    it('applies proper fill color with opacity', () => {
      const wrapper = mountComponent({ trend: 'improving' })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      const dataset = chartData.datasets[0]
      expect(dataset.backgroundColor).toBe('#22c55e1A') // 10% opacity fill
      expect(dataset.borderColor).toBe('#22c55e') // Solid line
    })
  })
})
