import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SoloAnalyticsCTA from '@/components/overview/SoloAnalyticsCTA.vue'

describe('SoloAnalyticsCTA', () => {
  function mountCTA(options = {}) {
    return mount(SoloAnalyticsCTA, {
      global: {
        stubs: {
          'router-link': {
            template: '<a :href="to" class="router-link-stub"><slot /></a>',
            props: ['to']
          },
          ChartBarIcon: { template: '<svg data-testid="chartbar-icon" />' },
          ArrowTrendingUpIcon: { template: '<svg data-testid="arrow-trending-up-icon" />' },
          ArrowTrendingDownIcon: { template: '<svg data-testid="arrow-trending-down-icon" />' }
        }
      },
      ...options
    })
  }

  it('renders title and subtitle', () => {
    const wrapper = mountCTA()
    expect(wrapper.find('.cta-title').text()).toBe('Solo Analytics')
    expect(wrapper.find('.cta-subtitle').text()).toBe('Track your trends and improve')
  })

  it('contains a router-link to /app/solo', () => {
    const wrapper = mountCTA()
    expect(wrapper.find('.router-link-stub').attributes('href')).toBe('/app/solo')
  })

  it('renders icon, content, and arrow elements', () => {
    const wrapper = mountCTA()
    expect(wrapper.find('.cta-icon-wrapper svg').exists()).toBe(true)
    expect(wrapper.find('.cta-content').exists()).toBe(true)
    expect(wrapper.find('.cta-arrow svg').exists()).toBe(true)
  })

  it('renders up icon and success color when trendDirection is up', () => {
    const wrapper = mountCTA({ props: { trendDirection: 'up' } })
    expect(wrapper.find('[data-testid="solo-kda-trend-up-icon"]').exists()).toBe(true)
    expect(wrapper.find('.cta-icon--up').exists()).toBe(true)
  })

  it('renders down icon and error color when trendDirection is down', () => {
    const wrapper = mountCTA({ props: { trendDirection: 'down' } })
    expect(wrapper.find('[data-testid="solo-kda-trend-down-icon"]').exists()).toBe(true)
    expect(wrapper.find('.cta-icon--down').exists()).toBe(true)
  })

  it('renders provided subtitle text', () => {
    const wrapper = mountCTA({ props: { subtitle: 'KDA trend: 3.1 (+0.4 vs overall)' } })
    expect(wrapper.find('.cta-subtitle').text()).toBe('KDA trend: 3.1 (+0.4 vs overall)')
  })
})