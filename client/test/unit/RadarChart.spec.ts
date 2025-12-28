import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import RadarChart from '@/components/RadarChart.vue'

// Mock the getComparison API call - must be defined inline to avoid hoisting issues
vi.mock('@/assets/getComparison.js', () => ({
  default: vi.fn().mockResolvedValue({
    winrate: [
      { value: 0.55, gamerName: 'Player1#EUW' },
      { value: 0.48, gamerName: 'Player1#EUNE' }
    ],
    kda: [
      { value: 3.2, gamerName: 'Player1#EUW' },
      { value: 2.8, gamerName: 'Player1#EUNE' }
    ],
    csPrMin: [
      { value: 5.5, gamerName: 'Player1#EUW' },
      { value: 4.8, gamerName: 'Player1#EUNE' }
    ],
    goldPrMin: [
      { value: 350, gamerName: 'Player1#EUW' },
      { value: 320, gamerName: 'Player1#EUNE' }
    ],
    gamesPlayed: [
      { value: 50, gamerName: 'Player1#EUW' },
      { value: 45, gamerName: 'Player1#EUNE' }
    ],
    avgKills: [
      { value: 6.5, gamerName: 'Player1#EUW' },
      { value: 5.2, gamerName: 'Player1#EUNE' }
    ],
    avgDeaths: [
      { value: 5.5, gamerName: 'Player1#EUW' },
      { value: 7.8, gamerName: 'Player1#EUNE' }
    ],
    avgAssists: [
      { value: 8.2, gamerName: 'Player1#EUW' },
      { value: 6.5, gamerName: 'Player1#EUNE' }
    ],
    avgTimeDeadSeconds: [
      { value: 120, gamerName: 'Player1#EUW' },
      { value: 180, gamerName: 'Player1#EUNE' }
    ]
  })
}))

describe('RadarChart', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('renders radar chart with data after loading', async () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    // Should render the SVG
    const svg = wrapper.find('svg.radar-svg')
    expect(svg.exists()).toBe(true)

    // Should have grid circles
    const gridCircles = wrapper.findAll('.grid-circles circle')
    expect(gridCircles.length).toBe(5) // 5 grid levels

    // Should have 6 axes (one for each metric)
    const axes = wrapper.findAll('.axes line')
    expect(axes.length).toBe(6)
  })

  it('renders axis labels for all 6 metrics', async () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const labels = wrapper.findAll('.axis-label')
    expect(labels.length).toBe(6)

    const labelTexts = labels.map(l => l.text())
    expect(labelTexts).toContain('Kills')
    expect(labelTexts).toContain('Deaths')
    expect(labelTexts).toContain('Assists')
    expect(labelTexts).toContain('CS/min')
    expect(labelTexts).toContain('Gold/min')
    expect(labelTexts).toContain('Time Dead')
  })

  it('renders data polygons for each gamer', async () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    // Should have 2 polygons (one for each gamer/server)
    const polygons = wrapper.findAll('.data-polygons polygon')
    expect(polygons.length).toBe(2)
  })

  it('renders legend with gamer names', async () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const legendTexts = wrapper.findAll('.legend-text')
    expect(legendTexts.length).toBe(2)
    expect(legendTexts[0].text()).toBe('Player1#EUW')
    expect(legendTexts[1].text()).toBe('Player1#EUNE')
  })

  it('renders suggestions panel with title', async () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const suggestionsPanel = wrapper.find('.suggestions-panel')
    expect(suggestionsPanel.exists()).toBe(true)

    const title = wrapper.find('.suggestions-title')
    expect(title.text()).toBe('Top Priority')
  })

  it('renders exactly one suggestion based on priority', async () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const suggestions = wrapper.findAll('.suggestion-item')
    expect(suggestions.length).toBe(1) // Only one suggestion
  })

  it('suggestion includes metric, text, and target', async () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const suggestion = wrapper.find('.suggestion-item')
    expect(suggestion.exists()).toBe(true)

    // Should have icon
    const icon = suggestion.find('.suggestion-icon')
    expect(icon.exists()).toBe(true)

    // Should have content with header, text, and target
    const header = suggestion.find('.suggestion-header')
    expect(header.exists()).toBe(true)

    const text = suggestion.find('.suggestion-text')
    expect(text.exists()).toBe(true)

    const target = suggestion.find('.suggestion-target')
    expect(target.exists()).toBe(true)
    expect(target.text()).toContain('Target:')
  })

  it('prioritizes deaths suggestion when deaths are high', async () => {
    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    // Player1#EUNE has 7.8 deaths, which should be the top priority
    const suggestionHeader = wrapper.find('.suggestion-header strong')
    expect(suggestionHeader.text()).toBe('Deaths')
  })

  it('handles empty data gracefully', async () => {
    const getComparison = await import('@/assets/getComparison.js')
    vi.mocked(getComparison.default).mockResolvedValueOnce({
      winrate: [],
      kda: [],
      csPrMin: [],
      goldPrMin: [],
      gamesPlayed: [],
      avgKills: [],
      avgDeaths: [],
      avgAssists: [],
      avgTimeDeadSeconds: []
    })

    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('No radar data available')
  })

  it('handles API errors gracefully', async () => {
    const getComparison = await import('@/assets/getComparison.js')
    vi.mocked(getComparison.default).mockRejectedValueOnce(new Error('API Error'))

    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('API Error')
  })

  it('reloads data when userId prop changes', async () => {
    const getComparison = await import('@/assets/getComparison.js')

    const wrapper = mount(RadarChart, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()
    expect(getComparison.default).toHaveBeenCalledTimes(1)

    // Change userId
    await wrapper.setProps({ userId: 2 })
    await flushPromises()

    expect(getComparison.default).toHaveBeenCalledTimes(2)
  })
})

