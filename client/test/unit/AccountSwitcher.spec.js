import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, TransitionStub } from '@vue/test-utils'
import AccountSwitcher from '@/components/sidebar/AccountSwitcher.vue'

// Stub AccountDropdownList to keep tests focused on AccountSwitcher behavior
vi.mock('@/components/sidebar/AccountDropdownList.vue', () => ({
  default: {
    name: 'AccountDropdownList',
    props: ['accounts', 'activeAccountPuuid', 'showOverall', 'focusedIndex', 'ddVersion'],
    emits: ['select'],
    template: `
      <div data-testid="account-dropdown-list">
        <div
          v-if="showOverall"
          data-testid="overall-option"
          @click="$emit('select', 'overall')"
        >Overall</div>
        <div
          v-for="account in accounts"
          :key="account.puuid || account.accountId"
          :data-testid="'account-option-' + account.gameName"
          :data-active="(account.accountId && account.accountId === activeAccountPuuid) || (account.puuid && account.puuid === activeAccountPuuid)"
          @click="$emit('select', account.accountId || account.puuid)"
        >{{ account.gameName }}</div>
      </div>
    `
  }
}))

const mockAccounts = [
  { puuid: 'puuid-1', accountId: 'id-1', gameName: 'FakerMain', tagLine: 'EUW', region: 'euw1', profileIconId: 1234 },
  { puuid: 'puuid-2', accountId: 'id-2', gameName: 'FakerSmurf', tagLine: 'EUW', region: 'euw1', profileIconId: 5678 }
]

const singleAccount = [mockAccounts[0]]

function createWrapper(props = {}) {
  return mount(AccountSwitcher, {
    props: {
      collapsed: false,
      accounts: mockAccounts,
      activeAccountPuuid: 'overall',
      showOverall: true,
      ...props
    },
    global: {
      stubs: { Transition: TransitionStub }
    }
  })
}

