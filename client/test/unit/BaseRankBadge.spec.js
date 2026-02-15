/**
 * Unit tests for BaseRankBadge.vue
 *
 * Tests cover:
 * - Rank badge rendering with tier/division/LP
 * - Unranked state display
 * - Various tier display (Iron, Bronze, ..., Challenger)
 * - Division handling for lower tiers vs Master+
 * - Size variants (sm, md, lg)
 * - LP display toggle
 */

import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseRankBadge from '@/components/base/BaseRankBadge.vue'

describe('BaseRankBadge', () => {
  const mountComponent = (props = {}) => {
    return mount(BaseRankBadge, {
      props: {
        tier: 'GOLD',
        division: 'II',
        lp: 78,
        hasRank: true,
        ...props
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="rank-badge"]').exists()).toBe(true)
    })

    it('displays rank emblem image', () => {
      const wrapper = mountComponent({ tier: 'GOLD' })
      const emblem = wrapper.find('[data-testid="rank-emblem"]')
      expect(emblem.exists()).toBe(true)
      expect(emblem.attributes('src')).toBe('/assets/ranked/emblem-gold.png')
      expect(emblem.attributes('alt')).toBe('GOLD rank emblem')
    })

    it('displays formatted tier with division', () => {
      const wrapper = mountComponent({ tier: 'GOLD', division: 'II' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Gold II')
    })

    it('displays LP value', () => {
      const wrapper = mountComponent({ lp: 78 })
      expect(wrapper.find('[data-testid="rank-lp"]').text()).toBe('78 LP')
    })

    it('shows rank when hasRank is true', () => {
      const wrapper = mountComponent({ hasRank: true })
      expect(wrapper.find('[data-testid="rank-badge"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="rank-badge-unranked"]').exists()).toBe(false)
    })
  })

  describe('Unranked state', () => {
    it('shows unranked badge when hasRank is false', () => {
      const wrapper = mountComponent({ hasRank: false })
      expect(wrapper.find('[data-testid="rank-badge-unranked"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="rank-emblem"]').exists()).toBe(false)
    })

    it('displays "Unranked" text', () => {
      const wrapper = mountComponent({ hasRank: false })
      expect(wrapper.text()).toContain('Unranked')
    })

    it('does not show emblem or LP when unranked', () => {
      const wrapper = mountComponent({ hasRank: false })
      expect(wrapper.find('[data-testid="rank-emblem"]').exists()).toBe(false)
      expect(wrapper.find('[data-testid="rank-lp"]').exists()).toBe(false)
    })
  })

  describe('Tier display', () => {
    it('formats Iron tier correctly', () => {
      const wrapper = mountComponent({ tier: 'IRON', division: 'IV' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Iron IV')
    })

    it('formats Bronze tier correctly', () => {
      const wrapper = mountComponent({ tier: 'BRONZE', division: 'III' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Bronze III')
    })

    it('formats Silver tier correctly', () => {
      const wrapper = mountComponent({ tier: 'SILVER', division: 'II' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Silver II')
    })

    it('formats Gold tier correctly', () => {
      const wrapper = mountComponent({ tier: 'GOLD', division: 'I' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Gold I')
    })

    it('formats Platinum tier correctly', () => {
      const wrapper = mountComponent({ tier: 'PLATINUM', division: 'III' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Platinum III')
    })

    it('formats Emerald tier correctly', () => {
      const wrapper = mountComponent({ tier: 'EMERALD', division: 'II' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Emerald II')
    })

    it('formats Diamond tier correctly', () => {
      const wrapper = mountComponent({ tier: 'DIAMOND', division: 'I' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Diamond I')
    })

    it('formats Master tier without division', () => {
      const wrapper = mountComponent({ tier: 'MASTER', division: null, lp: 150 })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Master')
    })

    it('formats Grandmaster tier without division', () => {
      const wrapper = mountComponent({ tier: 'GRANDMASTER', division: null, lp: 450 })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Grandmaster')
    })

    it('formats Challenger tier without division', () => {
      const wrapper = mountComponent({ tier: 'CHALLENGER', division: null, lp: 1000 })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Challenger')
    })
  })

  describe('Division handling', () => {
    it('shows division for lower tiers', () => {
      const wrapper = mountComponent({ tier: 'GOLD', division: 'III' })
      expect(wrapper.text()).toContain('Gold III')
    })

    it('does not show division for Master tier even if provided', () => {
      const wrapper = mountComponent({ tier: 'MASTER', division: 'I' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Master')
      expect(wrapper.find('[data-testid="rank-tier"]').text()).not.toContain('I')
    })

    it('does not show division for Grandmaster tier even if provided', () => {
      const wrapper = mountComponent({ tier: 'GRANDMASTER', division: 'I' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Grandmaster')
      expect(wrapper.find('[data-testid="rank-tier"]').text()).not.toContain('I')
    })

    it('does not show division for Challenger tier even if provided', () => {
      const wrapper = mountComponent({ tier: 'CHALLENGER', division: 'I' })
      expect(wrapper.find('[data-testid="rank-tier"]').text()).toBe('Challenger')
      expect(wrapper.find('[data-testid="rank-tier"]').text()).not.toContain('I')
    })
  })

  describe('LP display', () => {
    it('shows LP when showLp is true', () => {
      const wrapper = mountComponent({ lp: 45, showLp: true })
      expect(wrapper.find('[data-testid="rank-lp"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('45 LP')
    })

    it('hides LP when showLp is false', () => {
      const wrapper = mountComponent({ lp: 45, showLp: false })
      expect(wrapper.find('[data-testid="rank-lp"]').exists()).toBe(false)
    })

    it('formats LP with 0 value', () => {
      const wrapper = mountComponent({ lp: 0 })
      expect(wrapper.text()).toContain('0 LP')
    })

    it('formats high LP value (Master+)', () => {
      const wrapper = mountComponent({ tier: 'MASTER', lp: 450 })
      expect(wrapper.text()).toContain('450 LP')
    })
  })

  describe('Size variants', () => {
    it('applies small size class by default', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('.rank-badge--sm').exists()).toBe(true)
    })

    it('applies small size class when size="sm"', () => {
      const wrapper = mountComponent({ size: 'sm' })
      expect(wrapper.find('.rank-badge--sm').exists()).toBe(true)
    })

    it('applies medium size class when size="md"', () => {
      const wrapper = mountComponent({ size: 'md' })
      expect(wrapper.find('.rank-badge--md').exists()).toBe(true)
    })

    it('applies large size class when size="lg"', () => {
      const wrapper = mountComponent({ size: 'lg' })
      expect(wrapper.find('.rank-badge--lg').exists()).toBe(true)
    })
  })

  describe('Tier-specific styling', () => {
    it('applies iron tier class', () => {
      const wrapper = mountComponent({ tier: 'IRON' })
      expect(wrapper.find('.rank-badge--iron').exists()).toBe(true)
    })

    it('applies bronze tier class', () => {
      const wrapper = mountComponent({ tier: 'BRONZE' })
      expect(wrapper.find('.rank-badge--bronze').exists()).toBe(true)
    })

    it('applies silver tier class', () => {
      const wrapper = mountComponent({ tier: 'SILVER' })
      expect(wrapper.find('.rank-badge--silver').exists()).toBe(true)
    })

    it('applies gold tier class', () => {
      const wrapper = mountComponent({ tier: 'GOLD' })
      expect(wrapper.find('.rank-badge--gold').exists()).toBe(true)
    })

    it('applies platinum tier class', () => {
      const wrapper = mountComponent({ tier: 'PLATINUM' })
      expect(wrapper.find('.rank-badge--platinum').exists()).toBe(true)
    })

    it('applies emerald tier class', () => {
      const wrapper = mountComponent({ tier: 'EMERALD' })
      expect(wrapper.find('.rank-badge--emerald').exists()).toBe(true)
    })

    it('applies diamond tier class', () => {
      const wrapper = mountComponent({ tier: 'DIAMOND' })
      expect(wrapper.find('.rank-badge--diamond').exists()).toBe(true)
    })

    it('applies master tier class', () => {
      const wrapper = mountComponent({ tier: 'MASTER' })
      expect(wrapper.find('.rank-badge--master').exists()).toBe(true)
    })

    it('applies grandmaster tier class', () => {
      const wrapper = mountComponent({ tier: 'GRANDMASTER' })
      expect(wrapper.find('.rank-badge--grandmaster').exists()).toBe(true)
    })

    it('applies challenger tier class', () => {
      const wrapper = mountComponent({ tier: 'CHALLENGER' })
      expect(wrapper.find('.rank-badge--challenger').exists()).toBe(true)
    })
  })

  describe('Edge cases', () => {
    it('handles null tier gracefully when hasRank is true', () => {
      const wrapper = mountComponent({ tier: null, hasRank: true })
      // Should still render, may show "Unranked" or empty
      expect(wrapper.exists()).toBe(true)
    })

    it('handles missing division', () => {
      const wrapper = mountComponent({ tier: 'GOLD', division: null })
      expect(wrapper.text()).toContain('Gold')
    })

    it('handles null LP value', () => {
      const wrapper = mountComponent({ lp: null })
      // Should still render other elements
      expect(wrapper.find('[data-testid="rank-tier"]').exists()).toBe(true)
    })

    it('defaults to unranked when hasRank is false regardless of other props', () => {
      const wrapper = mountComponent({
        tier: 'CHALLENGER',
        division: 'I',
        lp: 1000,
        hasRank: false
      })
      expect(wrapper.text()).toContain('Unranked')
      expect(wrapper.find('[data-testid="rank-emblem"]').exists()).toBe(false)
    })
  })
})
