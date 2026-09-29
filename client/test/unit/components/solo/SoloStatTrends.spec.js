import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SoloStatTrends from '@/components/solo/SoloStatTrends.vue'
import { statTrendsResponse } from '@test/helpers/soloFixtures'

function mountCard(props = {}) {
  return mount(SoloStatTrends, { props: { data: statTrendsResponse(), queue: 'ranked_solo', ...props } })
}

describe('SoloStatTrends', () => {
  it('shows six tiles under a title that counts improved stats', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="solo-stat-trends-title"]').text()).toBe('2 of 6 match-deciding stats improved')
    expect(wrapper.get('[data-testid="solo-stat-trends-caption"]').text()).toBe('10-match average, last 20 matches')
    expect(wrapper.findAll('[data-testid^="trend-tile-"][role="group"]')).toHaveLength(6)
  })

  it('fills each tile from its stat', () => {
    const deaths = mountCard().get('[data-testid="trend-tile-deaths"]')

    expect(deaths.text()).toContain('Deaths')
    expect(deaths.text()).toContain('4.1')
    expect(deaths.text()).toContain('Improving')
    expect(deaths.attributes('aria-label')).toContain('your season average 4.9')
  })

  it('names the benchmark in the key', () => {
    expect(mountCard().text()).toContain('Your season average')
  })

  it('uses the caption as the title when no stat has a verdict', () => {
    const data = statTrendsResponse({ matches: 12 })
    data.stats = data.stats.map((s) => ({ ...s, verdict: null, was: null }))

    const wrapper = mountCard({ data })

    expect(wrapper.get('[data-testid="solo-stat-trends-title"]').text()).toBe('10-match average, last 12 matches')
    expect(wrapper.find('[data-testid="solo-stat-trends-caption"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="trend-tile-deaths"]').text()).toContain('Needs 20 matches')
  })

  it('shows a skeleton while loading', () => {
    const wrapper = mountCard({ data: null, loading: true })

    expect(wrapper.get('[data-testid="solo-stat-trends-loading"]').attributes('aria-busy')).toBe('true')
    expect(wrapper.find('[data-testid="solo-stat-trends"]').exists()).toBe(false)
  })

  it('shows an error with a retry', async () => {
    const wrapper = mountCard({ error: true })

    await wrapper.get('[data-testid="solo-stat-trends-retry"]').trigger('click')

    expect(wrapper.get('[data-testid="solo-stat-trends-error"]').text()).toContain("We couldn't load this card")
    expect(wrapper.emitted('retry')).toHaveLength(1)
  })

  it('offers all queues when the queue has no matches', async () => {
    const wrapper = mountCard({ data: statTrendsResponse({ matches: 0 }), queue: 'ranked_flex' })

    expect(wrapper.get('[data-testid="solo-stat-trends-empty"]').text()).toContain('No Flex matches yet')
    await wrapper.get('[data-testid="solo-stat-trends-show-all"]').trigger('click')
    expect(wrapper.emitted('show-all-queues')).toHaveLength(1)
  })

  it('offers a sync when there are no matches at all', async () => {
    const wrapper = mountCard({ data: statTrendsResponse({ matches: 0, queueType: 'all' }), queue: 'all' })

    await wrapper.get('[data-testid="solo-stat-trends-sync"]').trigger('click')
    expect(wrapper.emitted('sync')).toHaveLength(1)
  })
})
