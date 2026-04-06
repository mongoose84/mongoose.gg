import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { setupPinia } from '@test/helpers/testUtils'
import GoalsPage from '@/views/GoalsPage.vue'

describe('GoalsPage', () => {
  beforeEach(() => {
    setupPinia()
  })

  function mountPage() {
    return mount(GoalsPage)
  }

  it('renders the page heading', () => {
    const wrapper = mountPage()
    expect(wrapper.find('[data-testid="page-heading"]').text()).toBe('Goals')
  })

  it('shows the coming soon message', () => {
    const wrapper = mountPage()
    expect(wrapper.text()).toContain('coming soon')
  })
})
