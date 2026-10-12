import { describe, it, expect } from 'vitest'
import {
  buildGoldChart,
  buildGoldSubtitle,
  buildGoldTitle,
  buildSwingCaption,
  describeGoldTimeline,
  describeLeads,
  describeMoments
} from '@/utils/goldTimeline'

function timeline(leads, extra = {}) {
  return {
    source: 'minute',
    points: leads.map((lead, minute) => ({ minute, lead })),
    moments: [],
    swing: null,
    ...extra
  }
}

describe('goldTimeline', () => {
  describe('buildGoldTitle', () => {
    it('names a comeback from a decisive deficit', () => {
      expect(buildGoldTitle(timeline([0, -1200, -2600, -900, 1500]), true)).toBe('Came back from −2,600 at 2 min')
    })

    it('names a decisive lead that slipped away', () => {
      expect(buildGoldTitle(timeline([0, 1400, 3100, 200, -1800]), false)).toBe('Lost a +3,100 lead from 2 min')
    })

    it('says the team was ahead all match, with the peak', () => {
      expect(buildGoldTitle(timeline([0, 300, -400, 1800, 4200]), true)).toBe('Ahead all match, up to +4,200')
    })

    it('says the team was behind all match, with the low', () => {
      expect(buildGoldTitle(timeline([0, -300, 400, -1800, -5200]), false)).toBe('Behind all match, down to −5,200')
    })

    it('calls a match that never left the even band even', () => {
      expect(buildGoldTitle(timeline([0, 400, -600, 900, -300]), true)).toBe('Even gold all match')
    })

    it('falls back to where the lead ended', () => {
      // Swung both ways past 1k without a decisive lead changing hands
      expect(buildGoldTitle(timeline([0, 1500, -1500, 1200, 1600]), false)).toBe('Ended +1,600 gold')
    })
  })

  it('subtitles the metric and how often it was measured', () => {
    expect(buildGoldSubtitle(timeline([0, 0, 0]))).toBe('Your team\'s gold lead, minute by minute')
    expect(buildGoldSubtitle(timeline([0, 0, 0], { source: 'checkpoints' }))).toBe('Your team\'s gold lead, every 5 minutes')
  })

  it('describes the range, the end, the high and the low', () => {
    expect(describeGoldTimeline(timeline([0, 1500, -700, 300]))).toBe(
      'Your team\'s gold lead over 3 minutes: ended +300, highest +1,500 at 1 min, lowest −700 at 2 min.'
    )
  })

  describe('buildGoldChart', () => {
    it('plots the lead by minute around an "Even" guide', () => {
      const chart = buildGoldChart(timeline([0, 200, -400]))

      expect(chart.points).toEqual([{ x: 0, y: 0 }, { x: 1, y: 200 }, { x: 2, y: -400 }])
      expect(chart.length).toBe(3)
      expect(chart.guides).toEqual([{ y: 0, label: 'Even' }])
      expect(chart.markers).toEqual([])
    })

    it('marks the swing in warn with its signed change', () => {
      const down = buildGoldChart(timeline([0, 2000, -500], { swing: { fromMinute: 1, minute: 2, change: -2500 } }))
      const up = buildGoldChart(timeline([0, 2000, 300], { swing: { fromMinute: 0, minute: 1, change: 2000 } }))

      expect(down.markers).toEqual([{ x: 2, y: -500, label: '▼ −2,500', tone: 'warn' }])
      expect(up.markers).toEqual([{ x: 1, y: 2000, label: '▲ +2,000', tone: 'warn' }])
    })

    it('turns objectives into lettered moments, ours positive and theirs warn, within the line', () => {
      const chart = buildGoldChart(timeline([0, 0, 0], {
        moments: [
          { timestampSec: 90, type: 'dragon', ours: true },
          { timestampSec: 100, type: 'tower', ours: false },
          { timestampSec: 150, type: 'baron', ours: true },
          { timestampSec: 60, type: 'grubs', ours: true }
        ]
      }))

      expect(chart.moments).toEqual([
        { x: 1.5, letter: 'D', tone: 'positive' },
        { x: 100 / 60, letter: 'T', tone: 'warn' },
        { x: 2, letter: 'B', tone: 'positive' }
      ])
    })
  })

  it('lists each objective with its time and side for screen readers', () => {
    expect(describeMoments(timeline([0, 0, 0], {
      moments: [{ timestampSec: 400, type: 'dragon', ours: true }, { timestampSec: 845, type: 'herald', ours: false }]
    }))).toEqual(['6:40 Your team took a dragon', '14:05 The enemy took a Herald'])
  })

  it('lists the lead every 5 minutes and at the end', () => {
    const leads = Array.from({ length: 13 }, (_, minute) => minute * 100)
    expect(describeLeads(timeline(leads))).toEqual(['5 min: +500', '10 min: +1,000', '12 min: +1,200'])
  })

  it('puts the swing in words, or nothing without one', () => {
    expect(buildSwingCaption(timeline([0, 0, 0], { swing: { fromMinute: 18, minute: 21, change: -3200 } })))
      .toBe('Biggest swing against you: −3,200 gold from 18 to 21 min')
    expect(buildSwingCaption(timeline([0, 0, 0], { swing: { fromMinute: 5, minute: 7, change: 1800 } })))
      .toBe('Biggest swing for you: +1,800 gold from 5 to 7 min')
    expect(buildSwingCaption(timeline([0, 0, 0]))).toBeNull()
  })
})
