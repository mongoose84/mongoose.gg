import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SoloChampionLp from '@/components/solo/SoloChampionLp.vue'
import { climbResponse, winRateClimbResponse } from '@test/helpers/soloFixtures'

function mountCard(props = {}) {
  return mount(SoloChampionLp, { props: { data: climbResponse(), queue: 'ranked_solo', ...props } })
}

describe('SoloChampionLp', () => {
  it('shows a row per champion with its LP, under the top champion title', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="solo-champion-lp-title"]').text()).toBe('Ahri earned most of your climb')
    const ahri = wrapper.get('[data-testid="champion-lp-103"]')
    expect(ahri.text()).toContain('9 matches · 67% win rate')
    expect(ahri.get('[data-testid="champion-lp-value"]').text()).toBe('+58 LP')
    expect(ahri.get('[data-testid="champion-lp-value"]').classes()).toContain('mp-up')
    expect(wrapper.get('[data-testid="champion-lp-134"] [data-testid="champion-lp-value"]').classes()).toContain('mp-down')
  })

  it('scales bars to the largest value', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="champion-lp-103"] [data-testid="diverging-bar-ahead"]').attributes('style')).toContain('width: 100%')
    expect(wrapper.get('[data-testid="champion-lp-134"] [data-testid="diverging-bar-behind"]').attributes('style')).toContain('width: 31%')
  })

  it('names the champions left out', () => {
    expect(mountCard().get('[data-testid="solo-champion-lp-left-out"]').text()).toBe('Left out with fewer than 3 matches: Orianna.')
  })

  it('shows net wins in win-rate mode', () => {
    const wrapper = mountCard({ data: winRateClimbResponse() })

    expect(wrapper.get('[data-testid="solo-champion-lp-caption"]').text()).toBe('Wins minus losses per champion')
    expect(wrapper.get('[data-testid="champion-lp-103"] [data-testid="champion-lp-value"]').text()).toBe('+3')
  })

  it('shows the empty state when no champion has 3 matches', () => {
    const wrapper = mountCard({ data: climbResponse({ champions: [] }) })

    expect(wrapper.get('[data-testid="solo-champion-lp-empty"]').text()).toContain('No champion has 3 matches in this range yet')
  })

  it('shows a skeleton while loading and retries on error', async () => {
    expect(mountCard({ data: null, loading: true }).find('[data-testid="solo-champion-lp-loading"]').exists()).toBe(true)

    const failed = mountCard({ error: true })
    await failed.get('[data-testid="solo-champion-lp-retry"]').trigger('click')
    expect(failed.emitted('retry')).toHaveLength(1)
  })
})
