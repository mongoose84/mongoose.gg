import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import LaneMatchupDetails from '@/components/matches/LaneMatchupDetails.vue'

vi.mock('@/utils/formatters', () => ({
  formatGoldDiff: (n) => (n >= 0 ? `+${n}` : `${n}`),
  formatCsDiff: (n) => (n >= 0 ? `+${n}` : `${n}`),
  formatPercent: (n) => `${n != null ? n.toFixed(0) : 0}%`
}))

describe('LaneMatchupDetails.vue', () => {
  const allyParticipant = {
    championId: 1,
    championName: 'Ahri',
    championIconUrl: '/ahri.png',
    kills: 5,
    deaths: 2,
    assists: 4,
    goldDiffAt10: 400,
    csDiffAt10: 15,
    deathsPre10: 0,
    damageShare: 28,
    killParticipation: 50,
    visionScore: 20,
    isUserParticipant: true
  }

  const enemyParticipant = {
    championId: 2,
    championName: 'Zed',
    championIconUrl: '/zed.png',
    kills: 3,
    deaths: 5,
    assists: 2,
    goldDiffAt10: -400,
    csDiffAt10: -15,
    deathsPre10: 1,
    damageShare: 22,
    killParticipation: 35,
    visionScore: 15
  }

  const baseMatchup = {
    role: 'MIDDLE',
    laneWinner: 'ally',
    allyParticipant,
    enemyParticipant
  }

  const createWrapper = (matchupOverrides = {}) =>
    mount(LaneMatchupDetails, {
      props: { matchup: { ...baseMatchup, ...matchupOverrides } }
    })

  it('renders the lane matchup details container', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.lane-matchup-details').exists()).toBe(true)
  })

  it('renders Early Laning phase section', () => {
    const wrapper = createWrapper()
    expect(wrapper.text()).toContain('Early Laning')
  })

  it('renders Game Impact phase section', () => {
    const wrapper = createWrapper()
    expect(wrapper.text()).toContain('Game Impact')
  })

  it('renders an insight text', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('.insight-text').exists()).toBe(true)
    expect(wrapper.find('.insight-text').text().length).toBeGreaterThan(0)
  })

  it('shows Gold Diff stat label', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Gold Diff')
  })

  it('shows CS Diff stat label', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('CS Diff')
  })

  it('shows Deaths stat label', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Deaths')
  })

  it('shows Damage Share stat label', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Damage Share')
  })

  it('shows Kill Part. stat label', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Kill Part.')
  })

  it('shows Vision Score stat label', () => {
    const wrapper = createWrapper()
    const labels = wrapper.findAll('.stat-label').map(l => l.text())
    expect(labels).toContain('Vision Score')
  })

  describe('Gold diff sentiment', () => {
    it('applies positive class when ally gold diff >= 300', () => {
      const wrapper = createWrapper({
        allyParticipant: { ...allyParticipant, goldDiffAt10: 400 }
      })
      expect(wrapper.find('.stat-value.ally.positive').exists()).toBe(true)
    })

    it('applies negative class when ally gold diff <= -300', () => {
      const wrapper = createWrapper({
        allyParticipant: { ...allyParticipant, goldDiffAt10: -400 }
      })
      expect(wrapper.find('.stat-value.ally.negative').exists()).toBe(true)
    })

    it('applies neutral class when ally gold diff is within bounds', () => {
      const wrapper = createWrapper({
        allyParticipant: { ...allyParticipant, goldDiffAt10: 100 }
      })
      expect(wrapper.find('.stat-value.ally.neutral').exists()).toBe(true)
    })
  })

  describe('CS diff sentiment', () => {
    it('applies positive class when CS diff >= 10', () => {
      const wrapper = createWrapper({
        allyParticipant: { ...allyParticipant, csDiffAt10: 15 }
      })
      expect(wrapper.find('.stat-value.positive').exists()).toBe(true)
    })

    it('applies negative class when CS diff <= -10', () => {
      const wrapper = createWrapper({
        allyParticipant: { ...allyParticipant, csDiffAt10: -15 }
      })
      expect(wrapper.find('.stat-value.negative').exists()).toBe(true)
    })
  })

  describe('Death counts', () => {
    it('shows ally deaths pre-10 value', () => {
      const wrapper = createWrapper({
        allyParticipant: { ...allyParticipant, deathsPre10: 2 }
      })
      const allyStat = wrapper.find('.stat-value.ally')
      // The deaths row has ally and enemy side
      const deathRows = wrapper.findAll('.stat-row').filter(r =>
        r.find('.stat-label').text() === 'Deaths'
      )
      expect(deathRows[0].find('.stat-value.ally').text()).toBe('2')
    })

    it('shows enemy deaths pre-10 value', () => {
      const wrapper = createWrapper({
        enemyParticipant: { ...enemyParticipant, deathsPre10: 3 }
      })
      const deathRows = wrapper.findAll('.stat-row').filter(r =>
        r.find('.stat-label').text() === 'Deaths'
      )
      expect(deathRows[0].find('.stat-value.enemy').text()).toBe('3')
    })

    it('shows 0 when deathsPre10 is null', () => {
      const wrapper = createWrapper({
        allyParticipant: { ...allyParticipant, deathsPre10: null }
      })
      const deathRows = wrapper.findAll('.stat-row').filter(r =>
        r.find('.stat-label').text() === 'Deaths'
      )
      expect(deathRows[0].find('.stat-value.ally').text()).toBe('0')
    })
  })
})
