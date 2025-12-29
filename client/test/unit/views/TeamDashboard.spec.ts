import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import TeamDashboard from '@/views/TeamDashboard.vue'

// Mock all API calls
vi.mock('@/api/shared.js', () => ({
  getGamers: vi.fn().mockResolvedValue([
    { puuid: 'abc123', gamerName: 'Player1#EUW', server: 'euw1', tier: 'GOLD', rank: 'II' },
    { puuid: 'def456', gamerName: 'Player2#EUW', server: 'euw1', tier: 'PLATINUM', rank: 'IV' },
    { puuid: 'ghi789', gamerName: 'Player3#EUW', server: 'euw1', tier: 'DIAMOND', rank: 'III' }
  ]),
  getBaseApi: vi.fn().mockReturnValue('http://localhost:5000/api/v1.0'),
  getHost: vi.fn().mockReturnValue('http://localhost:5000')
}))

// Mock team API calls
vi.mock('@/api/team.js', () => ({
  getTeamStats: vi.fn().mockResolvedValue({
    gamesPlayed: 40,
    winRate: 55.0,
    queueType: 'Flex Queue',
    playerCount: 3
  }),
  getTeamWinRateTrend: vi.fn().mockResolvedValue({ dataPoints: [] }),
  getTeamDurationAnalysis: vi.fn().mockResolvedValue({}),
  getTeamMultiKills: vi.fn().mockResolvedValue({}),
  getTeamKillParticipation: vi.fn().mockResolvedValue({}),
  getTeamKillsByPhase: vi.fn().mockResolvedValue({}),
  getTeamKillsTrend: vi.fn().mockResolvedValue({}),
  getTeamDeathTimerImpact: vi.fn().mockResolvedValue({}),
  getTeamDeathShare: vi.fn().mockResolvedValue({}),
  getTeamDeathsByDuration: vi.fn().mockResolvedValue({}),
  getTeamDeathsTrend: vi.fn().mockResolvedValue({}),
  getTeamSynergy: vi.fn().mockResolvedValue({}),
  getTeamChampionCombos: vi.fn().mockResolvedValue({}),
  getTeamRolePairEffectiveness: vi.fn().mockResolvedValue({})
}))

// Create a mock router
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'Home', component: { template: '<div>Home</div>' } },
    { path: '/team/:userId/:userName', name: 'TeamDashboard', component: TeamDashboard }
  ]
})

// Stub child components
const stubs = {
  AppLogo: { template: '<div class="app-logo-stub">Logo</div>' },
  GamerCard: { template: '<div class="gamer-card-stub">Gamer</div>' },
  GamerCardsList: { template: '<div class="gamer-cards-stub"><slot /></div>' },
  SideWinRate: { template: '<div class="side-wr-stub">Side WR</div>' },
  TeamSynergyMatrix: { template: '<div class="synergy-stub">Synergy</div>' },
  TeamWinRateTrend: { template: '<div class="wr-trend-stub">WR Trend</div>' },
  TeamDurationAnalysis: { template: '<div class="dur-analysis-stub">Duration</div>' },
  TeamChampionCombos: { template: '<div class="combos-stub">Combos</div>' },
  TeamRolePairEffectiveness: { template: '<div class="role-pair-stub">Role Pairs</div>' },
  TeamDeathTimerImpact: { template: '<div class="death-timer-stub">Death Timer</div>' },
  TeamDeathsByDuration: { template: '<div class="deaths-dur-stub">Deaths Dur</div>' },
  TeamDeathShare: { template: '<div class="death-share-stub">Death Share</div>' },
  TeamDeathsTrend: { template: '<div class="deaths-trend-stub">Deaths Trend</div>' },
  TeamKillParticipation: { template: '<div class="kill-part-stub">Kill Part</div>' },
  TeamKillsByPhase: { template: '<div class="kills-phase-stub">Kills Phase</div>' },
  TeamKillsTrend: { template: '<div class="kills-trend-stub">Kills Trend</div>' },
  TeamMultiKillShowcase: { template: '<div class="multi-kill-stub">Multi Kills</div>' }
}

describe('TeamDashboard', () => {
  beforeEach(async () => {
    vi.clearAllMocks()
    router.push('/')
    await router.isReady()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', async () => {
    const wrapper = mount(TeamDashboard, {
      props: { userId: 1, userName: 'TeamSquad' },
      global: { plugins: [router], stubs }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('displays the user name with Team label', async () => {
    const wrapper = mount(TeamDashboard, {
      props: { userId: 1, userName: 'TeamSquad' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('h2').text()).toContain('TeamSquad')
    expect(wrapper.find('.dashboard-type').text()).toBe('Team')
  })

  it('displays team context stats', async () => {
    const wrapper = mount(TeamDashboard, {
      props: { userId: 1, userName: 'TeamSquad' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    const teamContext = wrapper.find('.team-context')
    expect(teamContext.exists()).toBe(true)
    expect(teamContext.text()).toContain('40 games together')
    expect(teamContext.text()).toContain('55% WR')
    expect(teamContext.text()).toContain('3 players')
  })

  it('renders all gamer cards for team members', async () => {
    const wrapper = mount(TeamDashboard, {
      props: { userId: 1, userName: 'TeamSquad' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    const gamerCards = wrapper.findAll('.gamer-card-stub')
    expect(gamerCards.length).toBe(3)
  })

  it('renders team feature components', async () => {
    const wrapper = mount(TeamDashboard, {
      props: { userId: 1, userName: 'TeamSquad' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('.wr-trend-stub').exists()).toBe(true)
    expect(wrapper.find('.side-wr-stub').exists()).toBe(true)
    expect(wrapper.find('.synergy-stub').exists()).toBe(true)
    expect(wrapper.find('.combos-stub').exists()).toBe(true)
  })

  it('shows error message on API failure', async () => {
    const sharedApi = await import('@/api/shared.js')
    vi.mocked(sharedApi.getGamers).mockRejectedValueOnce(new Error('Server Error'))

    const wrapper = mount(TeamDashboard, {
      props: { userId: 1, userName: 'TeamSquad' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.text()).toContain('Server Error')
  })

  it('applies correct win rate class for high win rate', async () => {
    const wrapper = mount(TeamDashboard, {
      props: { userId: 1, userName: 'TeamSquad' },
      global: { plugins: [router], stubs }
    })

    await flushPromises()

    expect(wrapper.find('.win-rate-high').exists()).toBe(true)
  })
})

