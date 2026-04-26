import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { setupPinia } from '@test/helpers/testUtils'
import TeamAnalyticsPage from '@/views/TeamAnalyticsPage.vue'

describe('TeamAnalyticsPage', () => {
  beforeEach(() => {
    setupPinia()
  })

  function mountPage() {
    return mount(TeamAnalyticsPage)
  }

  it('renders the page heading', () => {
    const wrapper = mountPage()
    expect(wrapper.find('[data-testid="page-heading"]').text()).toBe('Team Analytics')
  })

  it('shows the coming soon message', () => {
    const wrapper = mountPage()
    expect(wrapper.text()).toContain('coming soon')
  })
})
