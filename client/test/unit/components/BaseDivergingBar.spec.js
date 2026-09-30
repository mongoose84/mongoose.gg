import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseDivergingBar from '@/components/base/BaseDivergingBar.vue'

const mountBar = (value, max = 100) =>
  mount(BaseDivergingBar, { props: { value, max, description: 'Ahri: +50 LP.' } })

describe('BaseDivergingBar', () => {
  it('grows right for a gain, scaled to the maximum', () => {
    const wrapper = mountBar(50)

    expect(wrapper.get('[data-testid="diverging-bar-ahead"]').attributes('style')).toContain('width: 50%')
    expect(wrapper.find('[data-testid="diverging-bar-behind"]').exists()).toBe(false)
  })

  it('grows left for a loss', () => {
    const wrapper = mountBar(-25)

    expect(wrapper.get('[data-testid="diverging-bar-behind"]').attributes('style')).toContain('width: 25%')
    expect(wrapper.find('[data-testid="diverging-bar-ahead"]').exists()).toBe(false)
  })

  it('caps at a full half and draws nothing for zero', () => {
    expect(mountBar(300).get('[data-testid="diverging-bar-ahead"]').attributes('style')).toContain('width: 100%')
    const zero = mountBar(0)
    expect(zero.find('[data-testid="diverging-bar-ahead"]').exists()).toBe(false)
    expect(zero.find('[data-testid="diverging-bar-behind"]').exists()).toBe(false)
  })

  it('is an image described in words', () => {
    const bar = mountBar(50).get('[data-testid="diverging-bar"]')
    expect(bar.attributes('role')).toBe('img')
    expect(bar.attributes('aria-label')).toBe('Ahri: +50 LP.')
  })
})
