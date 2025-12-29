import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import DuoDashboard from '@/views/DuoDashboard.vue'

// Mock all API calls
vi.mock('@/api/shared.js', () => ({
  getGamers: vi.fn().mockResolvedValue([
    { puuid: 'abc123', gamerName: 'Player1#EUW', server: 'euw1', tier: 'GOLD', rank: 'II' },
    { puuid: 'def456', gamerName: 'Player2#EUW', server: 'euw1', tier: 'PLATINUM', rank: 'IV' }
  ]),
  getBaseApi: vi.fn().mockReturnValue('http://localhost:5000/api/v1.0'),
  getHost: vi.fn().mockReturnValue('http://localhost:5000')
}))

// Mock duo API calls
vi.mock('@/api/duo.js', () => ({
  getDuoStats: vi.fn().mockResolvedValue({
    gamesPlayed: 50,
    winRate: 58.0,
    queueType: 'Ranked Solo/Duo'
  }),
  getDuoWinRateTrend: vi.fn().mockResolvedValue({ dataPoints: [] }),
  getDuoStreak: vi.fn().mockResolvedValue({ currentStreak: 3 }),
  getDuoPerformanceRadar: vi.fn().mockResolvedValue({ metrics: [] }),
  getDuoMultiKills: vi.fn().mockResolvedValue({}),
  getDuoKillParticipation: vi.fn().mockResolvedValue({}),
  getDuoKillsByPhase: vi.fn().mockResolvedValue({}),
  getDuoKillsTrend: vi.fn().mockResolvedValue({}),
  getDuoDeathTimerImpact: vi.fn().mockResolvedValue({}),
  getDuoDeathsByDuration: vi.fn().mockResolvedValue({}),
  getDuoDeathsTrend: vi.fn().mockResolvedValue({}),
  getDuoMatchDuration: vi.fn().mockResolvedValue({}),
  getDuoVsEnemy: vi.fn().mockResolvedValue({}),
  getDuoImprovementSummary: vi.fn().mockResolvedValue({})
}))

// Create a mock router
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'Home', component: { template: '<div>Home</div>' } },
    { path: '/duo/:userId/:userName', name: 'DuoDashboard', component: DuoDashboard }
  ]
})

// Stub child components
const stubs = {
  AppLogo: { template: '<div class="app-logo-stub">Logo</div>' },
  GamerCard: { template: '<div class="gamer-card-stub">Gamer</div>' },
  GamerCardsList: { template: '<div class="gamer-cards-stub"><slot /></div>' },
  SideWinRate: { template: '<div class="side-wr-stub">Side WR</div>' },
  ChampionSynergyMatrix: { template: '<div class="synergy-stub">Synergy</div>' },
  DuoVsEnemyMatrix: { template: '<div class="vs-enemy-stub">Vs Enemy</div>' },
  DuoMatchDuration: { template: '<div class="match-dur-stub">Duration</div>' },
  DuoImprovementSummary: { template: '<div class="improvement-stub">Improvement</div>' },
  DuoMultiKillShowcase: { template: '<div class="multi-kill-stub">Multi Kills</div>' },
  DuoKillsByPhase: { template: '<div class="kills-phase-stub">Kills Phase</div>' },
  DuoKillParticipation: { template: '<div class="kill-part-stub">Kill Part</div>' },
  DuoKillsTrend: { template: '<div class="kills-trend-stub">Kills Trend</div>' },
  DuoDeathTimerImpact: { template: '<div class="death-timer-stub">Death Timer</div>' },
  DuoDeathsByDuration: { template: '<div class="deaths-dur-stub">Deaths Dur</div>' },
  DuoDeathShare: { template: '<div class="death-share-stub">Death Share</div>' },
  DuoDeathsTrend: { template: '<div class="deaths-trend-stub">Deaths Trend</div>' },
  DuoWinRateTrend: { template: '<div class="wr-trend-stub">WR Trend</div>' },
  DuoPerformanceRadar: { template: '<div class="perf-radar-stub">Radar</div>' },
  DuoStreak: { template: '<div class="streak-stub">Streak</div>' }
}

describe('DuoDashboard', () => {
  beforeEach(async () => {
    vi.clearAllMocks()
    router.push('/')
    await router.isReady()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', async () => {
    const wrapper = mount(DuoDashboard, {
      props: { userId: 1, userName: 'DuoPartners' },
      global: { plugins: [router], stubs }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('displays the user name with Duo label', async () => {
    const wrapper = mount(DuoDashboard, {
      props: { userId: 1, userName: 'DuoPartners' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('h2').text()).toContain('DuoPartners')
    expect(wrapper.find('.dashboard-type').text()).toBe('Duo')
  })

  it('displays duo context stats', async () => {
    const wrapper = mount(DuoDashboard, {
      props: { userId: 1, userName: 'DuoPartners' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    const duoContext = wrapper.find('.duo-context')
    expect(duoContext.exists()).toBe(true)
    expect(duoContext.text()).toContain('50 games together')
    expect(duoContext.text()).toContain('58% WR')
  })

  it('displays handshake icon between gamer cards', async () => {
    const wrapper = mount(DuoDashboard, {
      props: { userId: 1, userName: 'DuoPartners' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('.handshake-icon').exists()).toBe(true)
    expect(wrapper.find('.handshake-icon').text()).toBe('🤝')
  })

  it('renders duo feature components', async () => {
    const wrapper = mount(DuoDashboard, {
      props: { userId: 1, userName: 'DuoPartners' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('.wr-trend-stub').exists()).toBe(true)
    expect(wrapper.find('.streak-stub').exists()).toBe(true)
    expect(wrapper.find('.synergy-stub').exists()).toBe(true)
    expect(wrapper.find('.improvement-stub').exists()).toBe(true)
  })

  it('shows error message on API failure', async () => {
    const sharedApi = await import('@/api/shared.js')
    vi.mocked(sharedApi.getGamers).mockRejectedValueOnce(new Error('Network Error'))

    const wrapper = mount(DuoDashboard, {
      props: { userId: 1, userName: 'DuoPartners' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('Network Error')
  })

  it('applies correct win rate class for good win rate', async () => {
    const wrapper = mount(DuoDashboard, {
      props: { userId: 1, userName: 'DuoPartners' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('.wr-good').exists()).toBe(true)
  })
})

