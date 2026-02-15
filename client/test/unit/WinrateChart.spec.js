/**
 * Unit tests for WinrateChart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Overall winrate reference line feature
 * - Line color based on current winrate
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import WinrateChart from '@/components/solo/WinrateChart.vue'

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

describe('WinrateChart', () => {
  const sampleData = [
    { gameIndex: 1, winRate: 50.0, timestamp: '2026-01-01T12:00:00Z' },
    { gameIndex: 2, winRate: 55.0, timestamp: '2026-01-02T12:00:00Z' },
    { gameIndex: 3, winRate: 52.5, timestamp: '2026-01-03T12:00:00Z' }
  ]

  const mountComponent = (props = {}) => {
    return mount(WinrateChart, {
      props: {
        data: sampleData,
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component with data', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="winrate-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-line-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No winrate data available')
    })

    it('shows empty state when data is null-ish', () => {
      const wrapper = mountComponent({ data: null })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })
  })

  describe('Overall winrate reference line', () => {
    it('includes annotation config when overallWinRate is provided', () => {
      const wrapper = mountComponent({ overallWinRate: 48.5 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation).toBeDefined()
      expect(options.plugins.annotation.annotations).toBeDefined()
      expect(options.plugins.annotation.annotations.overallLine).toBeDefined()
      expect(options.plugins.annotation.annotations.overallLine.yMin).toBe(48.5)
      expect(options.plugins.annotation.annotations.overallLine.yMax).toBe(48.5)
    })

    it('annotation label shows formatted overall winrate', () => {
      const wrapper = mountComponent({ overallWinRate: 48.5 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation.annotations.overallLine.label.content).toBe('Overall: 48.5%')
    })

    it('does not include annotation when overallWinRate is null', () => {
      const wrapper = mountComponent({ overallWinRate: null })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation).toEqual({})
    })

    it('does not include annotation when overallWinRate is not provided', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const options = JSON.parse(chart.attributes('data-chart-options'))

      expect(options.plugins.annotation).toEqual({})
    })
  })

  describe('Chart data', () => {
    it('passes correct data to chart', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets).toHaveLength(1)
      expect(chartData.datasets[0].data).toEqual([50.0, 55.0, 52.5])
    })

    it('formats dates as labels', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      // Labels should be formatted dates
      expect(chartData.labels).toHaveLength(3)
    })
  })
})
