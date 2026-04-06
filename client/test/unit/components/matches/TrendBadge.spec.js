import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import TrendBadge from '@/components/matches/TrendBadge.vue'

describe('TrendBadge.vue', () => {
  it('renders nothing when badge is null', () => {
    const wrapper = mount(TrendBadge, { props: { badge: null } })
    expect(wrapper.find('.trend-badge').exists()).toBe(false)
  })

  it('renders nothing when badge is not provided', () => {
    const wrapper = mount(TrendBadge)
    expect(wrapper.find('.trend-badge').exists()).toBe(false)
  })

  it('renders badge text when badge is provided', () => {
    const wrapper = mount(TrendBadge, { props: { badge: { text: 'Good CS', type: 'positive' } } })
    expect(wrapper.find('.badge-text').text()).toBe('Good CS')
  })

  it('applies trend-positive class for positive type', () => {
    const wrapper = mount(TrendBadge, { props: { badge: { text: 'Up', type: 'positive' } } })
    expect(wrapper.find('.trend-badge').classes()).toContain('trend-positive')
  })

  it('applies trend-negative class for negative type', () => {
    const wrapper = mount(TrendBadge, { props: { badge: { text: 'Down', type: 'negative' } } })
    expect(wrapper.find('.trend-badge').classes()).toContain('trend-negative')
  })

  it('applies trend-neutral class for neutral type', () => {
    const wrapper = mount(TrendBadge, { props: { badge: { text: 'Average', type: 'neutral' } } })
    expect(wrapper.find('.trend-badge').classes()).toContain('trend-neutral')
  })

  it('applies trend-neutral class when type is absent', () => {
    const wrapper = mount(TrendBadge, { props: { badge: { text: 'Average' } } })
    expect(wrapper.find('.trend-badge').classes()).toContain('trend-neutral')
  })

  it('shows upward arrow icon for positive type', () => {
    const wrapper = mount(TrendBadge, { props: { badge: { text: 'Up', type: 'positive' } } })
    expect(wrapper.find('.badge-icon').text()).toBe('↑')
  })

  it('shows downward arrow icon for negative type', () => {
    const wrapper = mount(TrendBadge, { props: { badge: { text: 'Down', type: 'negative' } } })
    expect(wrapper.find('.badge-icon').text()).toBe('↓')
  })

  it('shows no arrow icon for neutral type', () => {
    const wrapper = mount(TrendBadge, { props: { badge: { text: 'Average', type: 'neutral' } } })
    expect(wrapper.find('.badge-icon').exists()).toBe(false)
  })
})
