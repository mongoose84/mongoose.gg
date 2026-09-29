import { describe, it, expect, vi, afterEach } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import BaseMatchRow from '@/components/base/BaseMatchRow.vue'

function mountRow(props = {}) {
  return mount(BaseMatchRow, {
    props: {
      to: { name: 'app-matches', params: { matchId: 'EUW1_1' } },
      championName: 'Ahri',
      win: true,
      kda: '7/2/9',
      queue: 'Ranked Solo/Duo',
      ...props
    },
    global: { stubs: { RouterLink: RouterLinkStub } }
  })
}

describe('BaseMatchRow', () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it('is one link to the match', () => {
    const wrapper = mountRow()
    expect(wrapper.getComponent(RouterLinkStub).props('to')).toEqual({ name: 'app-matches', params: { matchId: 'EUW1_1' } })
  })

  it('shows champion, KDA and a win in positive text', () => {
    const wrapper = mountRow()
    expect(wrapper.get('[data-testid="match-row-champion"]').text()).toBe('Ahri')
    expect(wrapper.get('[data-testid="match-row-kda"]').text()).toBe('7/2/9')
    const result = wrapper.get('[data-testid="match-row-result"]')
    expect(result.text()).toBe('Victory')
    expect(result.classes()).toContain('mp-up')
    expect(wrapper.get('[data-testid="match-row-portrait"]').classes()).not.toContain('mp-portrait--loss')
  })

  it('marks a loss with warn colours and the word Defeat', () => {
    const wrapper = mountRow({ win: false })
    const result = wrapper.get('[data-testid="match-row-result"]')
    expect(result.text()).toBe('Defeat')
    expect(result.classes()).toContain('mp-down')
    expect(wrapper.get('[data-testid="match-row-portrait"]').classes()).toContain('mp-portrait--loss')
  })

  it('builds the meta line from queue, length and time ago', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-09-27T12:00:00Z'))
    const wrapper = mountRow({ durationSeconds: 1860, timestamp: Date.parse('2026-09-27T10:00:00Z') })
    const meta = wrapper.get('[data-testid="match-row-meta"]').text()
    expect(meta).toContain('Ranked Solo/Duo')
    expect(meta).toContain('31')
    expect(meta).toContain('2 hours ago')
  })

  it('shows signed LP with a real minus and hides it when unknown', () => {
    expect(mountRow({ lpChange: 19 }).get('[data-testid="match-row-lp"]').text()).toBe('+19 LP')
    expect(mountRow({ lpChange: -17 }).get('[data-testid="match-row-lp"]').text()).toBe('−17 LP')
    expect(mountRow().find('[data-testid="match-row-lp"]').exists()).toBe(false)
  })

  it('colours a gain purple, a loss orange and no change neither', () => {
    expect(mountRow({ lpChange: 19 }).get('[data-testid="match-row-lp"]').classes()).toContain('mp-up')
    expect(mountRow({ lpChange: -17 }).get('[data-testid="match-row-lp"]').classes()).toContain('mp-down')
    const none = mountRow({ lpChange: 0, win: false }).get('[data-testid="match-row-lp"]')
    expect(none.text()).toBe('±0 LP')
    expect(none.classes()).not.toContain('mp-up')
    expect(none.classes()).not.toContain('mp-down')
  })

  it('shows a remake as neither a win nor a loss', () => {
    const wrapper = mountRow({ win: false, remake: true })
    const result = wrapper.get('[data-testid="match-row-result"]')
    expect(result.text()).toBe('Remake')
    expect(result.classes()).not.toContain('mp-down')
    expect(wrapper.get('[data-testid="match-row-portrait"]').classes()).not.toContain('mp-portrait--loss')
  })

  it('adds the Riot ID to the meta line in Overall mode', () => {
    const meta = mountRow({ riotId: 'Faker#EUW' }).get('[data-testid="match-row-meta"]').text()
    expect(meta).toContain('Ranked Solo/Duo · Faker#EUW')
  })

  it('falls back to the Data Dragon icon and to a plain circle when the image fails', async () => {
    const wrapper = mountRow()
    const img = wrapper.get('[data-testid="match-row-portrait"]')
    expect(img.attributes('src')).toContain('/img/champion/Ahri.png')
    expect(img.attributes('alt')).toBe('')

    await img.trigger('error')
    expect(wrapper.find('[data-testid="match-row-portrait"]').exists()).toBe(false)
    expect(wrapper.find('.match-row__portrait-fallback').exists()).toBe(true)
  })
})
