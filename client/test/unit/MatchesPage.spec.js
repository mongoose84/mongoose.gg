import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { ref } from 'vue'
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

vi.mock('vue-router', () => ({
  useRoute: () => ({ query: {} })
}))

// ── Auth store mock — reactive so watchers fire correctly ──────────────────────
const mockIsOverallMode = ref(false)
const mockActiveAccountPuuid = ref('acc_primary')
const mockActiveAccount = ref({ accountId: 'acc_primary', puuid: 'puuid-primary' })
const mockRiotAccounts = ref([])
const mockPrimaryRiotAccount = ref({ accountId: 'acc_primary', puuid: 'puuid-primary' })

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    get isOverallMode() { return mockIsOverallMode.value },
    get activeAccountPuuid() { return mockActiveAccountPuuid.value },
    get activeAccount() { return mockActiveAccount.value },
    get riotAccounts() { return mockRiotAccounts.value },
    get primaryRiotAccount() { return mockPrimaryRiotAccount.value }
  })
}))

// ── Component stubs ────────────────────────────────────────────────────────────
const MatchListStub = {
  name: 'MatchList',
  props: ['matches', 'selectedMatchId', 'loading'],
  emits: ['select'],
  template: `
    <div data-testid="match-list-stub">
      <button
        v-for="m in matches"
        :key="m.matchId"
        :data-testid="'select-' + m.matchId"
        @click="$emit('select', m.matchId)"
      >{{ m.matchId }}</button>
    </div>
  `
}

const BaseQueueToggleStub = {
  name: 'BaseQueueToggle',
  props: ['modelValue'],
  emits: ['update:modelValue'],
  template: '<div data-testid="queue-toggle-stub"></div>'
}

const MatchDetailsStub = {
  name: 'MatchDetails',
  props: ['match', 'baseline', 'loading', 'error'],
  template: '<div data-testid="match-details-stub"></div>'
}

const pageStubs = {
  MatchList: MatchListStub,
  MatchDetails: MatchDetailsStub,
  BaseQueueToggle: BaseQueueToggleStub
}

// ── Helpers ────────────────────────────────────────────────────────────────────
function makeMatchListResponse(matches) {
  return { matches, totalMatches: matches.length, queueType: 'all' }
}

let currentWrapper = null

function mountPage() {
  currentWrapper = mount(MatchesPage, { global: { stubs: pageStubs } })
  return currentWrapper
}

