import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import GamerCard from '@/views/GamerCard.vue'

const sampleGamer = {
  puuid: 'test-puuid',
  userId: 23,
  gamerName: 'Doend',
  tagline: '9765',
  iconId: 0,
  iconUrl: 'https://ddragon.leagueoflegends.com/cdn/15.24.1/img/profileicon/0.png',
  level: 30,
  stats: {
    totalMatches: 8,
    wins: 5,
    totalKills: 46,
    totalDeaths: 57,
    totalAssists: 76,
  },
  lastChecked: '2025-12-16T23:14:07.607'
}

describe('GamerCard', () => {
  it('renders icon, level, and name with tagline', () => {
    const wrapper = mount(GamerCard, { props: { gamer: sampleGamer } })

    // Icon image
    const img = wrapper.get('img.icon')
    expect(img.attributes('src')).toBe(sampleGamer.iconUrl)
    expect(img.attributes('alt')).toContain(sampleGamer.gamerName)

    // Level
    expect(wrapper.text()).toContain(`Level ${sampleGamer.level}`)

    // Name and tagline
    expect(wrapper.text()).toContain(sampleGamer.gamerName)
    expect(wrapper.text()).toContain(`#${sampleGamer.tagline}`)
  })

  it('shows winrate label and wins/losses aria on chart', () => {
    const wrapper = mount(GamerCard, { props: { gamer: sampleGamer } })

    const label = wrapper.get('.chart-label')
    // 5/8 = 62.5% → rounds to 63%
    expect(label.text()).toBe('63%')

    const chart = wrapper.get('.chart')
    expect(chart.attributes('role')).toBe('img')
    expect(chart.attributes('aria-label')).toBe('Wins 5, Losses 3')
  })

  it('uses a zero dasharray when there are no games', () => {
    const empty = { ...sampleGamer, stats: { totalMatches: 0, wins: 0 } }
    const wrapper = mount(GamerCard, { props: { gamer: empty } })
    const circles = wrapper.findAll('svg circle')
    // Second circle is the wins arc
    const winsArc = circles[1]
    expect(winsArc.exists()).toBe(true)
    const dash = winsArc.attributes('stroke-dasharray')
    expect(dash?.startsWith('0 ')).toBe(true)
  })

  it('renders games/wins/losses summary and average KDA', () => {
    const wrapper = mount(GamerCard, { props: { gamer: sampleGamer } })
    const summary = wrapper.get('.game-info')
    expect(summary.text()).toBe('8G 5W 3L')

    const kda = wrapper.get('.kda')
    expect(kda.text()).toBe('5.8/7.1/9.5')
  })
})
