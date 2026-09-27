import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { ref } from 'vue'
import ChampionSelectPage from '@/views/ChampionSelectPage.vue'

const mockGetChampionSelectData = vi.fn()
const mockGetChampionMatchups = vi.fn()

const mockHasLinkedAccount = ref(true)
const mockActiveAccountPuuid = ref('acc_1')

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    userId: 1,
    isInitialized: true,
    refreshUser: vi.fn(),
    get hasLinkedAccount() {
      return mockHasLinkedAccount.value
    },
    get activeAccountPuuid() {
      return mockActiveAccountPuuid.value
    }
  })
}))

vi.mock('@/services/soloApi', () => ({
  getChampionSelectData: (...args) => mockGetChampionSelectData(...args),
  getChampionMatchups: (...args) => mockGetChampionMatchups(...args)
}))

function champion(championId, championName, role, winRate, gamesPlayed) {
  return { championId, championName, role, winRate, gamesPlayed, mScore: 60, avgKda: 3.4, avgCsPerMin: 7.2 }
}

const pageData = {
  gamesPlayed: 60,
  winRate: 55,
  mainChampions: [
    {
      role: 'TOP',
      champions: [champion(86, 'Garen', 'TOP', 58, 6)]
    },
    {
      role: 'MIDDLE',
      champions: [
        champion(103, 'Ahri', 'MIDDLE', 63.6, 22),
        champion(134, 'Syndra', 'MIDDLE', 55.6, 18),
        champion(112, 'Viktor', 'MIDDLE', 50, 10),
        champion(61, 'Orianna', 'MIDDLE', 48, 4)
      ]
    }
  ]
}

const matchupsData = {
  matchups: [
    {
      championId: 103,
      championName: 'Ahri',
      role: 'MIDDLE',
      opponents: [
        { opponentChampionId: 238, opponentChampionName: 'Zed', inLaneWins: 4, inLaneLosses: 1, outOfLaneWins: 0, outOfLaneLosses: 0 },
        { opponentChampionId: 7, opponentChampionName: 'LeBlanc', inLaneWins: 1, inLaneLosses: 3, outOfLaneWins: 0, outOfLaneLosses: 0 }
      ]
    },
    {
      championId: 134,
      championName: 'Syndra',
      role: 'MIDDLE',
      opponents: [
        { opponentChampionId: 238, opponentChampionName: 'Zed', inLaneWins: 0, inLaneLosses: 3, outOfLaneWins: 0, outOfLaneLosses: 0 }
      ]
    }
  ]
}

const mounted = []

function mountPage() {
  const wrapper = mount(ChampionSelectPage, {
    global: {
      stubs: {
        LinkRiotAccountModal: { props: ['isOpen'], template: '<div data-testid="link-modal" :data-open="isOpen" />' }
      }
    }
  })
  mounted.push(wrapper)
  return wrapper
}

