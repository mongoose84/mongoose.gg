import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import PerformanceCharts from '@/components/PerformanceCharts.vue'

// Mock performance data - must be defined inline to avoid hoisting issues
vi.mock('@/assets/getPerformance.js', () => ({
  default: vi.fn().mockResolvedValue({
    gamers: [
      {
        gamerName: 'Player1#EUW',
        dataPoints: [
          {
            gameEndTimestamp: '2024-01-01T10:00:00Z',
            win: true,
            goldPerMin: 350,
            csPerMin: 6.5
          },
          {
            gameEndTimestamp: '2024-01-02T10:00:00Z',
            win: false,
            goldPerMin: 320,
            csPerMin: 5.8
          },
          {
            gameEndTimestamp: '2024-01-03T10:00:00Z',
            win: true,
            goldPerMin: 380,
            csPerMin: 7.2
          },
          {
            gameEndTimestamp: '2024-01-04T10:00:00Z',
            win: true,
            goldPerMin: 360,
            csPerMin: 6.8
          },
          {
            gameEndTimestamp: '2024-01-05T10:00:00Z',
            win: false,
            goldPerMin: 310,
            csPerMin: 5.5
          },
          {
            gameEndTimestamp: '2024-01-06T10:00:00Z',
            win: true,
            goldPerMin: 370,
            csPerMin: 7.0
          },
          {
            gameEndTimestamp: '2024-01-07T10:00:00Z',
            win: true,
            goldPerMin: 390,
            csPerMin: 7.5
          },
          {
            gameEndTimestamp: '2024-01-08T10:00:00Z',
            win: false,
            goldPerMin: 330,
            csPerMin: 6.0
          },
          {
            gameEndTimestamp: '2024-01-09T10:00:00Z',
            win: true,
            goldPerMin: 360,
            csPerMin: 6.8
          },
          {
            gameEndTimestamp: '2024-01-10T10:00:00Z',
            win: true,
            goldPerMin: 375,
            csPerMin: 7.1
          }
        ]
      },
      {
        gamerName: 'Player1#EUNE',
        dataPoints: [
          {
            gameEndTimestamp: '2024-01-01T11:00:00Z',
            win: false,
            goldPerMin: 300,
            csPerMin: 5.0
          },
          {
            gameEndTimestamp: '2024-01-02T11:00:00Z',
            win: true,
            goldPerMin: 340,
            csPerMin: 6.2
          },
          {
            gameEndTimestamp: '2024-01-03T11:00:00Z',
            win: false,
            goldPerMin: 290,
            csPerMin: 4.8
          },
          {
            gameEndTimestamp: '2024-01-04T11:00:00Z',
            win: true,
            goldPerMin: 350,
            csPerMin: 6.5
          },
          {
            gameEndTimestamp: '2024-01-05T11:00:00Z',
            win: false,
            goldPerMin: 280,
            csPerMin: 4.5
          },
          {
            gameEndTimestamp: '2024-01-06T11:00:00Z',
            win: true,
            goldPerMin: 360,
            csPerMin: 6.8
          },
          {
            gameEndTimestamp: '2024-01-07T11:00:00Z',
            win: false,
            goldPerMin: 310,
            csPerMin: 5.5
          },
          {
            gameEndTimestamp: '2024-01-08T11:00:00Z',
            win: true,
            goldPerMin: 345,
            csPerMin: 6.3
          },
          {
            gameEndTimestamp: '2024-01-09T11:00:00Z',
            win: false,
            goldPerMin: 295,
            csPerMin: 5.0
          },
          {
            gameEndTimestamp: '2024-01-10T11:00:00Z',
            win: true,
            goldPerMin: 355,
            csPerMin: 6.6
          }
        ]
      }
    ]
  })
}))

