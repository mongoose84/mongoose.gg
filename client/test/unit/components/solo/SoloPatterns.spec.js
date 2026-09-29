import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SoloPatterns from '@/components/solo/SoloPatterns.vue'
import { winFactorsResponse } from '@test/helpers/soloFixtures'

function mountSection(props = {}) {
  return mount(SoloPatterns, { props: { data: winFactorsResponse(), ...props } })
}

describe('SoloPatterns', () => {
  it('shows a card per pattern whose rules are met', () => {
    const wrapper = mountSection()

    expect(wrapper.find('[data-testid="solo-pattern-session"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="solo-pattern-afterLoss"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="solo-pattern-length"]').exists()).toBe(false)
  })

  it('gives each card its chip, takeaway title and chart', () => {
    const session = mountSection().get('[data-testid="solo-pattern-session"]')

    expect(session.get('[data-testid="solo-pattern-chip"]').text()).toBe('Pattern')
    expect(session.get('[data-testid="solo-pattern-title"]').text()).toBe('You drop off after your third match')
    expect(session.findAll('[data-testid="column-chart-group"]')).toHaveLength(3)
    expect(session.findAll('.mp-columns__bar--warn')).toHaveLength(1)
  })

  it('hides the section when no pattern is shown', () => {
    const wrapper = mountSection({ data: winFactorsResponse({ patterns: { session: null, afterLoss: null, length: null } }) })

    expect(wrapper.find('[data-testid="solo-patterns"]').exists()).toBe(false)
  })

  it('shows a skeleton while loading and an error with a retry', async () => {
    expect(mountSection({ data: null, loading: true }).find('[data-testid="solo-patterns-loading"]').exists()).toBe(true)

    const failed = mountSection({ error: true })
    await failed.get('[data-testid="solo-patterns-retry"]').trigger('click')
    expect(failed.emitted('retry')).toHaveLength(1)
  })
})