describe('AccountSwitcher.vue', () => {
  describe('Expanded mode — trigger row', () => {
    it('shows "Overall" label when activeAccountPuuid is "overall"', () => {
      const wrapper = createWrapper({ activeAccountPuuid: 'overall' })
      const trigger = wrapper.find('[data-testid="account-switcher-trigger"]')
      expect(trigger.text()).toContain('Overall')
    })

    it('shows active account gameName and tagLine when an account is active', () => {
      const wrapper = createWrapper({ activeAccountPuuid: 'puuid-1', accounts: mockAccounts })
      const trigger = wrapper.find('[data-testid="account-switcher-trigger"]')
      expect(trigger.text()).toContain('FakerMain')
      expect(trigger.text()).toContain('#EUW')
    })

    it('shows region label for active account', () => {
      const wrapper = createWrapper({ activeAccountPuuid: 'puuid-1', accounts: mockAccounts })
      const trigger = wrapper.find('[data-testid="account-switcher-trigger"]')
      expect(trigger.text()).toContain('EUW')
    })

    it('renders Σ icon when active account is Overall', () => {
      const wrapper = createWrapper({ activeAccountPuuid: 'overall' })
      const trigger = wrapper.find('[data-testid="account-switcher-trigger"]')
      expect(trigger.text()).toContain('Σ')
    })

    it('sets aria-haspopup="listbox" on trigger', () => {
      const wrapper = createWrapper()
      const trigger = wrapper.find('[data-testid="account-switcher-trigger"]')
      expect(trigger.attributes('aria-haspopup')).toBe('listbox')
    })

    it('sets aria-expanded="false" when closed', () => {
      const wrapper = createWrapper()
      const trigger = wrapper.find('[data-testid="account-switcher-trigger"]')
      expect(trigger.attributes('aria-expanded')).toBe('false')
    })
  })

  describe('Expanded mode — dropdown', () => {
    it('does not show dropdown by default', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="account-switcher-dropdown"]').exists()).toBe(false)
    })

    it('opens dropdown when trigger is clicked', async () => {
      const wrapper = createWrapper()
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      expect(wrapper.find('[data-testid="account-switcher-dropdown"]').exists()).toBe(true)
    })

    it('sets aria-expanded="true" when dropdown is open', async () => {
      const wrapper = createWrapper()
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      const trigger = wrapper.find('[data-testid="account-switcher-trigger"]')
      expect(trigger.attributes('aria-expanded')).toBe('true')
    })

    it('closes dropdown when trigger is clicked again', async () => {
      const wrapper = createWrapper()
      const trigger = wrapper.find('[data-testid="account-switcher-trigger"]')
      await trigger.trigger('click')
      await trigger.trigger('click')
      expect(wrapper.find('[data-testid="account-switcher-dropdown"]').exists()).toBe(false)
    })

    it('dropdown has role="listbox"', async () => {
      const wrapper = createWrapper()
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      const dropdown = wrapper.find('[data-testid="account-switcher-dropdown"]')
      expect(dropdown.attributes('role')).toBe('listbox')
    })

    it('passes showOverall=true when 2+ accounts are linked', async () => {
      const wrapper = createWrapper({ showOverall: true, accounts: mockAccounts })
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      expect(wrapper.find('[data-testid="overall-option"]').exists()).toBe(true)
    })

    it('hides Overall option in dropdown when showOverall is false', async () => {
      const wrapper = createWrapper({ showOverall: false, accounts: singleAccount, activeAccountPuuid: 'puuid-1' })
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      expect(wrapper.find('[data-testid="overall-option"]').exists()).toBe(false)
    })

    it('lists all accounts in the dropdown', async () => {
      const wrapper = createWrapper({ accounts: mockAccounts })
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      expect(wrapper.find('[data-testid="account-option-FakerMain"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="account-option-FakerSmurf"]').exists()).toBe(true)
    })

    it('emits select with correct identifier when account option is clicked', async () => {
      const wrapper = createWrapper({ accounts: mockAccounts, activeAccountPuuid: 'overall' })
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      await wrapper.find('[data-testid="account-option-FakerMain"]').trigger('click')
      expect(wrapper.emitted('select')).toBeTruthy()
      expect(wrapper.emitted('select')[0]).toEqual(['id-1'])
    })

    it('closes dropdown after selecting an account', async () => {
      const wrapper = createWrapper({ accounts: mockAccounts })
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      await wrapper.find('[data-testid="account-option-FakerMain"]').trigger('click')
      expect(wrapper.find('[data-testid="account-switcher-dropdown"]').exists()).toBe(false)
    })

    it('emits select with "overall" when Overall option is clicked', async () => {
      const wrapper = createWrapper({ accounts: mockAccounts, showOverall: true, activeAccountPuuid: 'puuid-1' })
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      await wrapper.find('[data-testid="overall-option"]').trigger('click')
      expect(wrapper.emitted('select')[0]).toEqual(['overall'])
    })

    it('closes dropdown on Escape key', async () => {
      const wrapper = createWrapper()
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('click')
      expect(wrapper.find('[data-testid="account-switcher-dropdown"]').exists()).toBe(true)
      await wrapper.find('[data-testid="account-switcher-trigger"]').trigger('keydown', { key: 'Escape' })
      expect(wrapper.find('[data-testid="account-switcher-dropdown"]').exists()).toBe(false)
    })
  })

  describe('Collapsed mode', () => {
    it('renders collapsed trigger instead of expanded trigger', () => {
      const wrapper = createWrapper({ collapsed: true })
      expect(wrapper.find('[data-testid="account-switcher-trigger-collapsed"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="account-switcher-trigger"]').exists()).toBe(false)
    })

    it('shows Σ icon in collapsed trigger when overall is active', () => {
      const wrapper = createWrapper({ collapsed: true, activeAccountPuuid: 'overall' })
      const trigger = wrapper.find('[data-testid="account-switcher-trigger-collapsed"]')
      expect(trigger.text()).toContain('Σ')
    })

    it('opens popover when collapsed trigger is clicked', async () => {
      const wrapper = createWrapper({ collapsed: true })
      await wrapper.find('[data-testid="account-switcher-trigger-collapsed"]').trigger('click')
      expect(wrapper.find('[data-testid="account-switcher-popover"]').exists()).toBe(true)
    })

    it('popover has role="listbox"', async () => {
      const wrapper = createWrapper({ collapsed: true })
      await wrapper.find('[data-testid="account-switcher-trigger-collapsed"]').trigger('click')
      const popover = wrapper.find('[data-testid="account-switcher-popover"]')
      expect(popover.attributes('role')).toBe('listbox')
    })

    it('popover lists all accounts', async () => {
      const wrapper = createWrapper({ collapsed: true, accounts: mockAccounts, showOverall: true })
      await wrapper.find('[data-testid="account-switcher-trigger-collapsed"]').trigger('click')
      expect(wrapper.find('[data-testid="account-option-FakerMain"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="account-option-FakerSmurf"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="overall-option"]').exists()).toBe(true)
    })

    it('closes popover after selecting an account', async () => {
      const wrapper = createWrapper({ collapsed: true, accounts: mockAccounts })
      await wrapper.find('[data-testid="account-switcher-trigger-collapsed"]').trigger('click')
      await wrapper.find('[data-testid="account-option-FakerMain"]').trigger('click')
      expect(wrapper.find('[data-testid="account-switcher-popover"]').exists()).toBe(false)
    })
  })
})
