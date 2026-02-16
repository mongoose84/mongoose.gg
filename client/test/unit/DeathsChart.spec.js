/**
 * Unit tests for DeathsChart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Dual-line chart (deaths + rolling average)
 * - Line color based on trend (improving/worsening/neutral)
 * - Overall average reference line
 * - Tooltip callbacks (title, label, filter functions)
 * - Dataset styling and configuration
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
    it('creates two datasets (deaths and rolling average)', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets).toHaveLength(2)
      expect(chartData.datasets[0].label).toBe('Deaths')
      expect(chartData.datasets[1].label).toBe('Rolling Average')
    })

    it('passes correct death values to first dataset', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].data).toEqual([5, 3, 4])
    })

    it('passes correct rolling average values to second dataset', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[1].data).toEqual([5.0, 4.0, 4.0])
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

      expect(chartData.datasets[0].pointBackgroundColor).toBe('#22c55e') // Green
      expect(chartData.datasets[1].borderColor).toBe('#22c55e') // Green
    })

    it('uses red color when trend is worsening', () => {
      const wrapper = mountComponent({ trend: 'worsening' })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].pointBackgroundColor).toBe('#ef4444') // Red
      expect(chartData.datasets[1].borderColor).toBe('#ef4444') // Red
    })

    it('uses purple color when trend is neutral', () => {
      const wrapper = mountComponent({ trend: 'neutral' })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].pointBackgroundColor).toBe('#6d28d9') // Purple
      expect(chartData.datasets[1].borderColor).toBe('#6d28d9') // Purple
    })

    it('defaults to purple when trend is not provided', () => {
      const wrapper = mount(DeathsChart, {
        props: { data: sampleData }
      })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      expect(chartData.datasets[0].pointBackgroundColor).toBe('#6d28d9') // Purple
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
    it('displays legend at the top', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      expect(options.plugins.legend.display).toBe(true)
      expect(options.plugins.legend.position).toBe('top')
    })

    it('configures tooltip with proper callbacks', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      // Check that tooltip plugin is configured
      expect(options.plugins.tooltip).toBeDefined()
      expect(options.plugins.tooltip.backgroundColor).toBe('rgba(0, 0, 0, 0.9)')
      expect(options.plugins.tooltip.displayColors).toBe(false)
      
      // Verify filter function exists and works correctly
      expect(options.plugins.tooltip.filter).toBeTypeOf('function')
      const mockFilterContext = { datasetIndex: 0 }
      expect(options.plugins.tooltip.filter(mockFilterContext)).toBe(true)
      
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

    it('tooltip filter only shows tooltips for deaths dataset', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const options = lineChart.props('options')

      const filterFn = options.plugins.tooltip.filter
      
      // Should return true for dataset index 0 (deaths)
      expect(filterFn({ datasetIndex: 0 })).toBe(true)
      
      // Should return false for other datasets
      expect(filterFn({ datasetIndex: 1 })).toBe(false)
      expect(filterFn({ datasetIndex: 2 })).toBe(false)
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
    it('styles deaths dataset with points visible', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      const deathsDataset = chartData.datasets[0]
      expect(deathsDataset.pointRadius).toBe(3)
      expect(deathsDataset.pointHoverRadius).toBe(6)
      expect(deathsDataset.tension).toBe(0) // No smoothing
    })

    it('styles rolling average dataset with no points', () => {
      const wrapper = mountComponent()
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      const rollingAvgDataset = chartData.datasets[1]
      expect(rollingAvgDataset.pointRadius).toBe(0)
      expect(rollingAvgDataset.pointHoverRadius).toBe(0)
      expect(rollingAvgDataset.tension).toBe(0.3) // Smooth line
    })

    it('applies opacity to deaths dataset border color', () => {
      const wrapper = mountComponent({ trend: 'improving' })
      const lineChart = wrapper.findComponent({ name: 'Line' })
      const chartData = lineChart.props('data')

      const deathsDataset = chartData.datasets[0]
      expect(deathsDataset.borderColor).toBe('#22c55e80') // 50% opacity
    })
  })
})
