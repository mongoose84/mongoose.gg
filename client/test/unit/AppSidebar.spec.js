import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import AppSidebar from '@/components/AppSidebar.vue'

const mockAuthStore = {
  username: 'TestUser',
  hasLinkedAccount: false,
  primaryRiotAccount: null,
  hasReachedRiotAccountLimit: false
}

const mockUiStore = {
  isSidebarCollapsed: false,
  initializeSidebar: vi.fn(),
  handleResize: vi.fn(),
  toggleSidebar: vi.fn()
}

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => mockAuthStore
}))

vi.mock('@/stores/uiStore', () => ({
  useUiStore: () => mockUiStore
}))

vi.mock('@/composables/useAnalysisStatus', () => ({
  useAnalysisStatus: () => ({ isRunning: false })
}))

describe('AppSidebar.vue', () => {
  const createWrapper = () => mount(AppSidebar, {
    global: {
      stubs: {
        Transition: false,
        'router-link': {
          props: ['to'],
          template: '<a :href="typeof to === \'string\' ? to : \'#\'"><slot /></a>'
        }
      }
    }
  })

  beforeEach(() => {
    mockAuthStore.hasReachedRiotAccountLimit = false
    mockUiStore.isSidebarCollapsed = false
    mockUiStore.initializeSidebar.mockReset()
    mockUiStore.handleResize.mockReset()
    mockUiStore.toggleSidebar.mockReset()
  })

  it('shows compact upgrade CTA when user is free-tier and at account limit', () => {
    mockAuthStore.hasReachedRiotAccountLimit = true

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="sidebar-upgrade-link"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('+ Link')
  })

  it('hides compact upgrade CTA when user is not at account limit', () => {
    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="sidebar-upgrade-link"]').exists()).toBe(false)
  })
})