// ── Tests ──────────────────────────────────────────────────────────────────────
describe('MatchesPage.vue — MA-07 Overall Mode', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockIsOverallMode.value = false
    mockActiveAccountPuuid.value = 'acc_primary'
    mockActiveAccount.value = { accountId: 'acc_primary', puuid: 'puuid-primary' }
    mockRiotAccounts.value = []
    mockPrimaryRiotAccount.value = { accountId: 'acc_primary', puuid: 'puuid-primary' }
    mockGetMatchDetails.mockResolvedValue({ match: { matchId: 'MATCH_1' }, baseline: null })
  })

  afterEach(() => {
    if (currentWrapper) {
      currentWrapper.unmount()
      currentWrapper = null
    }
  })

  // ── Account-switch watcher ─────────────────────────────────────────────────
  describe('activeAccountPuuid watcher', () => {
    it('fetches matches on mount', async () => {
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([]))
      mountPage()
      await flushPromises()
      expect(mockGetMatchList).toHaveBeenCalledTimes(1)
    })

    it('re-fetches match list when active account changes', async () => {
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([]))
      mountPage()
      await flushPromises()

      mockGetMatchList.mockClear()
      mockActiveAccountPuuid.value = 'acc_alt'
      await flushPromises()

      expect(mockGetMatchList).toHaveBeenCalledOnce()
    })

    it('clears selected match and details when active account changes', async () => {
      const match = { matchId: 'MATCH_1', accountGameName: null, accountTagLine: null, accountRegion: null }
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([match]))
      const wrapper = mountPage()
      await flushPromises()

      // Select a match to set state
      await wrapper.find('[data-testid="select-MATCH_1"]').trigger('click')
      await flushPromises()

      // Switch account
      mockGetMatchList.mockClear()
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([]))
      mockActiveAccountPuuid.value = 'acc_alt'
      await flushPromises()

      // selectedMatchId is cleared — MatchList receives null
      expect(wrapper.findComponent(MatchListStub).props('selectedMatchId')).toBeNull()
      // matchDetails is cleared — MatchDetails receives null
      expect(wrapper.findComponent(MatchDetailsStub).props('match')).toBeNull()
      // match list was re-fetched for the new account
      expect(mockGetMatchList).toHaveBeenCalledOnce()
    })
  })

  // ── Account resolution for match details ───────────────────────────────────
  describe('account resolution in getMatchDetailsAccountId', () => {
    it('uses activeAccount.accountId directly in single-account mode', async () => {
      mockIsOverallMode.value = false
      mockActiveAccount.value = { accountId: 'acc_single', puuid: 'puuid-single' }
      const match = { matchId: 'MATCH_A', accountGameName: null, accountTagLine: null, accountRegion: null }
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([match]))
      const wrapper = mountPage()
      await flushPromises()

      await wrapper.find('[data-testid="select-MATCH_A"]').trigger('click')
      await flushPromises()

      expect(mockGetMatchDetails).toHaveBeenCalledWith('MATCH_A', 'acc_single')
    })

    it('resolves accountId from accountGameName/tagLine/region in overall mode', async () => {
      mockIsOverallMode.value = true
      mockRiotAccounts.value = [
        { gameName: 'FakerMain', tagLine: 'EUW', region: 'euw1', accountId: 'acc_faker', puuid: 'puuid-faker' }
      ]
      const match = {
        matchId: 'EUW_001',
        accountGameName: 'FakerMain',
        accountTagLine: 'EUW',
        accountRegion: 'euw1'
      }
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([match]))
      const wrapper = mountPage()
      await flushPromises()

      await wrapper.find('[data-testid="select-EUW_001"]').trigger('click')
      await flushPromises()

      expect(mockGetMatchDetails).toHaveBeenCalledWith('EUW_001', 'acc_faker')
    })

    it('account field lookup is case-insensitive', async () => {
      mockIsOverallMode.value = true
      mockRiotAccounts.value = [
        { gameName: 'FakerMain', tagLine: 'EUW', region: 'EUW1', accountId: 'acc_faker', puuid: 'puuid-faker' }
      ]
      const match = {
        matchId: 'EUW_002',
        accountGameName: 'fakermain',
        accountTagLine: 'euw',
        accountRegion: 'euw1'
      }
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([match]))
      const wrapper = mountPage()
      await flushPromises()

      await wrapper.find('[data-testid="select-EUW_002"]').trigger('click')
      await flushPromises()

      expect(mockGetMatchDetails).toHaveBeenCalledWith('EUW_002', 'acc_faker')
    })

    it('falls back to primaryRiotAccount.accountId when no store account matches in overall mode', async () => {
      mockIsOverallMode.value = true
      mockActiveAccount.value = null
      mockRiotAccounts.value = [] // no accounts in store to match
      mockPrimaryRiotAccount.value = { accountId: 'acc_fallback', puuid: 'puuid-fallback' }
      const match = {
        matchId: 'MATCH_B',
        accountGameName: 'UnknownSmurf',
        accountTagLine: 'NA1',
        accountRegion: 'na1'
      }
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([match]))
      const wrapper = mountPage()
      await flushPromises()

      await wrapper.find('[data-testid="select-MATCH_B"]').trigger('click')
      await flushPromises()

      expect(mockGetMatchDetails).toHaveBeenCalledWith('MATCH_B', 'acc_fallback')
    })
  })

  // ── Account tags passed through ────────────────────────────────────────────
  describe('account tag fields passed through to MatchList', () => {
    it('passes accountGameName and accountRegion from overall-mode match list to MatchList', async () => {
      const matches = [
        { matchId: 'EUW_001', accountGameName: 'FakerMain', accountTagLine: 'EUW', accountRegion: 'euw1' },
        { matchId: 'NA1_002', accountGameName: 'SmurfAcc', accountTagLine: 'NA1', accountRegion: 'na1' }
      ]
      mockGetMatchList.mockResolvedValue(makeMatchListResponse(matches))
      const wrapper = mountPage()
      await flushPromises()

      const matchListStub = wrapper.findComponent(MatchListStub)
      const passedMatches = matchListStub.props('matches')
      expect(passedMatches[0].accountGameName).toBe('FakerMain')
      expect(passedMatches[1].accountGameName).toBe('SmurfAcc')
    })

    it('passes null account fields when in single-account mode', async () => {
      const matches = [
        { matchId: 'MATCH_1', accountGameName: null, accountTagLine: null, accountRegion: null }
      ]
      mockGetMatchList.mockResolvedValue(makeMatchListResponse(matches))
      const wrapper = mountPage()
      await flushPromises()

      const matchListStub = wrapper.findComponent(MatchListStub)
      const passedMatches = matchListStub.props('matches')
      expect(passedMatches[0].accountGameName).toBeNull()
    })
  })

  // ── Queue filter watcher ───────────────────────────────────────────────────
  describe('queue filter watcher', () => {
    it('re-fetches match list when queue filter changes', async () => {
      mockGetMatchList.mockResolvedValue(makeMatchListResponse([]))
      const wrapper = mountPage()
      await flushPromises()

      const toggle = wrapper.findComponent(BaseQueueToggleStub)
      await toggle.vm.$emit('update:modelValue', 'ranked_solo')
      await flushPromises()

      expect(mockGetMatchList).toHaveBeenCalledTimes(2)
      expect(mockTrackFilterChange).toHaveBeenCalledWith('queue', 'ranked_solo')
    })
  })

  // ── Race condition guard ───────────────────────────────────────────────────
  describe('race condition guard', () => {
    it('ignores stale match detail responses when a newer match is already selected', async () => {
      let resolveFirst
      const firstDetailsPromise = new Promise(resolve => { resolveFirst = resolve })
      mockGetMatchDetails
        .mockReturnValueOnce(firstDetailsPromise)
        .mockResolvedValue({ match: { matchId: 'MATCH_2' }, baseline: null })

      const matches = [
        { matchId: 'MATCH_1', accountGameName: null, accountTagLine: null, accountRegion: null },
        { matchId: 'MATCH_2', accountGameName: null, accountTagLine: null, accountRegion: null }
      ]
      mockGetMatchList.mockResolvedValue(makeMatchListResponse(matches))
      const wrapper = mountPage()
      await flushPromises()

      // Select first match (slow response not yet resolved)
      await wrapper.find('[data-testid="select-MATCH_1"]').trigger('click')

      // Immediately select second match before first resolves
      await wrapper.find('[data-testid="select-MATCH_2"]').trigger('click')
      await flushPromises()

      // Now resolve the first (stale) request
      resolveFirst({ match: { matchId: 'MATCH_1' }, baseline: null })
      await flushPromises()

      // The details panel should show MATCH_2, not MATCH_1
      expect(mockGetMatchDetails).toHaveBeenCalledTimes(2)
    })
  })
})
