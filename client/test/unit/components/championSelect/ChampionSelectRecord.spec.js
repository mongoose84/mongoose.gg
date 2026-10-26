import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChampionSelectRecord from '@/components/championSelect/ChampionSelectRecord.vue'

const records = [
  { championId: 222, championName: 'Jinx', matches: 29, wins: 15, net: 1, recent: null },
  { championId: 18, championName: 'Tristana', matches: 32, wins: 16, net: 0, recent: { matches: 13, wins: 5 } },
  { championId: 523, championName: 'Aphelios', matches: 27, wins: 11, net: -5, recent: { matches: 4, wins: 2 } }
]

function mountRecord(props = {}) {
  return mount(ChampionSelectRecord, {
    props: { records, leftOut: ['Ezreal'], role: 'BOTTOM', queueLabel: 'Flex', timeLabel: 'This season', ...props }
  })
}

describe('ChampionSelectRecord', () => {
  it('titles the record by the clear margin and captions the filters', () => {
    const wrapper = mountRecord()

    expect(wrapper.get('[data-testid="champion-select-record-title"]').text()).toBe('Aphelios costs you the most ADC matches')
    expect(wrapper.text()).toContain('Wins minus losses, Flex · This season')
  })

  it('shows one row per champion with matches, win rate and net wins', () => {
    const row = mountRecord().get('[data-testid="champion-record-523"]')

    expect(row.text()).toContain('Aphelios')
    expect(row.text()).toContain('27 matches · 41% win rate')
    expect(row.get('[data-testid="champion-record-value"]').text()).toBe('−5')
    expect(row.get('[data-testid="champion-record-value"]').classes()).toContain('mp-down')
  })

  it('shows recent form from 3 matches and marks a cold run in words', () => {
    const wrapper = mountRecord()
    const cold = wrapper.get('[data-testid="champion-record-form-18"]')

    expect(cold.text()).toBe('Last 20: 5–8 ▼ Cold')
    expect(cold.classes()).toContain('record__form--cold')
    expect(wrapper.get('[data-testid="champion-record-form-523"]').text()).toBe('Last 20: 2–2')
    expect(wrapper.find('[data-testid="champion-record-form-222"]').exists()).toBe(false)
  })

  it('names the champions left out', () => {
    expect(mountRecord().get('[data-testid="champion-select-record-left-out"]').text())
      .toBe('Left out with fewer than 3 matches: Ezreal.')
  })

  it('asks for 3 matches when the role has no record yet', () => {
    const wrapper = mountRecord({ records: [], leftOut: [] })

    expect(wrapper.get('[data-testid="champion-select-record-empty"]').text())
      .toBe('Play 3 matches on a champion in this role to see its record here.')
    expect(wrapper.find('[data-testid="champion-select-record-left-out"]').exists()).toBe(false)
  })
})
