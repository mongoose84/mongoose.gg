import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SoloWinFactors from '@/components/solo/SoloWinFactors.vue'
import { winFactorsResponse } from '@test/helpers/soloFixtures'

function mountCard(props = {}) {
  return mount(SoloWinFactors, { props: { data: winFactorsResponse(), queue: 'ranked_solo', ...props } })
}

describe('SoloWinFactors', () => {
  it('shows one row per factor, in server order, under the top factor title', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="solo-win-factors-title"]').text()).toBe('Your gold at 15 decides your matches most')
    const rows = wrapper.get('[data-testid="solo-win-factors-rows"]').findAll('li')
    expect(rows.map((r) => r.attributes('data-testid'))).toEqual(['win-factor-aheadAt15', 'win-factor-vision', 'win-factor-lowDeaths'])
  })

  it('labels vision with the role mark', () => {
    expect(mountCard().get('[data-testid="win-factor-vision"]').text()).toContain('0.9+ vision per minute')
  })

  it('asks for more matches under 20', () => {
    const wrapper = mountCard({ data: winFactorsResponse({ matches: 15, factors: [] }) })

    expect(wrapper.get('[data-testid="solo-win-factors-empty"]').text()).toContain('Play 5 more matches to see what decides your matches')
    expect(wrapper.find('[data-testid="solo-win-factors-sync"]').exists()).toBe(true)
  })

  it('shows the empty state with fewer than two rows', () => {
    const data = winFactorsResponse()
    data.factors = data.factors.slice(0, 1)

    expect(mountCard({ data }).find('[data-testid="solo-win-factors-empty"]').exists()).toBe(true)
  })

  it('shows a skeleton while loading and an error with a retry', async () => {
    expect(mountCard({ data: null, loading: true }).find('[data-testid="solo-win-factors-loading"]').exists()).toBe(true)

    const failed = mountCard({ error: true })
    await failed.get('[data-testid="solo-win-factors-retry"]').trigger('click')
    expect(failed.emitted('retry')).toHaveLength(1)
  })
})
