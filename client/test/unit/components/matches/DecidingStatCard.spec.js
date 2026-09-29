import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import DecidingStatCard from '@/components/matches/DecidingStatCard.vue'

function mountCard(decidingStat, role = 'MIDDLE') {
  return mount(DecidingStatCard, { props: { decidingStat, role } })
}

const fullDecidingStat = {
  outcome: 'strength',
  stat: 'goldLeadAt10',
  meters: [
    { stat: 'goldLeadAt10', value: 1240, usual: 180, score: 2.4 },
    { stat: 'killParticipation', value: 71, usual: 58, score: 1.3 },
    { stat: 'csAt10', value: 84, usual: 71, score: 1.2 }
  ],
  fix: { stat: 'visionPerMin', value: 0.6, usual: 0.9, score: -0.8 },
  usualMatches: 20
}

describe('DecidingStatCard.vue', () => {
  it('renders the finding, the meters, the usual note and the fix', () => {
    const wrapper = mountCard(fullDecidingStat)

    expect(wrapper.get('[data-testid="deciding-stat-card"]').exists()).toBe(true)
    expect(wrapper.get('#deciding-stat-finding').text()).toBe('You won your lane by 1,240 gold at 10')
    expect(wrapper.get('[data-testid="deciding-stat-card"]').attributes('aria-labelledby')).toBe('deciding-stat-finding')

    expect(wrapper.find('[data-testid="deciding-stat-meter-goldLeadAt10"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="deciding-stat-meter-killParticipation"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="deciding-stat-meter-csAt10"]').exists()).toBe(true)

    expect(wrapper.text()).toContain('Your usual · last 20 matches as Mid')

    const fix = wrapper.get('[data-testid="deciding-stat-fix"]')
    expect(fix.text()).toContain('Next match:')
    expect(fix.text()).toContain('Buy a control ward on your first back.')
  })

  it('renders 1–3 meters depending on how many are eligible', () => {
    const wrapper = mountCard({ ...fullDecidingStat, meters: fullDecidingStat.meters.slice(0, 1) })
    expect(wrapper.findAll('[role="meter"]')).toHaveLength(1)
  })

  it('caps at three meters', () => {
    const wrapper = mountCard(fullDecidingStat)
    expect(wrapper.findAll('[role="meter"]')).toHaveLength(3)
  })

  it('renders no fix row when fix is null', () => {
    const wrapper = mountCard({ ...fullDecidingStat, fix: null })
    expect(wrapper.find('[data-testid="deciding-stat-fix"]').exists()).toBe(false)
  })

  it('shows the "none" copy when nothing stood out', () => {
    const wrapper = mountCard({
      outcome: 'none',
      stat: null,
      meters: fullDecidingStat.meters,
      fix: null,
      usualMatches: 20
    })
    expect(wrapper.get('#deciding-stat-finding').text()).toBe('A match like your usual: nothing stood out')
  })

  it('picks the Jungle CS fix variant for a Jungle role', () => {
    const wrapper = mountCard(
      { ...fullDecidingStat, fix: { stat: 'csAt10', value: 60, usual: 71, score: -0.7 } },
      'JUNGLE'
    )
    expect(wrapper.get('[data-testid="deciding-stat-fix"]').text()).toContain('Finish your full first clear before the first gank.')
  })

  it('picks the Support vision fix variant for the Support role', () => {
    const wrapper = mountCard(fullDecidingStat, 'UTILITY')
    expect(wrapper.get('[data-testid="deciding-stat-fix"]').text()).toContain('Place a control ward before every dragon.')
  })
})
