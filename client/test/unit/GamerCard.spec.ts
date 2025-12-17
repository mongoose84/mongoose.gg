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
  wins: 5,
  losses: 3,
  lastChecked: '2025-12-16T23:14:07.607'
}

describe('GamerCard', () => {
  it('renders icon, level, and name with tagline', () => {
    const wrapper = mount(GamerCard, { props: { gamer: sampleGamer } })

    // Icon image
    const img = wrapper.find('img.icon')
    expect(img.exists()).toBe(true)
    expect(img.attributes('src')).toBe(sampleGamer.iconUrl)
    expect(img.attributes('alt')).toContain(sampleGamer.gamerName)

    // Level
    expect(wrapper.text()).toContain(`Level ${sampleGamer.level}`)

    // Name and tagline
    expect(wrapper.text()).toContain(sampleGamer.gamerName)
    expect(wrapper.text()).toContain(`#${sampleGamer.tagline}`)
  })

  it('shows wins/losses label and aria on chart', () => {
    const wrapper = mount(GamerCard, { props: { gamer: sampleGamer } })

    const label = wrapper.find('.chart-label')
    expect(label.exists()).toBe(true)
    expect(label.text()).toBe(`${sampleGamer.wins} | ${sampleGamer.losses}`)

    const chart = wrapper.find('.chart')
    expect(chart.attributes('role')).toBe('img')
    expect(chart.attributes('aria-label')).toBe(`Wins ${sampleGamer.wins}, Losses ${sampleGamer.losses}`)
  })

  it('uses a zero dasharray when there are no games', () => {
    const empty = { ...sampleGamer, wins: 0, losses: 0 }
    const wrapper = mount(GamerCard, { props: { gamer: empty } })
    const circles = wrapper.findAll('svg circle')
    // Second circle is the wins arc
    const winsArc = circles[1]
    expect(winsArc.exists()).toBe(true)
    const dash = winsArc.attributes('stroke-dasharray')
    expect(dash?.startsWith('0 ')).toBe(true)
  })
})
