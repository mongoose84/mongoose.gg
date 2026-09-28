import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseSkeleton from '@/components/base/BaseSkeleton.vue'

describe('BaseSkeleton', () => {
  it('renders a hidden text shape by default', () => {
    const wrapper = mount(BaseSkeleton)
    const el = wrapper.get('[data-testid="base-skeleton"]')
    expect(el.classes()).toEqual(expect.arrayContaining(['mp-skeleton', 'mp-skeleton--text']))
    expect(el.attributes('aria-hidden')).toBe('true')
  })

  it('applies the variant class', () => {
    expect(mount(BaseSkeleton, { props: { variant: 'ring' } }).classes()).toContain('mp-skeleton--ring')
    expect(mount(BaseSkeleton, { props: { variant: 'portrait' } }).classes()).toContain('mp-skeleton--portrait')
  })

  it('uses no variant class for a block and sets its size', () => {
    const wrapper = mount(BaseSkeleton, { props: { variant: 'block', width: '11rem', height: '3rem' } })
    expect(wrapper.classes()).toEqual(['mp-skeleton'])
    expect(wrapper.attributes('style')).toContain('width: 11rem')
    expect(wrapper.attributes('style')).toContain('height: 3rem')
  })
})
