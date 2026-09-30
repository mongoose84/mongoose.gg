import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseWinFactorRow from '@/components/base/BaseWinFactorRow.vue'

function mountRow(props = {}) {
  return mount(BaseWinFactorRow, {
    props: {
      label: 'Ahead at 15 minutes',
      hitWinRate: 71,
      missWinRate: 34,
      description: 'Ahead at 15 minutes: you win 71% of 12 matches when you hit it, and 34% of 8 when you miss it.',
      ...props
    }
  })
}

describe('BaseWinFactorRow', () => {
  it('shows the label and both win rates', () => {
    const wrapper = mountRow()

    expect(wrapper.get('[data-testid="win-factor-label"]').text()).toBe('Ahead at 15 minutes')
    expect(wrapper.text()).toContain('71%')
    expect(wrapper.text()).toContain('34%')
  })

  it('places each dot at its win rate', () => {
    const wrapper = mountRow()

    expect(wrapper.get('[data-testid="win-factor-hit"]').attributes('style')).toContain('left: 71%')
    expect(wrapper.get('[data-testid="win-factor-miss"]').attributes('style')).toContain('left: 34%')
  })

  it('draws a negative gap the same way, hit left of miss', () => {
    const wrapper = mountRow({ hitWinRate: 40, missWinRate: 55 })

    expect(wrapper.get('[data-testid="win-factor-hit"]').attributes('style')).toContain('left: 40%')
    expect(wrapper.get('[data-testid="win-factor-miss"]').attributes('style')).toContain('left: 55%')
  })

  it('says the row in words as an image', () => {
    const track = mountRow().get('[data-testid="win-factor-track"]')

    expect(track.attributes('role')).toBe('img')
    expect(track.attributes('aria-label')).toContain('you win 71% of 12 matches when you hit it')
  })
})
