import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import OverviewLayout from '../../src/components/overview/OverviewLayout.vue'

const stubs = {
  BaseCard: { template: '<div><slot /></div>' },
  BaseButton: { template: '<button><slot /></button>' }
}

describe('OverviewLayout', () => {
  it('section heading reads "At a glance" (not "Today at a glance")', () => {
    const wrapper = mount(OverviewLayout, {
      props: { isLoading: false, error: null, isEmpty: false },
      slots: { 'glance-left': '<div>left</div>' },
      global: { stubs }
    })

    expect(wrapper.text()).toContain('At a glance')
    expect(wrapper.text()).not.toContain('Today at a glance')
  })

  it('section heading reads "Quick actions" (not "Recent matches")', () => {
    const wrapper = mount(OverviewLayout, {
      props: { isLoading: false, error: null, isEmpty: false },
      slots: { 'recent-left': '<div>actions</div>' },
      global: { stubs }
    })

    expect(wrapper.text()).toContain('Quick actions')
    expect(wrapper.text()).not.toContain('Recent matches')
  })

  it('slot #recent-left renders content correctly', () => {
    const wrapper = mount(OverviewLayout, {
      props: { isLoading: false, error: null, isEmpty: false },
      slots: { 'recent-left': '<div data-testid="actions-left-content">actions left</div>' },
      global: { stubs }
    })

    expect(wrapper.find('[data-testid="actions-left-content"]').exists()).toBe(true)
  })

  it('slot #recent-right renders content correctly', () => {
    const wrapper = mount(OverviewLayout, {
      props: { isLoading: false, error: null, isEmpty: false },
      slots: { 'recent-right': '<div data-testid="actions-right-content">actions right</div>' },
      global: { stubs }
    })

    expect(wrapper.find('[data-testid="actions-right-content"]').exists()).toBe(true)
  })
})
