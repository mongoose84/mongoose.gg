import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import OverviewAccountCards from '@/components/overview/OverviewAccountCards.vue'

describe('OverviewAccountCards.vue', () => {
  const mockAccounts = [
    {
      accountId: 'acc_1',
      gameName: 'FakerMain',
      tagLine: 'EUW',
      region: 'EUW',
      rank: 'Diamond II',
      lp: 67,
      gamesToday: 5,
      gamesThisWeek: 23
    },
    {
      accountId: 'acc_2',
      gameName: 'FakerSmurf',
      tagLine: 'NA1',
      region: 'NA',
      rank: 'Platinum I',
      lp: 45,
      gamesToday: 2,
      gamesThisWeek: 8
    },
    {
      accountId: 'acc_3',
      gameName: 'FakerFlex',
      tagLine: 'KR',
      region: 'KR',
      rank: 'Gold II',
      lp: 20,
      gamesToday: 0,
      gamesThisWeek: 1
    }
  ]

  const createWrapper = (props = {}) => {
    return mount(OverviewAccountCards, {
      props: {
        accounts: mockAccounts,
        activeAccountPuuid: null,
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component', () => {
      const wrapper = createWrapper()
      expect(wrapper.exists()).toBe(true)
      expect(wrapper.find('[data-testid="overview-account-cards"]').exists()).toBe(true)
    })

    it('renders section title', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('.section-title').text()).toBe('Your Accounts')
    })

    it('renders correct number of account cards', () => {
      const wrapper = createWrapper()
      const cards = wrapper.findAll('.account-card')
      expect(cards).toHaveLength(3)
    })

    it('renders account with correct data-testid', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="account-card-acc_1"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="account-card-acc_2"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="account-card-acc_3"]').exists()).toBe(true)
    })

    it('renders empty cards container when no accounts', () => {
      const wrapper = createWrapper({ accounts: [] })
      const cards = wrapper.findAll('.account-card')
      expect(cards).toHaveLength(0)
    })
  })

  describe('Account Card Content', () => {
    it('displays game name and tag line', () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.find('.game-name').text()).toBe('FakerMain')
      expect(firstCard.find('.tag-line').text()).toBe('#EUW')
    })

    it('displays region and rank', () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.find('.region').text()).toBe('EUW')
      expect(firstCard.find('.rank').text()).toBe('Diamond II')
    })

    it('displays LP value', () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.find('.lp-value').text()).toBe('67 LP')
    })

    it('displays games today count', () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.find('.games-count').text()).toBe('5 games today')
    })

    it('displays "game" singular when 1 game today', () => {
      const accounts = [
        {
          accountId: 'acc_1',
          gameName: 'Test',
          tagLine: 'EUW',
          region: 'EUW',
          rank: 'Gold I',
          lp: 50,
          gamesToday: 1,
          gamesThisWeek: 1
        }
      ]
      const wrapper = createWrapper({ accounts })
      expect(wrapper.find('.games-count').text()).toBe('1 game today')
    })

    it('adds has-games class when games today > 0', () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.find('.games-today').classes()).toContain('has-games')
    })

    it('does not add has-games class when games today = 0', () => {
      const wrapper = createWrapper()
      const thirdCard = wrapper.find('[data-testid="account-card-acc_3"]')
      expect(thirdCard.find('.games-today').classes()).not.toContain('has-games')
    })

    it('does not display LP when lp is null', () => {
      const accounts = [
        {
          accountId: 'acc_1',
          gameName: 'Test',
          tagLine: 'EUW',
          region: 'EUW',
          rank: null,
          lp: null,
          gamesToday: 0,
          gamesThisWeek: 0
        }
      ]
      const wrapper = createWrapper({ accounts })
      expect(wrapper.find('.account-lp').exists()).toBe(false)
    })

    it('does not display rank when rank is null', () => {
      const accounts = [
        {
          accountId: 'acc_1',
          gameName: 'Test',
          tagLine: 'EUW',
          region: 'EUW',
          rank: null,
          lp: null,
          gamesToday: 0,
          gamesThisWeek: 0
        }
      ]
      const wrapper = createWrapper({ accounts })
      expect(wrapper.find('.rank').exists()).toBe(false)
    })
  })

  describe('Interactions', () => {
    it('emits select event when card is clicked', async () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      
      await firstCard.trigger('click')
      
      expect(wrapper.emitted('select')).toBeTruthy()
      expect(wrapper.emitted('select')[0]).toEqual(['acc_1'])
    })

    it('emits select event with correct accountId', async () => {
      const wrapper = createWrapper()
      const secondCard = wrapper.find('[data-testid="account-card-acc_2"]')
      
      await secondCard.trigger('click')
      
      expect(wrapper.emitted('select')[0]).toEqual(['acc_2'])
    })

    it('emits select event on Enter keydown', async () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      
      await firstCard.trigger('keydown.enter')
      
      expect(wrapper.emitted('select')).toBeTruthy()
      expect(wrapper.emitted('select')[0]).toEqual(['acc_1'])
    })

    it('emits select event on Space keydown', async () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      
      await firstCard.trigger('keydown.space')
      
      expect(wrapper.emitted('select')).toBeTruthy()
      expect(wrapper.emitted('select')[0]).toEqual(['acc_1'])
    })
  })

  describe('Active Account', () => {
    it('adds is-active class when activeAccountPuuid matches', () => {
      const wrapper = createWrapper({ activeAccountPuuid: 'acc_1' })
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.classes()).toContain('is-active')
    })

    it('does not add is-active class when activeAccountPuuid does not match', () => {
      const wrapper = createWrapper({ activeAccountPuuid: 'acc_1' })
      const secondCard = wrapper.find('[data-testid="account-card-acc_2"]')
      expect(secondCard.classes()).not.toContain('is-active')
    })

    it('no card is active when activeAccountPuuid is null', () => {
      const wrapper = createWrapper({ activeAccountPuuid: null })
      const cards = wrapper.findAll('.account-card')
      cards.forEach(card => {
        expect(card.classes()).not.toContain('is-active')
      })
    })
  })

  describe('Accessibility', () => {
    it('account cards are keyboard navigable (button elements)', () => {
      const wrapper = createWrapper()
      const cards = wrapper.findAll('.account-card')
      cards.forEach(card => {
        expect(card.element.tagName).toBe('BUTTON')
      })
    })

    it('has proper data-testid for testing', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="overview-account-cards"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="account-card-acc_1"]').exists()).toBe(true)
    })
  })
})
