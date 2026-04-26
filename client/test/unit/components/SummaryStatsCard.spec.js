/**
 * Unit tests for SummaryStatsCard.vue
 *
 * Tests cover:
 * - Component rendering with data
 * - Loading state display
 * - Empty state display
 * - Winrate color coding at various thresholds
 * - K/D/A breakdown formatting
 * - K/D/A trend-based coloring
 * - Edge cases (null values, zero games)
 */

import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SummaryStatsCard from '@/components/solo/SummaryStatsCard.vue'

describe('SummaryStatsCard', () => {
  const mountComponent = (props = {}) => {
    return mount(SummaryStatsCard, {
      props: {
        gamesPlayed: 10,
        winRate: 55.5,
        avgKda: 3.25,
        avgKills: 5.5,
        avgDeaths: 3.2,
        avgAssists: 8.1,
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="summary-stats-card"]').exists()).toBe(true)
    })

    it('displays games played', () => {
      const wrapper = mountComponent({ gamesPlayed: 42 })
      expect(wrapper.text()).toContain('Games')
      expect(wrapper.text()).toContain('42')
    })

    it('displays winrate with percentage', () => {
      const wrapper = mountComponent({ winRate: 55.5 })
      expect(wrapper.text()).toContain('Winrate')
      expect(wrapper.text()).toContain('55.5%')
    })

    it('displays K/D/A breakdown', () => {
      const wrapper = mountComponent({ avgKills: 5.5, avgDeaths: 3.2, avgAssists: 8.1 })
      expect(wrapper.text()).toContain('K / D / A')
      expect(wrapper.find('[data-testid="stat-kills-value"]').text()).toBe('5.5')
      expect(wrapper.find('[data-testid="stat-deaths-value"]').text()).toBe('3.2')
      expect(wrapper.find('[data-testid="stat-assists-value"]').text()).toBe('8.1')
    })

    it('displays KDA ratio as secondary text', () => {
      const wrapper = mountComponent({ avgKda: 3.25 })
      expect(wrapper.find('[data-testid="stat-kda-ratio"]').text()).toContain('3.25')
      expect(wrapper.find('[data-testid="stat-kda-ratio"]').text()).toContain('KDA')
    })

    it('shows stats display when data is present', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="stats-display"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(false)
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(false)
    })
  })

  describe('Loading state', () => {
    it('shows loading skeleton when loading is true', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)
    })

    it('hides stats display when loading', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="stats-display"]').exists()).toBe(false)
    })

    it('hides empty state when loading', () => {
      const wrapper = mountComponent({ loading: true, gamesPlayed: 0 })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(false)
    })

    it('renders 3 skeleton items', () => {
      const wrapper = mountComponent({ loading: true })
      const skeletons = wrapper.findAll('.skeleton-value')
      expect(skeletons.length).toBe(3)
    })
  })

  describe('Empty state', () => {
    it('shows empty state when gamesPlayed is 0', () => {
      const wrapper = mountComponent({ gamesPlayed: 0, loading: false })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })

    it('displays empty message', () => {
      const wrapper = mountComponent({ gamesPlayed: 0 })
      expect(wrapper.text()).toContain('No games found for this filter')
    })

    it('hides stats display when empty', () => {
      const wrapper = mountComponent({ gamesPlayed: 0 })
      expect(wrapper.find('[data-testid="stats-display"]').exists()).toBe(false)
    })

    it('hides loading state when empty', () => {
      const wrapper = mountComponent({ gamesPlayed: 0, loading: false })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(false)
    })
  })

  describe('Winrate formatting', () => {
    it('formats winrate with one decimal place', () => {
      const wrapper = mountComponent({ winRate: 52.333 })
      expect(wrapper.text()).toContain('52.3%')
    })

    it('shows -- when winrate is null', () => {
      const wrapper = mountComponent({ winRate: null })
      expect(wrapper.text()).toContain('--')
    })

    it('shows -- when winrate is undefined', () => {
      const wrapper = mountComponent({ winRate: undefined })
      expect(wrapper.text()).toContain('--')
    })

    it('handles 0% winrate', () => {
      const wrapper = mountComponent({ winRate: 0 })
      expect(wrapper.text()).toContain('0.0%')
    })

    it('handles 100% winrate', () => {
      const wrapper = mountComponent({ winRate: 100 })
      expect(wrapper.text()).toContain('100.0%')
    })
  })

  describe('Winrate color classes', () => {
    it('applies winrate-terrible class for winrate < 40', () => {
      const wrapper = mountComponent({ winRate: 35 })
      expect(wrapper.find('.winrate-terrible').exists()).toBe(true)
    })

    it('applies winrate-poor class for winrate 45-48', () => {
      const wrapper = mountComponent({ winRate: 46 })
      expect(wrapper.find('.winrate-poor').exists()).toBe(true)
    })

    it('applies winrate-average class for winrate 48-52', () => {
      const wrapper = mountComponent({ winRate: 50 })
      expect(wrapper.find('.winrate-average').exists()).toBe(true)
    })

    it('applies winrate-average class for winrate 51-52', () => {
      const wrapper = mountComponent({ winRate: 51.5 })
      expect(wrapper.find('.winrate-average').exists()).toBe(true)
    })

    it('applies winrate-good class for winrate 52-55', () => {
      const wrapper = mountComponent({ winRate: 52.5 })
      expect(wrapper.find('.winrate-good').exists()).toBe(true)
    })

    it('applies winrate-great class for winrate >= 55', () => {
      const wrapper = mountComponent({ winRate: 57 })
      expect(wrapper.find('.winrate-great').exists()).toBe(true)
    })

    it('applies winrate-neutral class for null winrate', () => {
      const wrapper = mountComponent({ winRate: null })
      expect(wrapper.find('.winrate-neutral').exists()).toBe(true)
    })
  })

  describe('K/D/A formatting', () => {
    it('formats kills with one decimal place', () => {
      const wrapper = mountComponent({ avgKills: 5.0 })
      expect(wrapper.find('[data-testid="stat-kills-value"]').text()).toBe('5.0')
    })

    it('formats deaths with one decimal place', () => {
      const wrapper = mountComponent({ avgDeaths: 3.0 })
      expect(wrapper.find('[data-testid="stat-deaths-value"]').text()).toBe('3.0')
    })

    it('formats assists with one decimal place', () => {
      const wrapper = mountComponent({ avgAssists: 8.0 })
      expect(wrapper.find('[data-testid="stat-assists-value"]').text()).toBe('8.0')
    })

    it('shows -- when avgKills is null', () => {
      const wrapper = mountComponent({ avgKills: null })
      expect(wrapper.find('[data-testid="stat-kills-value"]').text()).toBe('--')
    })

    it('shows -- when avgDeaths is null', () => {
      const wrapper = mountComponent({ avgDeaths: null })
      expect(wrapper.find('[data-testid="stat-deaths-value"]').text()).toBe('--')
    })

    it('shows -- when avgAssists is null', () => {
      const wrapper = mountComponent({ avgAssists: null })
      expect(wrapper.find('[data-testid="stat-assists-value"]').text()).toBe('--')
    })

    it('displays KDA ratio with two decimal places', () => {
      const wrapper = mountComponent({ avgKda: 3.10 })
      expect(wrapper.find('[data-testid="stat-kda-ratio"]').text()).toContain('3.10')
    })

    it('shows -- for KDA ratio when avgKda is null and K/D/A are null', () => {
      const wrapper = mountComponent({ avgKda: null, avgKills: null, avgDeaths: null, avgAssists: null })
      expect(wrapper.find('[data-testid="stat-kda-ratio"]').text()).toContain('--')
    })

    it('handles 0 values', () => {
      const wrapper = mountComponent({ avgKills: 0, avgDeaths: 0, avgAssists: 0 })
      expect(wrapper.find('[data-testid="stat-kills-value"]').text()).toBe('0.0')
      expect(wrapper.find('[data-testid="stat-deaths-value"]').text()).toBe('0.0')
      expect(wrapper.find('[data-testid="stat-assists-value"]').text()).toBe('0.0')
    })

    it('handles high K/D/A values', () => {
      const wrapper = mountComponent({ avgKills: 15.5, avgDeaths: 2.3, avgAssists: 12.7 })
      expect(wrapper.find('[data-testid="stat-kills-value"]').text()).toBe('15.5')
      expect(wrapper.find('[data-testid="stat-deaths-value"]').text()).toBe('2.3')
      expect(wrapper.find('[data-testid="stat-assists-value"]').text()).toBe('12.7')
    })
  })

  describe('K/D/A trend coloring', () => {
    it('applies positive trend to kills when selected period > overall avg', () => {
      const wrapper = mountComponent({
        avgKills: 6.0,
        avgDeaths: 3.0,
        avgAssists: 8.0,
        overallAvgKills: 5.0,
        overallAvgDeaths: 3.0,
        overallAvgAssists: 8.0
      })
      expect(wrapper.find('[data-testid="stat-kills-value"]').classes()).toContain('trend-positive')
    })

    it('applies negative trend to kills when selected period < overall avg', () => {
      const wrapper = mountComponent({
        avgKills: 4.0,
        avgDeaths: 3.0,
        avgAssists: 8.0,
        overallAvgKills: 5.0,
        overallAvgDeaths: 3.0,
        overallAvgAssists: 8.0
      })
      expect(wrapper.find('[data-testid="stat-kills-value"]').classes()).toContain('trend-negative')
    })

    it('applies positive trend to deaths when selected period < overall avg (fewer deaths = good)', () => {
      const wrapper = mountComponent({
        avgKills: 5.0,
        avgDeaths: 3.0,
        avgAssists: 8.0,
        overallAvgKills: 5.0,
        overallAvgDeaths: 4.0,
        overallAvgAssists: 8.0
      })
      expect(wrapper.find('[data-testid="stat-deaths-value"]').classes()).toContain('trend-positive')
    })

    it('applies negative trend to deaths when selected period > overall avg (more deaths = bad)', () => {
      const wrapper = mountComponent({
        avgKills: 5.0,
        avgDeaths: 4.0,
        avgAssists: 8.0,
        overallAvgKills: 5.0,
        overallAvgDeaths: 3.0,
        overallAvgAssists: 8.0
      })
      expect(wrapper.find('[data-testid="stat-deaths-value"]').classes()).toContain('trend-negative')
    })

    it('applies positive trend to assists when selected period > overall avg', () => {
      const wrapper = mountComponent({
        avgKills: 5.0,
        avgDeaths: 3.0,
        avgAssists: 10.0,
        overallAvgKills: 5.0,
        overallAvgDeaths: 3.0,
        overallAvgAssists: 8.0
      })
      expect(wrapper.find('[data-testid="stat-assists-value"]').classes()).toContain('trend-positive')
    })

    it('applies negative trend to assists when selected period < overall avg', () => {
      const wrapper = mountComponent({
        avgKills: 5.0,
        avgDeaths: 3.0,
        avgAssists: 6.0,
        overallAvgKills: 5.0,
        overallAvgDeaths: 3.0,
        overallAvgAssists: 8.0
      })
      expect(wrapper.find('[data-testid="stat-assists-value"]').classes()).toContain('trend-negative')
    })

    it('does not apply trend colors when overall data is missing', () => {
      const wrapper = mountComponent({
        avgKills: 5.0,
        avgDeaths: 3.0,
        avgAssists: 8.0,
        overallAvgKills: null,
        overallAvgDeaths: null,
        overallAvgAssists: null
      })
      expect(wrapper.find('[data-testid="stat-kills-value"]').classes()).not.toContain('trend-positive')
      expect(wrapper.find('[data-testid="stat-kills-value"]').classes()).not.toContain('trend-negative')
    })

    it('does not apply trend colors when values are equal', () => {
      const wrapper = mountComponent({
        avgKills: 5.0,
        avgDeaths: 3.0,
        avgAssists: 8.0,
        overallAvgKills: 5.0,
        overallAvgDeaths: 3.0,
        overallAvgAssists: 8.0
      })
      expect(wrapper.find('[data-testid="stat-kills-value"]').classes()).not.toContain('trend-positive')
      expect(wrapper.find('[data-testid="stat-kills-value"]').classes()).not.toContain('trend-negative')
    })
  })

  describe('Winrate trend coloring', () => {
    it('applies positive trend to winrate when selected period > overall', () => {
      const wrapper = mountComponent({
        winRate: 55.0,
        overallWinRate: 50.0
      })
      expect(wrapper.find('[data-testid="stat-winrate-value"]').classes()).toContain('trend-positive')
    })

    it('applies negative trend to winrate when selected period < overall', () => {
      const wrapper = mountComponent({
        winRate: 45.0,
        overallWinRate: 50.0
      })
      expect(wrapper.find('[data-testid="stat-winrate-value"]').classes()).toContain('trend-negative')
    })

    it('does not apply trend colors to winrate when overall data is missing', () => {
      const wrapper = mountComponent({
        winRate: 50.0,
        overallWinRate: null
      })
      expect(wrapper.find('[data-testid="stat-winrate-value"]').classes()).not.toContain('trend-positive')
      expect(wrapper.find('[data-testid="stat-winrate-value"]').classes()).not.toContain('trend-negative')
    })

    it('does not apply trend colors to winrate when values are equal', () => {
      const wrapper = mountComponent({
        winRate: 50.0,
        overallWinRate: 50.0
      })
      expect(wrapper.find('[data-testid="stat-winrate-value"]').classes()).not.toContain('trend-positive')
      expect(wrapper.find('[data-testid="stat-winrate-value"]').classes()).not.toContain('trend-negative')
    })
  })

  describe('Edge cases', () => {
    it('handles all default props', () => {
      const wrapper = mount(SummaryStatsCard)
      // With default gamesPlayed: 0, should show empty state
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })

    it('handles very large games count', () => {
      const wrapper = mountComponent({ gamesPlayed: 9999 })
      expect(wrapper.text()).toContain('9999')
    })

    it('handles decimal winrate edge at 45', () => {
      const wrapper = mountComponent({ winRate: 44.9 })
      expect(wrapper.find('.winrate-bad').exists()).toBe(true)
    })

    it('handles decimal winrate edge at 55', () => {
      const wrapper = mountComponent({ winRate: 55.0 })
      expect(wrapper.find('.winrate-good').exists()).toBe(true)
    })

    it('transitions from loading to data correctly', async () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)

      await wrapper.setProps({ loading: false })
      expect(wrapper.find('[data-testid="stats-display"]').exists()).toBe(true)
    })

    it('transitions from loading to empty correctly', async () => {
      const wrapper = mountComponent({ loading: true, gamesPlayed: 0 })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)

      await wrapper.setProps({ loading: false })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })
  })

  describe('Stat labels', () => {
    it('displays correct stat labels', () => {
      const wrapper = mountComponent()

      expect(wrapper.find('[data-testid="stat-games"]').text()).toContain('Games')
      expect(wrapper.find('[data-testid="stat-winrate"]').text()).toContain('Winrate')
      expect(wrapper.find('[data-testid="stat-kda"]').text()).toContain('Average K / D / A')
    })
  })

  describe('Overall mode — accountCount label', () => {
    it('shows "Across N accounts" sublabel when accountCount > 1', () => {
      const wrapper = mountComponent({ accountCount: 3 })
      const sublabel = wrapper.find('[data-testid="stat-games-sublabel"]')
      expect(sublabel.exists()).toBe(true)
      expect(sublabel.text()).toBe('Across 3 accounts')
    })

    it('hides "Across N accounts" sublabel when accountCount is 1', () => {
      const wrapper = mountComponent({ accountCount: 1 })
      expect(wrapper.find('[data-testid="stat-games-sublabel"]').exists()).toBe(false)
    })

    it('hides sublabel when accountCount is not provided (defaults to 1)', () => {
      const wrapper = mountComponent({})
      expect(wrapper.find('[data-testid="stat-games-sublabel"]').exists()).toBe(false)
    })
  })

  describe('Overall mode — stacked ranks', () => {
    const ranks = [
      { gameName: 'FakerMain', soloDuoRank: { tier: 'DIAMOND', division: 'II', lp: 50, hasRank: true }, flexRank: null },
      { gameName: 'FakerSmurf', soloDuoRank: { tier: 'PLATINUM', division: 'I', lp: 75, hasRank: true }, flexRank: null },
      { gameName: 'FakerAlt', soloDuoRank: { tier: null, division: null, lp: null, hasRank: false }, flexRank: null }
    ]

    it('renders stat-ranks section when ranks prop is provided', () => {
      const wrapper = mountComponent({ ranks })
      expect(wrapper.find('[data-testid="stat-ranks"]').exists()).toBe(true)
    })

    it('renders one rank-pill per account entry', () => {
      const wrapper = mountComponent({ ranks })
      const pills = wrapper.findAll('[data-testid^="rank-pill-"]')
      expect(pills).toHaveLength(3)
    })

    it('highlights first rank pill (highest rank)', () => {
      const wrapper = mountComponent({ ranks })
      const firstPill = wrapper.find('[data-testid="rank-pill-0"]')
      expect(firstPill.classes()).toContain('rank-pill--highlight')
    })

    it('does not highlight subsequent rank pills', () => {
      const wrapper = mountComponent({ ranks })
      const secondPill = wrapper.find('[data-testid="rank-pill-1"]')
      expect(secondPill.classes()).not.toContain('rank-pill--highlight')
    })

    it('shows account gameName in rank pill text', () => {
      const wrapper = mountComponent({ ranks })
      expect(wrapper.find('[data-testid="rank-pill-0"]').text()).toContain('FakerMain')
    })

    it('shows rank tier for ranked accounts', () => {
      const wrapper = mountComponent({ ranks })
      expect(wrapper.find('[data-testid="rank-pill-0"]').text()).toContain('Diamond')
    })

    it('shows "Unranked" for unranked accounts', () => {
      const wrapper = mountComponent({ ranks })
      expect(wrapper.find('[data-testid="rank-pill-2"]').text()).toContain('Unranked')
    })

    it('hides per-account solo/duo rank when ranks prop is present', () => {
      const wrapper = mountComponent({ ranks, queueFilter: 'all' })
      expect(wrapper.find('[data-testid="solo-duo-rank-wrapper"]').exists()).toBe(false)
    })

    it('hides per-account flex rank when ranks prop is present', () => {
      const wrapper = mountComponent({ ranks, queueFilter: 'ranked_flex' })
      expect(wrapper.find('[data-testid="flex-rank-wrapper"]').exists()).toBe(false)
    })

    it('does not render stat-ranks section when ranks prop is null', () => {
      const wrapper = mountComponent({ ranks: null })
      expect(wrapper.find('[data-testid="stat-ranks"]').exists()).toBe(false)
    })

    it('does not render stat-ranks section when ranks is empty array', () => {
      const wrapper = mountComponent({ ranks: [] })
      expect(wrapper.find('[data-testid="stat-ranks"]').exists()).toBe(false)
    })
  })

  describe('Rank display', () => {
    const soloDuoRank = { tier: 'GOLD', division: 'II', lp: 78, hasRank: true }
    const flexRank = { tier: 'SILVER', division: 'I', lp: 45, hasRank: true }

    it('shows both ranks for All Queues filter', () => {
      const wrapper = mountComponent({
        queueFilter: 'all',
        soloDuoRank,
        flexRank
      })
      expect(wrapper.find('[data-testid="solo-duo-rank-wrapper"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="flex-rank-wrapper"]').exists()).toBe(true)
    })

    it('shows only Solo/Duo rank for ranked_solo filter', () => {
      const wrapper = mountComponent({
        queueFilter: 'ranked_solo',
        soloDuoRank,
        flexRank
      })
      expect(wrapper.find('[data-testid="solo-duo-rank-wrapper"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="flex-rank-wrapper"]').exists()).toBe(false)
    })

    it('shows only Flex rank for ranked_flex filter', () => {
      const wrapper = mountComponent({
        queueFilter: 'ranked_flex',
        soloDuoRank,
        flexRank
      })
      expect(wrapper.find('[data-testid="flex-rank-wrapper"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="solo-duo-rank-wrapper"]').exists()).toBe(false)
    })

    it('hides both ranks for normal queue filter', () => {
      const wrapper = mountComponent({
        queueFilter: 'normal',
        soloDuoRank,
        flexRank
      })
      expect(wrapper.find('[data-testid="solo-duo-rank-wrapper"]').exists()).toBe(false)
      expect(wrapper.find('[data-testid="flex-rank-wrapper"]').exists()).toBe(false)
    })

    it('hides both ranks for aram queue filter', () => {
      const wrapper = mountComponent({
        queueFilter: 'aram',
        soloDuoRank,
        flexRank
      })
      expect(wrapper.find('[data-testid="solo-duo-rank-wrapper"]').exists()).toBe(false)
      expect(wrapper.find('[data-testid="flex-rank-wrapper"]').exists()).toBe(false)
    })

    it('handles unranked Solo/Duo in All Queues view', () => {
      const wrapper = mountComponent({
        queueFilter: 'all',
        soloDuoRank: { tier: null, division: null, lp: null, hasRank: false },
        flexRank
      })
      expect(wrapper.find('[data-testid="solo-duo-rank-wrapper"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="flex-rank-wrapper"]').exists()).toBe(true)
    })

    it('handles null rank props gracefully', () => {
      const wrapper = mountComponent({
        queueFilter: 'all',
        soloDuoRank: null,
        flexRank: null
      })
      // Should still render without errors
      expect(wrapper.exists()).toBe(true)
    })

    it('displays rank labels correctly', () => {
      const wrapper = mountComponent({
        queueFilter: 'all',
        soloDuoRank,
        flexRank
      })
      expect(wrapper.text()).toContain('Solo/Duo')
      expect(wrapper.text()).toContain('Flex')
    })
  })
})

