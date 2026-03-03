import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AccountDropdownList from '@/components/sidebar/AccountDropdownList.vue'

const mockAccounts = [
  { puuid: 'puuid-1', accountId: 'id-1', gameName: 'FakerMain', tagLine: 'EUW', region: 'euw1', profileIconId: 1234 },
  { puuid: 'puuid-2', accountId: 'id-2', gameName: 'FakerSmurf', tagLine: 'NA', region: 'na1', profileIconId: 5678 }
]

function mountComponent(props = {}) {
  return mount(AccountDropdownList, {
    props: {
      accounts: mockAccounts,
      activeAccountPuuid: 'overall',
      showOverall: true,
      focusedIndex: -1,
      ddVersion: '16.1.1',
      ...props
    }
  })
}

describe('AccountDropdownList.vue', () => {
  describe('Rendering', () => {
    it('renders the list wrapper', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="account-dropdown-list"]').exists()).toBe(true)
    })

    it('renders Overall option when showOverall is true', () => {
      const wrapper = mountComponent({ showOverall: true })
      expect(wrapper.find('[data-testid="account-option-overall"]').exists()).toBe(true)
    })

    it('does not render Overall option when showOverall is false', () => {
      const wrapper = mountComponent({ showOverall: false })
      expect(wrapper.find('[data-testid="account-option-overall"]').exists()).toBe(false)
    })

    it('renders all accounts', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="account-option-FakerMain"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="account-option-FakerSmurf"]').exists()).toBe(true)
    })

    it('renders Link Account as a <button> element', () => {
      const wrapper = mountComponent()
      const linkBtn = wrapper.find('[data-testid="account-switcher-link-button"]')
      expect(linkBtn.element.tagName).toBe('BUTTON')
      expect(linkBtn.attributes('type')).toBe('button')
    })

    it('displays formatted region label for each account', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="account-option-FakerMain"]').text()).toContain('EUW')
      expect(wrapper.find('[data-testid="account-option-FakerSmurf"]').text()).toContain('NA')
    })
  })

  describe('Active selection', () => {
    it('marks overall option as active when activeAccountPuuid is "overall"', () => {
      const wrapper = mountComponent({ activeAccountPuuid: 'overall' })
      const overall = wrapper.find('[data-testid="account-option-overall"]')
      expect(overall.attributes('aria-selected')).toBe('true')
    })

    it('does not mark overall as active when a specific account is active', () => {
      const wrapper = mountComponent({ activeAccountPuuid: 'id-1' })
      const overall = wrapper.find('[data-testid="account-option-overall"]')
      expect(overall.attributes('aria-selected')).toBe('false')
    })

    it('marks account as active when its accountId matches activeAccountPuuid', () => {
      const wrapper = mountComponent({ activeAccountPuuid: 'id-1' })
      const option = wrapper.find('[data-testid="account-option-FakerMain"]')
      expect(option.attributes('aria-selected')).toBe('true')
    })

    it('marks account as active when its puuid matches activeAccountPuuid', () => {
      const accounts = [{ puuid: 'puuid-only', gameName: 'PuuidAccount', tagLine: 'KR', region: 'kr' }]
      const wrapper = mountComponent({ accounts, activeAccountPuuid: 'puuid-only', showOverall: false })
      const option = wrapper.find('[data-testid="account-option-PuuidAccount"]')
      expect(option.attributes('aria-selected')).toBe('true')
    })

    it('does not mark other accounts as active when one is selected', () => {
      const wrapper = mountComponent({ activeAccountPuuid: 'id-1' })
      const inactive = wrapper.find('[data-testid="account-option-FakerSmurf"]')
      expect(inactive.attributes('aria-selected')).toBe('false')
    })
  })

  describe('Focus indication', () => {
    it('marks the Overall option with data-focused when focusedIndex is 0 and showOverall is true', () => {
      const wrapper = mountComponent({ focusedIndex: 0 })
      const overall = wrapper.find('[data-testid="account-option-overall"]')
      expect(overall.attributes('data-focused')).toBe('true')
    })

    it('marks the first account with data-focused when focusedIndex is 1 and showOverall is true', () => {
      const wrapper = mountComponent({ focusedIndex: 1 })
      const option = wrapper.find('[data-testid="account-option-FakerMain"]')
      expect(option.attributes('data-focused')).toBe('true')
    })

    it('marks the first account with data-focused when focusedIndex is 0 and showOverall is false', () => {
      const wrapper = mountComponent({ showOverall: false, focusedIndex: 0 })
      const option = wrapper.find('[data-testid="account-option-FakerMain"]')
      expect(option.attributes('data-focused')).toBe('true')
    })

    it('does not mark any option when focusedIndex is -1', () => {
      const wrapper = mountComponent({ focusedIndex: -1 })
      const overall = wrapper.find('[data-testid="account-option-overall"]')
      expect(overall.attributes('data-focused')).toBe('false')
    })
  })

  describe('Events', () => {
    it('emits "select" with "overall" when Overall option is clicked', async () => {
      const wrapper = mountComponent({ showOverall: true })
      await wrapper.find('[data-testid="account-option-overall"]').trigger('click')
      expect(wrapper.emitted('select')).toBeTruthy()
      expect(wrapper.emitted('select')[0]).toEqual(['overall'])
    })

    it('emits "select" with accountId when an account option is clicked', async () => {
      const wrapper = mountComponent()
      await wrapper.find('[data-testid="account-option-FakerMain"]').trigger('click')
      expect(wrapper.emitted('select')[0]).toEqual(['id-1'])
    })

    it('emits "select" with puuid when account has no accountId', async () => {
      const accounts = [{ puuid: 'puuid-only', gameName: 'PuuidAccount', tagLine: 'KR', region: 'kr' }]
      const wrapper = mountComponent({ accounts, showOverall: false })
      await wrapper.find('[data-testid="account-option-PuuidAccount"]').trigger('click')
      expect(wrapper.emitted('select')[0]).toEqual(['puuid-only'])
    })

    it('emits "link" when Link Account button is clicked', async () => {
      const wrapper = mountComponent()
      await wrapper.find('[data-testid="account-switcher-link-button"]').trigger('click')
      expect(wrapper.emitted('link')).toBeTruthy()
    })
  })

  describe('Region formatting', () => {
    it('formats known region codes to display labels', () => {
      const wrapper = mountComponent()
      expect(wrapper.text()).toContain('EUW')
      expect(wrapper.text()).toContain('NA')
    })

    it('uppercases unknown region codes', () => {
      const accounts = [{ puuid: 'p', gameName: 'Test', tagLine: 'TEST', region: 'xyz99' }]
      const wrapper = mountComponent({ accounts, showOverall: false })
      expect(wrapper.find('[data-testid="account-option-Test"]').text()).toContain('XYZ99')
    })
  })

  describe('Profile icon', () => {
    it('renders profile icon img when profileIconId is provided', () => {
      const wrapper = mountComponent()
      const option = wrapper.find('[data-testid="account-option-FakerMain"]')
      expect(option.find('img').exists()).toBe(true)
    })

    it('renders fallback icon when profileIconId is absent', () => {
      const accounts = [{ puuid: 'p', gameName: 'NoIcon', tagLine: 'EUW', region: 'euw1' }]
      const wrapper = mountComponent({ accounts, showOverall: false })
      const option = wrapper.find('[data-testid="account-option-NoIcon"]')
      expect(option.find('img').exists()).toBe(false)
      expect(option.find('svg').exists()).toBe(true)
    })
  })
})
