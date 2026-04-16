import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { ref } from 'vue'
import AppSidebar from '@/components/AppSidebar.vue'

const mockAuthStore = {
  username: 'TestUser',
  tier: 'free',
  hasReachedRiotAccountLimit: false
}

const mockUiStore = {
  isSidebarCollapsed: false,
  initializeSidebar: vi.fn(),
  handleResize: vi.fn(),
  toggleSidebar: vi.fn()
}

const mockUserIconUrl = ref(null)

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => mockAuthStore
}))

vi.mock('@/stores/uiStore', () => ({
  useUiStore: () => mockUiStore
}))

vi.mock('@/composables/useAnalysisStatus', () => ({
  useAnalysisStatus: () => ({ isRunning: false })
}))

vi.mock('@/composables/useUserIcon', () => ({
  useUserIcon: () => ({ userIconUrl: mockUserIconUrl })
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
    mockAuthStore.username = 'TestUser'
    mockAuthStore.tier = 'free'
    mockUserIconUrl.value = null
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

  it('shows SVG fallback when user icon image fails to load', async () => {
    mockUserIconUrl.value = 'https://ddragon.leagueoflegends.com/cdn/16.1.1/img/profileicon/9999.png'

    const wrapper = createWrapper()
    const userSection = wrapper.find('.user-item')
    expect(userSection.find('img').exists()).toBe(true)
    expect(userSection.find('svg').exists()).toBe(false)

    await userSection.find('img').trigger('error')
    await wrapper.vm.$nextTick()

    expect(userSection.find('img').exists()).toBe(false)
    expect(userSection.find('svg').exists()).toBe(true)
  })

  describe('User info display', () => {
    it('shows mongoose username in user section', () => {
      mockAuthStore.username = 'JeppeKronborg'
      const wrapper = createWrapper()
      expect(wrapper.find('.user-item').text()).toContain('JeppeKronborg')
    })

    it('shows free tier label for free users', () => {
      mockAuthStore.tier = 'free'
      const wrapper = createWrapper()
      expect(wrapper.find('.user-item').text()).toContain('free')
    })

    it('shows pro tier label for pro users', () => {
      mockAuthStore.tier = 'pro'
      const wrapper = createWrapper()
      expect(wrapper.find('.user-item').text()).toContain('pro')
    })

    it('does not show riot account name in user section', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('.user-item').text()).not.toContain('Faker#KR1')
    })
  })
})
