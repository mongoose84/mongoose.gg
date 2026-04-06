import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MatchList from '@/components/matches/MatchList.vue'

vi.mock('@/components/matches/MatchRow.vue', () => ({
  default: {
    name: 'MatchRow',
    props: ['match', 'selected'],
    emits: ['select'],
    template: '<div class="mock-match-row" :data-match-id="match.matchId" @click="$emit(\'select\', match.matchId)"></div>'
  }
}))

describe('MatchList.vue', () => {
  const createWrapper = (props = {}) => mount(MatchList, { props })

  describe('Loading state', () => {
    it('shows loading state when loading is true', () => {
      const wrapper = createWrapper({ loading: true })
      expect(wrapper.find('.loading-state').exists()).toBe(true)
    })

    it('shows loading text', () => {
      const wrapper = createWrapper({ loading: true })
      expect(wrapper.find('.loading-text').text()).toBe('Loading matches...')
    })

    it('shows spinner element', () => {
      const wrapper = createWrapper({ loading: true })
      expect(wrapper.find('.loading-spinner').exists()).toBe(true)
    })

    it('hides matches container when loading', () => {
      const wrapper = createWrapper({ loading: true, matches: [{ matchId: 'A' }] })
      expect(wrapper.find('.matches-container').exists()).toBe(false)
    })

    it('hides empty state when loading', () => {
      const wrapper = createWrapper({ loading: true, matches: [] })
      expect(wrapper.find('.empty-state').exists()).toBe(false)
    })
  })

  describe('Empty state', () => {
    it('shows empty state when matches is an empty array', () => {
      const wrapper = createWrapper({ matches: [] })
      expect(wrapper.find('.empty-state').exists()).toBe(true)
    })

    it('shows empty state when matches prop is not provided', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('.empty-state').exists()).toBe(true)
    })

    it('shows "No matches found" message', () => {
      const wrapper = createWrapper({ matches: [] })
      expect(wrapper.find('.empty-text').text()).toBe('No matches found')
    })

    it('shows sub-message encouraging gameplay', () => {
      const wrapper = createWrapper({ matches: [] })
      expect(wrapper.find('.empty-subtext').text()).toContain('Play some games')
    })

    it('hides matches container in empty state', () => {
      const wrapper = createWrapper({ matches: [] })
      expect(wrapper.find('.matches-container').exists()).toBe(false)
    })
  })

  describe('Match list rendering', () => {
    const matches = [
      { matchId: 'EUW1_A', win: true, championName: 'Ahri' },
      { matchId: 'EUW1_B', win: false, championName: 'Zed' }
    ]

    it('renders a row for each match', () => {
      const wrapper = createWrapper({ matches })
      expect(wrapper.findAll('.mock-match-row')).toHaveLength(2)
    })

    it('hides empty state when matches are present', () => {
      const wrapper = createWrapper({ matches })
      expect(wrapper.find('.empty-state').exists()).toBe(false)
    })

    it('shows matches container when matches are present', () => {
      const wrapper = createWrapper({ matches })
      expect(wrapper.find('.matches-container').exists()).toBe(true)
    })

    it('passes selected=true to the matching row', () => {
      const wrapper = createWrapper({ matches, selectedMatchId: 'EUW1_A' })
      const rows = wrapper.findAllComponents({ name: 'MatchRow' })
      expect(rows[0].props('selected')).toBe(true)
    })

    it('passes selected=false to non-selected rows', () => {
      const wrapper = createWrapper({ matches, selectedMatchId: 'EUW1_A' })
      const rows = wrapper.findAllComponents({ name: 'MatchRow' })
      expect(rows[1].props('selected')).toBe(false)
    })

    it('passes no row as selected when selectedMatchId is null', () => {
      const wrapper = createWrapper({ matches, selectedMatchId: null })
      const rows = wrapper.findAllComponents({ name: 'MatchRow' })
      rows.forEach(row => expect(row.props('selected')).toBe(false))
    })
  })

  describe('Select event propagation', () => {
    it('emits select with matchId when a MatchRow emits select', async () => {
      const matches = [{ matchId: 'EUW1_99', win: true, championName: 'Jinx' }]
      const wrapper = createWrapper({ matches })
      await wrapper.find('.mock-match-row').trigger('click')
      expect(wrapper.emitted('select')).toBeTruthy()
      expect(wrapper.emitted('select')[0]).toEqual(['EUW1_99'])
    })
  })
})
