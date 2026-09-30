import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseGoalStrip from '@/components/base/BaseGoalStrip.vue'

const results = ['hit', 'miss', null, 'hit']

function mountStrip() {
  return mount(BaseGoalStrip, { props: { results, markLabel: '0.9+ vision per minute' } })
}

describe('BaseGoalStrip', () => {
  it('draws one cell per match, oldest first, by result', () => {
    const cells = mountStrip().findAll('[data-testid="goal-strip-cell"]')

    expect(cells).toHaveLength(4)
    expect(cells[0].classes()).toContain('is-hit')
    expect(cells[1].classes()).toContain('is-miss')
    expect(cells[2].classes()).toContain('is-none')
  })

  it('lists every match in its text alternative', () => {
    const strip = mountStrip().get('[data-testid="goal-strip-cells"]')

    expect(strip.attributes('role')).toBe('img')
    expect(strip.attributes('aria-label')).toBe(
      '0.9+ vision per minute, last 4 matches, oldest to newest: hit, missed, not counted, hit. Hit in 2 of 3.'
    )
  })

  it('labels both ends', () => {
    expect(mountStrip().text()).toContain('4 matches ago')
    expect(mountStrip().text()).toContain('Latest')
  })
})
