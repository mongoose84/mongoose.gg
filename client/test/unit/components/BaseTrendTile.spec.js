import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseTrendTile from '@/components/base/BaseTrendTile.vue'

function mountTile(props = {}) {
  return mount(BaseTrendTile, {
    props: {
      label: 'Deaths',
      value: '4.1',
      unit: 'per match',
      was: '5.6',
      verdict: 'Improving',
      tone: 'up',
      arrow: '▼',
      values: [6, null, 5, 4],
      rolling: [{ index: 2, value: 5.5 }, { index: 3, value: 4.5 }],
      length: 4,
      benchmark: 4.9,
      description: 'Deaths, last 4 matches: 4.1 per match, was 5.6, improving.',
      ...props
    }
  })
}

describe('BaseTrendTile', () => {
  it('shows the label, value, verdict and earlier value', () => {
    const wrapper = mountTile()

    expect(wrapper.get('[data-testid="trend-tile-label"]').text()).toBe('Deaths')
    expect(wrapper.get('[data-testid="trend-tile-value"]').text()).toBe('4.1\u00a0per match')
    expect(wrapper.get('[data-testid="trend-tile-verdict"]').text()).toBe('▼\u00a0Improving · was 5.6')
  })

  it('colours the verdict by its meaning, not the arrow', () => {
    expect(mountTile().get('[data-testid="trend-tile-verdict"]').classes()).toContain('mp-up')
    expect(mountTile({ tone: 'down', arrow: '▲' }).get('[data-testid="trend-tile-verdict"]').classes()).toContain('mp-down')
    const neutral = mountTile({ tone: 'neutral', arrow: '' }).get('[data-testid="trend-tile-verdict"]')
    expect(neutral.classes()).not.toContain('mp-up')
    expect(neutral.classes()).not.toContain('mp-down')
  })

  it('is a group labelled with its text alternative', () => {
    const tile = mountTile().get('[data-testid="trend-tile"]')
    expect(tile.attributes('role')).toBe('group')
    expect(tile.attributes('aria-label')).toBe('Deaths, last 4 matches: 4.1 per match, was 5.6, improving.')
  })

  it('draws a dot per present match, the rolling line and the benchmark', () => {
    const wrapper = mountTile()

    expect(wrapper.findAll('[data-testid="trend-tile-dot"]')).toHaveLength(3)
    expect(wrapper.get('[data-testid="trend-tile-line"]').attributes('points').split(' ')).toHaveLength(2)
    expect(wrapper.find('[data-testid="trend-tile-benchmark"]').exists()).toBe(true)
  })

  it('draws only the line when the values are sampled away', () => {
    const wrapper = mountTile({ values: null })

    expect(wrapper.findAll('[data-testid="trend-tile-dot"]')).toHaveLength(0)
    expect(wrapper.find('[data-testid="trend-tile-line"]').exists()).toBe(true)
  })

  it('leaves out the benchmark line without a benchmark', () => {
    expect(mountTile({ benchmark: null }).find('[data-testid="trend-tile-benchmark"]').exists()).toBe(false)
  })

  it('draws no chart without data', () => {
    const wrapper = mountTile({ values: [], rolling: [], was: null, verdict: 'Needs 20 matches', arrow: '' })

    expect(wrapper.find('[data-testid="trend-tile-chart"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="trend-tile-verdict"]').text()).toBe('Needs 20 matches')
  })
})
