import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import MatchNarrative from '@/components/matches/MatchNarrative.vue'

vi.mock('@/utils/formatters', () => ({
  formatRole: (role) => role,
  formatKdaFromParticipant: (p) => `${p.kills}/${p.deaths}/${p.assists}`,
  formatPercent: (n) => `${n != null ? n.toFixed(0) : 0}%`
}))

vi.mock('@/utils/leagueAssets', () => ({
  getRoleIconUrl: (role) => `/roles/${role}.png`
}))

vi.mock('@/services/matchesApi', () => ({
  getMatchNarrative: vi.fn()
}))

vi.mock('@/services/analyticsApi', () => ({
  trackLaneExpand: vi.fn()
}))

vi.mock('@/components/matches/LaneMatchupDetails.vue', () => ({
  default: {
    name: 'LaneMatchupDetails',
    props: ['matchup'],
    template: '<div data-testid="lane-matchup-details" />'
  }
}))

import { getMatchNarrative } from '@/services/matchesApi'

describe('MatchNarrative.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  const createWrapper = (props = {}) => mount(MatchNarrative, { props })

  describe('Pending / loading state', () => {
    it('shows loading state while network request is in-flight', async () => {
      getMatchNarrative.mockReturnValue(new Promise(() => {}))
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await wrapper.vm.$nextTick()
      expect(wrapper.find('.loading-state').exists()).toBe(true)
    })

    it('shows loading text', async () => {
      getMatchNarrative.mockReturnValue(new Promise(() => {}))
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await wrapper.vm.$nextTick()
      expect(wrapper.find('.loading-text').text()).toContain('Loading')
    })
  })

  describe('Error state', () => {
    it('shows error when accountId is not provided', async () => {
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: null })
      await flushPromises()
      expect(wrapper.find('.error-state').exists()).toBe(true)
    })

    it('shows "No linked Riot account" message when accountId is missing', async () => {
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: null })
      await flushPromises()
      expect(wrapper.find('.error-text').text()).toContain('No linked Riot account')
    })

    it('shows error state when API call rejects', async () => {
      getMatchNarrative.mockRejectedValue(new Error('Network error'))
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.error-state').exists()).toBe(true)
    })

    it('shows the API error message', async () => {
      getMatchNarrative.mockRejectedValue(new Error('Network error'))
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.error-text').text()).toContain('Network error')
    })
  })

  describe('Empty state', () => {
    it('shows empty state when matchId is null', async () => {
      const wrapper = createWrapper({ matchId: null, accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.empty-state').exists()).toBe(true)
    })

    it('shows empty state when API returns no lane matchups', async () => {
      getMatchNarrative.mockResolvedValue({
        userRole: 'MIDDLE',
        isAram: false,
        laneMatchups: []
      })
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.empty-state').exists()).toBe(true)
    })
  })

  describe('Lane matchup content', () => {
    const narrativeData = {
      userRole: 'MIDDLE',
      isAram: false,
      laneMatchups: [
        {
          role: 'MIDDLE',
          laneWinner: 'ally',
          allyParticipant: {
            championId: 1,
            championName: 'Ahri',
            championIconUrl: '/ahri.png',
            kills: 5,
            deaths: 2,
            assists: 4,
            damageShare: 28,
            killParticipation: 50,
            visionScore: 20,
            goldDiffAt10: 400,
            csDiffAt10: 15,
            deathsPre10: 0,
            isUserParticipant: true
          },
          enemyParticipant: {
            championId: 2,
            championName: 'Zed',
            championIconUrl: '/zed.png',
            kills: 3,
            deaths: 5,
            assists: 2,
            damageShare: 22,
            killParticipation: 35,
            visionScore: 15,
            goldDiffAt10: -400,
            csDiffAt10: -15,
            deathsPre10: 1
          }
        }
      ]
    }

    it('renders lane matchup rows after data loads', async () => {
      getMatchNarrative.mockResolvedValue(narrativeData)
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.lane-matchups').exists()).toBe(true)
      expect(wrapper.findAll('.lane-row')).toHaveLength(1)
    })

    it('shows YOU badge for the user role', async () => {
      getMatchNarrative.mockResolvedValue(narrativeData)
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.you-badge').text()).toBe('YOU')
    })

    it('marks the user lane row with user-role class', async () => {
      getMatchNarrative.mockResolvedValue(narrativeData)
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.lane-row.user-role').exists()).toBe(true)
    })

    it('expands lane details when a lane row is clicked', async () => {
      getMatchNarrative.mockResolvedValue(narrativeData)
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      await wrapper.find('.lane-row').trigger('click')
      expect(wrapper.find('.lane-details').exists()).toBe(true)
    })

    it('collapses lane details when the same row is clicked again', async () => {
      getMatchNarrative.mockResolvedValue(narrativeData)
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      const row = wrapper.find('.lane-row')
      await row.trigger('click')
      await row.trigger('click')
      expect(wrapper.find('.lane-details').exists()).toBe(false)
    })

    it('does not show loading or error after successful fetch', async () => {
      getMatchNarrative.mockResolvedValue(narrativeData)
      const wrapper = createWrapper({ matchId: 'EUW1_1', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.loading-state').exists()).toBe(false)
      expect(wrapper.find('.error-state').exists()).toBe(false)
    })
  })

  describe('ARAM match', () => {
    const aramData = {
      userRole: 'UNKNOWN',
      isAram: true,
      laneMatchups: [
        {
          role: 'UNKNOWN',
          laneWinner: 'even',
          allyParticipant: {
            championId: 1,
            championName: 'Ahri',
            championIconUrl: '/ahri.png',
            kills: 5,
            deaths: 2,
            assists: 4,
            damageShare: 22,
            isUserParticipant: true
          },
          enemyParticipant: {
            championId: 2,
            championName: 'Zed',
            championIconUrl: '/zed.png',
            kills: 3,
            deaths: 5,
            assists: 2,
            damageShare: 18,
            isUserParticipant: false
          }
        }
      ]
    }

    it('renders aram-players container for ARAM matches', async () => {
      getMatchNarrative.mockResolvedValue(aramData)
      const wrapper = createWrapper({ matchId: 'EUW1_2', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.aram-players').exists()).toBe(true)
    })

    it('shows Your Team header for ARAM', async () => {
      getMatchNarrative.mockResolvedValue(aramData)
      const wrapper = createWrapper({ matchId: 'EUW1_2', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.team-header.ally').text()).toBe('Your Team')
    })

    it('shows Enemy Team header for ARAM', async () => {
      getMatchNarrative.mockResolvedValue(aramData)
      const wrapper = createWrapper({ matchId: 'EUW1_2', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.team-header.enemy').text()).toBe('Enemy Team')
    })

    it('marks user champion row with user-row class', async () => {
      getMatchNarrative.mockResolvedValue(aramData)
      const wrapper = createWrapper({ matchId: 'EUW1_2', accountId: 'acc-1' })
      await flushPromises()
      expect(wrapper.find('.aram-player-row.user-row').exists()).toBe(true)
    })
  })
})
