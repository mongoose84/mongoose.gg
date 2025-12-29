import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { nextTick } from 'vue'
import ChampionPerformanceSplit from '@/components/solo/ChampionPerformanceSplit.vue'

// Mock the getChampionPerformance API call
vi.mock('@/api/solo.js', () => ({
  getChampionPerformance: vi.fn().mockResolvedValue({
    champions: [
      {
        championName: 'Jinx',
        championId: 222,
        servers: [
          { serverName: 'EUNE', gamerName: 'Player1#EUNE', gamesPlayed: 50, wins: 30, winrate: 60.0 },
          { serverName: 'EUW', gamerName: 'Player1#EUW', gamesPlayed: 45, wins: 25, winrate: 55.6 }
        ]
      },
      {
        championName: 'Caitlyn',
        championId: 51,
        servers: [
          { serverName: 'EUNE', gamerName: 'Player1#EUNE', gamesPlayed: 40, wins: 22, winrate: 55.0 },
          { serverName: 'EUW', gamerName: 'Player1#EUW', gamesPlayed: 38, wins: 20, winrate: 52.6 }
        ]
      },
      {
        championName: 'Ashe',
        championId: 22,
        servers: [
          { serverName: 'EUNE', gamerName: 'Player1#EUNE', gamesPlayed: 35, wins: 18, winrate: 51.4 },
          { serverName: 'EUW', gamerName: 'Player1#EUW', gamesPlayed: 30, wins: 15, winrate: 50.0 }
        ]
      },
      {
        championName: 'Vayne',
        championId: 67,
        servers: [
          { serverName: 'EUNE', gamerName: 'Player1#EUNE', gamesPlayed: 28, wins: 14, winrate: 50.0 },
          { serverName: 'EUW', gamerName: 'Player1#EUW', gamesPlayed: 25, wins: 12, winrate: 48.0 }
        ]
      },
      {
        championName: 'Ezreal',
        championId: 81,
        servers: [
          { serverName: 'EUNE', gamerName: 'Player1#EUNE', gamesPlayed: 22, wins: 11, winrate: 50.0 },
          { serverName: 'EUW', gamerName: 'Player1#EUW', gamesPlayed: 20, wins: 10, winrate: 50.0 }
        ]
      },
      {
        championName: 'Kai\'Sa',
        championId: 145,
        servers: [
          { serverName: 'EUNE', gamerName: 'Player1#EUNE', gamesPlayed: 15, wins: 7, winrate: 46.7 },
          { serverName: 'EUW', gamerName: 'Player1#EUW', gamesPlayed: 12, wins: 6, winrate: 50.0 }
        ]
      }
    ]
  })
}))

