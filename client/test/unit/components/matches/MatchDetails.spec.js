import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import MatchDetails from '@/components/matches/MatchDetails.vue'

vi.mock('@/components/matches/MatchHeader.vue', () => ({
  default: {
    name: 'MatchHeader',
    props: ['match', 'badge'],
    template: '<div data-testid="match-header" />'
  }
}))

vi.mock('@/components/matches/TeamComparison.vue', () => ({
  default: {
    name: 'TeamComparison',
    props: ['match'],
    template: '<div data-testid="team-comparison" />'
  }
}))

vi.mock('@/components/matches/WinPredictionStats.vue', () => ({
  default: {
    name: 'WinPredictionStats',
    props: ['match', 'baseline'],
    template: '<div data-testid="win-prediction-stats" />'
  }
}))

vi.mock('@/components/matches/StatSnapshot.vue', () => ({
  default: {
    name: 'StatSnapshot',
    props: ['match', 'baseline'],
    emits: ['download'],
    template: `<div data-testid="stat-snapshot" @click="$emit('download')" />`
  }
}))

vi.mock('@/components/matches/MatchNarrative.vue', () => ({
  default: {
    name: 'MatchNarrative',
    props: ['matchId', 'accountId'],
    template: '<div data-testid="match-narrative" />'
  }
}))

vi.mock('@/components/matches/MatchActions.vue', () => ({
  default: {
    name: 'MatchActions',
    props: ['match'],
    template: '<div data-testid="match-actions" />'
  }
}))

vi.mock('@/services/analyticsApi', () => ({
  trackMatchDetailsView: vi.fn()
}))

import { trackMatchDetailsView } from '@/services/analyticsApi'

