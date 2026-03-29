import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import DisplayPreferencesSection from '@/components/settings/DisplayPreferencesSection.vue'

const mockAuthStore = {
  riotAccounts: []
}

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => mockAuthStore
}))

const mockDefaultView = { defaultView: { value: 'overall' }, setDefaultView: vi.fn() }
vi.mock('@/composables/useDefaultView', () => ({
  useDefaultView: () => mockDefaultView
}))

const mockChartMode = { chartMode: { value: 'merged' }, setChartMode: vi.fn() }
vi.mock('@/composables/useChartDisplayMode', () => ({
  useChartDisplayMode: () => mockChartMode
}))

describe('DisplayPreferencesSection.vue', () => {
  const accounts = [
    { puuid: 'puuid-1', gameName: 'FakerMain', tagLine: 'EUW', isPrimary: true },
    { puuid: 'puuid-2', gameName: 'FakerSmurf', tagLine: 'EUW', isPrimary: false }
  ]

  const createWrapper = () => mount(DisplayPreferencesSection)

  beforeEach(() => {
    mockAuthStore.riotAccounts = []
    mockDefaultView.defaultView = { value: 'overall' }
    mockDefaultView.setDefaultView.mockReset()
    mockChartMode.chartMode = { value: 'merged' }
    mockChartMode.setChartMode.mockReset()
  })

  it('is hidden when only 1 account is linked', () => {
    mockAuthStore.riotAccounts = [accounts[0]]

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="display-preferences-section"]').exists()).toBe(false)
  })

  it('is hidden when no accounts are linked', () => {
    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="display-preferences-section"]').exists()).toBe(false)
  })

  it('renders when 2+ accounts are linked', () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="display-preferences-section"]').exists()).toBe(true)
  })

  it('default view dropdown lists "Overall" plus all linked accounts', () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()
    const options = wrapper.find('[data-testid="default-view-select"]').findAll('option')

    expect(options).toHaveLength(3)
    expect(options[0].text()).toBe('Overall')
    expect(options[1].text()).toBe('FakerMain#EUW')
    expect(options[2].text()).toBe('FakerSmurf#EUW')
  })

  it('selecting a default view calls setDefaultView', async () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()
    const select = wrapper.find('[data-testid="default-view-select"]')
    await select.setValue('puuid-1')

    expect(mockDefaultView.setDefaultView).toHaveBeenCalledWith('puuid-1')
  })

  it('chart mode dropdown lists both options', () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()
    const options = wrapper.find('[data-testid="chart-mode-select"]').findAll('option')

    expect(options).toHaveLength(2)
    expect(options[0].text()).toBe('Merged (single line)')
    expect(options[1].text()).toBe('Per-Account Lines')
  })

  it('selecting a chart mode calls setChartMode', async () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()
    const select = wrapper.find('[data-testid="chart-mode-select"]')
    await select.setValue('per-account')

    expect(mockChartMode.setChartMode).toHaveBeenCalledWith('per-account')
  })

  it('dropdowns have accessible labels and descriptions', () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()
    const defaultViewSelect = wrapper.find('[data-testid="default-view-select"]')
    const chartModeSelect = wrapper.find('[data-testid="chart-mode-select"]')

    // Each select should have an id that matches a label's for attribute
    const defaultViewId = defaultViewSelect.attributes('id')
    const chartModeId = chartModeSelect.attributes('id')

    expect(wrapper.find(`label[for="${defaultViewId}"]`).exists()).toBe(true)
    expect(wrapper.find(`label[for="${chartModeId}"]`).exists()).toBe(true)

    // Each select should have aria-describedby pointing to description text
    expect(defaultViewSelect.attributes('aria-describedby')).toBeTruthy()
    expect(chartModeSelect.attributes('aria-describedby')).toBeTruthy()
  })
})