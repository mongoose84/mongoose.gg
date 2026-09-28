import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChampionSelectMatchups from '@/components/championSelect/ChampionSelectMatchups.vue'

const strong = [{ championId: 238, championName: 'Zed', wins: 4, losses: 1, matches: 5, winRate: 80 }]
const weak = [{ championId: 134, championName: 'Syndra', wins: 1, losses: 3, matches: 4, winRate: 25 }]

function mountCard(props = {}) {
  return mount(ChampionSelectMatchups, {
    props: { championName: 'Ahri', strong, weak, ...props }
  })
}

describe('ChampionSelectMatchups', () => {
  it('titles the card with the selected pick', () => {
    expect(mountCard().get('h2').text()).toBe('Ahri in lane')
  })

  it('lists strong matchups in purple and weak ones in orange with the lane record', () => {
    const wrapper = mountCard()
    const strongSide = wrapper.get('[data-testid="matchups-strong"]')
    const weakSide = wrapper.get('[data-testid="matchups-weak"]')

    expect(strongSide.text()).toContain('Strong into')
    expect(strongSide.text()).toContain('Zed')
    expect(strongSide.text()).toContain('4–1 in lane')
    expect(strongSide.get('[data-testid="matchups-winrate"]').text()).toBe('80% lane win rate')
    expect(strongSide.get('[data-testid="matchups-winrate"]').classes()).toContain('mp-up')

    expect(weakSide.text()).toContain('Syndra')
    expect(weakSide.get('[data-testid="matchups-winrate"]').classes()).toContain('mp-down')
  })

  it('says so when one side has no matchups', () => {
    const wrapper = mountCard({ weak: [] })
    expect(wrapper.get('[data-testid="matchups-weak"] [data-testid="matchups-none"]').text()).toBe('No losing lane matchups yet.')
  })

  it('shows an empty state when there are no lane matchups at all', () => {
    const wrapper = mountCard({ strong: [], weak: [] })
    expect(wrapper.get('[data-testid="matchups-empty"]').text()).toContain('No lane matchups yet')
  })

  it('shows skeletons while loading', () => {
    const wrapper = mountCard({ status: 'loading' })
    expect(wrapper.attributes('aria-busy')).toBe('true')
    expect(wrapper.find('[data-testid="matchups-loading"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="matchups-row"]').exists()).toBe(false)
  })

  it('shows an error with retry', async () => {
    const wrapper = mountCard({ status: 'error' })
    expect(wrapper.get('[data-testid="matchups-error"]').attributes('role')).toBe('alert')
    await wrapper.get('[data-testid="matchups-retry"]').trigger('click')
    expect(wrapper.emitted('retry')).toHaveLength(1)
  })
})
