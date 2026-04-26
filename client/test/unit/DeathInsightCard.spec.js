import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import DeathInsightsCard from '../../src/components/overview/DeathInsightsCard.vue'

const motivationalStats = {
  avgDeathsPerGame: 6.8,
  winRateLowDeaths: 0.72,
  winRateHighDeaths: 0.41,
  gamesLowDeaths: 12,
  gamesHighDeaths: 3,
  lowDeathThreshold: 5,
  highDeathThreshold: 8,
  totalGames: 20
}

const warningStats = {
  avgDeathsPerGame: 9.2,
  winRateLowDeaths: 0.50,
  winRateHighDeaths: 0.32,
  gamesLowDeaths: 5,
  gamesHighDeaths: 14,
  lowDeathThreshold: 4,
  highDeathThreshold: 6,
  totalGames: 22
}

const neutralStats = {
  avgDeathsPerGame: 5.1,
  winRateLowDeaths: 0.54,
  winRateHighDeaths: 0.47,
  gamesLowDeaths: 8,
  gamesHighDeaths: 6,
  lowDeathThreshold: 5,
  highDeathThreshold: 8,
  totalGames: 18
}

describe('DeathInsightsCard', () => {
  it('renders motivational headline when winRateLowDeaths >= 0.55 and gap >= 0.15', () => {
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: motivationalStats, loading: false }
    })

    expect(wrapper.find('[data-testid="hero-wr"]').text()).toContain('72%')
    // Hero WR should use the low-death win rate
    expect(wrapper.find('[data-testid="hero-wr"]').classes()).toContain('winrate-great')
    // Contrast row shows the high-death win rate
    const contrastRow = wrapper.find('[data-testid="contrast-row"]')
    expect(contrastRow.exists()).toBe(true)
    expect(contrastRow.text()).toContain('41%')
  })

  it('renders warning headline when winRateHighDeaths <= 0.45 and gap >= 0.15', () => {
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: warningStats, loading: false }
    })

    // Hero shows the positive (low-death) win rate
    expect(wrapper.find('[data-testid="hero-wr"]').text()).toContain('50%')
    expect(wrapper.find('[data-testid="hero-wr"]').classes()).toContain('winrate-average')
    const contrastRow = wrapper.find('[data-testid="contrast-row"]')
    expect(contrastRow.exists()).toBe(true)
    // Contrast row shows the high-death win rate
    expect(contrastRow.text()).toContain('32%')
  })

  it('falls back to neutral when gap < 0.15', () => {
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: neutralStats, loading: false }
    })

    // neutral shows avgDeathsPerGame, no contrast row
    expect(wrapper.find('[data-testid="hero-wr"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="contrast-row"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="hero-wr"]').text()).toContain('5.1')
  })

  it('shows empty state when survivalStats is null', () => {
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: null, loading: false }
    })

    expect(wrapper.find('[data-testid="death-insight-empty"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Play a few games to unlock death insights')
    expect(wrapper.find('[data-testid="hero-wr"]').exists()).toBe(false)
  })

  it('shows empty state when survivalStats has totalGames === 0', () => {
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: { ...neutralStats, totalGames: 0 }, loading: false }
    })

    expect(wrapper.find('[data-testid="death-insight-empty"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Play a few games to unlock death insights')
  })

  it('shows loading skeleton when loading is true', () => {
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: null, loading: true }
    })

    expect(wrapper.find('[data-testid="death-insight-skeleton"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="hero-wr"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="death-insight-empty"]').exists()).toBe(false)
  })

  it('footer shows avg deaths and total games', () => {
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: motivationalStats, loading: false }
    })

    const footer = wrapper.find('[data-testid="death-insight-footer"]')
    expect(footer.exists()).toBe(true)
    expect(footer.text()).toContain('6.8 deaths/game')
    expect(footer.text()).toContain('20 games')
  })

  it('displays rank-adaptive thresholds in headline text', () => {
    // lowDeathThreshold: 5, highDeathThreshold: 8
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: motivationalStats, loading: false }
    })

    expect(wrapper.text()).toContain('≤5')
    expect(wrapper.find('[data-testid="contrast-row"]').text()).toContain('8+')
  })

  it('displays rank-adaptive thresholds for a lower-threshold player (Gold)', () => {
    // lowDeathThreshold: 4, highDeathThreshold: 6
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: warningStats, loading: false }
    })

    // Warning state: hero shows low-death threshold positively, contrast shows high-death threshold
    expect(wrapper.text()).toContain('≤4')
    expect(wrapper.find('[data-testid="contrast-row"]').text()).toContain('6+')
  })

  it('applies success border class when avg deaths <= lowDeathThreshold', () => {
    const stats = { ...motivationalStats, avgDeathsPerGame: 3.0, lowDeathThreshold: 5 }
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: stats, loading: false }
    })

    expect(wrapper.find('[data-testid="death-insights-card"]').classes()).toContain('border-success')
  })

  it('applies error border class when avg deaths >= highDeathThreshold', () => {
    const stats = { ...motivationalStats, avgDeathsPerGame: 9.0, highDeathThreshold: 8 }
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: stats, loading: false }
    })

    expect(wrapper.find('[data-testid="death-insights-card"]').classes()).toContain('border-error')
  })

  it('applies default border class when avg deaths is between thresholds', () => {
    // avgDeathsPerGame: 6.8, lowDeathThreshold: 5, highDeathThreshold: 8
    const wrapper = mount(DeathInsightsCard, {
      props: { survivalStats: motivationalStats, loading: false }
    })

    expect(wrapper.find('[data-testid="death-insights-card"]').classes()).toContain('border-default')
  })
})
