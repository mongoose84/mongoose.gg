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

  it('titles the banner with the champion and role', () => {
    expect(mountHeader().get('[data-testid="match-header-champion"]').text()).toBe('Ahri, Mid')
    expect(mountHeader({ role: 'UNKNOWN' }).get('[data-testid="match-header-champion"]').text()).toBe('Ahri')
  })

  it('marks a win in purple with the word Victory and the time', () => {
    const result = mountHeader().get('[data-testid="match-header-result"]')
    expect(result.text()).toBe('Victory · 2 hours ago')
    expect(result.classes()).toContain('mp-up')
  })

  it('marks a loss in orange with the word Defeat', () => {
    const result = mountHeader({ win: false }).get('[data-testid="match-header-result"]')
    expect(result.text()).toMatch(/^Defeat/)
    expect(result.classes()).toContain('mp-down')
  })

  it('calls a match under five minutes a remake', () => {
    const result = mountHeader({ gameDurationSec: 200, win: false }).get('[data-testid="match-header-result"]')
    expect(result.text()).toMatch(/^Remake/)
    expect(result.classes()).not.toContain('mp-down')
  })

  it('builds the meta line from queue and length', () => {
    expect(mountHeader().get('[data-testid="match-header-meta"]').text()).toBe('Ranked Solo · 30:00')
  })

  it('shows K / D / A as a glass chip', () => {
    const chip = mountHeader().get('[data-testid="match-header-kda"]')
    expect(chip.text()).toBe('7 / 3 / 11 KDA')
    expect(chip.classes()).toContain('mp-chip--glass')
  })

  it('shows the champion splash art and falls back to a plain card when it fails', async () => {
    const wrapper = mountHeader()
    const art = wrapper.get('[data-testid="match-header-art"]')
    expect(art.attributes('src')).toContain('/img/champion/splash/Ahri_0.jpg')
    expect(art.attributes('alt')).toBe('Ahri splash art')

    await art.trigger('error')
    expect(wrapper.find('[data-testid="match-header-art"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="match-header"]').classes()).toContain('match-header--plain')
  })

  it('adds the LP change and the rank after the match as glass chips', () => {
    const wrapper = mountHeader({ lpChange: 19, lpAfter: 64, tierAfter: 'EMERALD', rankAfter: 'II' })
    const lp = wrapper.get('[data-testid="match-header-lp"]')
    expect(lp.text()).toBe('+19 LP')
    expect(lp.find('.match-header__chip-number--up').exists()).toBe(true)

    const rank = wrapper.get('[data-testid="match-header-rank"]')
    expect(rank.text()).toBe('Emerald II · 64 LP')
    expect(rank.get('.match-header__tier-dot').attributes('style')).toContain('var(--color-rank-emerald')
  })

  it('shows a loss of LP in orange with a real minus', () => {
    const lp = mountHeader({ win: false, lpChange: -17 }).get('[data-testid="match-header-lp"]')
    expect(lp.text()).toBe('−17 LP')
    expect(lp.find('.match-header__chip-number--down').exists()).toBe(true)
  })

  it('leaves the LP and rank chips out without recorded LP', () => {
    const wrapper = mountHeader({ lpChange: null, tierAfter: null })
    expect(wrapper.find('[data-testid="match-header-lp"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="match-header-rank"]').exists()).toBe(false)
  })

  it('has no download button (it lives in All your stats)', () => {
    expect(mountHeader().find('[data-testid="match-download"]').exists()).toBe(false)
  })

  it('shows the standout finding as a strength or trend chip', () => {
    const good = mountHeader({}, { text: 'Strong vision control', type: 'positive' }).get('[data-testid="match-header-badge"]')
    expect(good.text()).toBe('Strong vision control')
    expect(good.classes()).toContain('mp-chip--strength')

    const needsWork = mountHeader({}, { text: 'Higher deaths vs trend', type: 'neutral' }).get('[data-testid="match-header-badge"]')
    expect(needsWork.classes()).toContain('mp-chip--trend')

    expect(mountHeader().find('[data-testid="match-header-badge"]').exists()).toBe(false)
  })
})
