import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseEmptyState from '@/components/base/BaseEmptyState.vue'

describe('BaseEmptyState', () => {
  it('renders the title, description and action', () => {
    const wrapper = mount(BaseEmptyState, {
      props: { title: 'No matches yet', description: 'Sync your matches.' },
      slots: { action: '<button data-testid="fix">Sync matches</button>' }
    })

    expect(wrapper.get('[data-testid="base-empty-state-title"]').text()).toBe('No matches yet')
    expect(wrapper.get('[data-testid="base-empty-state-description"]').text()).toBe('Sync your matches.')
    expect(wrapper.find('[data-testid="fix"]').exists()).toBe(true)
  })

  it('uses an h3 by default and the requested heading level', () => {
    expect(mount(BaseEmptyState, { props: { title: 'A' } }).find('h3').exists()).toBe(true)
    expect(mount(BaseEmptyState, { props: { title: 'A', headingLevel: 1 } }).find('h1').exists()).toBe(true)
  })

  it('leaves out the description and action when not given', () => {
    const wrapper = mount(BaseEmptyState, { props: { title: 'No insights yet' } })
    expect(wrapper.find('[data-testid="base-empty-state-description"]').exists()).toBe(false)
    expect(wrapper.find('.mp-empty__action').exists()).toBe(false)
  })
})
