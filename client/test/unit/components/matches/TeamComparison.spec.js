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

  it('falls back to a plain title and hides the bar without damage data', () => {
    const wrapper = mountTeam({ teamTotalDamage: 0, enemyTeamTotalDamage: 0 })
    expect(wrapper.get('[data-testid="team-title"]').text()).toBe('Team summary')
    expect(wrapper.find('[data-testid="team-damage"]').exists()).toBe(false)
  })

  it('gives the damage bar a text alternative', () => {
    const bar = mountTeam().get('[role="img"]')
    expect(bar.attributes('aria-label')).toBe('Damage: your team 60%, enemy team 40%')
  })

  it('writes out the gold lead at 15 for either side', () => {
    expect(mountTeam().get('[data-testid="team-gold-lead"]').text()).toBe('Your team led by 1,250 gold at 15 minutes.')
    expect(mountTeam({ teamGoldLeadAt15: -800 }).get('[data-testid="team-gold-lead"]').text())
      .toBe('The enemy team led by 800 gold at 15 minutes.')
    expect(mountTeam({ teamGoldLeadAt15: 0 }).get('[data-testid="team-gold-lead"]').text()).toBe('Gold was even at 15 minutes.')
    expect(mountTeam({ teamGoldLeadAt15: null }).get('[data-testid="team-gold-lead"]').text())
      .toBe('No gold lead recorded at 15 minutes.')
  })

  it('lists objectives for both teams in a table and marks the leader', () => {
    const wrapper = mountTeam()
    const dragons = wrapper.get('[data-testid="team-objective-dragons"]').findAll('td')
    expect(dragons.map((cell) => cell.text())).toEqual(['3', '1'])
    expect(dragons[0].classes()).toContain('team__cell--lead')
    expect(dragons[1].classes()).not.toContain('team__cell--lead')

    const towers = wrapper.get('[data-testid="team-objective-towers"]').findAll('td')
    expect(towers.map((cell) => cell.text())).toEqual(['8', '3'])
    expect(wrapper.get('[data-testid="team-objective-barons"]').findAll('td').map((c) => c.text())).toEqual(['1', '0'])
  })
})
