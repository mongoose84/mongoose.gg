import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import TrendLineChart from '@/components/solo/TrendLineChart.vue'

// Mock Chart.js
vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('TrendLineChart', () => {
  const mockData = [
    { timestamp: '2024-01-01T00:00:00Z', value: 10, gameIndex: 1 },
    { timestamp: '2024-01-02T00:00:00Z', value: 15, gameIndex: 2 },
    { timestamp: '2024-01-03T00:00:00Z', value: 12, gameIndex: 3 }
  ]

  const baseConfig = {
    dataKey: 'value',
    label: 'Test Metric'
  }

  const mountComponent = (props = {}) => {
    return mount(TrendLineChart, {
      props: {
        data: [],
        config: baseConfig,
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders chart when data is provided', () => {
      const wrapper = mountComponent({
        data: mockData
      })

      expect(wrapper.find('[data-testid="mock-chart"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(false)
    })

    it('shows empty state when no data', () => {
      const wrapper = mountComponent({
        data: [],
        emptyText: 'No data',
        emptySubtext: 'Play some games'
      })

      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No data')
      expect(wrapper.text()).toContain('Play some games')
    })

    it('uses custom test ID', () => {
      const wrapper = mountComponent({
        testId: 'custom-chart'
      })

      expect(wrapper.find('[data-testid="custom-chart"]').exists()).toBe(true)
    })
  })

  describe('Data Mapping', () => {
    it('maps data using configured dataKey', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          label: 'Test'
        }
      })

      // Access the Line component's data prop
      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      expect(chartData.datasets[0].data).toEqual([10, 15, 12])
    })

    it('formats labels using default formatter', () => {
      const wrapper = mountComponent({
        data: mockData
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      // Default formatter converts timestamp to short date
      expect(chartData.labels).toHaveLength(3)
      expect(chartData.labels[0]).toMatch(/Jan 1/)
    })

    it('uses custom label formatter', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          labelFormatter: (point) => `Game ${point.gameIndex}`
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      expect(chartData.labels).toEqual(['Game 1', 'Game 2', 'Game 3'])
    })
  })

  describe('Color Configuration', () => {
    it('uses default color when no config provided', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: { dataKey: 'value' }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      expect(chartData.datasets[0].borderColor).toBe('#6d28d9')
    })

    it('uses static color string', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          color: '#ff0000'
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      expect(chartData.datasets[0].borderColor).toBe('#ff0000')
    })

    it('calculates color using function', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          color: (data) => {
            const avg = data.reduce((sum, p) => sum + p.value, 0) / data.length
            return avg > 12 ? '#22c55e' : '#ef4444'
          }
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      // Average is ~12.33, should be green
      expect(chartData.datasets[0].borderColor).toBe('#22c55e')
    })

    it('uses trend-based coloring', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          color: {
            type: 'trend',
            trend: 'improving'
          }
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      expect(chartData.datasets[0].borderColor).toBe('#22c55e')
    })

    it('uses value-based coloring with thresholds', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          color: {
            type: 'value',
            thresholds: { good: 15, bad: 10 }
          }
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      // Last value is 12, between thresholds, should be neutral
      expect(chartData.datasets[0].borderColor).toBe('#6d28d9')
    })
  })

  describe('Annotations', () => {
    it('creates no annotations when config is empty', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          annotations: []
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      expect(options.plugins.annotation.annotations).toEqual({})
    })

    it('creates reference line annotation', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          annotations: [
            {
              value: 50,
              label: 'Target: 50%',
              color: 'rgba(255, 0, 0, 0.5)'
            }
          ]
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      expect(options.plugins.annotation.annotations.line0).toBeDefined()
      expect(options.plugins.annotation.annotations.line0.yMin).toBe(50)
      expect(options.plugins.annotation.annotations.line0.yMax).toBe(50)
      expect(options.plugins.annotation.annotations.line0.borderColor).toBe('rgba(255, 0, 0, 0.5)')
    })

    it('skips annotations with null values', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          annotations: [
            { value: null, label: 'Skipped' },
            { value: 50, label: 'Shown' }
          ]
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      // Only one annotation should be present (line1, since line0 was skipped)
      expect(options.plugins.annotation.annotations.line0).toBeUndefined()
      expect(options.plugins.annotation.annotations.line1).toBeDefined()
    })
  })

  describe('Tooltip Configuration', () => {
    it('uses default tooltip callbacks', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: { dataKey: 'value', label: 'Test' }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      const title = options.plugins.tooltip.callbacks.title([{ dataIndex: 0 }])
      expect(title).toBe('Game 1')

      const label = options.plugins.tooltip.callbacks.label({ parsed: { y: 10 }, dataIndex: 0 })
      expect(label).toContain('Test')
      expect(label).toContain('10.00')
    })

    it('uses custom tooltip callbacks', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          tooltip: {
            title: (point) => `Custom ${point.gameIndex}`,
            label: (point) => `Value: ${point.value}`
          }
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      const title = options.plugins.tooltip.callbacks.title([{ dataIndex: 1 }])
      expect(title).toBe('Custom 2')

      const label = options.plugins.tooltip.callbacks.label({ dataIndex: 1 })
      expect(label).toBe('Value: 15')
    })
  })

  describe('Y-Axis Configuration', () => {
    it('applies Y-axis min/max', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          yAxis: {
            min: 0,
            max: 100
          }
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      expect(options.scales.y.min).toBe(0)
      expect(options.scales.y.max).toBe(100)
    })

    it('applies custom Y-axis formatter', () => {
      const wrapper = mountComponent({
        data: mockData,
        config: {
          dataKey: 'value',
          yAxis: {
            formatter: (value) => `${value}%`
          }
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      const formatted = options.scales.y.ticks.callback(50)
      expect(formatted).toBe('50%')
    })
  })

  describe('Additional Datasets', () => {
    it('renders multiple datasets', () => {
      const dataWithOpponent = mockData.map(d => ({
        ...d,
        opponentValue: d.value + 5
      }))

      const wrapper = mountComponent({
        data: dataWithOpponent,
        config: {
          dataKey: 'value',
          label: 'Player',
          additionalDatasets: [
            {
              label: 'Opponent',
              dataKey: 'opponentValue',
              borderColor: 'rgba(255, 255, 255, 0.4)'
            }
          ]
        }
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      expect(chartData.datasets).toHaveLength(2)
      expect(chartData.datasets[0].label).toBe('Player')
      expect(chartData.datasets[1].label).toBe('Opponent')
      expect(chartData.datasets[1].data).toEqual([15, 20, 17])
    })
  })

  describe('Config Validation', () => {
    it('requires config.dataKey', () => {
      // This should log a validation error but not crash
      const wrapper = mountComponent({
        data: mockData,
        config: {
          // Missing dataKey
          label: 'Test'
        }
      })

      // Component should still render (with empty state or error handling)
      expect(wrapper.exists()).toBe(true)
    })
  })

  describe('Per-account mode', () => {
    const multiAccountData = [
      { timestamp: '2024-01-01T00:00:00Z', value: 10, gameIndex: 1, accountGameName: 'FakerMain' },
      { timestamp: '2024-01-02T00:00:00Z', value: 20, gameIndex: 2, accountGameName: 'FakerSmurf' },
      { timestamp: '2024-01-03T00:00:00Z', value: 15, gameIndex: 3, accountGameName: 'FakerMain' },
      { timestamp: '2024-01-04T00:00:00Z', value: 25, gameIndex: 4, accountGameName: 'FakerSmurf' }
    ]

    const accounts = [
      { gameName: 'FakerMain', color: '#7c3aed' },
      { gameName: 'FakerSmurf', color: '#3b82f6' }
    ]

    it('renders chart in merged mode by default', () => {
      const wrapper = mountComponent({
        data: multiAccountData,
        config: baseConfig,
        chartMode: 'merged'
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      // Merged mode produces a single dataset
      expect(chartData.datasets).toHaveLength(1)
    })

    it('renders per-account datasets when chartMode is per-account and data has accountGameName', () => {
      const wrapper = mountComponent({
        data: multiAccountData,
        config: baseConfig,
        chartMode: 'per-account',
        accounts
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      // Two accounts → two datasets
      expect(chartData.datasets).toHaveLength(2)
      expect(chartData.datasets[0].label).toBe('FakerMain')
      expect(chartData.datasets[1].label).toBe('FakerSmurf')
    })

    it('uses account colors for per-account datasets', () => {
      const wrapper = mountComponent({
        data: multiAccountData,
        config: baseConfig,
        chartMode: 'per-account',
        accounts
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      expect(chartData.datasets[0].borderColor).toBe('#7c3aed')
      expect(chartData.datasets[1].borderColor).toBe('#3b82f6')
    })

    it('falls back to merged mode when accounts list is empty', () => {
      const wrapper = mountComponent({
        data: multiAccountData,
        config: baseConfig,
        chartMode: 'per-account',
        accounts: [] // No accounts provided — isPerAccountMode guard requires accounts.length > 0
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      // No accounts list → per-account mode is disabled → single merged dataset
      expect(chartData.datasets).toHaveLength(1)
    })

    it('shows legend in per-account mode', () => {
      const wrapper = mountComponent({
        data: multiAccountData,
        config: baseConfig,
        chartMode: 'per-account',
        accounts
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      expect(options.plugins.legend.display).toBe(true)
    })

    it('hides legend in merged mode', () => {
      const wrapper = mountComponent({
        data: multiAccountData,
        config: { ...baseConfig, showLegend: false },
        chartMode: 'merged'
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      expect(options.plugins.legend.display).toBe(false)
    })

    it('falls back to merged mode when data has no accountGameName fields', () => {
      const wrapper = mountComponent({
        data: mockData, // no accountGameName
        config: baseConfig,
        chartMode: 'per-account',
        accounts
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const chartData = lineComponent.props('data')

      // Falls back to single dataset
      expect(chartData.datasets).toHaveLength(1)
    })

    it('shows account name in merged mode tooltip footer when accountGameName is present', () => {
      const wrapper = mountComponent({
        data: multiAccountData,
        config: baseConfig,
        chartMode: 'merged'
      })

      const lineComponent = wrapper.findComponent({ name: 'Line' })
      const options = lineComponent.props('options')

      const footer = options.plugins.tooltip.callbacks.footer([{ dataIndex: 0 }])
      expect(footer).toBeDefined()
      expect(footer.some(line => line.includes('FakerMain'))).toBe(true)
    })
  })
})
