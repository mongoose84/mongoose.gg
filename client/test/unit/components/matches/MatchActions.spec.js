import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import MatchActions from '@/components/matches/MatchActions.vue'

describe('MatchActions.vue', () => {
  it('renders the actions container', () => {
    const wrapper = mount(MatchActions)
    expect(wrapper.find('.match-actions').exists()).toBe(true)
  })

  it('renders two action buttons', () => {
    const wrapper = mount(MatchActions)
    expect(wrapper.findAll('button')).toHaveLength(2)
  })

  it('renders View Analysis button', () => {
    const wrapper = mount(MatchActions)
    expect(wrapper.text()).toContain('View Analysis')
  })

  it('renders View Goal Impact button', () => {
    const wrapper = mount(MatchActions)
    expect(wrapper.text()).toContain('View Goal Impact')
  })

  it('View Analysis button is disabled', () => {
    const wrapper = mount(MatchActions)
    const primaryBtn = wrapper.find('.action-btn.primary')
    expect(primaryBtn.attributes('disabled')).toBeDefined()
  })

  it('View Goal Impact button is disabled', () => {
    const wrapper = mount(MatchActions)
    const secondaryBtn = wrapper.find('.action-btn.secondary')
    expect(secondaryBtn.attributes('disabled')).toBeDefined()
  })

  it('View Analysis button has primary style class', () => {
    const wrapper = mount(MatchActions)
    const buttons = wrapper.findAll('button')
    expect(buttons[0].classes()).toContain('primary')
  })

  it('View Goal Impact button has secondary style class', () => {
    const wrapper = mount(MatchActions)
    const buttons = wrapper.findAll('button')
    expect(buttons[1].classes()).toContain('secondary')
  })
})
