import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { setupPinia } from '@test/helpers/testUtils'
import TeamAnalytics from '@/views/TeamAnalytics.vue'

describe('TeamAnalytics', () => {
  beforeEach(() => {
    setupPinia()
  })

  function mountPage() {
    return mount(TeamAnalytics)
  }

  it('renders the page heading', () => {
    const wrapper = mountPage()
    expect(wrapper.find('h1').text()).toBe('Team Analytics')
  })

  it('shows the coming soon message', () => {
    const wrapper = mountPage()
    expect(wrapper.text()).toContain('coming soon')
  })
})
