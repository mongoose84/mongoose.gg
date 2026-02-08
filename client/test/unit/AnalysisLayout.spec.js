/**
 * Unit tests for AnalysisLayout.vue
 * 
 * Tests cover:
 * - Component rendering
 * - Slot rendering for all 5 zones
 * - Conditional zone visibility (zones 4 & 5 only render when content provided)
 * - Props handling
 */

import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import AnalysisLayout from '@/components/shared/AnalysisLayout.vue'

describe('AnalysisLayout', () => {
  const mountComponent = (options = {}) => {
    return mount(AnalysisLayout, {
      ...options
    })
  }

  describe('Rendering', () => {
    it('renders the component', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="analysis-layout"]').exists()).toBe(true)
    })

    it('renders with default page title for screen readers', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('h1.sr-only').text()).toBe('Analysis Dashboard')
    })

    it('renders with custom page title', () => {
      const wrapper = mountComponent({
        props: { pageTitle: 'Solo Dashboard' }
      })
      expect(wrapper.find('h1.sr-only').text()).toBe('Solo Dashboard')
    })

    it('accepts matchId prop', () => {
      const wrapper = mountComponent({
        props: { matchId: 'NA1_12345' }
      })
      expect(wrapper.vm.matchId).toBe('NA1_12345')
    })
  })

  describe('Zone 1: Context Bar', () => {
    it('renders context-bar slot when content provided', () => {
      const wrapper = mountComponent({
        slots: {
          'context-bar': '<div class="test-context">Filters</div>'
        }
      })
      expect(wrapper.find('[data-testid="zone-context-bar"]').exists()).toBe(true)
      expect(wrapper.find('.test-context').text()).toBe('Filters')
    })

    it('does not render context-bar zone when no content', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="zone-context-bar"]').exists()).toBe(false)
    })
  })

  describe('Zone 2: Summary', () => {
    it('renders summary slot when content provided', () => {
      const wrapper = mountComponent({
        slots: {
          summary: '<div class="test-summary">Stats</div>'
        }
      })
      expect(wrapper.find('[data-testid="zone-summary"]').exists()).toBe(true)
      expect(wrapper.find('.test-summary').text()).toBe('Stats')
    })

    it('does not render summary zone when no content', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="zone-summary"]').exists()).toBe(false)
    })
  })

  describe('Zone 3: Trend Charts', () => {
    it('renders trend-charts slot when content provided', () => {
      const wrapper = mountComponent({
        slots: {
          'trend-charts': '<div class="test-charts">Charts</div>'
        }
      })
      expect(wrapper.find('[data-testid="zone-trend-charts"]').exists()).toBe(true)
      expect(wrapper.find('.test-charts').text()).toBe('Charts')
    })

    it('does not render trend-charts zone when no content', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="zone-trend-charts"]').exists()).toBe(false)
    })

    it('has 2-column grid layout', () => {
      const wrapper = mountComponent({
        slots: {
          'trend-charts': '<div>Chart 1</div><div>Chart 2</div>'
        }
      })
      const zone = wrapper.find('[data-testid="zone-trend-charts"]')
      expect(zone.classes()).toContain('zone-trend-charts')
    })
  })

  describe('Zone 4: Deep Analysis (conditional)', () => {
    it('renders deep-analysis slot when content provided', () => {
      const wrapper = mountComponent({
        slots: {
          'deep-analysis': '<div class="test-analysis">Analysis</div>'
        }
      })
      expect(wrapper.find('[data-testid="zone-deep-analysis"]').exists()).toBe(true)
      expect(wrapper.find('.test-analysis').text()).toBe('Analysis')
    })

    it('does not render deep-analysis zone when no content (v1 behavior)', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="zone-deep-analysis"]').exists()).toBe(false)
    })
  })

  describe('Zone 5: Goals (conditional)', () => {
    it('renders goals slot when content provided', () => {
      const wrapper = mountComponent({
        slots: {
          goals: '<div class="test-goals">Goals</div>'
        }
      })
      expect(wrapper.find('[data-testid="zone-goals"]').exists()).toBe(true)
      expect(wrapper.find('.test-goals').text()).toBe('Goals')
    })

    it('does not render goals zone when no content (v1 behavior)', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="zone-goals"]').exists()).toBe(false)
    })
  })

  describe('Multiple zones', () => {
    it('renders all zones when all slots provided', () => {
      const wrapper = mountComponent({
        slots: {
          'context-bar': '<div>Context</div>',
          summary: '<div>Summary</div>',
          'trend-charts': '<div>Charts</div>',
          'deep-analysis': '<div>Analysis</div>',
          goals: '<div>Goals</div>'
        }
      })
      expect(wrapper.find('[data-testid="zone-context-bar"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="zone-summary"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="zone-trend-charts"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="zone-deep-analysis"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="zone-goals"]').exists()).toBe(true)
    })

    it('renders only v1 zones (context-bar, summary, trend-charts)', () => {
      const wrapper = mountComponent({
        slots: {
          'context-bar': '<div>Context</div>',
          summary: '<div>Summary</div>',
          'trend-charts': '<div>Charts</div>'
        }
      })
      expect(wrapper.find('[data-testid="zone-context-bar"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="zone-summary"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="zone-trend-charts"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="zone-deep-analysis"]').exists()).toBe(false)
      expect(wrapper.find('[data-testid="zone-goals"]').exists()).toBe(false)
    })
  })
})

