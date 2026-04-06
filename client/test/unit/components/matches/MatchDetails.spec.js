import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import MatchDetails from '@/components/matches/MatchDetails.vue'

vi.mock('@/components/matches/MatchHeader.vue', () => ({
  default: {
    name: 'MatchHeader',
    props: ['match'],
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
    template: '<div data-testid="stat-snapshot" />'
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
    it('shows loading state when loading is true', () => {
      const wrapper = createWrapper({ loading: true })
      expect(wrapper.find('.loading-state').exists()).toBe(true)
    })

    it('shows loading text', () => {
      const wrapper = createWrapper({ loading: true })
      expect(wrapper.text()).toContain('Loading match details...')
    })

    it('hides details content when loading', () => {
      const wrapper = createWrapper({ loading: true, match: baseMatch })
      expect(wrapper.find('.details-content').exists()).toBe(false)
    })

    it('hides error state when loading', () => {
      const wrapper = createWrapper({ loading: true, error: 'Failed' })
      expect(wrapper.find('.error-state').exists()).toBe(false)
    })

    it('hides empty state when loading', () => {
      const wrapper = createWrapper({ loading: true })
      expect(wrapper.find('.empty-state').exists()).toBe(false)
    })
  })

  describe('Error state', () => {
    it('shows error state when error prop is set', () => {
      const wrapper = createWrapper({ error: 'Something went wrong' })
      expect(wrapper.find('.error-state').exists()).toBe(true)
    })

    it('displays the error message', () => {
      const wrapper = createWrapper({ error: 'Something went wrong' })
      expect(wrapper.find('.error-text').text()).toBe('Something went wrong')
    })

    it('hides details content when error is set', () => {
      const wrapper = createWrapper({ error: 'Failed', match: baseMatch })
      expect(wrapper.find('.details-content').exists()).toBe(false)
    })

    it('hides empty state when error is set', () => {
      const wrapper = createWrapper({ error: 'Failed' })
      expect(wrapper.find('.empty-state').exists()).toBe(false)
    })
  })

  describe('Empty state', () => {
    it('shows empty state when match is null and not loading', () => {
      const wrapper = createWrapper({ match: null })
      expect(wrapper.find('.empty-state').exists()).toBe(true)
    })

    it('shows empty state when no props are provided', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('.empty-state').exists()).toBe(true)
    })

    it('shows "Select a match to view details" message', () => {
      const wrapper = createWrapper()
      expect(wrapper.text()).toContain('Select a match to view details')
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