describe('MatchDetails.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  const baseMatch = {
    matchId: 'EUW1_123',
    win: true,
    role: 'MIDDLE',
    championName: 'Ahri',
    kills: 5,
    deaths: 2,
    assists: 8
  }

  const createWrapper = (props = {}) => mount(MatchDetails, { props })

  describe('Loading state', () => {
    it('shows the skeleton frame and nothing else while loading', () => {
      const wrapper = createWrapper({ loading: true, match: baseMatch, error: 'failed' })
      expect(wrapper.find('[data-testid="match-details-loading"]').attributes('aria-busy')).toBe('true')
      expect(wrapper.text()).toContain('Loading the match')
      expect(wrapper.find('.details-content').exists()).toBe(false)
      expect(wrapper.find('.error-state').exists()).toBe(false)
    })
  })

  describe('Error states', () => {
    it('offers a retry when the request failed', async () => {
      const wrapper = createWrapper({ error: 'failed' })
      expect(wrapper.get('[role="alert"]').text()).toContain("We couldn't load this match")
      await wrapper.get('[data-testid="match-details-retry"]').trigger('click')
      expect(wrapper.emitted('retry')).toHaveLength(1)
    })

    it('explains a match that was not found', () => {
      const wrapper = createWrapper({ error: 'not-found' })
      expect(wrapper.get('[data-testid="match-details-unavailable"]').text()).toContain("We couldn't find this match")
      expect(wrapper.find('[data-testid="match-details-retry"]').exists()).toBe(false)
    })

    it('explains a match with no linked account to read it from', () => {
      const wrapper = createWrapper({ error: 'no-account' })
      expect(wrapper.get('[data-testid="match-details-unavailable"]').text()).toContain('Riot account')
    })
  })

  describe('Empty state', () => {
    it('asks the player to pick a match when none is open', () => {
      const wrapper = createWrapper()
      expect(wrapper.get('[data-testid="match-details-empty"]').text()).toContain('Pick a match to see how it went')
    })
  })

  describe('Download', () => {
    it('downloads the match as a JSON file when All your stats asks for it', async () => {
      const createObjectURL = vi.fn(() => 'blob:match')
      const revokeObjectURL = vi.fn()
      globalThis.URL.createObjectURL = createObjectURL
      globalThis.URL.revokeObjectURL = revokeObjectURL
      const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})

      const match = {
        ...baseMatch,
        championId: 103, lane: 'MIDDLE', queueType: 'Ranked Solo', queueId: 420,
        gameDurationSec: 1800, gameStartTime: Date.now(), killParticipation: 60, damageDealt: 20000,
        damageShare: 28, damageTaken: 15000, creepScore: 200, csPerMin: 6.7, goldEarned: 12000,
        goldPerMin: 400, visionScore: 20, deathsPre10: 0, goldDiffAt15: 300
      }
      const wrapper = createWrapper({ match })
      await wrapper.get('[data-testid="stat-snapshot"]').trigger('click')

      expect(createObjectURL).toHaveBeenCalledOnce()
      expect(click).toHaveBeenCalledOnce()
      expect(revokeObjectURL).toHaveBeenCalledWith('blob:match')
      click.mockRestore()
    })
  })

  describe('Content state', () => {
    it('renders MatchHeader when match data is present', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('[data-testid="match-header"]').exists()).toBe(true)
    })

    it('renders TeamComparison', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('[data-testid="team-comparison"]').exists()).toBe(true)
    })

    it('does not render legacy impact-stats marker', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('[data-testid="impact-stats"]').exists()).toBe(false)
    })

    it('renders WinPredictionStats', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('[data-testid="win-prediction-stats"]').exists()).toBe(true)
    })

    it('WinPredictionStats appears before TeamComparison in DOM', () => {
      const wrapper = createWrapper({ match: baseMatch })
      const testIds = wrapper.findAll('[data-testid]').map(el => el.attributes('data-testid'))
      const wpIndex = testIds.indexOf('win-prediction-stats')
      const tcIndex = testIds.indexOf('team-comparison')
      expect(wpIndex).toBeGreaterThanOrEqual(0)
      expect(tcIndex).toBeGreaterThanOrEqual(0)
      expect(wpIndex).toBeLessThan(tcIndex)
    })

    it('passes match to WinPredictionStats', () => {
      const wrapper = createWrapper({ match: baseMatch })
      const winPred = wrapper.findComponent({ name: 'WinPredictionStats' })
      expect(winPred.props('match')).toEqual(baseMatch)
    })

    it('passes baseline to WinPredictionStats', () => {
      const baseline = { gamesCount: 10, avgKda: 3.0 }
      const wrapper = createWrapper({ match: baseMatch, baseline })
      const winPred = wrapper.findComponent({ name: 'WinPredictionStats' })
      expect(winPred.props('baseline')).toEqual(baseline)
    })

    it('renders StatSnapshot', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('[data-testid="stat-snapshot"]').exists()).toBe(true)
    })

    it('renders MatchNarrative', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('[data-testid="match-narrative"]').exists()).toBe(true)
    })

    it('renders MatchActions', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('[data-testid="match-actions"]').exists()).toBe(true)
    })

    it('hides loading state when match data is present', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('.loading-state').exists()).toBe(false)
    })

    it('hides empty state when match data is present', () => {
      const wrapper = createWrapper({ match: baseMatch })
      expect(wrapper.find('.empty-state').exists()).toBe(false)
    })

    it('passes the standout finding to MatchHeader', () => {
      const badge = { text: 'Clean game', type: 'positive' }
      const wrapper = createWrapper({ match: baseMatch, badge })
      expect(wrapper.findComponent({ name: 'MatchHeader' }).props('badge')).toEqual(badge)
    })

    it('passes accountId prop down to MatchNarrative', () => {
      const wrapper = createWrapper({ match: baseMatch, accountId: 'acc-42' })
      const narrative = wrapper.findComponent({ name: 'MatchNarrative' })
      expect(narrative.props('accountId')).toBe('acc-42')
    })

    it('passes matchId to MatchNarrative', () => {
      const wrapper = createWrapper({ match: baseMatch })
      const narrative = wrapper.findComponent({ name: 'MatchNarrative' })
      expect(narrative.props('matchId')).toBe(baseMatch.matchId)
    })

    it('passes baseline to StatSnapshot', () => {
      const baseline = { gamesCount: 10, avgKda: 3.0 }
      const wrapper = createWrapper({ match: baseMatch, baseline })
      const snapshot = wrapper.findComponent({ name: 'StatSnapshot' })
      expect(snapshot.props('baseline')).toEqual(baseline)
    })
  })

  describe('Analytics tracking', () => {
    it('calls trackMatchDetailsView when match is provided', async () => {
      createWrapper({ match: baseMatch })
      await flushPromises()
      expect(trackMatchDetailsView).toHaveBeenCalledWith(
        baseMatch.matchId,
        baseMatch.role,
        baseMatch.win
      )
    })

    it('does not call trackMatchDetailsView when match is null', async () => {
      createWrapper({ match: null })
      await flushPromises()
      expect(trackMatchDetailsView).not.toHaveBeenCalled()
    })
  })
})
