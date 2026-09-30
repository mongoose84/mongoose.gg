import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SoloFocusCard from '@/components/solo/SoloFocusCard.vue'
import { focusFixture } from '@test/helpers/soloFixtures'

function mountCard(props = {}) {
  return mount(SoloFocusCard, { props: { focus: focusFixture(), verdict: 'slipping', ...props } })
}

describe('SoloFocusCard', () => {
  it('is the highlight card with the finding, evidence, strip and fix', () => {
    const wrapper = mountCard()
    const card = wrapper.get('[data-testid="solo-focus"]')

    expect(card.classes()).toContain('mp-card--highlight')
    expect(card.text()).toContain('Your focus')
    expect(wrapper.get('[data-testid="solo-focus-finding"]').text()).toBe('Vision is the one stat slipping')
    expect(wrapper.get('[data-testid="solo-focus-evidence"]').text()).toContain('Down from 0.9 to 0.7 per minute.')
    expect(wrapper.get('[data-testid="solo-focus-strip-caption"]').text()).toBe('Hit the mark in 4 of 19 matches')
    expect(wrapper.findAll('[data-testid="goal-strip-cell"]')).toHaveLength(20)
    expect(wrapper.get('[data-testid="solo-focus-fix"]').text()).toBe('Next match: Buy a control ward on every back.')
  })

  it('calls a stat that is not slipping the biggest lever', () => {
    expect(mountCard({ verdict: 'steady' }).get('[data-testid="solo-focus-finding"]').text()).toBe('Vision is your biggest lever')
  })

  it('has no goal button (decision 2)', () => {
    expect(mountCard().find('button').exists()).toBe(false)
  })

  it('shows a skeleton while loading, and nothing without a focus', () => {
    expect(mountCard({ focus: null, loading: true }).find('[data-testid="solo-focus-loading"]').exists()).toBe(true)
    expect(mountCard({ focus: null }).html()).toBe('<!--v-if-->')
  })
})
