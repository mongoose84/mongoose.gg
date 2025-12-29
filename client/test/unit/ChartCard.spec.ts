import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChartCard from '@/components/shared/ChartCard.vue'

describe('ChartCard', () => {
  it('mounts without errors', () => {
    const wrapper = mount(ChartCard, {
      props: { title: 'Test Chart' }
    })

    expect(wrapper.exists()).toBe(true)
  })

  it('renders the title correctly', () => {
    const wrapper = mount(ChartCard, {
      props: { title: 'My Chart Title' }
    })

    expect(wrapper.find('h4').text()).toBe('My Chart Title')
  })

  it('renders slot content correctly', () => {
    const wrapper = mount(ChartCard, {
      props: { title: 'Test Chart' },
      slots: {
        default: '<div class="test-content">Chart Content</div>'
      }
    })

    expect(wrapper.find('.test-content').exists()).toBe(true)
    expect(wrapper.find('.test-content').text()).toBe('Chart Content')
  })

  it('has the correct CSS class', () => {
    const wrapper = mount(ChartCard, {
      props: { title: 'Test Chart' }
    })

    expect(wrapper.find('.chart-card').exists()).toBe(true)
  })

  it('renders complex slot content', () => {
    const wrapper = mount(ChartCard, {
      props: { title: 'Complex Chart' },
      slots: {
        default: `
          <svg class="chart-svg">
            <rect width="100" height="50" />
          </svg>
          <div class="chart-legend">Legend</div>
        `
      }
    })

    expect(wrapper.find('.chart-svg').exists()).toBe(true)
    expect(wrapper.find('.chart-legend').exists()).toBe(true)
    expect(wrapper.find('.chart-legend').text()).toBe('Legend')
  })

  it('handles empty slot content', () => {
    const wrapper = mount(ChartCard, {
      props: { title: 'Empty Chart' }
    })

    // Should still render the card structure
    expect(wrapper.find('.chart-card').exists()).toBe(true)
    expect(wrapper.find('h4').text()).toBe('Empty Chart')
  })

  it('handles special characters in title', () => {
    const wrapper = mount(ChartCard, {
      props: { title: 'Win Rate (%) & KDA' }
    })

    expect(wrapper.find('h4').text()).toBe('Win Rate (%) & KDA')
  })

  it('handles emoji in title', () => {
    const wrapper = mount(ChartCard, {
      props: { title: '📊 Performance Chart' }
    })

    expect(wrapper.find('h4').text()).toBe('📊 Performance Chart')
  })
})

