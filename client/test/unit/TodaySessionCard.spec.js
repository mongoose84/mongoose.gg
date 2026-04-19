import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import TodaySessionCard from '../../src/components/overview/TodaySessionCard.vue'

vi.mock('../../src/utils/leagueAssets', () => ({
  getChampionIconUrl: (name) => `https://cdn.example.com/icon/${name}.png`,
  getChampionSplashUrl: (name) => `https://cdn.example.com/splash/${name}.jpg`
}))

const sessionStatsToday = {
  gamesToday: 7,
  winsToday: 5,
  lossesToday: 2,
  avgKdaToday: 2.8,
  bestChampionToday: { championName: 'Jinx', wins: 3, losses: 1, avgKda: 3.2 },
  gamesThisWeek: 15,
  winsThisWeek: 9,
  lossesThisWeek: 6,
  avgKdaThisWeek: 3.1
}

const sessionStatsWeekOnly = {
  gamesToday: 0,
  winsToday: 0,
  lossesToday: 0,
  avgKdaToday: null,
  bestChampionToday: null,
  gamesThisWeek: 12,
  winsThisWeek: 8,
  lossesThisWeek: 4,
  avgKdaThisWeek: 3.1
}

const combinedStats = {
  totalGames: 148,
  winRate: 52,
  avgKda: 2.6
}

describe('TodaySessionCard', () => {
  it('renders today\'s stats when gamesToday > 0', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: sessionStatsToday, combinedStats, loading: false }
    })

    expect(wrapper.find('[data-testid="today-session-card"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('TODAY\'S SESSION')
    expect(wrapper.find('[data-testid="win-rate"]').text()).toContain('71%')
    expect(wrapper.text()).toContain('5W 2L')
  })

  it('shows champion splash mural when best champion exists', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: sessionStatsToday, combinedStats, loading: false }
    })

    const muralImg = wrapper.find('.session-mural-image')
    expect(muralImg.exists()).toBe(true)
    expect(muralImg.attributes('src')).toContain('Jinx')
  })

  it('shows champion badge with icon in today state', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: sessionStatsToday, combinedStats, loading: false }
    })

    const badge = wrapper.find('[data-testid="champion-badge"]')
    expect(badge.exists()).toBe(true)
    expect(badge.text()).toContain('Jinx')
  })

  it('left border uses success color when WR >= 55%', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: sessionStatsToday, combinedStats, loading: false }
    })
    // 5W/2L = 71% -> accent-success
    expect(wrapper.find('[data-testid="today-session-card"]').classes()).toContain('accent-success')
  })

  it('left border uses error color when WR < 45%', () => {
    const lowWrSession = { ...sessionStatsToday, winsToday: 1, lossesToday: 4 }
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: lowWrSession, combinedStats, loading: false }
    })
    // 1W/4L = 20% -> accent-error
    expect(wrapper.find('[data-testid="today-session-card"]').classes()).toContain('accent-error')
  })

  it('falls back to "THIS WEEK" when gamesToday === 0 and gamesThisWeek > 0', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: sessionStatsWeekOnly, combinedStats, loading: false }
    })

    expect(wrapper.text()).toContain('THIS WEEK')
    expect(wrapper.text()).toContain('8W 4L')
    // No mural in week state
    expect(wrapper.find('.session-mural-image').exists()).toBe(false)
  })

  it('falls back to "THIS SEASON" using combinedStats when no session data', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: null, combinedStats, loading: false }
    })

    expect(wrapper.text()).toContain('THIS SEASON')
    expect(wrapper.find('[data-testid="win-rate"]').text()).toContain('52%')
    expect(wrapper.text()).toContain('148 games')
  })

  it('"THIS SEASON" state does not render W/L strip', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: null, combinedStats, loading: false }
    })

    expect(wrapper.find('[data-testid="wl-strip"]').exists()).toBe(false)
  })

  it('shows loading skeleton when loading is true', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: null, combinedStats: null, loading: true }
    })

    expect(wrapper.find('[data-testid="session-skeleton"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="win-rate"]').exists()).toBe(false)
  })

  it('renders W/L strip in today state', () => {
    const wrapper = mount(TodaySessionCard, {
      props: { sessionStats: sessionStatsToday, combinedStats, loading: false }
    })

    const strip = wrapper.find('[data-testid="wl-strip"]')
    expect(strip.exists()).toBe(true)
    const dots = strip.findAll('.wl-indicator')
    expect(dots).toHaveLength(7) // 5W + 2L
  })
})
