import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { ref } from 'vue'
import DisplayPreferencesSection from '@/components/settings/DisplayPreferencesSection.vue'

const mockAuthStore = {
  riotAccounts: []
}

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => mockAuthStore
}))

const mockSetDefaultView = vi.fn()
const mockDefaultView = ref('overall')
vi.mock('@/composables/useDefaultView', () => ({
  useDefaultView: () => ({ defaultView: mockDefaultView, setDefaultView: mockSetDefaultView })
}))

describe('DisplayPreferencesSection.vue', () => {
  const accounts = [
    { puuid: 'acc-1', accountId: 'aid-1', gameName: 'Main', tagLine: 'EUW', isPrimary: true },
    { puuid: 'acc-2', accountId: 'aid-2', gameName: 'Smurf', tagLine: 'EUW', isPrimary: false }
  ]

  const createWrapper = () => mount(DisplayPreferencesSection)

  beforeEach(() => {
    mockAuthStore.riotAccounts = []
    mockDefaultView.value = 'overall'
    mockSetDefaultView.mockReset()
  })

  it('is hidden when only 1 account is linked', () => {
    mockAuthStore.riotAccounts = [accounts[0]]

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="display-preferences-section"]').exists()).toBe(false)
  })

  it('is hidden when no accounts are linked', () => {
    mockAuthStore.riotAccounts = []

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="display-preferences-section"]').exists()).toBe(false)
  })

  it('renders when 2+ accounts are linked', () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="display-preferences-section"]').exists()).toBe(true)
  })

  it('default view dropdown lists "Overall" and all linked accounts', () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()
    const select = wrapper.find('[data-testid="default-view-select"]')
    const options = select.findAll('option')

    expect(options.length).toBe(3)
    expect(options[0].text()).toBe('Overall')
    expect(options[1].text()).toBe('Main#EUW')
    expect(options[2].text()).toBe('Smurf#EUW')
  })

  it('selecting a default view calls setDefaultView', async () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()
    const select = wrapper.find('[data-testid="default-view-select"]')

    await select.setValue('aid-1')

    expect(mockSetDefaultView).toHaveBeenCalledWith('aid-1')
  })

  it('omits accounts without accountId from default view dropdown', () => {
    mockAuthStore.riotAccounts = [
      { puuid: 'p-1', accountId: 'aid-1', gameName: 'Main', tagLine: 'EUW', isPrimary: true },
      { puuid: 'p-2', accountId: '', gameName: 'NoId', tagLine: 'NA', isPrimary: false }
    ]

    const wrapper = createWrapper()
    const options = wrapper.find('[data-testid="default-view-select"]').findAll('option')

    expect(options.length).toBe(2)
    expect(options[0].text()).toBe('Overall')
    expect(options[1].text()).toBe('Main#EUW')
  })
})