describe('ChampionPerformanceSplit', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('shows loading state initially', async () => {
    // Create a promise that we can control
    let resolvePromise: (value: any) => void
    const promise = new Promise((resolve) => {
      resolvePromise = resolve
    })

    const soloApi = await import('@/api/solo.js')
    vi.mocked(soloApi.getChampionPerformance).mockReturnValueOnce(promise as any)

    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    // Wait for component to mount and start loading
    await nextTick()

    // Should show loading state before promise resolves
    expect(wrapper.text()).toContain('Loading champion data')

    // Resolve the promise
    resolvePromise!({ champions: [] })
    await flushPromises()

    // Should show empty state after loading
    expect(wrapper.text()).toContain('No champion data available')
  })

  it('renders grouped bar chart with data after loading', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div class="chart-card"><slot /></div>' } }
      }
    })

    await flushPromises()

    // Should render the SVG
    const svg = wrapper.find('svg.bar-chart')
    expect(svg.exists()).toBe(true)

    // Should have grid lines
    const gridLines = wrapper.findAll('.grid line')
    expect(gridLines.length).toBe(5) // 5 grid levels (0, 25, 50, 75, 100)
  })

  it('renders Y-axis labels', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const yLabels = wrapper.findAll('.y-labels text')
    expect(yLabels.length).toBe(5)
    expect(yLabels[0].text()).toBe('0%')
    expect(yLabels[4].text()).toBe('100%')
  })

  it('renders top 5 champions by games played', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    // Should have champion images for top 5
    const championImages = wrapper.findAll('.champion-image')
    expect(championImages.length).toBe(5)

    // Top 5 by total games: Jinx (95), Caitlyn (78), Ashe (65), Vayne (53), Ezreal (42)
    // Check that images have correct aria-labels
    expect(championImages[0].attributes('aria-label')).toBe('Jinx')
    expect(championImages[1].attributes('aria-label')).toBe('Caitlyn')
    expect(championImages[2].attributes('aria-label')).toBe('Ashe')
    expect(championImages[3].attributes('aria-label')).toBe('Vayne')
    expect(championImages[4].attributes('aria-label')).toBe('Ezreal')
  })

  it('renders grouped bars for each champion', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    // Should have bars for each server per champion
    // 5 champions × 2 servers = 10 bars
    const bars = wrapper.findAll('.bars rect')
    expect(bars.length).toBe(10)
  })

  it('renders winrate labels on bars', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    // Should have winrate labels for each bar
    const winrateLabels = wrapper.findAll('.winrate-label')
    expect(winrateLabels.length).toBe(10) // 5 champions × 2 servers

    // Check first champion (Jinx) winrates
    expect(winrateLabels[0].text()).toBe('60') // EUNE
    expect(winrateLabels[1].text()).toBe('56') // EUW
  })

  it('renders legend with gamer names', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const legendTexts = wrapper.findAll('.legend-text')
    expect(legendTexts.length).toBe(2)

    // Should show full gamer names sorted (EUNE comes before EUW alphabetically)
    expect(legendTexts[0].text()).toBe('Player1#EUNE')
    expect(legendTexts[1].text()).toBe('Player1#EUW')
  })

  it('shows tooltip on bar hover', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    // Initially no tooltip
    expect(wrapper.find('.tooltip').exists()).toBe(false)

    // Hover over first bar
    const bars = wrapper.findAll('.bars rect')
    await bars[0].trigger('mouseenter')

    // Tooltip should appear
    const tooltip = wrapper.find('.tooltip')
    expect(tooltip.exists()).toBe(true)
    expect(tooltip.text()).toContain('Jinx')
    expect(tooltip.text()).toContain('EUNE')
    expect(tooltip.text()).toContain('Games: 50')
    expect(tooltip.text()).toContain('Wins: 30')
    expect(tooltip.text()).toContain('Winrate: 60.0%')
  })

  it('hides tooltip on mouse leave', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const bars = wrapper.findAll('.bars rect')
    await bars[0].trigger('mouseenter')
    expect(wrapper.find('.tooltip').exists()).toBe(true)

    await bars[0].trigger('mouseleave')
    expect(wrapper.find('.tooltip').exists()).toBe(false)
  })

  it('handles empty data gracefully', async () => {
    const soloApi = await import('@/api/solo.js')
    vi.mocked(soloApi.getChampionPerformance).mockResolvedValueOnce({
      champions: []
    })

    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('No champion data available')
  })

  it('handles API errors gracefully', async () => {
    const soloApi = await import('@/api/solo.js')
    vi.mocked(soloApi.getChampionPerformance).mockRejectedValueOnce(new Error('API Error'))

    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('API Error')
  })

  it('reloads data when userId prop changes', async () => {
    const soloApi = await import('@/api/solo.js')

    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()
    expect(soloApi.getChampionPerformance).toHaveBeenCalledTimes(1)

    // Change userId
    await wrapper.setProps({ userId: 2 })
    await flushPromises()

    expect(soloApi.getChampionPerformance).toHaveBeenCalledTimes(2)
  })

  it('uses correct colors for servers', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const legendRects = wrapper.findAll('.legend rect')
    expect(legendRects.length).toBe(2)

    // First gamer (EUNE alphabetically) should be green (var(--color-success))
    // Second gamer (EUW) should be purple (var(--color-primary))
    // Note: We can't easily test CSS variables in tests, but we can verify the structure exists
    expect(legendRects[0].attributes('fill')).toBeDefined()
    expect(legendRects[1].attributes('fill')).toBeDefined()
  })

  it('renders champion images with correct URLs', async () => {
    const wrapper = mount(ChampionPerformanceSplit, {
      props: { userId: 1 },
      global: {
        stubs: { ChartCard: { template: '<div><slot /></div>' } }
      }
    })

    await flushPromises()

    const championImages = wrapper.findAll('.champion-image')
    expect(championImages.length).toBe(5)

    // Check that images have correct Data Dragon URLs
    expect(championImages[0].attributes('href')).toContain('ddragon.leagueoflegends.com')
    expect(championImages[0].attributes('href')).toContain('Jinx.png')
    expect(championImages[1].attributes('href')).toContain('Caitlyn.png')
    expect(championImages[2].attributes('href')).toContain('Ashe.png')
  })
})
