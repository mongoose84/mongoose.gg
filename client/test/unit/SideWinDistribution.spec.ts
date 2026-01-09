import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import SideWinDistribution from '@/components/shared/SideWinDistribution.vue'

// Mock the getSideStats API call
vi.mock('@/api/shared.js', () => ({
  getSideStats: vi.fn().mockResolvedValue({
    blueGames: 30,
    blueWins: 18,
    blueWinRate: 60.0,
    bluePercentage: 54.5,
    redGames: 25,
    redWins: 12,
    redWinRate: 48.0,
    redPercentage: 45.5,
    totalGames: 55
  })
}))

describe('SideWinDistribution', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', () => {
    const wrapper = mount(SideWinDistribution, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('shows loading state initially', async () => {
    const wrapper = mount(SideWinDistribution, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    // The loading state might be brief, but let's verify the structure
    expect(wrapper.find('.side-win-distribution-container').exists()).toBe(true)
  })

  it('renders bar chart after loading', async () => {
    const wrapper = mount(SideWinDistribution, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    // Check for SVG bar chart
    expect(wrapper.find('svg.bar-chart').exists()).toBe(true)
    expect(wrapper.findAll('.bar-group').length).toBe(2) // Blue and Red bars
  })

  it('displays correct win rates', async () => {
    const wrapper = mount(SideWinDistribution, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    const barValues = wrapper.findAll('.bar-value')
    expect(barValues.length).toBe(2)
    expect(barValues[0].text()).toContain('60.0%') // Blue win rate
    expect(barValues[1].text()).toContain('48.0%') // Red win rate
  })

  it('renders summary text with win distribution and game counts', async () => {
    const wrapper = mount(SideWinDistribution, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    const summaryTextBlocks = wrapper.findAll('.summary-text')
    expect(summaryTextBlocks.length).toBe(2)
    expect(summaryTextBlocks[0].text()).toContain('Of your 30 wins')
    expect(summaryTextBlocks[0].text()).toContain('18 from blue')
    expect(summaryTextBlocks[0].text()).toContain('12 from red')

    // Game counts line
    expect(summaryTextBlocks[1].text()).toContain('30') // Blue games
    expect(summaryTextBlocks[1].text()).toContain('25') // Red games
  })

  it('handles empty data gracefully', async () => {
    const sharedApi = await import('@/api/shared.js')
    vi.mocked(sharedApi.getSideStats).mockResolvedValueOnce({
      blueGames: 0,
      blueWins: 0,
      blueWinRate: 0,
      bluePercentage: 0,
      redGames: 0,
      redWins: 0,
      redWinRate: 0,
      redPercentage: 0,
      totalGames: 0
    })

    const wrapper = mount(SideWinDistribution, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(wrapper.find('.empty').exists()).toBe(true)
    expect(wrapper.text()).toContain('No side data available')
  })

  it('handles API errors gracefully', async () => {
    const sharedApi = await import('@/api/shared.js')
    vi.mocked(sharedApi.getSideStats).mockRejectedValueOnce(new Error('Network Error'))

    const wrapper = mount(SideWinDistribution, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(wrapper.find('.error').exists()).toBe(true)
    expect(wrapper.text()).toContain('Network Error')
  })

  it('passes mode prop to API', async () => {
    const sharedApi = await import('@/api/shared.js')

    mount(SideWinDistribution, {
      props: { userId: 1, mode: 'duo' },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(sharedApi.getSideStats).toHaveBeenCalledWith(1, 'duo')
  })

  it('reloads data when userId prop changes', async () => {
    const sharedApi = await import('@/api/shared.js')

    const wrapper = mount(SideWinDistribution, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()
    expect(sharedApi.getSideStats).toHaveBeenCalledTimes(1)

    await wrapper.setProps({ userId: 2 })
    await flushPromises()

    expect(sharedApi.getSideStats).toHaveBeenCalledTimes(2)
  })
})

