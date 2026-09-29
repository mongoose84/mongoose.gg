import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseFormStrip from '@/components/base/BaseFormStrip.vue'

function mountStrip(results) {
  return mount(BaseFormStrip, { props: { results } })
}

describe('BaseFormStrip', () => {
  it('draws one bar per match, oldest first, with height classes for the result', () => {
    const bars = mountStrip(['loss', 'win', 'remake', 'win']).findAll('[data-testid="form-strip-bar"]')
    expect(bars.map((b) => b.attributes('data-result'))).toEqual(['loss', 'win', 'remake', 'win'])
    expect(bars[0].classes()).toEqual([])
    expect(bars[1].classes()).toContain('is-win')
    expect(bars[2].classes()).toContain('is-remake')
  })

  it('states the sequence and totals as its text alternative', () => {
    const strip = mountStrip(['loss', 'win', 'remake', 'win']).get('[data-testid="form-strip-bars"]')
    expect(strip.attributes('role')).toBe('img')
    expect(strip.attributes('aria-label'))
      .toBe('Last 4 matches, oldest to newest: loss, win, remake, win. 2 wins, 1 loss, 1 remake.')
  })

  it('leaves remakes out of the totals when there are none', () => {
    const label = mountStrip(['win', 'loss', 'loss']).get('[data-testid="form-strip-bars"]').attributes('aria-label')
    expect(label).toBe('Last 3 matches, oldest to newest: win, loss, loss. 1 win, 2 losses.')
  })

  it('labels both ends and sizes the grid to the matches shown', () => {
    const wrapper = mountStrip(['win', 'loss', 'win'])
    expect(wrapper.get('.mp-form-scale').text()).toBe('3 matches agoLatest')
    expect(wrapper.get('.mp-form').attributes('style')).toContain('repeat(3, minmax(0, 1fr))')
  })

  it('shows at most the newest 20 matches', () => {
    const results = ['loss', ...Array.from({ length: 21 }, () => 'win')]
    const bars = mountStrip(results).findAll('[data-testid="form-strip-bar"]')
    expect(bars).toHaveLength(20)
    expect(bars.every((b) => b.attributes('data-result') === 'win')).toBe(true)
  })

  it('has no clickable bars', () => {
    expect(mountStrip(['win', 'loss']).findAll('button, a')).toHaveLength(0)
  })
})
