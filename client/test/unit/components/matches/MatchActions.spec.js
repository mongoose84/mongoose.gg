import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import MatchActions from '@/components/matches/MatchActions.vue'

const router = createRouter({
  history: createMemoryHistory(),
  routes: [{ path: '/app/solo', name: 'app-solo', component: { template: '<div />' } }]
})

const createWrapper = () => mount(MatchActions, { global: { plugins: [router] } })

describe('MatchActions.vue', () => {
  it('renders the actions container', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.match-actions').exists()).toBe(true)
  })

  it('renders two action buttons', () => {
    const wrapper = createWrapper()
    expect(wrapper.findAll('button')).toHaveLength(2)
  })

  it('renders View Analysis button', () => {
    const wrapper = createWrapper()
    expect(wrapper.text()).toContain('View Analysis')
  })

  it('renders View Goal Impact button', () => {
    const wrapper = createWrapper()
    expect(wrapper.text()).toContain('View Goal Impact')
  })

  it('View Analysis button is enabled', () => {
    const wrapper = createWrapper()
    const primaryBtn = wrapper.find('.action-btn.primary')
    expect(primaryBtn.attributes('disabled')).toBeUndefined()
  })

  it('View Analysis button has aria-label', () => {
    const wrapper = createWrapper()
    const primaryBtn = wrapper.find('.action-btn.primary')
    expect(primaryBtn.attributes('aria-label')).toBe('View analysis on Solo Dashboard')
  })

  it('View Analysis button navigates to app-solo on click', async () => {
    const wrapper = createWrapper()
    const pushSpy = vi.spyOn(router, 'push')
    await wrapper.find('.action-btn.primary').trigger('click')
    expect(pushSpy).toHaveBeenCalledWith({ name: 'app-solo' })
  })

  it('View Goal Impact button is disabled', () => {
    const wrapper = createWrapper()
    const secondaryBtn = wrapper.find('.action-btn.secondary')
    expect(secondaryBtn.attributes('disabled')).toBeDefined()
  })

  it('View Analysis button has primary style class', () => {
    const wrapper = createWrapper()
    const buttons = wrapper.findAll('button')
    expect(buttons[0].classes()).toContain('primary')
  })

  it('View Goal Impact button has secondary style class', () => {
    const wrapper = createWrapper()
    const buttons = wrapper.findAll('button')
    expect(buttons[1].classes()).toContain('secondary')
  })
})
