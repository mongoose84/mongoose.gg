import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SoloClimbCard from '@/components/solo/SoloClimbCard.vue'
import { climbResponse, winRateClimbResponse } from '@test/helpers/soloFixtures'

function mountCard(props = {}) {
  return mount(SoloClimbCard, { props: { data: climbResponse(), queue: 'ranked_solo', ...props } })
}

describe('SoloClimbCard', () => {
  it('draws the LP ladder under the division title, with the stats beside it', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="solo-climb-title"]').text()).toBe('From Emerald III to Emerald II')
    expect(wrapper.get('[data-testid="solo-climb-caption"]').text()).toBe('LP after each ranked match, last 20 matches')
    expect(wrapper.get('[data-testid="solo-climb-stats"]').text()).toContain('+3.2')
    expect(wrapper.get('[data-testid="line-chart-line"]').attributes('points').split(' ')).toHaveLength(4)
  })

  it('marks promotions and the biggest drop, labelling only events far enough apart', () => {
    const labels = mountCard().findAll('[data-testid="line-chart-marker-label"]').map((l) => l.text())

    // The promotion at match 2 is 2 matches before the demotion, so only the demotion is labelled
    expect(labels).toEqual(['Emerald III', 'Emerald II', '▼ −60 LP'])
    expect(mountCard().findAll('[data-testid="line-chart-marker"]')).toHaveLength(4)
  })

  it('draws the rolling win rate in win-rate mode, with the LP note for a ranked queue', () => {
    const ranked = mountCard({ data: winRateClimbResponse({ queueType: 'ranked_solo' }) })

    expect(ranked.get('[data-testid="solo-climb-title"]').text()).toBe('Win rate up from 50% to 70%')
    expect(ranked.find('[data-testid="solo-climb-lp-note"]').exists()).toBe(true)
    expect(ranked.text()).toContain('50%')

    const all = mountCard({ data: winRateClimbResponse(), queue: 'all' })
    expect(all.find('[data-testid="solo-climb-lp-note"]').exists()).toBe(false)
  })

  it('leaves out the LP note when several accounts are in scope', () => {
    const wrapper = mountCard({ data: winRateClimbResponse({ queueType: 'ranked_solo' }), singleAccount: false })

    expect(wrapper.find('[data-testid="solo-climb-lp-note"]').exists()).toBe(false)
  })

  it('asks for more matches before the win-rate line', () => {
    const wrapper = mountCard({ data: winRateClimbResponse({ matches: 12, winRate: null }) })

    expect(wrapper.get('[data-testid="solo-climb-empty"]').text()).toContain('Play 8 more matches to see your climb')
  })

  it('offers all queues when the queue has no matches, and retries on error', async () => {
    const empty = mountCard({ data: climbResponse({ matches: 0 }), queue: 'ranked_flex' })
    await empty.get('[data-testid="solo-climb-show-all"]').trigger('click')
    expect(empty.emitted('show-all-queues')).toHaveLength(1)

    const failed = mountCard({ error: true })
    await failed.get('[data-testid="solo-climb-retry"]').trigger('click')
    expect(failed.emitted('retry')).toHaveLength(1)
  })

  it('shows a skeleton while loading', () => {
    expect(mountCard({ data: null, loading: true }).find('[data-testid="solo-climb-loading"]').exists()).toBe(true)
  })
})
