import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { ref } from 'vue'
import { setupPinia } from '@test/helpers/testUtils'
import AppLayout from '@/layouts/AppLayout.vue'

const mockRefreshUser = vi.fn()
const mockIsAuthenticated = ref(true)

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    get isAuthenticated() {
      return mockIsAuthenticated.value
    },
    refreshUser: mockRefreshUser
  })
}))

vi.mock('@/components/AppHeader.vue', () => ({
  default: {
    name: 'AppHeader',
    template: '<header data-testid="app-header"></header>'
  }
}))

vi.mock('@/components/AppTabBar.vue', () => ({
  default: {
    name: 'AppTabBar',
    template: '<nav data-testid="app-tabbar"></nav>'
  }
}))

describe('AppLayout', () => {
  let addEventListenerSpy
  let removeEventListenerSpy
  let documentAddEventListenerSpy
  let documentRemoveEventListenerSpy

  beforeEach(() => {
    setupPinia()
    vi.clearAllMocks()
    mockIsAuthenticated.value = true
    localStorage.clear()

    addEventListenerSpy = vi.spyOn(window, 'addEventListener')
    removeEventListenerSpy = vi.spyOn(window, 'removeEventListener')
    documentAddEventListenerSpy = vi.spyOn(document, 'addEventListener')
    documentRemoveEventListenerSpy = vi.spyOn(document, 'removeEventListener')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  function mountLayout() {
    return mount(AppLayout, {
      global: {
        stubs: {
          RouterView: { template: '<div data-testid="router-view"></div>' }
        }
      }
    })
  }

  it('renders the AppHeader and AppTabBar', () => {
    const wrapper = mountLayout()
    expect(wrapper.find('[data-testid="app-header"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="app-tabbar"]').exists()).toBe(true)
  })

  it('renders a main element with the router-view slot', () => {
    const wrapper = mountLayout()
    expect(wrapper.find('main').exists()).toBe(true)
    expect(wrapper.find('[data-testid="router-view"]').exists()).toBe(true)
  })

  it('does not render a sidebar', () => {
    const wrapper = mountLayout()
    expect(wrapper.find('[data-testid="app-sidebar"]').exists()).toBe(false)
  })

  it('registers visibilitychange listener on mount', () => {
    mountLayout()
    expect(documentAddEventListenerSpy).toHaveBeenCalledWith('visibilitychange', expect.any(Function))
  })

  it('registers window activity listeners on mount', () => {
    mountLayout()
    const windowEvents = addEventListenerSpy.mock.calls.map(([event]) => event)
    expect(windowEvents).toContain('mousemove')
    expect(windowEvents).toContain('keydown')
    expect(windowEvents).toContain('click')
    expect(windowEvents).toContain('scroll')
  })

  it('removes event listeners on unmount', async () => {
    const wrapper = mountLayout()
    wrapper.unmount()
    expect(documentRemoveEventListenerSpy).toHaveBeenCalledWith('visibilitychange', expect.any(Function))
    const windowRemovals = removeEventListenerSpy.mock.calls.map(([event]) => event)
    expect(windowRemovals).toContain('mousemove')
    expect(windowRemovals).toContain('keydown')
    expect(windowRemovals).toContain('click')
    expect(windowRemovals).toContain('scroll')
  })

  it('sets last active time in localStorage on mount', () => {
    mountLayout()
    expect(localStorage.getItem('mongoose_last_active_time')).not.toBeNull()
  })

  it('calls refreshUser when returning from long idle as authenticated user', async () => {
    mockIsAuthenticated.value = true
    mountLayout()

    // onMounted sets current time — override with a stale timestamp after mounting
    const longAgo = Date.now() - 35 * 60 * 1000
    localStorage.setItem('mongoose_last_active_time', longAgo.toString())

    // Capture the visibilitychange handler
    const handler = documentAddEventListenerSpy.mock.calls.find(
      ([event]) => event === 'visibilitychange'
    )?.[1]

    expect(handler).toBeDefined()

    // Simulate returning to tab
    Object.defineProperty(document, 'visibilityState', { value: 'visible', configurable: true })
    handler()

    expect(mockRefreshUser).toHaveBeenCalledTimes(1)
  })

  it('does not call refreshUser when idle time is within threshold', async () => {
    localStorage.setItem('mongoose_last_active_time', Date.now().toString())
    mockIsAuthenticated.value = true

    mountLayout()

    const handler = documentAddEventListenerSpy.mock.calls.find(
      ([event]) => event === 'visibilitychange'
    )?.[1]

    Object.defineProperty(document, 'visibilityState', { value: 'visible', configurable: true })
    handler()

    expect(mockRefreshUser).not.toHaveBeenCalled()
  })

  it('does not call refreshUser when user is not authenticated', async () => {
    const longAgo = Date.now() - 35 * 60 * 1000
    localStorage.setItem('mongoose_last_active_time', longAgo.toString())
    mockIsAuthenticated.value = false

    mountLayout()

    const handler = documentAddEventListenerSpy.mock.calls.find(
      ([event]) => event === 'visibilitychange'
    )?.[1]

    Object.defineProperty(document, 'visibilityState', { value: 'visible', configurable: true })
    handler()

    expect(mockRefreshUser).not.toHaveBeenCalled()
  })
})
