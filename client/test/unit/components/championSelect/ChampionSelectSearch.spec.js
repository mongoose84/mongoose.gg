import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChampionSelectSearch from '@/components/championSelect/ChampionSelectSearch.vue'

const matchups = [
  {
    championId: 103,
    championName: 'Ahri',
    role: 'MIDDLE',
    opponents: [
      { opponentChampionId: 238, opponentChampionName: 'Zed', inLaneWins: 3, inLaneLosses: 1, outOfLaneWins: 1, outOfLaneLosses: 1 },
      { opponentChampionId: 91, opponentChampionName: 'Talon', inLaneWins: 0, inLaneLosses: 0, outOfLaneWins: 0, outOfLaneLosses: 2 }
    ]
  },
  {
    championId: 157,
    championName: 'Yasuo',
    role: 'TOP',
    opponents: [
      { opponentChampionId: 238, opponentChampionName: 'Zed', inLaneWins: 1, inLaneLosses: 0, outOfLaneWins: 0, outOfLaneLosses: 0 }
    ]
  }
]

function mountSearch(props = {}) {
  return mount(ChampionSelectSearch, {
    props: { matchups, role: 'MIDDLE', roleName: 'Mid', ...props }
  })
}

describe('ChampionSelectSearch', () => {
  it('has a labelled search field and no results before typing', () => {
    const wrapper = mountSearch()
    const input = wrapper.get('input')
    expect(wrapper.get(`label[for="${input.attributes('id')}"]`).text()).toBe('Enemy champion')
    expect(wrapper.find('[data-testid="matchup-search-results"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="matchup-search-status"]').exists()).toBe(false)
  })

  it('shows your champions in the role against the enemy with lane and overall records', async () => {
    const wrapper = mountSearch()
    await wrapper.get('input').setValue('zed')

    const rows = wrapper.findAll('[data-testid="matchup-search-row"]')
    expect(rows).toHaveLength(1)
    expect(rows[0].text()).toContain('Ahri vs Zed')
    expect(rows[0].get('[data-testid="matchup-search-record"]').text()).toBe('3–1 in lane · 4–2 overall')
    expect(rows[0].get('[data-testid="matchup-search-winrate"]').text()).toBe('67% win rate')
    expect(rows[0].get('[data-testid="matchup-search-winrate"]').classes()).toContain('mp-up')
    expect(wrapper.get('[data-testid="matchup-search-status"]').text()).toBe('1 matchup found')
  })

  it('shows only the overall record when you never met in lane', async () => {
    const wrapper = mountSearch()
    await wrapper.get('input').setValue('talon')
    expect(wrapper.get('[data-testid="matchup-search-record"]').text()).toBe('0–2 overall')
    expect(wrapper.get('[data-testid="matchup-search-winrate"]').classes()).toContain('mp-down')
  })

  it('says when you have not played the role against that champion', async () => {
    const wrapper = mountSearch()
    await wrapper.get('input').setValue('teemo')
    expect(wrapper.get('[data-testid="matchup-search-status"]').text()).toBe('You haven\'t played Mid against "teemo" yet.')
  })

  it('disables the field while matchups are unavailable', () => {
    expect(mountSearch({ disabled: true }).get('input').attributes('disabled')).toBeDefined()
  })
})
