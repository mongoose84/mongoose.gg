import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { mount, flushPromises, RouterLinkStub } from '@vue/test-utils'
import { ref } from 'vue'
import OverviewPage from '@/views/OverviewPage.vue'

const mockGetOverview = vi.fn()

const mockIsOverallMode = ref(false)
const mockActiveAccountPuuid = ref('acc_1')
const mockHasLinkedAccount = ref(true)

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    isInitialized: true,
    riotAccounts: [],
    refreshUser: vi.fn(),
    get hasLinkedAccount() {
      return mockHasLinkedAccount.value
    },
    get isOverallMode() {
      return mockIsOverallMode.value
    },
    get activeAccountPuuid() {
      return mockActiveAccountPuuid.value
    }
  })
}))

vi.mock('@/composables/useSyncWebSocket', () => ({
  useSyncWebSocket: () => ({
    syncProgress: ref(new Map()),
    resetProgress: vi.fn()
  })
}))

const mockSyncState = ref(null)
const mockIsSyncing = ref(false)
const mockStartSync = vi.fn()

vi.mock('@/composables/useSyncMatches', () => ({
  useSyncMatches: () => ({
    syncState: mockSyncState,
    isSyncing: mockIsSyncing,
    progressCurrent: ref(12),
    progressTotal: ref(40),
    syncedCount: ref(0),
    lastSyncAt: ref(null),
    startSync: mockStartSync
  })
}))

vi.mock('@/services/soloApi', () => ({
  getOverview: (...args) => mockGetOverview(...args)
}))

const fullOverview = {
  playerHeader: { summonerName: 'Faker#KR1', level: 100, region: 'EUW', rank: 'GOLD II', lp: 45 },
  mostPlayedChampion: { championName: 'Ahri', gamesPlayed: 28, source: 'current_season' },
  lastMatch: { matchId: 'EUW1_1', championIconUrl: 'ahri.png', championName: 'Ahri', result: 'Victory', kda: '7/2/9', timestamp: Date.now(), queueType: 'Ranked Solo/Duo' },
  sessionStats: { gamesToday: 3, winsToday: 2, lossesToday: 1, gamesThisWeek: 12, winsThisWeek: 7, lossesThisWeek: 5 },
  survivalStats: { avgDeathsPerGame: 5.2, winRateLowDeaths: 0.64, winRateHighDeaths: 0.38, gamesLowDeaths: 9, gamesHighDeaths: 8, lowDeathThreshold: 4, highDeathThreshold: 7, totalGames: 20 },
  championPool: {
    champions: [
      { championId: 103, championName: 'Ahri', role: 'MIDDLE', matches: 22, wins: 14, winRate: 63.6, avgKda: 4.1, mScore: 71.2, strengthTag: 'Best laning' },
      { championId: 134, championName: 'Syndra', role: 'MIDDLE', matches: 18, wins: 10, winRate: 55.6, avgKda: 3.3, mScore: 60.1, strengthTag: 'Most damage' }
    ],
    alsoPlayed: []
  }
}

const mounted = []

function mountPage() {
  const wrapper = mount(OverviewPage, {
    global: {
      stubs: {
        RouterLink: RouterLinkStub,
        OverviewAccountCards: true,
        LinkRiotAccountModal: { props: ['isOpen'], template: '<div data-testid="link-modal" :data-open="isOpen" />' }
      }
    }
  })
  mounted.push(wrapper)
  return wrapper
}

