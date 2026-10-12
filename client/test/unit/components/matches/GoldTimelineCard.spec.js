import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import GoldTimelineCard from '@/components/matches/GoldTimelineCard.vue'

const timeline = {
  source: 'minute',
  points: [
    { minute: 0, lead: 0 },
    { minute: 5, lead: 1200 },
    { minute: 10, lead: 3100 },
    { minute: 11, lead: 400 },
    { minute: 12, lead: -1800 },
    { minute: 13, lead: -1800 },
    { minute: 14, lead: -1800 },
    { minute: 15, lead: -1800 }
  ],
  moments: [
    { timestampSec: 400, type: 'dragon', ours: true },
    { timestampSec: 650, type: 'baron', ours: false }
  ],
  swing: { fromMinute: 10, minute: 12, change: -4900 }
}

function mountCard(props = {}) {
  return mount(GoldTimelineCard, { props: { timeline, win: false, ...props } })
}

describe('GoldTimelineCard.vue', () => {
  it('titles the takeaway with the metric as the subtitle', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="gold-timeline-card"]').attributes('aria-labelledby')).toBe('gold-timeline-title')
    expect(wrapper.get('[data-testid="gold-timeline-title"]').text()).toBe('Lost a +3,100 lead from 10 min')
    expect(wrapper.get('[data-testid="gold-timeline-subtitle"]').text()).toBe('Your team\'s gold lead, minute by minute')
  })

  it('draws the line with its text alternative, the swing point and the objective moments', () => {
    const wrapper = mountCard()
    const chart = wrapper.get('[data-testid="line-chart"]')

    expect(chart.attributes('aria-label')).toBe(
      'Your team\'s gold lead over 15 minutes: ended −1,800, highest +3,100 at 10 min, lowest −1,800 at 12 min.'
    )
    expect(wrapper.findAll('[data-testid="line-chart-marker"]')).toHaveLength(1)
    expect(wrapper.get('[data-testid="line-chart-marker-label"]').text()).toBe('▼ −4,900')
    expect(wrapper.findAll('[data-testid="line-chart-moment"]').map((m) => m.text())).toEqual(['D', 'B'])
  })

  it('keys the moments and puts the swing in words', () => {
    const wrapper = mountCard()

    expect(wrapper.get('[data-testid="gold-timeline-key"]').text()).toContain('D dragon · B Baron · H Herald · T tower')
    expect(wrapper.get('[data-testid="gold-timeline-swing"]').text()).toBe('Biggest swing against you: −4,900 gold from 10 to 12 min')
  })

  it('lists the leads and objectives for screen readers', () => {
    const text = mountCard().get('.visually-hidden').text()

    expect(text).toContain('5 min: +1,200')
    expect(text).toContain('15 min: −1,800')
    expect(text).toContain('6:40 Your team took a dragon')
    expect(text).toContain('10:50 The enemy took a Baron')
  })

  it('leaves out the key and the swing caption when there is nothing to show', () => {
    const wrapper = mountCard({ timeline: { ...timeline, moments: [], swing: null } })

    expect(wrapper.find('[data-testid="gold-timeline-key"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="gold-timeline-swing"]').exists()).toBe(false)
    expect(wrapper.find('[data-testid="line-chart-moments"]').exists()).toBe(false)
  })
})
