import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import HomeView from '@/views/HomeView.vue'

const push = vi.fn()

vi.mock('vue-router', async () => {
  // Return only the functions used by the component
  return {
    useRouter: () => ({ push })
  }
})

vi.mock('@/api/shared.js', () => ({
  getUsers: vi.fn().mockResolvedValue([
    { userId: 1, userName: 'Alice', userType: 1 },
    { userId: 2, userName: 'Bob', userType: 1 },
  ]),
  createUser: vi.fn().mockResolvedValue({}),
  isDevelopment: true,
  getBaseApi: vi.fn().mockReturnValue('http://localhost:5000/api/v1.0')
}))

describe('HomeView', () => {
  beforeEach(() => {
    push.mockReset()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('renders users from API and shows count', async () => {
    const wrapper = mount(HomeView, {
      global: {
        stubs: {
          // Stub popup to keep DOM simple
          CreateDashboardPopup: { template: '<div data-testid="popup">popup</div>' }
        }
      }
    })

    await flushPromises()

    const items = wrapper.findAll('li.user-item')
    expect(items.length).toBe(2)
    expect(items[0].text()).toContain('Alice')
    expect(items[1].text()).toContain('Bob')
  })

  it('navigates to SoloDashboard with userId and userName on click', async () => {
    const wrapper = mount(HomeView, {
      global: {
        stubs: { CreateDashboardPopup: { template: '<div />' } }
      }
    })

    await flushPromises()
    const first = wrapper.findAll('li.user-item')[0]
    await first.trigger('click')

    expect(push).toHaveBeenCalledWith({
      name: 'SoloDashboard',
      query: { userId: 1, userName: 'Alice' }
    })
  })

  it('toggles the create popup from the header button', async () => {
    const wrapper = mount(HomeView, {
      global: {
        stubs: { CreateDashboardPopup: { template: '<div data-testid="popup">popup</div>' } }
      }
    })

    await flushPromises()
    const btn = wrapper.findAll('button.toggle-user-btn')[0]
    await btn.trigger('click')
    await flushPromises()
    expect(wrapper.findAll('[data-testid="popup"]').length > 0).toBe(true)

    await btn.trigger('click')
    await flushPromises()
    expect(wrapper.findAll('[data-testid="popup"]').length).toBe(0)
  })
})