describe('OverviewPage', () => {
  beforeEach(() => {
    mockGetOverview.mockReset()
    mockIsOverallMode.value = false
    mockActiveAccountPuuid.value = 'acc_1'
    mockHasLinkedAccount.value = true
    mockSyncState.value = null
    mockIsSyncing.value = false
    mockStartSync.mockReset()
  })

  afterEach(() => {
    mounted.splice(0).forEach((wrapper) => wrapper.unmount())
  })

  it('shows the skeleton frame while loading', () => {
    mockGetOverview.mockReturnValue(new Promise(() => {}))
    const wrapper = mountPage()
    const loading = wrapper.get('[data-testid="overview-loading"]')
    expect(loading.attributes('aria-busy')).toBe('true')
    expect(loading.text()).toContain('Loading your Overview')
  })

  it('renders the sections in design-system order with real data', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    const wrapper = mountPage()
    await flushPromises()

    const order = ['champion-hero', 'overview-champion-pool', 'today-matches-card', 'overview-insights', 'overview-next-steps']
    const html = wrapper.html()
    const positions = order.map((id) => html.indexOf(`data-testid="${id}"`))
    expect(positions.every((p) => p >= 0)).toBe(true)
    expect([...positions].sort((a, b) => a - b)).toEqual(positions)
  })

  it('builds the hero from this week and the most-played champion', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('h1').text()).toBe("You've won 7 of 12 matches this week")
    expect(wrapper.get('[data-testid="champion-hero-player"]').text()).toBe('Faker#KR1 · Ahri main · Gold II · 45 LP')
    expect(wrapper.get('[data-testid="champion-hero-art"]').attributes('alt')).toBe('Ahri splash art')
    expect(wrapper.findAll('[data-testid="champion-hero-chip"]').map((c) => c.text())).toEqual(['12 matches this week', '58% win rate'])
  })

  it('shows today\'s summary and the last match as a row', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="today-matches-summary"]').text()).toBe('3 matches today · 2 wins, 1 loss')
    expect(wrapper.get('[data-testid="match-row-champion"]').text()).toBe('Ahri')
    expect(wrapper.get('[data-testid="match-row-result"]').text()).toBe('Victory')
  })

  it('shows empty states when there is no match or insight yet', async () => {
    mockGetOverview.mockResolvedValue({ ...fullOverview, lastMatch: null, survivalStats: null, mostPlayedChampion: null })
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('[data-testid="today-matches-empty"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="overview-insights-empty"]').exists()).toBe(true)
    expect(wrapper.get('[data-testid="champion-hero"]').classes()).toContain('champion-hero--plain')
  })

  it('shows your ranked champions as cards', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    const wrapper = mountPage()
    await flushPromises()

    const names = wrapper.findAll('[data-testid="champion-card-name"]').map((n) => n.text())
    expect(names).toEqual(['Ahri', 'Syndra'])
    expect(wrapper.find('[data-testid="champion-pool-empty"]').exists()).toBe(false)
  })

  it('shows the champion pool empty state without ranked matches this season', async () => {
    mockGetOverview.mockResolvedValue({ ...fullOverview, championPool: undefined })
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="champion-pool-empty"]').text()).toContain('No ranked matches this season')
  })

  it('shows the deaths finding as an insight', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="insight-card-chip"]').text()).toBe('Pattern')
    expect(wrapper.get('[data-testid="insight-card-title"]').text()).toBe('You win 64% of matches with 4 or fewer deaths')
  })

  it('links the next steps to Champion Select and Solo', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    const wrapper = mountPage()
    await flushPromises()

    const targets = wrapper.findAllComponents(RouterLinkStub).map((link) => link.props('to'))
    expect(targets).toEqual(expect.arrayContaining(['/app/champion-select', '/app/solo', '/app/matches']))
  })

  it('starts a sync from the Today\'s matches card and disables the button while syncing', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    const wrapper = mountPage()
    await flushPromises()

    const button = wrapper.get('[data-testid="overview-sync-button"]')
    expect(button.text()).toBe('Sync matches')
    await button.trigger('click')
    expect(mockStartSync).toHaveBeenCalledTimes(1)

    mockIsSyncing.value = true
    await flushPromises()
    expect(button.attributes('disabled')).toBeDefined()
    expect(button.text()).toBe('Syncing…')
  })

  it('shows SyncProgress at the top while a sync runs, without hiding the content', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    mockSyncState.value = 'running'
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="sync-progress-title"]').text()).toBe('Syncing 12 of 40 matches')
    expect(wrapper.find('[data-testid="champion-hero"]').exists()).toBe(true)
  })

  it('shows an error with retry when the Overview fails to load', async () => {
    mockGetOverview.mockRejectedValueOnce(new Error('boom')).mockResolvedValueOnce(fullOverview)
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="overview-error"]').text()).toContain("We couldn't load your Overview")
    await wrapper.get('[data-testid="overview-retry"]').trigger('click')
    await flushPromises()

    expect(mockGetOverview).toHaveBeenCalledTimes(2)
    expect(wrapper.find('[data-testid="champion-hero"]').exists()).toBe(true)
  })

  it('asks to link a Riot account when none is linked', async () => {
    mockHasLinkedAccount.value = false
    mockGetOverview.mockRejectedValue(new Error('No linked account'))
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('[data-testid="overview-error"]').exists()).toBe(false)
    expect(wrapper.get('h1').text()).toBe('Link your Riot account to see your Overview')

    await wrapper.get('[data-testid="overview-link-account"]').trigger('click')
    expect(wrapper.get('[data-testid="link-modal"]').attributes('data-open')).toBe('true')
  })

  it('shows account cards instead of the hero in Overall mode', async () => {
    mockIsOverallMode.value = true
    mockGetOverview.mockResolvedValue({
      ...fullOverview,
      accountSummaries: [
        { accountId: 'acc_1', gameName: 'Test1', tagLine: 'EUW', region: 'EUW', rank: 'Gold I', lp: 50, gamesToday: 2, gamesThisWeek: 10 }
      ]
    })
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('overview-account-cards-stub').exists()).toBe(true)
    expect(wrapper.find('[data-testid="champion-hero"]').exists()).toBe(false)
  })

  it('refetches when the active account changes', async () => {
    mockGetOverview.mockResolvedValue(fullOverview)
    mountPage()
    await flushPromises()
    expect(mockGetOverview).toHaveBeenCalledTimes(1)

    mockActiveAccountPuuid.value = 'acc_2'
    await flushPromises()
    expect(mockGetOverview).toHaveBeenCalledTimes(2)
  })
})
