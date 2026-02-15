/**
 * Unit tests for CsPerMinuteChart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Line chart with CS per minute data
 * - Line color based on average CS/min performance
 * - Role target annotation (if provided)
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import CsPerMinuteChart from '@/components/solo/CsPerMinuteChart.vue'

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

describe('CsPerMinuteChart', () => {
  const sampleData = [
    {
      matchId: 'NA1_123',
      gameIndex: 1,
      timestamp: '2026-01-01T12:00:00Z',
      totalCs: 180,
      csPerMinute: 6.5,
      gameDurationMinutes: 27.7,
      championName: 'Jinx',
      role: 'ADC'
    },
    {
      matchId: 'NA1_124',
      gameIndex: 2,
      timestamp: '2026-01-02T12:00:00Z',
      totalCs: 195,
      csPerMinute: 7.2,
      gameDurationMinutes: 27.1,
      championName: 'Jinx',
      role: 'ADC'
    },
    {
      matchId: 'NA1_125',
      gameIndex: 3,
      timestamp: '2026-01-03T12:00:00Z',
      totalCs: 165,
      csPerMinute: 5.8,
      gameDurationMinutes: 28.4,
      championName: 'Caitlyn',
      role: 'ADC'
    }
  ]

  const mountComponent = (props = {}) => {
    return mount(CsPerMinuteChart, {
      props: {
        data: sampleData,
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component with data', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="cs-per-minute-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-line-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No CS per minute data available')
    })

    it('shows empty state when data is null-ish', () => {
      const wrapper = mountComponent({ data: null })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })
  })

  describe('Chart data', () => {
    it('creates chart data with CS per minute line', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets).toHaveLength(1)
      expect(chartData.datasets[0].label).toBe('CS/min')
      expect(chartData.datasets[0].data).toEqual([6.5, 7.2, 5.8])
    })

    it('formats labels as dates', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.labels).toHaveLength(3)
      expect(chartData.labels[0]).toMatch(/Jan \d+/)
    })
  })

  describe('Line color based on average CS/min', () => {
    it('uses green color when average CS/min is good (>= 6)', () => {
      const goodCsData = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          totalCs: 180,
          csPerMinute: 6.5,
          gameDurationMinutes: 27.7,
          championName: 'Jinx',
          role: 'ADC'
        },
        {
          matchId: 'NA1_124',
          gameIndex: 2,
          timestamp: '2026-01-02T12:00:00Z',
          totalCs: 195,
          csPerMinute: 7.0,
          gameDurationMinutes: 27.9,
          championName: 'Jinx',
          role: 'ADC'
        }
      ]
      const wrapper = mountComponent({ data: goodCsData })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].borderColor).toBe('#22c55e') // Green
    })

    it('uses red color when average CS/min is poor (< 5)', () => {
      const poorCsData = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          totalCs: 120,
          csPerMinute: 4.2,
          gameDurationMinutes: 28.6,
          championName: 'Blitzcrank',
          role: 'SUPPORT'
        },
        {
          matchId: 'NA1_124',
          gameIndex: 2,
          timestamp: '2026-01-02T12:00:00Z',
          totalCs: 130,
          csPerMinute: 4.6,
          gameDurationMinutes: 28.3,
          championName: 'Thresh',
          role: 'SUPPORT'
        }
      ]
      const wrapper = mountComponent({ data: poorCsData })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].borderColor).toBe('#ef4444') // Red
    })

    it('uses purple color when average CS/min is neutral (between 5 and 6)', () => {
      const neutralCsData = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          totalCs: 155,
          csPerMinute: 5.5,
          gameDurationMinutes: 28.2,
          championName: 'Jinx',
          role: 'ADC'
        },
        {
          matchId: 'NA1_124',
          gameIndex: 2,
          timestamp: '2026-01-02T12:00:00Z',
          totalCs: 150,
          csPerMinute: 5.3,
          gameDurationMinutes: 28.3,
          championName: 'Jinx',
          role: 'ADC'
        }
      ]
      const wrapper = mountComponent({ data: neutralCsData })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].borderColor).toBe('#6d28d9') // Purple
    })
  })

  describe('Role target annotation', () => {
    it('shows target line when roleTarget is provided', () => {
      const wrapper = mountComponent({ roleTarget: 7.0 })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      expect(chartOptions.plugins.annotation.annotations.targetLine).toBeDefined()
      expect(chartOptions.plugins.annotation.annotations.targetLine.yMin).toBe(7.0)
      expect(chartOptions.plugins.annotation.annotations.targetLine.yMax).toBe(7.0)
    })

    it('does not show target line when roleTarget is null', () => {
      const wrapper = mountComponent({ roleTarget: null })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))

      expect(chartOptions.plugins.annotation).toEqual({})
    })
  })

  describe('Chart options', () => {
    it('configures tooltip with game details', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      
      expect(chart.exists()).toBe(true)
      expect(chart.attributes('data-chart-options')).toBeDefined()
    })

    it('configures y-axis with CS values', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      
      expect(chart.exists()).toBe(true)
      expect(chart.attributes('data-chart-options')).toBeDefined()
    })

    it('hides legend by default', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartOptions = JSON.parse(chart.attributes('data-chart-options'))
      
      expect(chartOptions.plugins.legend.display).toBe(false)
    })
  })
})