describe('PerformanceCharts', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', () => {
    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('renders header with title and period toggle buttons', async () => {
    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const header = wrapper.find('.charts-header')
    expect(header.exists()).toBe(true)
    expect(header.text()).toContain('Performance Over Time')

    // Should have 4 period buttons
    const buttons = wrapper.findAll('.limit-btn')
    expect(buttons.length).toBe(4)
    expect(buttons[0].text()).toBe('20 Games')
    expect(buttons[1].text()).toBe('50 Games')
    expect(buttons[2].text()).toBe('100 Games')
    expect(buttons[3].text()).toBe('All')

    // Default should be 50 Games
    expect(buttons[1].classes()).toContain('active')
  })

  it('renders all three chart cards after loading', async () => {
    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    const chartCards = wrapper.findAll('.chart-card')
    expect(chartCards.length).toBe(3) // Win-rate, Gold/min, CS/min
  })

  it('renders win-rate chart with correct elements', async () => {
    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const svgs = wrapper.findAll('svg.line-chart')
    expect(svgs.length).toBe(3)

    const winrateChart = svgs[0]

    // Should have grid lines
    const gridLines = winrateChart.findAll('.grid line')
    expect(gridLines.length).toBe(5) // 0%, 25%, 50%, 75%, 100%

    // Should have Y-axis labels
    const yLabels = winrateChart.findAll('.y-labels text')
    expect(yLabels.length).toBe(5)

    // Should have polylines for each gamer
    const polylines = winrateChart.findAll('polyline')
    expect(polylines.length).toBe(2) // One for each gamer
  })

  it('renders gold/min chart with correct elements', async () => {
    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const svgs = wrapper.findAll('svg.line-chart')
    const goldChart = svgs[1]

    // Should have grid lines
    const gridLines = goldChart.findAll('.grid line')
    expect(gridLines.length).toBeGreaterThan(0)

    // Should have polylines for each gamer
    const polylines = goldChart.findAll('polyline')
    expect(polylines.length).toBe(2)
  })

  it('renders cs/min chart with correct elements', async () => {
    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const svgs = wrapper.findAll('svg.line-chart')
    const csChart = svgs[2]

    // Should have grid lines
    const gridLines = csChart.findAll('.grid line')
    expect(gridLines.length).toBeGreaterThan(0)

    // Should have polylines for each gamer
    const polylines = csChart.findAll('polyline')
    expect(polylines.length).toBe(2)
  })

  it('renders legends for all charts', async () => {
    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const svgs = wrapper.findAll('svg.line-chart')

    svgs.forEach(svg => {
      const legend = svg.find('.legend')
      expect(legend.exists()).toBe(true)

      const legendTexts = svg.findAll('.legend-text')
      expect(legendTexts.length).toBe(2)
      expect(legendTexts[0].text()).toBe('Player1#EUW')
      expect(legendTexts[1].text()).toBe('Player1#EUNE')
    })
  })

  it('changes period when button is clicked', async () => {
    const getPerformance = await import('@/assets/getPerformance.js')

    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    // Initial call with default period '50'
    expect(getPerformance.default).toHaveBeenCalledWith(1, '50')

    // Click on '20 Games' button
    const buttons = wrapper.findAll('.limit-btn')
    await buttons[0].trigger('click')
    await flushPromises()

    // Should call API with new period
    expect(getPerformance.default).toHaveBeenCalledWith(1, '20')

    // Button should be active
    expect(buttons[0].classes()).toContain('active')
  })

  it('handles empty data gracefully', async () => {
    const getPerformance = await import('@/assets/getPerformance.js')
    vi.mocked(getPerformance.default).mockResolvedValueOnce({ gamers: [] })

    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('No performance data available')
  })

  it('handles API errors gracefully', async () => {
    const getPerformance = await import('@/assets/getPerformance.js')
    vi.mocked(getPerformance.default).mockRejectedValueOnce(new Error('Network Error'))

    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('Network Error')
  })

  it('reloads data when userId prop changes', async () => {
    const getPerformance = await import('@/assets/getPerformance.js')

    const wrapper = mount(PerformanceCharts, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()
    expect(getPerformance.default).toHaveBeenCalledTimes(1)

    // Change userId
    await wrapper.setProps({ userId: 2 })
    await flushPromises()

    expect(getPerformance.default).toHaveBeenCalledTimes(2)
    expect(getPerformance.default).toHaveBeenCalledWith(2, '50')
  })
})

