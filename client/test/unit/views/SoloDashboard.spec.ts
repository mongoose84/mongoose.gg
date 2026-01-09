import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import SoloDashboard from '@/views/SoloDashboard.vue'

// Mock all API calls
vi.mock('@/api/shared.js', () => ({
  getGamers: vi.fn().mockResolvedValue([
    { puuid: 'abc123', gamerName: 'Player1#EUW', server: 'euw1', tier: 'GOLD', rank: 'II' },
    { puuid: 'def456', gamerName: 'Player1#EUNE', server: 'eun1', tier: 'PLATINUM', rank: 'IV' }
  ]),
  getBaseApi: vi.fn().mockReturnValue('http://localhost:5000/api/v1.0'),
  getHost: vi.fn().mockReturnValue('http://localhost:5000'),
  isDevelopment: false
}))

// Mock solo API calls
vi.mock('@/api/solo.js', () => ({
  getOverallStats: vi.fn().mockResolvedValue({ totalGames: 100, winRate: 55 }),
  getPerformance: vi.fn().mockResolvedValue({ gamers: [] }),
  getComparison: vi.fn().mockResolvedValue({ winrate: [], kda: [] }),
  getMatchDuration: vi.fn().mockResolvedValue({ gamers: [] }),
  getChampionPerformance: vi.fn().mockResolvedValue({ champions: [] }),
  getChampionMatchups: vi.fn().mockResolvedValue({ matchups: [] })
}))

// Create a mock router
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'Home', component: { template: '<div>Home</div>' } },
    { path: '/solo/:userId/:userName', name: 'SoloDashboard', component: SoloDashboard }
  ]
})

// Stub child components to avoid deep rendering
const stubs = {
  AppLogo: { template: '<div class="app-logo-stub">Logo</div>' },
  GamerCardsList: { template: '<div class="gamer-cards-stub"><slot name="after-cards" /></div>' },
  ComparisonStrip: { template: '<div class="comparison-strip-stub">Comparison</div>' },
  PerformanceCharts: { template: '<div class="performance-charts-stub">Performance</div>' },
  RadarChart: { template: '<div class="radar-chart-stub">Radar</div>' },
  ChampionPerformanceSplit: { template: '<div class="champion-perf-stub">Champions</div>' },
  RoleDistribution: { template: '<div class="role-dist-stub">Roles</div>' },
  DeathEfficiency: { template: '<div class="death-eff-stub">Deaths</div>' },
  MatchDuration: { template: '<div class="match-dur-stub">Duration</div>' },
  SideWinDistribution: { template: '<div class="side-wd-stub">Side Win Distribution</div>' },
  SummaryInsights: { template: '<div class="summary-stub">Insights</div>' },
  ChampionMatchups: { template: '<div class="matchups-stub">Matchups</div>' }
}

describe('SoloDashboard', () => {
  beforeEach(async () => {
    vi.clearAllMocks()
    router.push('/')
    await router.isReady()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', async () => {
    const wrapper = mount(SoloDashboard, {
      props: { userId: 1, userName: 'TestPlayer' },
      global: { plugins: [router], stubs }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('displays the user name in the header', async () => {
    const wrapper = mount(SoloDashboard, {
      props: { userId: 1, userName: 'TestPlayer' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('h2').text()).toBe('TestPlayer')
  })

  it('displays app branding', async () => {
    const wrapper = mount(SoloDashboard, {
      props: { userId: 1, userName: 'TestPlayer' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('.title').text()).toBe('Do End')
    expect(wrapper.find('.subtitle').text()).toBe('Cross Account LoL Statistics')
  })

  it('shows loading state initially', async () => {
    const sharedApi = await import('@/api/shared.js')
    // Make the API call hang indefinitely
    let resolvePromise: (value: any) => void
    vi.mocked(sharedApi.getGamers).mockImplementationOnce(() => new Promise((resolve) => {
      resolvePromise = resolve
    }))

    const wrapper = mount(SoloDashboard, {
      props: { userId: 1, userName: 'TestPlayer' },
      global: { plugins: [router], stubs }
    })

    // Check loading state before the promise resolves
    await wrapper.vm.$nextTick()
    expect(wrapper.text()).toContain('Loading')

    // Clean up by resolving the promise
    resolvePromise!([])
    await flushPromises()
  })

  it('shows error message on API failure', async () => {
    const sharedApi = await import('@/api/shared.js')
    vi.mocked(sharedApi.getGamers).mockRejectedValueOnce(new Error('Failed to fetch gamers'))

    const wrapper = mount(SoloDashboard, {
      props: { userId: 1, userName: 'TestPlayer' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('Failed to fetch gamers')
  })

  it('shows missing user message when no userId', async () => {
    const wrapper = mount(SoloDashboard, {
      props: { userId: '', userName: '' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('Missing user details')
  })

  it('renders all dashboard components when data loads', async () => {
    const wrapper = mount(SoloDashboard, {
      props: { userId: 1, userName: 'TestPlayer' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('.comparison-strip-stub').exists()).toBe(true)
    expect(wrapper.find('.performance-charts-stub').exists()).toBe(true)
    expect(wrapper.find('.radar-chart-stub').exists()).toBe(true)
    expect(wrapper.find('.champion-perf-stub').exists()).toBe(true)
    expect(wrapper.find('.side-wd-stub').exists()).toBe(true)
    expect(wrapper.find('.summary-stub').exists()).toBe(true)
    expect(wrapper.find('.matchups-stub').exists()).toBe(true)
  })

  it('has a clickable logo that links to home', async () => {
    const wrapper = mount(SoloDashboard, {
      props: { userId: 1, userName: 'TestPlayer' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    const brandLink = wrapper.find('.brand-link')
    expect(brandLink.exists()).toBe(true)
    expect(brandLink.attributes('href')).toBe('/')
  })
})

