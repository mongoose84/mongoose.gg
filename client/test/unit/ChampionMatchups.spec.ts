import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ChampionMatchups from '@/components/solo/ChampionMatchups.vue'

// Mock the getChampionMatchups API call
vi.mock('@/api/solo.js', () => ({
  getChampionMatchups: vi.fn().mockResolvedValue({
    matchups: [
      {
        championName: 'Jinx',
        championId: 222,
        role: 'BOTTOM',
        totalGames: 25,
        totalWins: 15,
        winrate: 60.0,
        opponents: [
          { opponentChampionName: 'Caitlyn', gamesPlayed: 8, wins: 5, losses: 3, winrate: 62.5 },
          { opponentChampionName: 'Vayne', gamesPlayed: 6, wins: 4, losses: 2, winrate: 66.7 },
          { opponentChampionName: 'Draven', gamesPlayed: 5, wins: 2, losses: 3, winrate: 40.0 },
          { opponentChampionName: 'Lucian', gamesPlayed: 4, wins: 3, losses: 1, winrate: 75.0 },
          { opponentChampionName: 'Ezreal', gamesPlayed: 2, wins: 1, losses: 1, winrate: 50.0 }
        ]
      },
      {
        championName: 'Yasuo',
        championId: 157,
        role: 'MIDDLE',
        totalGames: 20,
        totalWins: 10,
        winrate: 50.0,
        opponents: [
          { opponentChampionName: 'Zed', gamesPlayed: 5, wins: 3, losses: 2, winrate: 60.0 },
          { opponentChampionName: 'Syndra', gamesPlayed: 4, wins: 2, losses: 2, winrate: 50.0 }
        ]
      },
      {
        championName: 'Darius',
        championId: 122,
        role: 'TOP',
        totalGames: 15,
        totalWins: 9,
        winrate: 60.0,
        opponents: [
          { opponentChampionName: 'Garen', gamesPlayed: 5, wins: 3, losses: 2, winrate: 60.0 }
        ]
      }
    ]
  })
}))

describe('ChampionMatchups', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('mounts without errors', () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('shows loading state initially', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    expect(wrapper.find('.champion-matchups-container').exists()).toBe(true)
  })

  it('renders matchups card after loading', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    expect(wrapper.find('.matchups-card').exists()).toBe(true)
    expect(wrapper.find('.card-title').text()).toBe('Champion Matchups')
  })

  it('renders role filter dropdown', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    const roleSelect = wrapper.find('#role-filter')
    expect(roleSelect.exists()).toBe(true)
    
    const options = roleSelect.findAll('option')
    expect(options.length).toBeGreaterThan(1)
    expect(options[0].text()).toBe('All Roles')
  })

  it('renders search input', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    const searchInput = wrapper.find('#opponent-search')
    expect(searchInput.exists()).toBe(true)
  })

  it('renders matchup cards with champion data', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    const matchupCards = wrapper.findAll('.matchup-card')
    expect(matchupCards.length).toBeGreaterThan(0)

    const firstCard = matchupCards[0]
    expect(firstCard.find('.champion-name').exists()).toBe(true)
    expect(firstCard.find('.role-badge').exists()).toBe(true)
  })

  it('filters matchups by role', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    const roleSelect = wrapper.find('#role-filter')
    await roleSelect.setValue('MIDDLE')
    await flushPromises()

    const matchupCards = wrapper.findAll('.matchup-card')
    // Should only show Yasuo (MIDDLE role)
    expect(matchupCards.length).toBe(1)
    expect(matchupCards[0].find('.champion-name').text()).toBe('Yasuo')
  })

  it('displays opponent rows', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    const opponentRows = wrapper.findAll('.opponent-row')
    expect(opponentRows.length).toBeGreaterThan(0)
  })

  it('shows expand button for matchups with many opponents', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    // Jinx has 5 opponents, should show expand button
    const expandBtn = wrapper.find('.expand-btn')
    expect(expandBtn.exists()).toBe(true)
    expect(expandBtn.text()).toContain('Show All')
  })

  it('handles empty data gracefully', async () => {
    const soloApi = await import('@/api/solo.js')
    vi.mocked(soloApi.getChampionMatchups).mockResolvedValueOnce({ matchups: [] })

    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    expect(wrapper.find('.matchups-empty').exists()).toBe(true)
    expect(wrapper.text()).toContain('No matchup data available')
  })

  it('handles API errors gracefully', async () => {
    const soloApi = await import('@/api/solo.js')
    vi.mocked(soloApi.getChampionMatchups).mockRejectedValueOnce(new Error('API Error'))

    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    expect(wrapper.find('.matchups-error').exists()).toBe(true)
    expect(wrapper.text()).toContain('API Error')
  })

  it('searches for opponents', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    const searchInput = wrapper.find('#opponent-search')
    await searchInput.setValue('Caitlyn')
    await flushPromises()

    // Should show search results
    expect(wrapper.find('.search-results').exists()).toBe(true)
    expect(wrapper.text()).toContain('Your Champions vs Caitlyn')
  })

  it('shows no results message for unknown opponent', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    const searchInput = wrapper.find('#opponent-search')
    await searchInput.setValue('UnknownChampion')
    await flushPromises()

    expect(wrapper.find('.no-results').exists()).toBe(true)
  })

  it('reloads data when userId prop changes', async () => {
    const soloApi = await import('@/api/solo.js')

    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()
    expect(soloApi.getChampionMatchups).toHaveBeenCalledTimes(1)

    await wrapper.setProps({ userId: 2 })
    await flushPromises()

    expect(soloApi.getChampionMatchups).toHaveBeenCalledTimes(2)
  })

  it('displays winrate with correct styling', async () => {
    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    const positiveWinrate = wrapper.find('.winrate.positive')
    expect(positiveWinrate.exists()).toBe(true)
  })

  it('shows low sample size warning', async () => {
    const soloApi = await import('@/api/solo.js')
    vi.mocked(soloApi.getChampionMatchups).mockResolvedValueOnce({
      matchups: [{
        championName: 'Lux',
        championId: 99,
        role: 'MIDDLE',
        totalGames: 3, // Low sample size
        totalWins: 2,
        winrate: 66.7,
        opponents: [{ opponentChampionName: 'Zed', gamesPlayed: 3, wins: 2, losses: 1, winrate: 66.7 }]
      }]
    })

    const wrapper = mount(ChampionMatchups, {
      props: { userId: 1 }
    })

    await flushPromises()

    expect(wrapper.find('.warning').exists()).toBe(true)
    expect(wrapper.text()).toContain('Low sample size')
  })
})

