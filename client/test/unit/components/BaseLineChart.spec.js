import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseLineChart from '@/components/base/BaseLineChart.vue'

function mountChart(props = {}) {
  return mount(BaseLineChart, {
    props: {
      points: [{ x: 0, y: 2190 }, { x: 2, y: 2210 }, { x: 4, y: 2150 }],
      length: 5,
      ariaLabel: 'LP, last 5 matches: from Emerald III to Emerald II.',
      ...props
    }
  })
}

describe('BaseLineChart', () => {
  it('is an image with its text alternative', () => {
    const chart = mountChart().get('[data-testid="line-chart"]')

    expect(chart.attributes('role')).toBe('img')
    expect(chart.attributes('aria-label')).toBe('LP, last 5 matches: from Emerald III to Emerald II.')
  })

  it('places points by match index across the full width', () => {
    const points = mountChart().get('[data-testid="line-chart-line"]').attributes('points').split(' ')

    expect(points).toHaveLength(3)
    expect(points[0].startsWith('0,')).toBe(true)
    expect(points[1].startsWith('500,')).toBe(true)
    expect(points[2].startsWith('1000,')).toBe(true)
  })

  it('draws guides inside the scale and leaves out the rest', () => {
    const wrapper = mountChart({ guides: [{ y: 2200, label: 'Emerald II' }, { y: 2600, label: 'Emerald I' }] })

    expect(wrapper.findAll('[data-testid="line-chart-guide"]')).toHaveLength(1)
    expect(wrapper.text()).toContain('Emerald II')
    expect(wrapper.text()).not.toContain('Emerald I ')
  })

  it('labels markers, in warn for a drop, and draws unlabelled ones as dots only', () => {
    const wrapper = mountChart({
      markers: [
        { x: 2, y: 2210, label: 'Emerald II', tone: 'primary' },
        { x: 4, y: 2150, label: '▼ −60 LP', tone: 'warn' },
        { x: 0, y: 2190, label: '', tone: 'primary' }
      ]
    })

    expect(wrapper.findAll('[data-testid="line-chart-marker"]')).toHaveLength(3)
    const labels = wrapper.findAll('[data-testid="line-chart-marker-label"]')
    expect(labels.map((l) => l.text())).toEqual(['Emerald II', '▼ −60 LP'])
    expect(labels[1].classes()).toContain('line-chart__marker-label--warn')
  })

  it('draws no line from a single point', () => {
    expect(mountChart({ points: [{ x: 0, y: 50 }] }).find('[data-testid="line-chart-line"]').exists()).toBe(false)
  })
})
