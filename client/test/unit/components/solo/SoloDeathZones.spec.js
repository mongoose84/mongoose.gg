import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SoloDeathZones from '@/components/solo/SoloDeathZones.vue'
import { deathZonesResponse } from '@test/helpers/soloFixtures'

function mountCard(props = {}) {
  return mount(SoloDeathZones, { props: { data: deathZonesResponse(), queue: 'ranked_solo', ...props } })
}

describe('SoloDeathZones', () => {
  it('titles the card after the zone that cost the most objectives', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="solo-death-zones-title"]').text()).toBe('Deaths in their bot-side jungle cost you the most objectives')
    expect(wrapper.get('[data-testid="solo-death-zones-caption"]').text())
      .toBe('236 deaths over your last 50 matches. Red-side matches are mirrored, so your base is always bottom left.')
  })

  it('lists each zone with its deaths, and marks only the zone to fix first', () => {
    const wrapper = mountCard()
    const focus = wrapper.get('[data-testid="death-zone-jungleEnemyBot"]')
    const other = wrapper.get('[data-testid="death-zone-midLaneYours"]')

    expect(focus.text()).toContain('Enemy jungle, bot side')
    expect(focus.text()).toContain('38')
    expect(focus.find('[data-testid="death-zone-fix-first"]').exists()).toBe(true)
    expect(focus.attributes('aria-label')).toBe('Enemy jungle, bot side, 38 deaths, fix first')
    expect(other.find('[data-testid="death-zone-fix-first"]').exists()).toBe(false)
  })

  it('draws each zone\'s deaths against the zone with most', () => {
    const bar = mountCard().get('[data-testid="death-zone-bar-jungleEnemyBot"] > span')
    expect(parseFloat(bar.attributes('style').match(/width:\s*([\d.]+)%/)[1])).toBeCloseTo((38 / 44) * 100, 1)
  })

  it('opens on the zone to fix first with its pattern, facts and a tip', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="death-zone-jungleEnemyBot"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('[data-testid="solo-death-zones-filter"]').text()).toBe('Enemy jungle, bot side')
    expect(wrapper.get('[data-testid="solo-death-zones-pattern"]').text()).toBe('Mostly caught alone, after 25 min')
    expect(wrapper.get('[data-testid="death-zones-fact-cost"]').text()).toContain('lost an objective after · mostly dragons')
    expect(wrapper.get('[data-testid="solo-death-zones-tip"]').text())
      .toBe('Next match: After 25 minutes, stay with your team instead of farming here alone.')
  })

  it('shows a pressed zone in the panel, with all its numbers on request', async () => {
    const wrapper = mountCard()

    await wrapper.get('[data-testid="death-zone-midLaneYours"]').trigger('click')
    expect(wrapper.get('[data-testid="death-zone-midLaneYours"]').attributes('aria-pressed')).toBe('true')
    expect(wrapper.get('[data-testid="solo-death-zones-filter"]').text()).toBe('Mid lane, your half')
    expect(wrapper.get('[data-testid="solo-death-zones-pattern"]').text()).toBe('Mostly ganked, before 14 min')

    expect(wrapper.find('[data-testid="death-zones-phase-early"]').exists()).toBe(false)
    const more = wrapper.get('[data-testid="solo-death-zones-more"]')
    await more.trigger('click')
    expect(more.attributes('aria-expanded')).toBe('true')
    expect(wrapper.get('[data-testid="death-zones-phase-early"]').text()).toContain('30')
    expect(wrapper.findAll('.death-zones__group h4').map((h) => h.text()))
      .toEqual(['Deaths when', 'Deaths how', 'Objectives lost after your death'])
  })

  it('shows every zone on the map', () => {
    const wrapper = mountCard()
    expect(wrapper.find('[data-testid="death-map-zone-jungleEnemyBot"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="death-map-zone-midLaneYours"]').exists()).toBe(true)
  })

  it('says where you die most when no zone lost 3 objectives', () => {
    const data = deathZonesResponse()
    data.zones = data.zones.map((z) => ({ ...z, lostObjectives: 2 }))

    expect(mountCard({ data }).get('[data-testid="solo-death-zones-title"]').text()).toBe('You die most in your half of mid lane')
  })

  it('shows the backfill progress in its slot while detail is missing', () => {
    const wrapper = mountCard({ data: deathZonesResponse({ ready: false, deaths: 12, backfill: { status: 'running', done: 12, total: 50 } }) })

    const backfill = wrapper.get('[data-testid="solo-death-zones-backfill"]')
    expect(backfill.text()).toContain('Adding detail to your older matches · 12 of 50')
    expect(backfill.text()).toContain('Your death map appears when this finishes.')
    expect(backfill.get('[role="progressbar"]').attributes('aria-valuenow')).toBe('12')
    expect(wrapper.find('[data-testid="death-map"]').exists()).toBe(false)
  })

  it('says it is waiting on Riot, or queued, without error styling', () => {
    const waiting = mountCard({ data: deathZonesResponse({ ready: false, backfill: { status: 'waiting', done: 12, total: 50, retryAt: '2026-10-01T18:04:00Z' } }) })
    const queued = mountCard({ data: deathZonesResponse({ ready: false, backfill: { status: 'queued', done: 0, total: 0 } }) })

    expect(waiting.get('[data-testid="solo-death-zones-backfill-line"]').text()).toBe("Waiting on Riot's servers. We'll continue automatically.")
    expect(waiting.find('[role="alert"]').exists()).toBe(false)
    expect(queued.get('[data-testid="solo-death-zones-backfill-line"]').text()).toBe('Queued behind your match sync.')
    expect(queued.get('[role="progressbar"]').attributes('aria-valuenow')).toBeUndefined()
  })

  it('asks for more matches below 30 counted deaths once nothing is left to backfill', async () => {
    const wrapper = mountCard({ data: deathZonesResponse({ ready: false, deaths: 12, zones: [] }) })

    expect(wrapper.get('[data-testid="solo-death-zones-empty"]').text()).toContain('Play a few more matches to see where your deaths cost you')
    await wrapper.get('[data-testid="solo-death-zones-sync"]').trigger('click')
    expect(wrapper.emitted('sync')).toHaveLength(1)
  })

  it('shows a skeleton while loading and retries on error', async () => {
    expect(mountCard({ data: null, loading: true }).find('[data-testid="solo-death-zones-loading"]').exists()).toBe(true)

    const failed = mountCard({ error: true })
    await failed.get('[data-testid="solo-death-zones-retry"]').trigger('click')
    expect(failed.emitted('retry')).toHaveLength(1)
  })
})
