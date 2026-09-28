import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import TeamComparison from '@/components/matches/TeamComparison.vue'

const baseMatch = {
  teamTotalDamage: 60000,
  enemyTeamTotalDamage: 40000,
  teamGoldLeadAt15: 1250,
  teamDragons: 3,
  enemyTeamDragons: 1,
  teamBarons: 1,
  enemyTeamBarons: 0,
  teamTowers: 8,
  enemyTeamTowers: 3
}

function mountTeam(overrides = {}) {
  return mount(TeamComparison, { props: { match: { ...baseMatch, ...overrides } } })
}

describe('TeamComparison', () => {
  it('states the damage share as the title', () => {
    expect(mountTeam().get('[data-testid="team-title"]').text()).toBe('Your team dealt 60% of the damage')
  })

  it('falls back to a plain title and leaves the damage bar out without damage data', () => {
    const wrapper = mountTeam({ teamTotalDamage: 0, enemyTeamTotalDamage: 0 })
    expect(wrapper.get('[data-testid="team-title"]').text()).toBe('Your team vs theirs')
    expect(wrapper.find('[data-testid="team-split-damage"]').exists()).toBe(false)
  })

  it('draws damage as a split bar with compact totals and a text alternative', () => {
    const damage = mountTeam().get('[data-testid="team-split-damage"]')
    expect(damage.get('[data-testid="team-split-ally"]').text()).toBe('60.0k')
    expect(damage.get('[data-testid="team-split-enemy"]').text()).toBe('40.0k')
    const bar = damage.get('[data-testid="team-split-bar"]')
    expect(bar.classes()).toContain('mp-split')
    expect(bar.attributes('role')).toBe('img')
    expect(bar.attributes('aria-label')).toBe('Damage: your team 60%, enemy team 40%')
    expect(bar.findAll('span')[0].attributes('style')).toContain('width: 60%')
  })

  it('writes out the gold lead at 15 for either side', () => {
    expect(mountTeam().get('[data-testid="team-gold-lead"]').text()).toBe('Your team led by 1,250 gold at 15 minutes.')
    expect(mountTeam({ teamGoldLeadAt15: -800 }).get('[data-testid="team-gold-lead"]').text())
      .toBe('The enemy team led by 800 gold at 15 minutes.')
    expect(mountTeam({ teamGoldLeadAt15: 0 }).get('[data-testid="team-gold-lead"]').text()).toBe('Gold was even at 15 minutes.')
    expect(mountTeam({ teamGoldLeadAt15: null }).get('[data-testid="team-gold-lead"]').text())
      .toBe('No gold lead recorded at 15 minutes.')
  })

  it('draws dragons, barons and towers as split bars and marks the leader', () => {
    const wrapper = mountTeam()
    const dragons = wrapper.get('[data-testid="team-split-dragons"]')
    expect(dragons.get('[data-testid="team-split-ally"]').text()).toBe('3')
    expect(dragons.get('[data-testid="team-split-enemy"]').text()).toBe('1')
    expect(dragons.get('[data-testid="team-split-ally"]').classes()).toContain('team__number--lead')
    expect(dragons.get('[data-testid="team-split-enemy"]').classes()).not.toContain('team__number--lead')
    const dragonBar = dragons.get('[data-testid="team-split-bar"]')
    expect(dragonBar.attributes('aria-label')).toBe('Dragons: your team 3, enemy team 1')
    expect(dragonBar.findAll('span')[0].attributes('style')).toContain('width: 75%')

    const towers = wrapper.get('[data-testid="team-split-towers"]')
    expect(towers.get('[data-testid="team-split-ally"]').text()).toBe('8')
    expect(towers.get('[data-testid="team-split-enemy"]').text()).toBe('3')
    expect(wrapper.get('[data-testid="team-split-barons"]').get('[data-testid="team-split-ally"]').text()).toBe('1')
  })

  it('shows an empty track, not a made-up split, when neither team took an objective', () => {
    const barons = mountTeam({ teamBarons: 0, enemyTeamBarons: 0 }).get('[data-testid="team-split-barons"]')
    const bar = barons.get('[data-testid="team-split-bar"]')
    expect(bar.classes()).not.toContain('mp-split')
    expect(bar.attributes('aria-label')).toBe('Barons: none taken')
  })
})
