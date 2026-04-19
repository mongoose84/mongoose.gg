import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import TodaySessionCard from '@/components/overview/TodaySessionCard.vue'

const stubs = {
  ChevronLeftIcon: { template: '<svg data-testid="chevron-left" />' },
  ChevronRightIcon: { template: '<svg data-testid="chevron-right" />' }
}

const sessionStats = {
  gamesToday: 3,
  winsToday: 2,
  lossesToday: 1,
  avgKdaToday: 3.5,
  bestChampionToday: { championName: 'Jinx', wins: 2, losses: 1, avgKda: 3.5 },
  gamesThisWeek: 10,
  winsThisWeek: 6,
  lossesThisWeek: 4,
  avgKdaThisWeek: 2.8
}

const combinedStats = {
  winRate: 52,
  totalGames: 134,
  avgKda: 2.6
}

function mountCard(props = {}) {
  return mount(TodaySessionCard, {
    props: { sessionStats, combinedStats, ...props },
    global: { stubs }
  })
}

describe('TodaySessionCard', () => {
  describe('Rendering', () => {
    it('shows skeleton when loading', () => {
      const wrapper = mountCard({ loading: true })
      expect(wrapper.find('[data-testid="session-skeleton"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="session-prev-btn"]').exists()).toBe(false)
    })

    it('shows nav buttons when not loading', () => {
      const wrapper = mountCard()
      expect(wrapper.find('[data-testid="session-prev-btn"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="session-next-btn"]').exists()).toBe(true)
    })

    it('shows three dot indicators', () => {
      const wrapper = mountCard()
      expect(wrapper.findAll('.session-dot')).toHaveLength(3)
    })

    it('shows champion badge on today page (page 0)', () => {
      const wrapper = mountCard()
      expect(wrapper.find('[data-testid="champion-badge"]').exists()).toBe(true)
    })
  })

  describe('Default page selection (waterfall)', () => {
    it('starts on today page when gamesToday > 0', () => {
      const wrapper = mountCard()
      expect(wrapper.find('.session-label').text()).toBe("TODAY'S SESSION")
      expect(wrapper.find('.session-dot.active').element)
        .toBe(wrapper.findAll('.session-dot')[0].element)
    })

    it('starts on week page when no games today but games this week', () => {
      const wrapper = mountCard({
        sessionStats: { ...sessionStats, gamesToday: 0, winsToday: 0, lossesToday: 0 }
      })
      expect(wrapper.find('.session-label').text()).toBe('THIS WEEK')
      expect(wrapper.find('.session-dot.active').element)
        .toBe(wrapper.findAll('.session-dot')[1].element)
    })

    it('starts on season page when no session data', () => {
      const wrapper = mountCard({ sessionStats: null })
      expect(wrapper.find('.session-label').text()).toBe('THIS SEASON')
      expect(wrapper.find('.session-dot.active').element)
        .toBe(wrapper.findAll('.session-dot')[2].element)
    })
  })

  describe('Navigation', () => {
    it('advances to next page on next button click', async () => {
      const wrapper = mountCard()
      expect(wrapper.find('.session-label').text()).toBe("TODAY'S SESSION")

      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(wrapper.find('.session-label').text()).toBe('THIS WEEK')
    })

    it('advances to season page on second next click', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(wrapper.find('.session-label').text()).toBe('THIS SEASON')
    })

    it('wraps around from season to today on next', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(wrapper.find('.session-label').text()).toBe("TODAY'S SESSION")
    })

    it('goes back to season page on prev from today', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-prev-btn"]').trigger('click')
      expect(wrapper.find('.session-label').text()).toBe('THIS SEASON')
    })

    it('goes back to today from week on prev', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      await wrapper.find('[data-testid="session-prev-btn"]').trigger('click')
      expect(wrapper.find('.session-label').text()).toBe("TODAY'S SESSION")
    })

    it('updates active dot to match current page', async () => {
      const wrapper = mountCard()
      const dots = wrapper.findAll('.session-dot')

      expect(dots[0].classes()).toContain('active')
      expect(dots[1].classes()).not.toContain('active')

      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(dots[0].classes()).not.toContain('active')
      expect(dots[1].classes()).toContain('active')
    })
  })

  describe('Page content', () => {
    it('shows today win rate on today page', () => {
      const wrapper = mountCard()
      // 2W 1L = 67%
      expect(wrapper.find('[data-testid="win-rate"]').text()).toBe('67%')
    })

    it('shows week win rate on week page', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      // 6W 4L = 60%
      expect(wrapper.find('[data-testid="win-rate"]').text()).toBe('60%')
    })

    it('shows season win rate on season page', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(wrapper.find('[data-testid="win-rate"]').text()).toBe('52%')
    })

    it('shows W/L strip on today page', () => {
      const wrapper = mountCard()
      expect(wrapper.find('[data-testid="wl-strip"]').exists()).toBe(true)
    })

    it('shows W/L strip on week page', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(wrapper.find('[data-testid="wl-strip"]').exists()).toBe(true)
    })

    it('hides W/L strip on season page', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(wrapper.find('[data-testid="wl-strip"]').exists()).toBe(false)
    })

    it('hides champion badge on week page', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(wrapper.find('[data-testid="champion-badge"]').exists()).toBe(false)
    })

    it('hides champion badge on season page', async () => {
      const wrapper = mountCard()
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      await wrapper.find('[data-testid="session-next-btn"]').trigger('click')
      expect(wrapper.find('[data-testid="champion-badge"]').exists()).toBe(false)
    })

    it('shows -- for win rate when no data on current page', () => {
      const wrapper = mountCard({
        sessionStats: { ...sessionStats, gamesToday: 0, winsToday: 0, lossesToday: 0 },
        combinedStats: null
      })
      // Starts on week page (has data), navigate to season
      mount(TodaySessionCard, {
        props: {
          sessionStats: null,
          combinedStats: null
        },
        global: { stubs }
      })
      const noDataWrapper = mount(TodaySessionCard, {
        props: { sessionStats: null, combinedStats: null },
        global: { stubs }
      })
      expect(noDataWrapper.find('[data-testid="win-rate"]').text()).toBe('--%')
    })
  })

  describe('Accessibility', () => {
    it('prev button has aria-label', () => {
      const wrapper = mountCard()
      expect(wrapper.find('[data-testid="session-prev-btn"]').attributes('aria-label'))
        .toBe('Previous time period')
    })

    it('next button has aria-label', () => {
      const wrapper = mountCard()
      expect(wrapper.find('[data-testid="session-next-btn"]').attributes('aria-label'))
        .toBe('Next time period')
    })

    it('label has aria-live="polite"', () => {
      const wrapper = mountCard()
      expect(wrapper.find('.session-label').attributes('aria-live')).toBe('polite')
    })

    it('dot container has aria-hidden="true"', () => {
      const wrapper = mountCard()
      expect(wrapper.find('.session-dots').attributes('aria-hidden')).toBe('true')
    })
  })
})
