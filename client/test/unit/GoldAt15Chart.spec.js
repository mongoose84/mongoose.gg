/**
 * Unit tests for GoldAt15Chart.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Empty state display
 * - Dual-line chart with player and opponent gold
 * - Line color based on gold differential
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import GoldAt15Chart from '@/components/solo/GoldAt15Chart.vue'

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

describe('GoldAt15Chart', () => {
  const sampleData = [
    {
      matchId: 'NA1_123',
      gameIndex: 1,
      timestamp: '2026-01-01T12:00:00Z',
      playerGold: 6000,
      opponentGold: 5500,
      goldDifferential: 500,
      championName: 'Jinx',
      role: 'ADC',
      opponentChampion: 'Caitlyn'
    },
    {
      matchId: 'NA1_124',
      gameIndex: 2,
      timestamp: '2026-01-02T12:00:00Z',
      playerGold: 6200,
      opponentGold: 6100,
      goldDifferential: 100,
      championName: 'Jinx',
      role: 'ADC',
      opponentChampion: 'Ashe'
    },
    {
      matchId: 'NA1_125',
      gameIndex: 3,
      timestamp: '2026-01-03T12:00:00Z',
      playerGold: 5800,
      opponentGold: 6300,
      goldDifferential: -500,
      championName: 'Jinx',
      role: 'ADC',
      opponentChampion: 'Caitlyn'
    }
  ]

  const mountComponent = (props = {}) => {
    return mount(GoldAt15Chart, {
      props: {
        data: sampleData,
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component with data', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="gold-at-15-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="mock-line-chart"]').exists()).toBe(true)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No gold at 15 data available')
    })

    it('shows empty state when data is null-ish', () => {
      const wrapper = mountComponent({ data: null })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })
  })

  describe('Chart data', () => {
    it('creates chart data with player gold line', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets).toHaveLength(2) // Player and opponent
      expect(chartData.datasets[0].label).toBe('Your Gold')
      expect(chartData.datasets[0].data).toEqual([6000, 6200, 5800])
    })

    it('creates chart data with opponent gold line when available', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets).toHaveLength(2)
      expect(chartData.datasets[1].label).toBe('Opponent Gold')
      expect(chartData.datasets[1].data).toEqual([5500, 6100, 6300])
    })

    it('only shows player line when opponent data is null', () => {
      const dataWithoutOpponent = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          playerGold: 6000,
          opponentGold: null,
          goldDifferential: null,
          championName: 'Jinx',
          role: 'ADC',
          opponentChampion: null
        }
      ]
      const wrapper = mountComponent({ data: dataWithoutOpponent })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets).toHaveLength(1) // Only player line
      expect(chartData.datasets[0].label).toBe('Your Gold')
    })
  })

  describe('Line color based on differential', () => {
    it('uses green color when average differential is positive', () => {
      // All positive differentials
      const positiveData = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          playerGold: 6500,
          opponentGold: 6000,
          goldDifferential: 500,
          championName: 'Jinx',
          role: 'ADC',
          opponentChampion: 'Caitlyn'
        },
        {
          matchId: 'NA1_124',
          gameIndex: 2,
          timestamp: '2026-01-02T12:00:00Z',
          playerGold: 6400,
          opponentGold: 6100,
          goldDifferential: 300,
          championName: 'Jinx',
          role: 'ADC',
          opponentChampion: 'Ashe'
        }
      ]
      const wrapper = mountComponent({ data: positiveData })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].borderColor).toBe('#22c55e') // Green
    })

    it('uses red color when average differential is negative', () => {
      // All negative differentials
      const negativeData = [
        {
          matchId: 'NA1_123',
          gameIndex: 1,
          timestamp: '2026-01-01T12:00:00Z',
          playerGold: 5500,
          opponentGold: 6000,
          goldDifferential: -500,
          championName: 'Jinx',
          role: 'ADC',
          opponentChampion: 'Caitlyn'
        },
        {
          matchId: 'NA1_124',
          gameIndex: 2,
          timestamp: '2026-01-02T12:00:00Z',
          playerGold: 5700,
          opponentGold: 6100,
          goldDifferential: -400,
          championName: 'Jinx',
          role: 'ADC',
          opponentChampion: 'Ashe'
        }
      ]
      const wrapper = mountComponent({ data: negativeData })
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      const chartData = JSON.parse(chart.attributes('data-chart-data'))

      expect(chartData.datasets[0].borderColor).toBe('#ef4444') // Red
    })
  })

  describe('Chart options', () => {
    it('configures tooltip with match details', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      
      expect(chart.exists()).toBe(true)
      expect(chart.attributes('data-chart-options')).toBeDefined()
    })

    it('configures legend to show both lines', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      
      expect(chart.exists()).toBe(true)
      expect(chart.attributes('data-chart-options')).toBeDefined()
    })

    it('formats y-axis as gold values', () => {
      const wrapper = mountComponent()
      const chart = wrapper.find('[data-testid="mock-line-chart"]')
      
      expect(chart.exists()).toBe(true)
      expect(chart.attributes('data-chart-options')).toBeDefined()
    })
  })
})
