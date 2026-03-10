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

    it('displays flex and solo rank lines', () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.findAll('.rank-line')).toHaveLength(2)
      expect(firstCard.text()).toContain('Flex')
      expect(firstCard.text()).toContain('Solo')
    })

    it('displays LP in rank value when rank data includes lp', () => {
      const accounts = [
        {
          accountId: 'acc_1',
          gameName: 'Test',
          tagLine: 'EUW',
          soloTier: 'DIAMOND',
          soloRank: 'II',
          soloLp: 67
        }
      ]
      const wrapper = createWrapper({ accounts })
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.text()).toContain('Diamond II - 67 LP')
    })

    it('shows unranked values when rank data is missing', () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      const rankValues = firstCard.findAll('.rank-value')
      expect(rankValues).toHaveLength(2)
      expect(rankValues[0].text()).toBe('Unranked')
      expect(rankValues[1].text()).toBe('Unranked')
    })

    it('renders primary chip when account is primary', () => {
      const accounts = [
        {
          accountId: 'acc_1',
          gameName: 'Test',
          tagLine: 'EUW',
          isPrimary: true
        }
      ]
      const wrapper = createWrapper({ accounts })
      expect(wrapper.find('.primary-chip').text()).toBe('Primary')
    })

    it('renders avatar fallback when no profile icon url is provided', () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.find('.account-avatar-fallback').exists()).toBe(true)
    })

    it('renders summoner level badge when summonerLevel is provided', () => {
      const accounts = [
        {
          accountId: 'acc_1',
          gameName: 'Test',
          tagLine: 'EUW',
          summonerLevel: 120
        }
      ]
      const wrapper = createWrapper({ accounts })
      expect(wrapper.find('.level-badge').text()).toBe('120')
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
    it('does not emit select event when card is clicked', async () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      
      await firstCard.trigger('click')
      
      expect(wrapper.emitted('select')).toBeFalsy()
    })

    it('does not emit select event when any card is clicked', async () => {
      const wrapper = createWrapper()
      const secondCard = wrapper.find('[data-testid="account-card-acc_2"]')
      
      await secondCard.trigger('click')
      
      expect(wrapper.emitted('select')).toBeFalsy()
    })

    it('does not emit select event on Enter keydown', async () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      
      await firstCard.trigger('keydown.enter')
      
      expect(wrapper.emitted('select')).toBeFalsy()
    })

    it('does not emit select event on Space keydown', async () => {
      const wrapper = createWrapper()
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      
      await firstCard.trigger('keydown.space')
      
      expect(wrapper.emitted('select')).toBeFalsy()
    })
  })

  describe('Active Account', () => {
    it('adds account-card--active class when activeAccountPuuid matches account puuid', () => {
      const accounts = [
        { ...mockAccounts[0], puuid: 'puuid_1' },
        { ...mockAccounts[1], puuid: 'puuid_2' }
      ]
      const wrapper = createWrapper({ accounts, activeAccountPuuid: 'puuid_1' })
      const firstCard = wrapper.find('[data-testid="account-card-acc_1"]')
      expect(firstCard.classes()).toContain('account-card--active')
    })

    it('does not add account-card--active class when activeAccountPuuid does not match', () => {
      const accounts = [
        { ...mockAccounts[0], puuid: 'puuid_1' },
        { ...mockAccounts[1], puuid: 'puuid_2' }
      ]
      const wrapper = createWrapper({ accounts, activeAccountPuuid: 'puuid_1' })
      const secondCard = wrapper.find('[data-testid="account-card-acc_2"]')
      expect(secondCard.classes()).not.toContain('account-card--active')
    })

    it('no card is active when activeAccountPuuid is null', () => {
      const wrapper = createWrapper({ activeAccountPuuid: null })
      const cards = wrapper.findAll('.account-card')
      cards.forEach(card => {
        expect(card.classes()).not.toContain('account-card--active')
      })
    })
  })

  describe('Accessibility', () => {
    it('account cards render as div elements', () => {
      const wrapper = createWrapper()
      const cards = wrapper.findAll('.account-card')
      cards.forEach(card => {
        expect(card.element.tagName).toBe('DIV')
      })
    })

    it('has proper data-testid for testing', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="overview-account-cards"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="account-card-acc_1"]').exists()).toBe(true)
    })
  })
})
