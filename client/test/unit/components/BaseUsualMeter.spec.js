import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseUsualMeter from '@/components/base/BaseUsualMeter.vue'

function mountMeter(props = {}) {
  return mount(BaseUsualMeter, {
    props: {
      label: 'Gold lead at 10',
      value: '+1,240',
      now: 1240,
      min: -2000,
      max: 2000,
      usual: 180,
      better: 'higher',
      valueText: '+1,240 gold, your usual is +180',
      ...props
    }
  })
}

describe('BaseUsualMeter', () => {
  it('positions the fill and tick against min/max, including a negative min for gold', () => {
    const wrapper = mountMeter()
    // now=1240 on [-2000,2000] -> (1240 - -2000) / 4000 = 81%
    expect(wrapper.get('.mp-usual__fill').attributes('style')).toContain('width: 81%')
    // usual=180 -> (180 - -2000) / 4000 = 54.5%
    expect(wrapper.get('.mp-usual__tick').attributes('style')).toContain('left: 54.5%')
  })

  it('clamps the fill and tick to the scale', () => {
    const wrapper = mountMeter({ now: 5000, usual: -5000 })
    expect(wrapper.get('.mp-usual__fill').attributes('style')).toContain('width: 100%')
    expect(wrapper.get('.mp-usual__tick').attributes('style')).toContain('left: 0%')
  })

  it('shows the label and the formatted value', () => {
    const wrapper = mountMeter()
    expect(wrapper.get('.mp-usual__label').text()).toBe('Gold lead at 10')
    expect(wrapper.get('.mp-usual__value').text()).toBe('+1,240')
  })

  it('fills primary when this match beats the usual', () => {
    const wrapper = mountMeter({ now: 1240, usual: 180, better: 'higher' })
    expect(wrapper.get('.mp-usual__fill').classes()).not.toContain('mp-usual__fill--warn')
  })

  it('fills warn when this match falls short of the usual', () => {
    const wrapper = mountMeter({ now: 100, usual: 180, better: 'higher' })
    expect(wrapper.get('.mp-usual__fill').classes()).toContain('mp-usual__fill--warn')
  })

  it('colours deaths by score, not by the length of the bar', () => {
    // Fewer deaths than usual is better, even though the bar is short — no warn
    const fewer = mountMeter({
      label: 'Deaths before 10', value: '0', now: 0, min: 0, max: 4, usual: 1.4,
      better: 'lower', valueText: '0 deaths before 10, your usual is 1.4'
    })
    expect(fewer.get('.mp-usual__fill').classes()).not.toContain('mp-usual__fill--warn')

    // More deaths than usual is worse, even though the bar grows further right — warn
    const more = mountMeter({
      label: 'Deaths before 10', value: '3', now: 3, min: 0, max: 4, usual: 1.4,
      better: 'lower', valueText: '3 deaths before 10, your usual is 1.4'
    })
    expect(more.get('.mp-usual__fill').classes()).toContain('mp-usual__fill--warn')
  })

  it('exposes role=meter with aria-valuemin/max/now and a valuetext stating both numbers', () => {
    const root = mountMeter().get('[role="meter"]')
    expect(root.attributes('aria-valuemin')).toBe('-2000')
    expect(root.attributes('aria-valuemax')).toBe('2000')
    expect(root.attributes('aria-valuenow')).toBe('1240')
    expect(root.attributes('aria-valuetext')).toBe('+1,240 gold, your usual is +180')
    expect(root.attributes('aria-label')).toBe('Gold lead at 10')
  })
})
