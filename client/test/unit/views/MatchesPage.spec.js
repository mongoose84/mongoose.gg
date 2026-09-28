import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises, RouterLinkStub } from '@vue/test-utils'
import { reactive, ref } from 'vue'
import MatchesPage from '@/views/MatchesPage.vue'

// ── API mocks ──────────────────────────────────────────────────────────────────
const mockGetMatchList = vi.fn()
const mockGetMatchDetails = vi.fn()
const mockTrackFilterChange = vi.fn()
const mockTrackMatchSelect = vi.fn()

vi.mock('@/services/matchesApi', () => ({
  getMatchList: (...args) => mockGetMatchList(...args),
  getMatchDetails: (...args) => mockGetMatchDetails(...args)
}))

vi.mock('@/services/analyticsApi', () => ({
  trackFilterChange: (...args) => mockTrackFilterChange(...args),
  trackMatchSelect: (...args) => mockTrackMatchSelect(...args)
}))

// ── Router mock: the open match lives in route.params.matchId ──────────────────
const mockRoute = reactive({ params: {} })
const mockReplace = vi.fn((location) => {
  mockRoute.params = { ...(location.params || {}) }
})

vi.mock('vue-router', () => ({
  useRoute: () => mockRoute,
  useRouter: () => ({ replace: mockReplace })
}))

// ── Sync composable mock ───────────────────────────────────────────────────────
const mockSyncState = ref(null)
const mockStartSync = vi.fn()

vi.mock('@/composables/useSyncMatches', () => ({
  useSyncMatches: () => ({
    syncState: mockSyncState,
    isSyncing: ref(false),
    progressCurrent: ref(0),
    progressTotal: ref(0),
    syncedCount: ref(0),
    startSync: mockStartSync
  })
}))

// ── Auth store mock — reactive so watchers fire correctly ──────────────────────
const mockIsOverallMode = ref(false)
const mockActiveAccountPuuid = ref('acc_primary')
const mockActiveAccount = ref({ accountId: 'acc_primary', puuid: 'puuid-primary' })
const mockRiotAccounts = ref([])
const mockPrimaryRiotAccount = ref({ accountId: 'acc_primary', puuid: 'puuid-primary' })
const mockHasLinkedAccount = ref(true)

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    isInitialized: true,
    get hasLinkedAccount() { return mockHasLinkedAccount.value },
    get isOverallMode() { return mockIsOverallMode.value },
    get activeAccountPuuid() { return mockActiveAccountPuuid.value },
    get activeAccount() { return mockActiveAccount.value },
    get riotAccounts() { return mockRiotAccounts.value },
    get primaryRiotAccount() { return mockPrimaryRiotAccount.value },
    refreshUser: vi.fn()
  })
}))

// ── Component stubs ────────────────────────────────────────────────────────────
const MatchDetailsStub = {
  name: 'MatchDetails',
  props: ['match', 'baseline', 'accountId', 'loading', 'error', 'badge'],
  emits: ['retry'],
  template: '<div data-testid="match-details-stub" />'
}

const pageStubs = {
  MatchDetails: MatchDetailsStub,
  SyncProgress: { name: 'SyncProgress', props: ['state'], template: '<div data-testid="sync-progress-stub" />' },
  LinkRiotAccountModal: true,
  BaseIcon: true,
  RouterLink: RouterLinkStub
}

// ── Helpers ────────────────────────────────────────────────────────────────────
function makeMatch(matchId, overrides = {}) {
  return {
    matchId,
    championName: 'Ahri',
    championIconUrl: null,
    win: true,
    kills: 5,
    deaths: 2,
    assists: 8,
    queueType: 'Ranked Solo/Duo',
    gameDurationSec: 1800,
    gameStartTime: Date.now(),
    accountGameName: null,
    accountTagLine: null,
    accountRegion: null,
    trendBadge: null,
    ...overrides
  }
}

