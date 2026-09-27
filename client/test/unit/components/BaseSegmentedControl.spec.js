import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseSegmentedControl from '@/components/base/BaseSegmentedControl.vue'

const options = [
  { value: 'all', label: 'All queues' },
  { value: 'ranked_solo', label: 'Solo/Duo' }
]

describe('BaseSegmentedControl', () => {
  it('renders a labelled group of buttons with the selected one pressed', () => {
    const wrapper = mount(BaseSegmentedControl, {
      props: { modelValue: 'ranked_solo', options, ariaLabel: 'Queue', testIdPrefix: 'queue' }
    })
    const group = wrapper.get('[data-testid="segmented-control"]')
    expect(group.attributes('role')).toBe('group')
    expect(group.attributes('aria-label')).toBe('Queue')
    expect(wrapper.get('[data-testid="queue-all"]').attributes('aria-pressed')).toBe('false')
    expect(wrapper.get('[data-testid="queue-ranked_solo"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('[data-testid="queue-all"]').attributes('type')).toBe('button')
  })

  it('emits the clicked value', async () => {
    const wrapper = mount(BaseSegmentedControl, {
      props: { modelValue: 'all', options, ariaLabel: 'Queue', testIdPrefix: 'queue' }
    })
    await wrapper.get('[data-testid="queue-ranked_solo"]').trigger('click')
    expect(wrapper.emitted('update:modelValue')).toEqual([['ranked_solo']])
  })
})
