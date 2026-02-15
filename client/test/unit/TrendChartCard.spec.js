/**
 * Unit tests for TrendChartCard.vue
 * 
 * Tests cover:
 * - Component rendering with title and subtitle
 * - Loading state display
 * - Expand/collapse toggle functionality
 * - Slot props (isExpanded, dataLimit)
 * - Event emission on toggle
 * - Accessibility attributes
 * - Default props behavior
 */

import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import TrendChartCard from '@/components/solo/TrendChartCard.vue'

describe('TrendChartCard', () => {
  const mountComponent = (props = {}, slots = {}) => {
    return mount(TrendChartCard, {
      props: {
        title: 'Test Chart',
        ...props
      },
      slots: {
        default: '<div data-testid="chart-content">Chart Content</div>',
        ...slots
      }
    })
  }

  describe('Rendering', () => {
    it('renders the component', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="trend-chart-card"]').exists()).toBe(true)
    })

    it('displays the title', () => {
      const wrapper = mountComponent({ title: 'Winrate Trend' })
      expect(wrapper.find('.chart-title').text()).toBe('Winrate Trend')
    })

    it('displays subtitle when provided', () => {
      const wrapper = mountComponent({ subtitle: 'Last 20 games' })
      expect(wrapper.find('.chart-subtitle').text()).toBe('Last 20 games')
    })

    it('does not render subtitle when not provided', () => {
      const wrapper = mountComponent({ subtitle: null })
      expect(wrapper.find('.chart-subtitle').exists()).toBe(false)
    })

    it('uses custom testId when provided', () => {
      const wrapper = mountComponent({ testId: 'lp-chart-card' })
      expect(wrapper.find('[data-testid="lp-chart-card"]').exists()).toBe(true)
    })

    it('uses default testId when not provided', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="trend-chart-card"]').exists()).toBe(true)
    })
  })

  describe('Loading state', () => {
    it('shows loading skeleton when loading is true', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)
    })

    it('hides slot content when loading', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="chart-content"]').exists()).toBe(false)
    })

    it('shows slot content when not loading', () => {
      const wrapper = mountComponent({ loading: false })
      expect(wrapper.find('[data-testid="chart-content"]').exists()).toBe(true)
    })

    it('hides loading skeleton when not loading', () => {
      const wrapper = mountComponent({ loading: false })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(false)
    })

    it('renders skeleton chart element', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('.skeleton-chart').exists()).toBe(true)
    })
  })

  describe('Expand/Collapse toggle', () => {
    it('renders expand toggle button', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('.expand-toggle').exists()).toBe(true)
    })

    it('shows "Full Season" text when collapsed', () => {
      const wrapper = mountComponent({ defaultExpanded: false })
      expect(wrapper.find('.toggle-text').text()).toBe('Full Season')
    })

    it('shows "Last 20" text when expanded', () => {
      const wrapper = mountComponent({ defaultExpanded: true })
      expect(wrapper.find('.toggle-text').text()).toBe('Last 20')
    })

    it('toggles state when button is clicked', async () => {
      const wrapper = mountComponent({ defaultExpanded: false })
      expect(wrapper.find('.toggle-text').text()).toBe('Full Season')
      
      await wrapper.find('.expand-toggle').trigger('click')
      expect(wrapper.find('.toggle-text').text()).toBe('Last 20')
    })

    it('toggles back when clicked again', async () => {
      const wrapper = mountComponent({ defaultExpanded: false })
      
      await wrapper.find('.expand-toggle').trigger('click')
      expect(wrapper.find('.toggle-text').text()).toBe('Last 20')
      
      await wrapper.find('.expand-toggle').trigger('click')
      expect(wrapper.find('.toggle-text').text()).toBe('Full Season')
    })

    it('emits toggle-expand event with new state', async () => {
      const wrapper = mountComponent({ defaultExpanded: false })
      
      await wrapper.find('.expand-toggle').trigger('click')
      
      expect(wrapper.emitted('toggle-expand')).toBeTruthy()
      expect(wrapper.emitted('toggle-expand')[0]).toEqual([true])
    })

    it('emits false when collapsing', async () => {
      const wrapper = mountComponent({ defaultExpanded: true })
      
      await wrapper.find('.expand-toggle').trigger('click')
      
      expect(wrapper.emitted('toggle-expand')[0]).toEqual([false])
    })

    it('applies rotated class to icon when expanded', () => {
      const wrapper = mountComponent({ defaultExpanded: true })
      expect(wrapper.find('.toggle-icon').classes()).toContain('icon-rotated')
    })

    it('does not apply rotated class when collapsed', () => {
      const wrapper = mountComponent({ defaultExpanded: false })
      expect(wrapper.find('.toggle-icon').classes()).not.toContain('icon-rotated')
    })
  })

  describe('Slot props', () => {
    it('passes isExpanded=false to slot when collapsed', () => {
      let slotProps = null
      const wrapper = mount(TrendChartCard, {
        props: { title: 'Test', defaultExpanded: false },
        slots: {
          default: (props) => {
            slotProps = props
            return '<div>Chart</div>'
          }
        }
      })

      expect(slotProps.isExpanded).toBe(false)
    })

    it('passes isExpanded=true to slot when expanded', () => {
      let slotProps = null
      const wrapper = mount(TrendChartCard, {
        props: { title: 'Test', defaultExpanded: true },
        slots: {
          default: (props) => {
            slotProps = props
            return '<div>Chart</div>'
          }
        }
      })

      expect(slotProps.isExpanded).toBe(true)
    })

    it('passes dataLimit=20 when collapsed', () => {
      let slotProps = null
      const wrapper = mount(TrendChartCard, {
        props: { title: 'Test', defaultExpanded: false },
        slots: {
          default: (props) => {
            slotProps = props
            return '<div>Chart</div>'
          }
        }
      })

      expect(slotProps.dataLimit).toBe(20)
    })

    it('passes dataLimit=500 when expanded', () => {
      let slotProps = null
      const wrapper = mount(TrendChartCard, {
        props: { title: 'Test', defaultExpanded: true },
        slots: {
          default: (props) => {
            slotProps = props
            return '<div>Chart</div>'
          }
        }
      })

      expect(slotProps.dataLimit).toBe(500)
    })

    it('updates slot props when toggle is clicked', async () => {
      let slotProps = null
      const wrapper = mount(TrendChartCard, {
        props: { title: 'Test', defaultExpanded: false },
        slots: {
          default: (props) => {
            slotProps = props
            return '<div>Chart</div>'
          }
        }
      })

      expect(slotProps.dataLimit).toBe(20)

      await wrapper.find('.expand-toggle').trigger('click')

      expect(slotProps.dataLimit).toBe(500)
      expect(slotProps.isExpanded).toBe(true)
    })
  })

  describe('Accessibility', () => {
    it('toggle button has aria-label when collapsed', () => {
      const wrapper = mountComponent({ defaultExpanded: false })
      expect(wrapper.find('.expand-toggle').attributes('aria-label')).toBe('Show full season')
    })

    it('toggle button has aria-label when expanded', () => {
      const wrapper = mountComponent({ defaultExpanded: true })
      expect(wrapper.find('.expand-toggle').attributes('aria-label')).toBe('Show last 20 games')
    })

    it('toggle button has aria-pressed attribute', () => {
      const wrapper = mountComponent({ defaultExpanded: false })
      expect(wrapper.find('.expand-toggle').attributes('aria-pressed')).toBe('false')
    })

    it('aria-pressed reflects expanded state', () => {
      const wrapper = mountComponent({ defaultExpanded: true })
      expect(wrapper.find('.expand-toggle').attributes('aria-pressed')).toBe('true')
    })

    it('toggle button has type="button"', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('.expand-toggle').attributes('type')).toBe('button')
    })

    it('icon has aria-hidden attribute', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('.toggle-icon').attributes('aria-hidden')).toBe('true')
    })
  })

  describe('Default props', () => {
    it('defaults to collapsed state', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('.toggle-text').text()).toBe('Full Season')
    })

    it('defaults loading to false', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(false)
    })

    it('defaults subtitle to null', () => {
      const wrapper = mount(TrendChartCard, {
        props: { title: 'Test' },
        slots: { default: '<div>Chart</div>' }
      })
      expect(wrapper.find('.chart-subtitle').exists()).toBe(false)
    })
  })

  describe('Structure', () => {
    it('has card header with left section', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('.card-header').exists()).toBe(true)
      expect(wrapper.find('.header-left').exists()).toBe(true)
    })

    it('has chart container', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('.chart-container').exists()).toBe(true)
    })

    it('title is inside header-left', () => {
      const wrapper = mountComponent()
      const headerLeft = wrapper.find('.header-left')
      expect(headerLeft.find('.chart-title').exists()).toBe(true)
    })

    it('subtitle is inside header-left when present', () => {
      const wrapper = mountComponent({ subtitle: 'Test subtitle' })
      const headerLeft = wrapper.find('.header-left')
      expect(headerLeft.find('.chart-subtitle').exists()).toBe(true)
    })
  })

  describe('State transitions', () => {
    it('transitions from loading to content', async () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)

      await wrapper.setProps({ loading: false })
      expect(wrapper.find('[data-testid="chart-content"]').exists()).toBe(true)
    })

    it('maintains expand state during loading change', async () => {
      const wrapper = mountComponent({ loading: false, defaultExpanded: true })
      expect(wrapper.find('.toggle-text').text()).toBe('Last 20')

      await wrapper.setProps({ loading: true })
      await wrapper.setProps({ loading: false })

      // Expand state should be preserved
      expect(wrapper.find('.toggle-text').text()).toBe('Last 20')
    })
  })
})

