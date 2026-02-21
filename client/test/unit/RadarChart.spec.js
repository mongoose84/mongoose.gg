import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import RadarChart from '@/components/solo/RadarChart.vue'

vi.mock('vue-chartjs', () => ({
  Radar: {
    name: 'Radar',
    props: ['data', 'options'],
    template: '<div data-testid="mock-radar-chart"></div>'
  }
}))

describe('RadarChart', () => {
  const mockAxes = [
    { key: 'laning', label: 'Laning', value: 62.5, rawValue: '+500', rawUnit: 'Gold diff @15' },
    { key: 'farming', label: 'Farming', value: 58, rawValue: '7.2', rawUnit: 'CS/min' },
    { key: 'combat', label: 'Combat', value: 64.2, rawValue: '620', rawUnit: 'DPM' },
    { key: 'vision', label: 'Vision', value: 44, rawValue: '1.0', rawUnit: 'Vision/min' },
    { key: 'objectives', label: 'Objectives', value: 55.3, rawValue: '61%', rawUnit: 'Participation' },
    { key: 'survivability', label: 'Survivability', value: 56, rawValue: '5.1', rawUnit: 'Deaths/game' }
  ]

  const mountComponent = (props = {}) => {
    return mount(RadarChart, {
      props: {
        axes: [],
        gamesAnalyzed: 0,
        loading: false,
        ...props
      }
    })
  }

  it('renders radar chart when data is provided', () => {
    const wrapper = mountComponent({ axes: mockAxes, gamesAnalyzed: 20 })

    expect(wrapper.find('[data-testid="radar-chart"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="mock-radar-chart"]').exists()).toBe(true)
  })

  it('shows loading state', () => {
    const wrapper = mountComponent({ loading: true })

    expect(wrapper.find('[data-testid="radar-loading"]').exists()).toBe(true)
  })

  it('shows empty state when no data', () => {
    const wrapper = mountComponent({ axes: [], gamesAnalyzed: 0 })

    expect(wrapper.find('[data-testid="radar-empty"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('No performance data available')
  })

  it('shows games analyzed count', () => {
    const wrapper = mountComponent({ axes: mockAxes, gamesAnalyzed: 17 })

    expect(wrapper.find('[data-testid="radar-games-context"]').text()).toContain('Based on 17 games')
  })

  it('computes chart data correctly from axes prop', () => {
    const wrapper = mountComponent({ axes: mockAxes, gamesAnalyzed: 20 })
    const radarProps = wrapper.findComponent({ name: 'Radar' }).props()

    expect(radarProps.data.labels).toEqual(['Laning', 'Farming', 'Combat', 'Vision', 'Objectives', 'Survivability'])
    expect(radarProps.data.datasets[0].data).toEqual([62.5, 58, 64.2, 44, 55.3, 56])
  })

  it('uses 0-100 normalized scale in chart options', () => {
    const wrapper = mountComponent({ axes: mockAxes, gamesAnalyzed: 20 })
    const radarOptions = wrapper.findComponent({ name: 'Radar' }).props('options')

    expect(radarOptions.scales.r.min).toBe(0)
    expect(radarOptions.scales.r.max).toBe(100)
  })
})
