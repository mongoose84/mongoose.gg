import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import BaseDeathMap from '@/components/base/BaseDeathMap.vue'

const zones = [
  { key: 'jungleEnemyBot', deaths: 38, anchor: { u: 0.74, v: 0.39 } },
  { key: 'midLaneYours', deaths: 10, anchor: { u: 0.4, v: 0.4 } }
]

function mountMap(props = {}) {
  return mount(BaseDeathMap, { props: { zones, description: 'Death map, last 20 matches: …', ...props } })
}

describe('BaseDeathMap', () => {
  it('places each zone at its anchor with v flipped for SVG', () => {
    const circle = mountMap().get('[data-testid="death-map-zone-jungleEnemyBot"]')

    expect(circle.attributes('cx')).toBe('74')
    expect(circle.attributes('cy')).toBe('61')
  })

  it('sizes circles by deaths, the most deaths the largest', () => {
    const wrapper = mountMap()
    const big = Number(wrapper.get('[data-testid="death-map-zone-jungleEnemyBot"]').attributes('r'))
    const small = Number(wrapper.get('[data-testid="death-map-zone-midLaneYours"]').attributes('r'))

    expect(big).toBe(8)
    expect(small).toBeGreaterThanOrEqual(4)
    expect(small).toBeLessThan(big)
  })

  it('draws only the zone to fix first in warn and marks the selected one', () => {
    const wrapper = mountMap({ selectedKey: 'midLaneYours', focusKey: 'jungleEnemyBot' })
    const focus = wrapper.get('[data-testid="death-map-zone-jungleEnemyBot"]')
    const selected = wrapper.get('[data-testid="death-map-zone-midLaneYours"]')

    expect(focus.classes()).toContain('death-map__zone--focus')
    expect(selected.classes()).not.toContain('death-map__zone--focus')
    expect(selected.classes()).toContain('death-map__zone--selected')
  })

  it('draws the zones over the minimap with each zone\'s death count', () => {
    const wrapper = mountMap()

    expect(wrapper.get('[data-testid="death-map-image"]').attributes('href')).toBe('/assets/images/summoners-rift-minimap.png')
    expect(wrapper.get('[data-testid="death-map-count-jungleEnemyBot"]').text()).toBe('38')
  })

  it('has a key that says what the colours mean', () => {
    const key = mountMap().get('[data-testid="death-map-key"]')

    expect(key.text()).toContain('Deaths')
    expect(key.text()).toContain('Fix first')
  })

  it('is an image with every zone in its text alternative', () => {
    const map = mountMap().get('[data-testid="death-map"]')
    expect(map.attributes('role')).toBe('img')
    expect(map.attributes('aria-label')).toBe('Death map, last 20 matches: …')
  })
})