describe('ChampionSelectPage', () => {
  beforeEach(() => {
    mockGetChampionSelectData.mockReset()
    mockGetChampionMatchups.mockReset()
    mockHasLinkedAccount.value = true
    mockActiveAccountPuuid.value = 'acc_1'
    mockGetChampionSelectData.mockResolvedValue(pageData)
    mockGetChampionMatchups.mockResolvedValue(matchupsData)
  })

  afterEach(() => {
    mounted.splice(0).forEach((wrapper) => wrapper.unmount())
  })

  it('shows the skeleton frame while loading', () => {
    mockGetChampionSelectData.mockReturnValue(new Promise(() => {}))
    const wrapper = mountPage()
    const loading = wrapper.get('[data-testid="champion-select-loading"]')
    expect(loading.attributes('aria-busy')).toBe('true')
    expect(loading.text()).toContain('Loading your champion picks')
  })

  it('opens on the best pick of your most-played role', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('h1').text()).toBe('Ahri is your best pick for Mid')
    expect(wrapper.get('[data-testid="champion-hero-player"]').text()).toBe('Mid · All queues · This season')
    expect(wrapper.get('[data-testid="champion-hero-text"]').text()).toBe(
      'You win 64% over 22 matches with a 3.4 KDA. Strongest into Zed (4–1 in lane), weakest into LeBlanc (1–3).'
    )
    expect(wrapper.findAll('[data-testid="champion-hero-chip"]').map((c) => c.text())).toEqual(['64% win rate', '7.2 CS per minute'])
    expect(wrapper.get('[data-testid="role-MIDDLE"]').attributes('aria-pressed')).toBe('true')
  })

  it('shows the top three picks as cards with the first one selected', async () => {
    const wrapper = mountPage()
    await flushPromises()

    const cards = wrapper.findAll('[data-testid="champion-card"]')
    expect(cards.map((c) => c.get('[data-testid="champion-card-name"]').text())).toEqual(['Ahri', 'Syndra', 'Viktor'])
    expect(cards.map((c) => c.attributes('aria-pressed'))).toEqual(['true', 'false', 'false'])
    expect(cards[0].get('[data-testid="champion-card-tag"]').text()).toBe('Best pick')
  })

  it('swaps the hero and matchups when you pick another card', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.findAll('[data-testid="champion-card"]')[1].trigger('click')

    expect(wrapper.get('h1').text()).toBe('Syndra is your #2 pick for Mid')
    expect(wrapper.findAll('[data-testid="champion-card"]')[1].attributes('aria-pressed')).toBe('true')
    const matchups = wrapper.get('[data-testid="champion-select-matchups"]')
    expect(matchups.get('h2').text()).toBe('Syndra in lane')
    expect(matchups.get('[data-testid="matchups-weak"]').text()).toContain('Zed')
  })

  it('switches role and resets to that role\'s best pick', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('[data-testid="champion-card"]')[1].trigger('click')

    await wrapper.get('[data-testid="role-TOP"]').trigger('click')

    expect(wrapper.get('h1').text()).toBe('Garen is your best pick for Top')
    expect(wrapper.findAll('[data-testid="champion-card"]')).toHaveLength(1)
    expect(wrapper.get('[data-testid="champion-hero-text"]').text()).toBe('You win 58% over 6 matches with a 3.4 KDA.')
  })

  it('refetches both requests when a filter changes', async () => {
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.get('[data-testid="queue-ranked_solo"]').trigger('click')
    await flushPromises()

    expect(mockGetChampionSelectData).toHaveBeenLastCalledWith(1, 'ranked_solo', 'current_season')
    expect(mockGetChampionMatchups).toHaveBeenLastCalledWith(1, 'ranked_solo', 'current_season')
    expect(wrapper.get('[data-testid="queue-ranked_solo"]').attributes('aria-pressed')).toBe('true')
  })

  it('shows an empty state that widens the filters', async () => {
    mockGetChampionSelectData.mockResolvedValue({ ...pageData, mainChampions: [] })
    const wrapper = mountPage()
    await flushPromises()

    const empty = wrapper.get('[data-testid="champion-select-empty"]')
    expect(empty.get('h1').text()).toBe('No champions for these filters yet')

    mockGetChampionSelectData.mockClear()
    await wrapper.get('[data-testid="champion-select-show-all"]').trigger('click')
    await flushPromises()
    expect(mockGetChampionSelectData).toHaveBeenLastCalledWith(1, 'all', 'all')
    expect(wrapper.find('[data-testid="champion-select-show-all"]').exists()).toBe(false)
  })

  it('shows an error with retry when the picks fail to load', async () => {
    mockGetChampionSelectData.mockRejectedValue(new Error('boom'))
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('[data-testid="champion-select-error"] [role="alert"]').exists()).toBe(true)

    mockGetChampionSelectData.mockResolvedValue(pageData)
    await wrapper.get('[data-testid="champion-select-retry"]').trigger('click')
    await flushPromises()
    expect(wrapper.get('h1').text()).toBe('Ahri is your best pick for Mid')
  })

  it('keeps the picks when only the matchups fail, with a retry in the matchups card', async () => {
    mockGetChampionMatchups.mockRejectedValue(new Error('boom'))
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.findAll('[data-testid="champion-card"]')).toHaveLength(3)
    expect(wrapper.find('[data-testid="matchups-error"]').exists()).toBe(true)
    expect(wrapper.get('#matchup-search-input').attributes('disabled')).toBeDefined()

    mockGetChampionMatchups.mockResolvedValue(matchupsData)
    await wrapper.get('[data-testid="matchups-retry"]').trigger('click')
    await flushPromises()
    expect(wrapper.find('[data-testid="matchups-error"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="matchups-strong"]').text()).toContain('Zed')
  })

  it('asks you to link a Riot account before loading anything', async () => {
    mockHasLinkedAccount.value = false
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('[data-testid="champion-select-no-account"] h1').text()).toBe('Link your Riot account to see your picks')
    expect(mockGetChampionSelectData).not.toHaveBeenCalled()
    await wrapper.get('[data-testid="champion-select-link-account"]').trigger('click')
    expect(wrapper.get('[data-testid="link-modal"]').attributes('data-open')).toBe('true')
  })

  it('refetches and resets the focus when the active account changes', async () => {
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.get('[data-testid="role-TOP"]').trigger('click')

    mockActiveAccountPuuid.value = 'acc_2'
    await flushPromises()

    expect(mockGetChampionSelectData).toHaveBeenCalledTimes(2)
    expect(wrapper.get('h1').text()).toBe('Ahri is your best pick for Mid')
  })
})
