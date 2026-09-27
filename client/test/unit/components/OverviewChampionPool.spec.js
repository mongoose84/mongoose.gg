import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import OverviewChampionPool from '@/components/overview/OverviewChampionPool.vue'

const champion = (id, name, overrides = {}) => ({
  championId: id,
  championName: name,
  role: 'MIDDLE',
  matches: 20,
  wins: 12,
  winRate: 60,
  avgKda: 3.5,
  mScore: 60,
  strengthTag: null,
  ...overrides
})

const cards = [champion(103, 'Ahri'), champion(134, 'Syndra'), champion(112, 'Viktor')]
const alsoPlayed = [
  champion(61, 'Orianna', { matches: 11, winRate: 45.5 }),
  champion(163, 'Taliyah', { matches: 1, winRate: 100 })
]

function mountPool(props = {}) {
  return mount(OverviewChampionPool, { props: { champions: cards, alsoPlayed, ...props } })
}

describe('OverviewChampionPool', () => {
  it('titles the section "Your champions"', () => {
    const wrapper = mountPool()
    expect(wrapper.get('h2').text()).toBe('Your champions')
    expect(wrapper.attributes('aria-labelledby')).toBe('champion-pool-title')
  })

  it('renders one card per champion in the given order', () => {
    const names = mountPool().findAll('[data-testid="champion-card-name"]').map((n) => n.text())
    expect(names).toEqual(['Ahri', 'Syndra', 'Viktor'])
  })

  it('lists "Also played" with matches and win rate, low win rates in orange', () => {
    const wrapper = mountPool()
    const items = wrapper.findAll('[data-testid="champion-pool-also-played-item"]')
    expect(items).toHaveLength(2)
    expect(items[0].text()).toContain('Orianna')
    expect(items[0].text()).toContain('11 matches')
    expect(items[1].text()).toContain('1 match')

    const rates = wrapper.findAll('[data-testid="champion-pool-also-played-winrate"]')
    expect(rates[0].text()).toBe('46% win rate')
    expect(rates[0].classes()).toContain('mp-down')
    expect(rates[1].classes()).not.toContain('mp-down')
  })

  it('leaves out the "Also played" list when there is nothing else', () => {
    const wrapper = mountPool({ alsoPlayed: [] })
    expect(wrapper.find('[data-testid="champion-pool-also-played"]').exists()).toBe(false)
  })

  it('shows an empty state without champions', () => {
    const wrapper = mountPool({ champions: [], alsoPlayed: [] })
    expect(wrapper.find('[data-testid="champion-card"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="champion-pool-empty"]').text()).toContain('No ranked matches this season')
  })

  it('swaps a broken icon for a placeholder', async () => {
    const wrapper = mountPool()
    const icon = wrapper.get('[data-testid="champion-pool-also-played-item"] img')
    await icon.trigger('error')
    expect(wrapper.findAll('[data-testid="champion-pool-also-played-item"] img')).toHaveLength(1)
  })
})