function listResponse(matches) {
  return { matches, totalMatches: matches.length, queueType: 'all' }
}

function setDesktop(isDesktop) {
  window.matchMedia = vi.fn().mockImplementation((query) => ({
    matches: query === '(min-width: 900px)' ? isDesktop : false,
    media: query,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn()
  }))
}

let currentWrapper = null

function mountPage() {
  currentWrapper = mount(MatchesPage, { global: { stubs: pageStubs } })
  return currentWrapper
}

async function openMatch(matchId) {
  mockRoute.params = { matchId }
  await flushPromises()
}

describe('MatchesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockRoute.params = {}
    mockSyncState.value = null
    mockHasLinkedAccount.value = true
    mockIsOverallMode.value = false
    mockActiveAccountPuuid.value = 'acc_primary'
    mockActiveAccount.value = { accountId: 'acc_primary', puuid: 'puuid-primary' }
    mockRiotAccounts.value = []
    mockPrimaryRiotAccount.value = { accountId: 'acc_primary', puuid: 'puuid-primary' }
    mockGetMatchDetails.mockResolvedValue({ match: { matchId: 'MATCH_1' }, baseline: null })
    setDesktop(false)
  })

  afterEach(() => {
    currentWrapper?.unmount()
    currentWrapper = null
    delete window.matchMedia
  })

  describe('states', () => {
    it('shows the skeleton list while matches load', async () => {
      mockGetMatchList.mockReturnValue(new Promise(() => {}))
      const wrapper = mountPage()
      await flushPromises()
      expect(wrapper.find('[data-testid="matches-list-loading"]').exists()).toBe(true)
      expect(wrapper.get('[data-testid="matches-list"]').attributes('aria-busy')).toBe('true')
    })

    it('asks to link a Riot account and skips the request without one', async () => {
      mockHasLinkedAccount.value = false
      const wrapper = mountPage()
      await flushPromises()
      expect(wrapper.find('[data-testid="matches-no-account"]').exists()).toBe(true)
      expect(mockGetMatchList).not.toHaveBeenCalled()
    })

    it('shows an inline error with a retry that reloads the list', async () => {
      mockGetMatchList.mockRejectedValueOnce(new Error('boom'))
      const wrapper = mountPage()
      await flushPromises()
      expect(wrapper.get('[data-testid="matches-list-error"]').text()).toContain("We couldn't load your matches")

      mockGetMatchList.mockResolvedValueOnce(listResponse([makeMatch('MATCH_1')]))
      await wrapper.get('[data-testid="matches-retry"]').trigger('click')
      await flushPromises()
      expect(wrapper.findAll('[data-testid="match-row"]')).toHaveLength(1)
    })

    it('offers Sync matches when there are no matches at all', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([]))
      const wrapper = mountPage()
      await flushPromises()
      expect(wrapper.get('[data-testid="matches-empty"]').text()).toContain('No matches yet')
      expect(wrapper.find('[data-testid="matches-detail"]').exists()).toBe(false)

      await wrapper.get('[data-testid="matches-empty-sync"]').trigger('click')
      expect(mockStartSync).toHaveBeenCalledOnce()
    })

    it('offers Show all queues when a queue filter has no matches', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([]))
      const wrapper = mountPage()
      await flushPromises()
      await wrapper.get('[data-testid="queue-ranked_flex"]').trigger('click')
      await flushPromises()
      expect(wrapper.get('[data-testid="matches-empty"]').text()).toContain('No Flex matches yet')

      await wrapper.get('[data-testid="matches-show-all"]').trigger('click')
      await flushPromises()
      expect(mockGetMatchList).toHaveBeenLastCalledWith(1, 'all')
    })
  })

  describe('content', () => {
    it('renders one row per match and a headline from the results', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([
        makeMatch('MATCH_1', { win: true }),
        makeMatch('MATCH_2', { win: false }),
        makeMatch('MATCH_3', { win: true, gameDurationSec: 200 })
      ]))
      const wrapper = mountPage()
      await flushPromises()

      expect(wrapper.findAll('[data-testid="match-row"]')).toHaveLength(3)
      expect(wrapper.get('[data-testid="matches-headline"]').text()).toBe('1 win in your last 2')
      expect(wrapper.findAll('[data-testid="match-row-result"]').map((r) => r.text())).toEqual(['Victory', 'Defeat', 'Remake'])
    })

    it('shows the list as a FormStrip beside the headline, oldest first', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([
        makeMatch('MATCH_1', { win: true }),
        makeMatch('MATCH_2', { win: false }),
        makeMatch('MATCH_3', { win: true, gameDurationSec: 200 })
      ]))
      const wrapper = mountPage()
      await flushPromises()

      const bars = wrapper.findAll('[data-testid="form-strip-bar"]')
      expect(bars.map((b) => b.attributes('data-result'))).toEqual(['remake', 'loss', 'win'])
    })

    it('adds the win rate by start time under the list when two groups can be compared', async () => {
      const at = (id, hour, win) => makeMatch(id, { win, gameStartTime: new Date(2026, 8, 27, hour, 30).getTime() })
      mockGetMatchList.mockResolvedValue(listResponse([
        at('M1', 14, true), at('M2', 15, true), at('M3', 16, true),
        at('M4', 23, false), at('M5', 0, false), at('M6', 1, true)
      ]))
      const wrapper = mountPage()
      await flushPromises()

      expect(wrapper.get('[data-testid="matches-hours-title"]').text()).toBe('You lose most after 11pm')
      expect(wrapper.get('[data-testid="column-chart"]').attributes('aria-label'))
        .toBe('Win rate by start time: afternoon 100 percent, after 11pm 33 percent.')
    })

    it('leaves the start-time chart out when there is too little to compare', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1'), makeMatch('MATCH_2')]))
      const wrapper = mountPage()
      await flushPromises()

      expect(wrapper.find('[data-testid="matches-hours"]').exists()).toBe(false)
    })

    it('links each row to its match URL and tracks the pick', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1'), makeMatch('MATCH_2')]))
      const wrapper = mountPage()
      await flushPromises()

      const links = wrapper.findAllComponents(RouterLinkStub).filter((l) => l.attributes('data-testid') === 'match-row')
      expect(links[1].props('to')).toEqual({ name: 'app-matches', params: { matchId: 'MATCH_2' } })

      await links[1].trigger('click')
      expect(mockTrackMatchSelect).toHaveBeenCalledWith('MATCH_2', 1, 'all')
    })

    it('shows the Riot ID on rows in Overall mode', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([
        makeMatch('EUW_1', { accountGameName: 'FakerMain', accountTagLine: 'EUW', accountRegion: 'euw1' })
      ]))
      const wrapper = mountPage()
      await flushPromises()
      expect(wrapper.get('[data-testid="match-row-meta"]').text()).toContain('FakerMain#EUW')
    })
  })

  describe('opening a match', () => {
    it('opens the newest match beside the list on desktop', async () => {
      setDesktop(true)
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1'), makeMatch('MATCH_2')]))
      mountPage()
      await flushPromises()
      expect(mockReplace).toHaveBeenCalledWith({ name: 'app-matches', params: { matchId: 'MATCH_1' } })
      expect(mockGetMatchDetails).toHaveBeenCalledWith('MATCH_1', 'acc_primary')
    })

    it('keeps the list as the page on phones', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1')]))
      const wrapper = mountPage()
      await flushPromises()
      expect(mockReplace).not.toHaveBeenCalled()
      expect(wrapper.get('[data-testid="matches-page"]').classes()).not.toContain('matches-page--open')
    })

    it('loads the match in the URL and passes its standout finding', async () => {
      const badge = { text: 'Clean game', type: 'positive' }
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1', { trendBadge: badge })]))
      mockRoute.params = { matchId: 'MATCH_1' }
      mockGetMatchDetails.mockResolvedValue({ match: { matchId: 'MATCH_1' }, baseline: { role: 'MIDDLE' } })
      const wrapper = mountPage()
      await flushPromises()

      const details = wrapper.findComponent(MatchDetailsStub)
      expect(mockGetMatchDetails).toHaveBeenCalledWith('MATCH_1', 'acc_primary')
      expect(details.props('match')).toEqual({ matchId: 'MATCH_1' })
      expect(details.props('baseline')).toEqual({ role: 'MIDDLE' })
      expect(details.props('badge')).toEqual(badge)
      expect(wrapper.get('[data-testid="matches-page"]').classes()).toContain('matches-page--open')
      expect(wrapper.findComponent(RouterLinkStub).exists()).toBe(true)
      expect(wrapper.find('[data-testid="matches-back"]').exists()).toBe(true)
    })

    it('reports a missing match as not found', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1')]))
      mockGetMatchDetails.mockResolvedValue(null)
      const wrapper = mountPage()
      await flushPromises()
      await openMatch('MATCH_1')
      expect(wrapper.findComponent(MatchDetailsStub).props('error')).toBe('not-found')
    })

    it('reports a failed request and retries it', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1')]))
      mockGetMatchDetails.mockRejectedValueOnce(new Error('boom'))
      const wrapper = mountPage()
      await flushPromises()
      await openMatch('MATCH_1')

      const details = wrapper.findComponent(MatchDetailsStub)
      expect(details.props('error')).toBe('failed')

      mockGetMatchDetails.mockResolvedValueOnce({ match: { matchId: 'MATCH_1' }, baseline: null })
      details.vm.$emit('retry')
      await flushPromises()
      expect(wrapper.findComponent(MatchDetailsStub).props('error')).toBeNull()
      expect(wrapper.findComponent(MatchDetailsStub).props('match')).toEqual({ matchId: 'MATCH_1' })
    })

    it('ignores a stale answer when another match was opened meanwhile', async () => {
      let resolveFirst
      mockGetMatchDetails
        .mockReturnValueOnce(new Promise((resolve) => { resolveFirst = resolve }))
        .mockResolvedValue({ match: { matchId: 'MATCH_2' }, baseline: null })
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1'), makeMatch('MATCH_2')]))
      const wrapper = mountPage()
      await flushPromises()

      await openMatch('MATCH_1')
      await openMatch('MATCH_2')
      resolveFirst({ match: { matchId: 'MATCH_1' }, baseline: null })
      await flushPromises()

      expect(wrapper.findComponent(MatchDetailsStub).props('match')).toEqual({ matchId: 'MATCH_2' })
    })
  })

  describe('account resolution in Overall mode', () => {
    it('resolves the account from the match Riot ID, case-insensitively', async () => {
      mockIsOverallMode.value = true
      mockRiotAccounts.value = [
        { gameName: 'FakerMain', tagLine: 'EUW', region: 'EUW1', accountId: 'acc_faker', puuid: 'puuid-faker' }
      ]
      mockGetMatchList.mockResolvedValue(listResponse([
        makeMatch('EUW_002', { accountGameName: 'fakermain', accountTagLine: 'euw', accountRegion: 'euw1' })
      ]))
      mountPage()
      await flushPromises()
      await openMatch('EUW_002')
      expect(mockGetMatchDetails).toHaveBeenCalledWith('EUW_002', 'acc_faker')
    })

    it('waits for the list before loading a match opened from the URL', async () => {
      mockIsOverallMode.value = true
      mockRiotAccounts.value = [
        { gameName: 'FakerMain', tagLine: 'EUW', region: 'euw1', accountId: 'acc_faker', puuid: 'puuid-faker' }
      ]
      mockRoute.params = { matchId: 'EUW_001' }
      mockGetMatchList.mockResolvedValue(listResponse([
        makeMatch('EUW_001', { accountGameName: 'FakerMain', accountTagLine: 'EUW', accountRegion: 'euw1' })
      ]))
      mountPage()
      await flushPromises()
      expect(mockGetMatchDetails).toHaveBeenCalledTimes(1)
      expect(mockGetMatchDetails).toHaveBeenCalledWith('EUW_001', 'acc_faker')
    })

    it('still loads the open match when the list request fails', async () => {
      mockIsOverallMode.value = true
      mockRoute.params = { matchId: 'EUW_009' }
      mockGetMatchList.mockRejectedValueOnce(new Error('list down'))
      const wrapper = mountPage()
      await flushPromises()

      expect(wrapper.find('[data-testid="matches-list-error"]').exists()).toBe(true)
      expect(mockGetMatchDetails).toHaveBeenCalledWith('EUW_009', 'acc_primary')
    })

    it('falls back to the primary account when no linked account matches', async () => {
      mockIsOverallMode.value = true
      mockActiveAccount.value = null
      mockPrimaryRiotAccount.value = { accountId: 'acc_fallback', puuid: 'puuid-fallback' }
      mockGetMatchList.mockResolvedValue(listResponse([
        makeMatch('MATCH_B', { accountGameName: 'UnknownSmurf', accountTagLine: 'NA1', accountRegion: 'na1' })
      ]))
      mountPage()
      await flushPromises()
      await openMatch('MATCH_B')
      expect(mockGetMatchDetails).toHaveBeenCalledWith('MATCH_B', 'acc_fallback')
    })
  })

  describe('reloading', () => {
    it('reloads, tracks and closes the open match when the queue filter changes', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([makeMatch('MATCH_1')]))
      mockRoute.params = { matchId: 'MATCH_1' }
      const wrapper = mountPage()
      await flushPromises()

      await wrapper.get('[data-testid="queue-ranked_solo"]').trigger('click')
      await flushPromises()

      expect(mockTrackFilterChange).toHaveBeenCalledWith('queue', 'ranked_solo')
      expect(mockGetMatchList).toHaveBeenLastCalledWith(1, 'ranked_solo')
      expect(mockReplace).toHaveBeenCalledWith({ name: 'app-matches' })
    })

    it('shows the newest queue when an older list response arrives last', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([]))
      const wrapper = mountPage()
      await flushPromises()

      let resolveSolo
      mockGetMatchList
        .mockReturnValueOnce(new Promise((resolve) => { resolveSolo = resolve }))
        .mockResolvedValueOnce(listResponse([makeMatch('FLEX_1', { championName: 'Syndra' })]))

      await wrapper.get('[data-testid="queue-ranked_solo"]').trigger('click')
      await wrapper.get('[data-testid="queue-ranked_flex"]').trigger('click')
      await flushPromises()
      resolveSolo(listResponse([makeMatch('SOLO_1'), makeMatch('SOLO_2')]))
      await flushPromises()

      const champions = wrapper.findAll('[data-testid="match-row-champion"]').map((c) => c.text())
      expect(champions).toEqual(['Syndra'])
    })

    it('reloads the list when the active account changes', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([]))
      mountPage()
      await flushPromises()

      mockGetMatchList.mockClear()
      mockActiveAccountPuuid.value = 'acc_alt'
      await flushPromises()
      expect(mockGetMatchList).toHaveBeenCalledOnce()
    })

    it('reloads the list when a sync finishes', async () => {
      mockGetMatchList.mockResolvedValue(listResponse([]))
      mountPage()
      await flushPromises()

      mockGetMatchList.mockClear()
      mockSyncState.value = 'running'
      await flushPromises()
      mockSyncState.value = 'done'
      await flushPromises()
      expect(mockGetMatchList).toHaveBeenCalledOnce()
    })
  })
})
