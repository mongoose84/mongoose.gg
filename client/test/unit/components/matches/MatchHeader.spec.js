import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import MatchHeader from '@/components/matches/MatchHeader.vue'

const baseMatch = {
  matchId: 'EUW1_1',
  championName: 'Ahri',
  championIconUrl: 'https://example.com/ahri.png',
  win: true,
  kills: 7,
  deaths: 3,
  assists: 11,
  role: 'MIDDLE',
  queueType: 'Ranked Solo',
  gameDurationSec: 1800,
  gameStartTime: Date.parse('2026-09-27T10:00:00Z'),
  teamKills: 30,
  enemyTeamKills: 25,
  creepScore: 210,
  csPerMin: 7.04
}

function mountHeader(overrides = {}, badge = null) {
  return mount(MatchHeader, {
    props: { match: { ...baseMatch, ...overrides }, badge },
    global: { stubs: { BaseIcon: true } }
  })
}

describe('MatchHeader', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-09-27T12:00:00Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('shows the champion as the card title', () => {
    expect(mountHeader().get('[data-testid="match-header-champion"]').text()).toBe('Ahri')
  })

  it('marks a win in purple with the word Victory', () => {
    const wrapper = mountHeader()
    const result = wrapper.get('[data-testid="match-header-result"]')
    expect(result.text()).toBe('Victory')
    expect(result.classes()).toContain('mp-up')
    expect(wrapper.get('[data-testid="match-header-portrait"]').classes()).not.toContain('mp-portrait--loss')
  })

  it('marks a loss in orange with the word Defeat', () => {
    const wrapper = mountHeader({ win: false })
    const result = wrapper.get('[data-testid="match-header-result"]')
    expect(result.text()).toBe('Defeat')
    expect(result.classes()).toContain('mp-down')
    expect(wrapper.get('[data-testid="match-header-portrait"]').classes()).toContain('mp-portrait--loss')
  })

  it('calls a match under five minutes a remake', () => {
    const result = mountHeader({ gameDurationSec: 200, win: false }).get('[data-testid="match-header-result"]')
    expect(result.text()).toBe('Remake')
    expect(result.classes()).not.toContain('mp-down')
  })

  it('builds the meta line from role, queue, length and time ago', () => {
    const meta = mountHeader().get('[data-testid="match-header-meta"]').text()
    expect(meta).toContain('Ranked Solo')
    expect(meta).toContain('30:00')
    expect(meta).toContain('2 hours ago')
  })

  it('leaves an unknown role out of the meta line', () => {
    const meta = mountHeader({ role: 'UNKNOWN' }).get('[data-testid="match-header-meta"]').text()
    expect(meta.startsWith('Ranked Solo')).toBe(true)
  })

  it('shows KDA, team kills and CS', () => {
    const wrapper = mountHeader()
    expect(wrapper.get('[data-testid="match-header-kda"]').text()).toBe('7 / 3 / 11')
    expect(wrapper.get('[data-testid="match-header-score"]').text()).toBe('30 – 25')
    expect(wrapper.get('[data-testid="match-header-cs"]').text()).toBe('210 · 7.0 per min')
  })

  it('falls back to the Data Dragon icon without an API icon', () => {
    const img = mountHeader({ championIconUrl: null }).get('[data-testid="match-header-portrait"]')
    expect(img.attributes('src')).toContain('/img/champion/Ahri.png')
  })

  it('shows the standout finding as a strength or trend chip', () => {
    const good = mountHeader({}, { text: 'Strong vision control', type: 'positive' }).get('[data-testid="match-header-badge"]')
    expect(good.text()).toBe('Strong vision control')
    expect(good.classes()).toContain('mp-chip--strength')

    const needsWork = mountHeader({}, { text: 'Higher deaths vs trend', type: 'neutral' }).get('[data-testid="match-header-badge"]')
    expect(needsWork.classes()).toContain('mp-chip--trend')

    expect(mountHeader().find('[data-testid="match-header-badge"]').exists()).toBe(false)
  })

  it('emits download from the Download data button', async () => {
    const wrapper = mountHeader()
    await wrapper.get('[data-testid="match-download"]').trigger('click')
    expect(wrapper.emitted('download')).toHaveLength(1)
  })
})
