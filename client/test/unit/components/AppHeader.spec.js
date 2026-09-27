import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'
import { ref } from 'vue'
import AppHeader from '@/components/AppHeader.vue'

const mockSetActiveAccount = vi.fn()
const mockLogout = vi.fn().mockResolvedValue(undefined)

const mockAuthStore = {
  username: 'TestUser',
  riotAccounts: [],
  activeAccountPuuid: 'overall',
  canUseOverallAccountView: false,
  setActiveAccount: mockSetActiveAccount,
  logout: mockLogout
}

const mockUserIconUrl = ref(null)

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => mockAuthStore
}))

vi.mock('@/composables/useUserIcon', () => ({
  useUserIcon: () => ({ userIconUrl: mockUserIconUrl })
}))

vi.mock('@/services/analyticsApi', () => ({
  trackAuth: vi.fn()
}))

async function createWrapper(initialPath = '/app/overview') {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div>Home</div>' } },
      { path: '/app/overview', component: { template: '<div>Overview</div>' } },
      { path: '/app/champion-select', component: { template: '<div>Champion Select</div>' } },
      { path: '/app/matches', component: { template: '<div>Matches</div>' } },
      { path: '/app/solo', component: { template: '<div>Solo</div>' } },
      { path: '/app/user', component: { template: '<div>Settings</div>' } },
      { path: '/app/feedback', component: { template: '<div>Feedback</div>' } }
    ]
  })
  router.push(initialPath)
  await router.isReady()

  const wrapper = mount(AppHeader, {
    global: { plugins: [router] }
  })
  await flushPromises()
  return { wrapper, router }
}

describe('AppHeader.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mockAuthStore.username = 'TestUser'
    mockAuthStore.riotAccounts = []
    mockAuthStore.activeAccountPuuid = 'overall'
    mockAuthStore.canUseOverallAccountView = false
    mockUserIconUrl.value = null
    mockSetActiveAccount.mockClear()
    mockLogout.mockClear()
  })

  it('renders the header with logo linking to Overview', async () => {
    const { wrapper } = await createWrapper()
    expect(wrapper.find('[data-testid="app-header"]').exists()).toBe(true)
    expect(wrapper.get('[data-testid="app-header-logo"]').attributes('href')).toBe('/app/overview')
  })

  it('marks the active pill with aria-current="page"', async () => {
    const { wrapper } = await createWrapper('/app/matches')
    expect(wrapper.get('[data-testid="nav-matches"]').attributes('aria-current')).toBe('page')
    expect(wrapper.get('[data-testid="nav-overview"]').attributes('aria-current')).toBeUndefined()
  })

  it('opens and closes the avatar menu', async () => {
    const { wrapper } = await createWrapper()
    const avatar = wrapper.get('[data-testid="app-header-avatar"]')
    expect(avatar.attributes('aria-expanded')).toBe('false')
    expect(wrapper.find('[data-testid="app-header-menu"]').exists()).toBe(false)

    await avatar.trigger('click')
    expect(avatar.attributes('aria-expanded')).toBe('true')
    expect(wrapper.find('[data-testid="app-header-menu"]').exists()).toBe(true)

    await avatar.trigger('click')
    expect(wrapper.find('[data-testid="app-header-menu"]').exists()).toBe(false)
  })

  it('closes the menu on Escape', async () => {
    const { wrapper } = await createWrapper()
    await wrapper.get('[data-testid="app-header-avatar"]').trigger('click')
    expect(wrapper.find('[data-testid="app-header-menu"]').exists()).toBe(true)

    await wrapper.get('[data-testid="app-header-menu"]').trigger('keydown', { key: 'Escape' })
    expect(wrapper.find('[data-testid="app-header-menu"]').exists()).toBe(false)
  })

  it('hides the account switcher with a single account and no overall view', async () => {
    mockAuthStore.riotAccounts = [{ puuid: 'p1', gameName: 'Faker', tagLine: 'KR1', region: 'kr' }]
    mockAuthStore.canUseOverallAccountView = false
    const { wrapper } = await createWrapper()
    await wrapper.get('[data-testid="app-header-avatar"]').trigger('click')
    expect(wrapper.find('[data-testid="app-header-accounts-label"]').exists()).toBe(false)
  })

  it('shows the account switcher with several accounts and calls setActiveAccount on select', async () => {
    mockAuthStore.riotAccounts = [
      { puuid: 'p1', gameName: 'Faker', tagLine: 'KR1', region: 'kr' },
      { puuid: 'p2', gameName: 'Chovy', tagLine: 'KR2', region: 'kr' }
    ]
    mockAuthStore.activeAccountPuuid = 'p1'
    const { wrapper } = await createWrapper()
    await wrapper.get('[data-testid="app-header-avatar"]').trigger('click')
    expect(wrapper.find('[data-testid="app-header-accounts-label"]').exists()).toBe(true)

    await wrapper.get('[data-testid="account-option-Chovy"]').trigger('click')
    expect(mockSetActiveAccount).toHaveBeenCalledWith('p2')
    // Selecting an account closes the menu
    expect(wrapper.find('[data-testid="app-header-menu"]').exists()).toBe(false)
  })

  it('never renders a raw puuid in the account switcher', async () => {
    mockAuthStore.riotAccounts = [
      { puuid: 'p1', gameName: 'Faker', tagLine: 'KR1', region: 'kr' },
      { puuid: 'p2', gameName: 'Chovy', tagLine: 'KR2', region: 'kr' }
    ]
    mockAuthStore.canUseOverallAccountView = true
    const { wrapper } = await createWrapper()
    await wrapper.get('[data-testid="app-header-avatar"]').trigger('click')
    expect(wrapper.text()).not.toContain('p1')
    expect(wrapper.text()).not.toContain('p2')
  })

  it('logs out and redirects to the landing page', async () => {
    const { wrapper, router } = await createWrapper()
    const pushSpy = vi.spyOn(router, 'push')
    await wrapper.get('[data-testid="app-header-avatar"]').trigger('click')
    await wrapper.get('[data-testid="app-header-logout"]').trigger('click')
    await flushPromises()

    expect(mockLogout).toHaveBeenCalled()
    expect(pushSpy).toHaveBeenCalledWith('/')
  })
})
