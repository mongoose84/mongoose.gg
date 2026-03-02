import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { nextTick } from 'vue'
import LinkedAccountsSection from '@/components/settings/LinkedAccountsSection.vue'
import { headlessUIStubs } from '../helpers/testUtils'

const mockAuthStore = {
  riotAccounts: [],
  tier: 'free',
  normalizedTier: 'free',
  hasReachedRiotAccountLimit: false,
  setPrimary: vi.fn(),
  triggerSync: vi.fn(),
  unlinkRiotAccount: vi.fn(),
  refreshUser: vi.fn()
}

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => mockAuthStore
}))

describe('LinkedAccountsSection.vue', () => {
  const accounts = [
    {
      puuid: 'acc-1',
      gameName: 'Main',
      tagLine: 'EUW',
      region: 'euw1',
      isPrimary: true,
      soloTier: 'DIAMOND',
      soloRank: 'II',
      soloLp: 70,
      flexTier: null,
      flexRank: null,
      flexLp: null,
      lastSyncAt: new Date().toISOString()
    },
    {
      puuid: 'acc-2',
      gameName: 'Smurf',
      tagLine: 'EUW',
      region: 'euw1',
      isPrimary: false,
      soloTier: 'PLATINUM',
      soloRank: 'I',
      soloLp: 45,
      flexTier: null,
      flexRank: null,
      flexLp: null,
      lastSyncAt: new Date().toISOString()
    }
  ]

  const createWrapper = () => mount(LinkedAccountsSection, {
    global: {
      stubs: {
        ...headlessUIStubs,
        LinkRiotAccountModal: true
      }
    }
  })

  beforeEach(() => {
    mockAuthStore.riotAccounts = []
    mockAuthStore.tier = 'free'
    mockAuthStore.normalizedTier = 'free'
    mockAuthStore.hasReachedRiotAccountLimit = false
    mockAuthStore.setPrimary.mockReset()
    mockAuthStore.triggerSync.mockReset()
    mockAuthStore.unlinkRiotAccount.mockReset()
    mockAuthStore.refreshUser.mockReset()
  })

  it('renders linked accounts list correctly', () => {
    mockAuthStore.riotAccounts = accounts

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="linked-accounts-list"]').exists()).toBe(true)
    expect(wrapper.findAll('[data-testid="linked-account-row"]').length).toBe(2)
  })

  it('shows upgrade prompt for free tier after first linked account', () => {
    mockAuthStore.riotAccounts = [accounts[0]]
    mockAuthStore.normalizedTier = 'free'
    mockAuthStore.hasReachedRiotAccountLimit = true

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="upgrade-prompt"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="link-another-account-button"]').exists()).toBe(false)
  })

  it('shows link another account button for pro tier', () => {
    mockAuthStore.riotAccounts = [accounts[0]]
    mockAuthStore.normalizedTier = 'pro'
    mockAuthStore.hasReachedRiotAccountLimit = false

    const wrapper = createWrapper()

    expect(wrapper.find('[data-testid="link-another-account-button"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="upgrade-prompt"]').exists()).toBe(false)
  })

  it('uses normalizedTier getter for pro tier label and CTA behavior', () => {
    mockAuthStore.riotAccounts = [accounts[0]]
    mockAuthStore.normalizedTier = 'pro'
    mockAuthStore.hasReachedRiotAccountLimit = false

    const wrapper = createWrapper()

    expect(wrapper.text()).toContain('Pro tier')
    expect(wrapper.find('[data-testid="link-another-account-button"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="upgrade-prompt"]').exists()).toBe(false)
  })

  it('uses normalizedTier getter for free tier label and CTA behavior', () => {
    mockAuthStore.riotAccounts = [accounts[0]]
    mockAuthStore.normalizedTier = 'free'
    mockAuthStore.hasReachedRiotAccountLimit = true

    const wrapper = createWrapper()

    expect(wrapper.text()).toContain('Free tier')
    expect(wrapper.find('[data-testid="upgrade-prompt"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="link-another-account-button"]').exists()).toBe(false)
  })

  it('remove confirmation flow unlinks account on confirm', async () => {
    mockAuthStore.riotAccounts = [accounts[0]]
    mockAuthStore.unlinkRiotAccount.mockResolvedValue({ success: true })

    const wrapper = createWrapper()

    await wrapper.find('[data-testid="remove-button"]').trigger('click')
    await nextTick()

    expect(wrapper.text()).toContain('Remove Main#EUW?')

    await wrapper.find('[data-testid="confirm-remove-account"]').trigger('click')
    await flushPromises()

    expect(mockAuthStore.unlinkRiotAccount).toHaveBeenCalledWith('acc-1')
  })
})
