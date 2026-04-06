import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MatchHeader from '@/components/matches/MatchHeader.vue'

vi.mock('@/utils/formatters', () => ({
  formatRole: (role) => role,
  formatDuration: (sec) => `${Math.floor(sec / 60)}m`,
  formatRelativeTime: () => '2h ago'
}))

describe('MatchHeader.vue', () => {
  const baseMatch = {
    matchId: 'EUW1_123',
    win: true,
    championName: 'Ahri',
    championIconUrl: 'https://example.com/ahri.png',
    role: 'MIDDLE',
    queueType: 'Ranked Solo',
    kills: 10,
    deaths: 2,
    assists: 8,
    gameDurationSec: 1800,
    gameStartTime: Date.now() - 7200000,
    teamKills: 30,
    enemyTeamKills: 25
  }

  const createWrapper = (matchOverrides = {}) =>
    mount(MatchHeader, { props: { match: { ...baseMatch, ...matchOverrides } } })

  it('renders the match header container', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.match-header').exists()).toBe(true)
  })

  it('renders champion name', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.champion-name').text()).toBe('Ahri')
  })

  it('displays champion icon when championIconUrl is provided', () => {
    const wrapper = createWrapper()
    const img = wrapper.find('.champion-icon')
    expect(img.exists()).toBe(true)
    expect(img.attributes('src')).toBe('https://example.com/ahri.png')
  })

  it('does not render champion icon when championIconUrl is null', () => {
    const wrapper = createWrapper({ championIconUrl: null })
    expect(wrapper.find('.champion-icon').exists()).toBe(false)
  })

  it('shows Victory badge for a win', () => {
    const wrapper = createWrapper({ win: true })
    expect(wrapper.find('.result-badge').text()).toBe('Victory')
  })

  it('shows Defeat badge for a loss', () => {
    const wrapper = createWrapper({ win: false })
    expect(wrapper.find('.result-badge').text()).toBe('Defeat')
  })

  it('applies win class to header for a win', () => {
    const wrapper = createWrapper({ win: true })
    expect(wrapper.find('.match-header').classes()).toContain('win')
  })

  it('applies loss class to header for a loss', () => {
    const wrapper = createWrapper({ win: false })
    expect(wrapper.find('.match-header').classes()).toContain('loss')
  })

  it('displays kills in the KDA display', () => {
    const wrapper = createWrapper({ kills: 7 })
    expect(wrapper.find('.kda-kills').text()).toBe('7')
  })

  it('displays deaths in the KDA display', () => {
    const wrapper = createWrapper({ deaths: 3 })
    expect(wrapper.find('.kda-deaths').text()).toBe('3')
  })

  it('displays assists in the KDA display', () => {
    const wrapper = createWrapper({ assists: 11 })
    expect(wrapper.find('.kda-assists').text()).toBe('11')
  })

  it('renders queue type in secondary row', () => {
    const wrapper = createWrapper({ queueType: 'Ranked Solo' })
    expect(wrapper.find('.queue').text()).toBe('Ranked Solo')
  })

  it('hides role element when role is UNKNOWN', () => {
    const wrapper = createWrapper({ role: 'UNKNOWN' })
    expect(wrapper.find('.role').exists()).toBe(false)
  })

  it('renders role element when role is known', () => {
    const wrapper = createWrapper({ role: 'MIDDLE' })
    expect(wrapper.find('.role').exists()).toBe(true)
  })

  it('displays formatted game duration', () => {
    const wrapper = createWrapper({ gameDurationSec: 1800 })
    expect(wrapper.find('.duration').text()).toBe('30m')
  })

  it('displays relative timestamp', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.timestamp').text()).toBe('2h ago')
  })

  it('renders team kill score', () => {
    const wrapper = createWrapper({ teamKills: 30 })
    const teamKillEls = wrapper.findAll('.team-kills')
    expect(teamKillEls[0].text()).toBe('30')
  })

  it('renders enemy team kill score', () => {
    const wrapper = createWrapper({ enemyTeamKills: 25 })
    const teamKillEls = wrapper.findAll('.team-kills')
    expect(teamKillEls[1].text()).toBe('25')
  })
})
