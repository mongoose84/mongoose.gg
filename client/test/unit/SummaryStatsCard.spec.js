/**
 * Unit tests for SummaryStatsCard.vue
 * 
 * Tests cover:
 * - Component rendering with data
 * - Loading state display
 * - Empty state display
 * - Winrate color coding at various thresholds
 * - KDA formatting
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

    it('displays average KDA', () => {
      const wrapper = mountComponent({ avgKda: 3.25 })
      expect(wrapper.text()).toContain('Avg KDA')
      expect(wrapper.text()).toContain('3.25')
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
    it('applies winrate-red class for winrate < 47', () => {
      const wrapper = mountComponent({ winRate: 45 })
      expect(wrapper.find('.winrate-red').exists()).toBe(true)
    })

    it('applies winrate-redorange class for winrate 47-49', () => {
      const wrapper = mountComponent({ winRate: 48 })
      expect(wrapper.find('.winrate-redorange').exists()).toBe(true)
    })

    it('applies winrate-orange class for winrate 49-51', () => {
      const wrapper = mountComponent({ winRate: 50 })
      expect(wrapper.find('.winrate-orange').exists()).toBe(true)
    })

    it('applies winrate-yellow class for winrate 51-52', () => {
      const wrapper = mountComponent({ winRate: 51.5 })
      expect(wrapper.find('.winrate-yellow').exists()).toBe(true)
    })

    it('applies winrate-yellowgreen class for winrate 52-53', () => {
      const wrapper = mountComponent({ winRate: 52.5 })
      expect(wrapper.find('.winrate-yellowgreen').exists()).toBe(true)
    })

    it('applies winrate-green class for winrate >= 53', () => {
      const wrapper = mountComponent({ winRate: 55 })
      expect(wrapper.find('.winrate-green').exists()).toBe(true)
    })

    it('applies winrate-neutral class for null winrate', () => {
      const wrapper = mountComponent({ winRate: null })
      expect(wrapper.find('.winrate-neutral').exists()).toBe(true)
    })
  })

  describe('KDA formatting', () => {
    it('formats KDA with two decimal places', () => {
      const wrapper = mountComponent({ avgKda: 3.1 })
      expect(wrapper.text()).toContain('3.10')
    })

    it('rounds KDA to two decimal places', () => {
      const wrapper = mountComponent({ avgKda: 2.567 })
      expect(wrapper.text()).toContain('2.57')
    })

    it('shows -- when avgKda is null', () => {
      const wrapper = mountComponent({ avgKda: null })
      const kdaValue = wrapper.find('[data-testid="stat-kda-value"]')
      expect(kdaValue.text()).toBe('--')
    })

    it('shows -- when avgKda is undefined', () => {
      const wrapper = mountComponent({ avgKda: undefined })
      const kdaValue = wrapper.find('[data-testid="stat-kda-value"]')
      expect(kdaValue.text()).toBe('--')
    })

    it('handles 0 KDA', () => {
      const wrapper = mountComponent({ avgKda: 0 })
      expect(wrapper.text()).toContain('0.00')
    })

    it('handles high KDA values', () => {
      const wrapper = mountComponent({ avgKda: 15.75 })
      expect(wrapper.text()).toContain('15.75')
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

    it('handles decimal winrate edge at 47', () => {
      const wrapper = mountComponent({ winRate: 46.9 })
      expect(wrapper.find('.winrate-red').exists()).toBe(true)
    })

    it('handles decimal winrate edge at 53', () => {
      const wrapper = mountComponent({ winRate: 53.0 })
      expect(wrapper.find('.winrate-green').exists()).toBe(true)
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
      expect(wrapper.find('[data-testid="stat-kda"]').text()).toContain('Avg KDA')
    })
  })
})

