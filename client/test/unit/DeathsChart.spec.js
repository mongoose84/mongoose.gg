/**
 * Unit tests for DeathsChart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Dual-line chart (deaths + rolling average)
 * - Line color based on trend (improving/worsening/neutral)
 * - Overall average reference line
 * - Tooltip filtering and content
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import DeathsChart from '@/components/solo/DeathsChart.vue'

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
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets).toHaveLength(2)
      expect(chartData.datasets[0].label).toBe('Deaths')
      expect(chartData.datasets[1].label).toBe('Rolling Average')
    })

    it('passes correct death values to first dataset', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].data).toEqual([5, 3, 4])
    })

    it('passes correct rolling average values to second dataset', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[1].data).toEqual([5.0, 4.0, 4.0])
    })

    it('formats dates as labels', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.labels).toHaveLength(3)
      expect(chartData.labels[0]).toMatch(/Jan \d+/)
    })
  })

  describe('Line color based on trend', () => {
    it('uses green color when trend is improving', () => {
      const wrapper = mountComponent({ trend: 'improving' })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].pointBackgroundColor).toBe('#22c55e') // Green
      expect(chartData.datasets[1].borderColor).toBe('#22c55e') // Green
    })

    it('uses red color when trend is worsening', () => {
      const wrapper = mountComponent({ trend: 'worsening' })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].pointBackgroundColor).toBe('#ef4444') // Red
      expect(chartData.datasets[1].borderColor).toBe('#ef4444') // Red
    })

    it('uses purple color when trend is neutral', () => {
      const wrapper = mountComponent({ trend: 'neutral' })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].pointBackgroundColor).toBe('#6d28d9') // Purple
      expect(chartData.datasets[1].borderColor).toBe('#6d28d9') // Purple
    })

    it('defaults to purple when trend is not provided', () => {
      const wrapper = mount(DeathsChart, {
        props: { data: sampleData }
      })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].pointBackgroundColor).toBe('#6d28d9') // Purple
    })
  })

  describe('Overall average reference line', () => {
    it('includes annotation when overallAverage is provided', () => {
      const wrapper = mountComponent({ overallAverage: 5.2 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation).toBeDefined()
      expect(options.plugins.annotation.annotations.overallLine).toBeDefined()
      expect(options.plugins.annotation.annotations.overallLine.yMin).toBe(5.2)
      expect(options.plugins.annotation.annotations.overallLine.yMax).toBe(5.2)
    })

    it('annotation label shows formatted overall average', () => {
      const wrapper = mountComponent({ overallAverage: 5.2 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation.annotations.overallLine.label.content).toBe('Overall: 5.2')
    })

    it('does not include annotation when overallAverage is null', () => {
      const wrapper = mountComponent({ overallAverage: null })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation).toEqual({})
    })

    it('does not include annotation when overallAverage is not provided', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation).toEqual({})
    })
  })

  describe('Chart configuration', () => {
    it('displays legend at the top', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.legend.display).toBe(true)
      expect(options.plugins.legend.position).toBe('top')
    })

    it('configures tooltip to filter only first dataset', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.tooltip.filter).toBeDefined()
      
      // Test the filter function
      const filterFn = options.plugins.tooltip.filter
      expect(filterFn({ datasetIndex: 0 })).toBe(true) // Deaths dataset
      expect(filterFn({ datasetIndex: 1 })).toBe(false) // Rolling average dataset
    })

    it('disables display colors in tooltip', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.tooltip.displayColors).toBe(false)
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
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      // Max should be ceiling of 12 * 1.2 = 14.4 -> 15
      expect(options.scales.y.max).toBe(15)
    })
  })

  describe('Tooltip callbacks', () => {
    it('formats tooltip title with game index and champion', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      const titleCallback = options.plugins.tooltip.callbacks.title
      const mockItems = [{ dataIndex: 0 }]
      const title = titleCallback(mockItems)

      expect(title).toBe('Game 1 - Jinx')
    })

    it('formats tooltip label with all relevant data', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      const labelCallback = options.plugins.tooltip.callbacks.label
      const mockContext = { dataIndex: 0 }
      const labels = labelCallback(mockContext)

      expect(labels).toHaveLength(4)
      expect(labels[0]).toBe('Deaths: 5')
      expect(labels[1]).toBe('Rolling Avg: 5.0')
      expect(labels[2]).toBe('Role: ADC')
      expect(labels[3]).toMatch(/Date: Jan \d+, 2026/)
    })

    it('omits role from tooltip when not provided', () => {
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
      const options = JSON.parse(chart.attributes('data-chart-options'))

      const labelCallback = options.plugins.tooltip.callbacks.label
      const mockContext = { dataIndex: 0 }
      const labels = labelCallback(mockContext)

      expect(labels).toHaveLength(3) // No role line
      expect(labels.some(label => label.includes('Role'))).toBe(false)
    })
  })

  describe('Dataset styling', () => {
    it('styles deaths dataset with points visible', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const deathsDataset = chartData.datasets[0]
      expect(deathsDataset.pointRadius).toBe(3)
      expect(deathsDataset.pointHoverRadius).toBe(6)
      expect(deathsDataset.tension).toBe(0) // No smoothing
    })

    it('styles rolling average dataset with no points', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const rollingAvgDataset = chartData.datasets[1]
      expect(rollingAvgDataset.pointRadius).toBe(0)
      expect(rollingAvgDataset.pointHoverRadius).toBe(0)
      expect(rollingAvgDataset.tension).toBe(0.3) // Smooth line
    })

    it('applies opacity to deaths dataset border color', () => {
      const wrapper = mountComponent({ trend: 'improving' })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      const deathsDataset = chartData.datasets[0]
      expect(deathsDataset.borderColor).toBe('#22c55e80') // 50% opacity
    })
  })
})
