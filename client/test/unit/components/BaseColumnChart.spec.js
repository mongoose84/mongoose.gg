import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseColumnChart from '@/components/base/BaseColumnChart.vue'

const groups = [
  { key: 'afternoon', label: 'Afternoon', value: 67 },
  { key: 'evening', label: 'Evening', value: 64 },
  { key: 'late', label: 'After 11pm', value: 25 }
]

function mountChart(props = {}) {
  return mount(BaseColumnChart, { props: { groups, measure: 'Win rate by start time', ...props } })
}

describe('BaseColumnChart', () => {
  it('draws one column per group with its value and label', () => {
    const columns = mountChart().findAll('[data-testid="column-chart-group"]')
    expect(columns).toHaveLength(3)
    expect(columns[0].text()).toBe('67%Afternoon')
    expect(columns[2].text()).toBe('25%After 11pm')
  })

  it('scales each column to its share of the maximum', () => {
    const bars = mountChart().findAll('[data-testid="column-chart-bar"]')
    expect(bars[0].attributes('style')).toContain('0.67 *')
    expect(bars[2].attributes('style')).toContain('0.25 *')
  })

  it('draws the weak spot in warn and the rest in primary', () => {
    const bars = mountChart({ weakKey: 'late' }).findAll('[data-testid="column-chart-bar"]')
    expect(bars[2].classes()).toContain('mp-columns__bar--warn')
    expect(bars[0].classes()).not.toContain('mp-columns__bar--warn')
  })

  it('lists every group and value in its text alternative', () => {
    const chart = mountChart().get('[data-testid="column-chart"]')
    expect(chart.attributes('role')).toBe('img')
    expect(chart.attributes('aria-label'))
      .toBe('Win rate by start time: afternoon 67 percent, evening 64 percent, after 11pm 25 percent.')
  })

  it('shows at most four columns', () => {
    const many = [...groups, { key: 'morning', label: 'Morning', value: 50 }, { key: 'x', label: 'X', value: 10 }]
    expect(mountChart({ groups: many }).findAll('[data-testid="column-chart-group"]')).toHaveLength(4)
  })
})
