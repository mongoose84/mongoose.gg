import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import SummaryInsights from '@/components/solo/SummaryInsights.vue'

// Mock the API calls
vi.mock('@/api/solo.js', () => ({
  getComparison: vi.fn().mockResolvedValue({
    winrate: [
      { value: 55, gamerName: 'Player1#EUW' },
      { value: 48, gamerName: 'Player1#EUNE' }
    ],
    kda: [
      { value: 3.2, gamerName: 'Player1#EUW' },
      { value: 2.5, gamerName: 'Player1#EUNE' }
    ],
    csPrMin: [
      { value: 6.5, gamerName: 'Player1#EUW' },
      { value: 5.8, gamerName: 'Player1#EUNE' }
    ],
    goldPrMin: [
      { value: 380, gamerName: 'Player1#EUW' },
      { value: 340, gamerName: 'Player1#EUNE' }
    ],
    avgKills: [
      { value: 6.0, gamerName: 'Player1#EUW' },
      { value: 5.0, gamerName: 'Player1#EUNE' }
    ],
    avgDeaths: [
      { value: 4.5, gamerName: 'Player1#EUW' },
      { value: 6.5, gamerName: 'Player1#EUNE' }
    ],
    avgAssists: [
      { value: 8.0, gamerName: 'Player1#EUW' },
      { value: 7.0, gamerName: 'Player1#EUNE' }
    ]
  }),
  getMatchDuration: vi.fn().mockResolvedValue({
    gamers: [{
      gamerName: 'Player1#EUW',
      buckets: [
        { durationRange: '20-25', gamesPlayed: 10, winrate: 60 },
        { durationRange: '25-30', gamesPlayed: 15, winrate: 55 }
      ]
    }]
  }),
  getChampionPerformance: vi.fn().mockResolvedValue({
    champions: [
      { championName: 'Jinx', servers: [{ gamesPlayed: 20 }] },
      { championName: 'Caitlyn', servers: [{ gamesPlayed: 15 }] },
      { championName: 'Ashe', servers: [{ gamesPlayed: 10 }] }
    ]
  })
}))

describe('SummaryInsights', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', () => {
    const wrapper = mount(SummaryInsights, {
      props: { userId: 1 }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('shows loading state initially', async () => {
    const wrapper = mount(SummaryInsights, {
      props: { userId: 1 }
    })

    // Component should exist with loading state
    expect(wrapper.find('.summary-insights').exists()).toBe(true)
  })

  it('renders header and subtitle', async () => {
    const wrapper = mount(SummaryInsights, {
      props: { userId: 1 }
    })

    await flushPromises()

    expect(wrapper.find('.insights-header').text()).toContain('Summary Insights')
    expect(wrapper.find('.insights-subtitle').exists()).toBe(true)
  })

  it('renders insight cards after loading', async () => {
    const wrapper = mount(SummaryInsights, {
      props: { userId: 1 }
    })

    await flushPromises()

    const insightCards = wrapper.findAll('.insight-card')
    expect(insightCards.length).toBeGreaterThan(0)
    expect(insightCards.length).toBeLessThanOrEqual(5) // Should show max 5 insights
  })

  it('renders insight card with correct structure', async () => {
    const wrapper = mount(SummaryInsights, {
      props: { userId: 1 }
    })

    await flushPromises()

    const firstCard = wrapper.find('.insight-card')
    expect(firstCard.find('.insight-icon').exists()).toBe(true)
    expect(firstCard.find('.insight-title').exists()).toBe(true)
    expect(firstCard.find('.insight-stat').exists()).toBe(true)
    expect(firstCard.find('.insight-description').exists()).toBe(true)
  })

  it('handles empty data gracefully', async () => {
    const soloApi = await import('@/api/solo.js')
    vi.mocked(soloApi.getComparison).mockResolvedValueOnce({
      winrate: [],
      kda: [],
      csPrMin: [],
      goldPrMin: [],
      avgKills: [],
      avgDeaths: [],
      avgAssists: []
    })

    const wrapper = mount(SummaryInsights, {
      props: { userId: 1 }
    })

    await flushPromises()

    expect(wrapper.find('.insights-empty').exists()).toBe(true)
    expect(wrapper.text()).toContain('Not enough data')
  })

  it('handles API errors gracefully', async () => {
    const soloApi = await import('@/api/solo.js')
    vi.mocked(soloApi.getComparison).mockRejectedValueOnce(new Error('API Error'))

    const wrapper = mount(SummaryInsights, {
      props: { userId: 1 }
    })

    await flushPromises()

    expect(wrapper.find('.insights-error').exists()).toBe(true)
    expect(wrapper.text()).toContain('API Error')
  })

  it('reloads data when userId prop changes', async () => {
    const soloApi = await import('@/api/solo.js')

    const wrapper = mount(SummaryInsights, {
      props: { userId: 1 }
    })

    await flushPromises()
    expect(soloApi.getComparison).toHaveBeenCalledTimes(1)

    await wrapper.setProps({ userId: 2 })
    await flushPromises()

    expect(soloApi.getComparison).toHaveBeenCalledTimes(2)
  })
})

