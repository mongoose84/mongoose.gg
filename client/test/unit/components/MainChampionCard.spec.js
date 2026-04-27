import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import MainChampionCard from '@/components/MainChampionCard.vue'
import { headlessUIStubs } from '../../helpers/testUtils'

vi.mock('@/utils/leagueAssets', () => ({
  getChampionIconUrl: (name) => `https://cdn.example.com/icon/${name}.png`
}))

vi.mock('@/composables/useWinRateColor', () => ({
  getWinRateColorClass: () => 'text-success'
}))

vi.mock('@/utils/formatters', () => ({
  formatRoleWithAdc: (role) => role,
  formatWinRate: (wr) => `${Math.round(wr)}%`
}))

vi.mock('@/services/soloApi', () => ({
  getChampionMatchups: vi.fn().mockResolvedValue({ matchups: [] })
}))

const tabStubs = {
  TabGroup: {
    template: '<div><slot :selected-index="0" /></div>',
    props: ['selectedIndex'],
    emits: ['change']
  },
  TabList: { template: '<div><slot /></div>' },
  Tab: { template: '<div><slot :selected="true" /></div>' },
  TabPanels: { template: '<div><slot /></div>' },
  TabPanel: { template: '<div><slot /></div>' }
}

const makeChampion = (overrides = {}) => ({
  championId: 103,
  championName: 'Ahri',
  role: 'MID',
  winRate: 58.3,
  gamesPlayed: 34,
  mScore: 74.0,
  avgKda: 3.21,
  avgCsPerMin: 7.4,
  ...overrides
})

const makeProps = (champions = [makeChampion()]) => ({
  mainChampions: [{ role: 'MID', champions }],
  userId: 1,
  queueType: 'ranked_solo',
  timeRange: '3m'
})

function createWrapper(props) {
  return mount(MainChampionCard, {
    props,
    global: {
      stubs: {
        ...headlessUIStubs,
        ...tabStubs
      }
    }
  })
}

describe('MainChampionCard', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders games played pill with correct text', async () => {
    const wrapper = createWrapper(makeProps())
    await flushPromises()

    const pill = wrapper.find('[data-testid="games-pill-103"]')
    expect(pill.exists()).toBe(true)
    expect(pill.text()).toBe('34g')
  })

  it('renders games pill with correct data-testid for each champion', async () => {
    const champs = [
      makeChampion({ championId: 103, championName: 'Ahri', gamesPlayed: 34 }),
      makeChampion({ championId: 75, championName: 'Nasus', gamesPlayed: 12 }),
      makeChampion({ championId: 99, championName: 'Lux', gamesPlayed: 7 })
    ]
    const wrapper = createWrapper(makeProps(champs))
    await flushPromises()

    expect(wrapper.find('[data-testid="games-pill-103"]').text()).toBe('34g')
    expect(wrapper.find('[data-testid="games-pill-75"]').text()).toBe('12g')
    expect(wrapper.find('[data-testid="games-pill-99"]').text()).toBe('7g')
  })

  it('renders CS stat row', async () => {
    const wrapper = createWrapper(makeProps())
    await flushPromises()

    expect(wrapper.text()).toContain('CS/m')
  })

  it('does not render KDA stat row', async () => {
    const wrapper = createWrapper(makeProps())
    await flushPromises()

    const statLabels = wrapper.findAll('.stat-label').map((el) => el.text())
    expect(statLabels).not.toContain('KDA')
  })

  it('does not render Games stat row', async () => {
    const wrapper = createWrapper(makeProps())
    await flushPromises()

    const statLabels = wrapper.findAll('.stat-label').map((el) => el.text())
    expect(statLabels).not.toContain('Games')
  })

  it('CS bar width reflects avgCsPerMin / 10 * 100 capped at 100', async () => {
    const wrapper = createWrapper(makeProps([makeChampion({ avgCsPerMin: 7.4 })]))
    await flushPromises()

    const expectedWidth = `${Math.min((7.4 / 10) * 100, 100)}%`
    const csRow = wrapper.findAll('.stat-row').find((row) => row.text().includes('CS/m'))
    expect(csRow).toBeDefined()
    const bar = csRow.find('.stat-bar')
    expect(bar.attributes('style')).toContain(`width: ${expectedWidth}`)
  })

  it('CS bar is capped at 100% for very high avgCsPerMin', async () => {
    const wrapper = createWrapper(makeProps([makeChampion({ avgCsPerMin: 15 })]))
    await flushPromises()

    const csRow = wrapper.findAll('.stat-row').find((row) => row.text().includes('CS/m'))
    const bar = csRow.find('.stat-bar')
    expect(bar.attributes('style')).toContain('width: 100%')
  })

  it('CS displays — when avgCsPerMin is null', async () => {
    const wrapper = createWrapper(makeProps([makeChampion({ avgCsPerMin: null })]))
    await flushPromises()

    const csRow = wrapper.findAll('.stat-row').find((row) => row.text().includes('CS/m'))
    expect(csRow.find('.stat-value').text()).toBe('—')
  })

  it('CS value shows 1 decimal when avgCsPerMin is present', async () => {
    const wrapper = createWrapper(makeProps([makeChampion({ avgCsPerMin: 7.4 })]))
    await flushPromises()

    const csRow = wrapper.findAll('.stat-row').find((row) => row.text().includes('CS/m'))
    expect(csRow.find('.stat-value').text()).toBe('7.4')
  })
})
