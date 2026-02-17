import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import DangerZonesMap from '@/components/solo/DangerZonesMap.vue'

// Mock simpleheat
vi.mock('simpleheat', () => ({
  default: () => ({
    radius: vi.fn().mockReturnThis(),
    max: vi.fn().mockReturnThis(),
    gradient: vi.fn().mockReturnThis(),
    data: vi.fn().mockReturnThis(),
    draw: vi.fn()
  })
}))

describe('DangerZonesMap', () => {
  const mockDeaths = [
    { x: 7500, y: 7500, minuteMark: 5, phase: 'early', killerChampionId: 1, assistCount: 2 },
    { x: 3000, y: 8000, minuteMark: 8, phase: 'early', killerChampionId: 2, assistCount: 1 },
    { x: 12000, y: 4000, minuteMark: 15, phase: 'mid', killerChampionId: 3, assistCount: 3 },
    { x: 9000, y: 10000, minuteMark: 25, phase: 'late', killerChampionId: 4, assistCount: 0 },
    { x: 6000, y: 5000, minuteMark: 35, phase: 'veryLate', killerChampionId: 5, assistCount: 2 }
  ]

  const mockPhaseSummary = {
    early: 2,
    mid: 1,
    late: 1,
    veryLate: 1
  }

  const mountComponent = (props = {}) => {
    return mount(DangerZonesMap, {
      props: {
        deaths: [],
        totalDeaths: 0,
        matchesAnalyzed: 0,
        phaseSummary: { early: 0, mid: 0, late: 0, veryLate: 0 },
        loading: false,
        error: null,
        queueType: 'all',
        timeRange: null,
        ...props
      }
    })
  }

  describe('Rendering States', () => {
    it('renders component with data-testid', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="danger-zones-map"]').exists()).toBe(true)
    })

    it('shows loading state when loading is true', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)
      expect(wrapper.find('.skeleton-map').exists()).toBe(true)
    })

    it('shows error state when error is provided', () => {
      const wrapper = mountComponent({ error: 'Failed to load data' })
      expect(wrapper.find('[data-testid="error-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('Failed to load data')
    })

    it('shows empty state when no deaths data', () => {
      const wrapper = mountComponent({ deaths: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No death position data available')
      expect(wrapper.text()).toContain('Play more matches')
    })

    it('renders map content when data is provided', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        matchesAnalyzed: 10,
        phaseSummary: mockPhaseSummary
      })

      expect(wrapper.find('[data-testid="map-container"]').exists()).toBe(true)
      expect(wrapper.find('.minimap-image').exists()).toBe(true)
      expect(wrapper.find('[data-testid="heat-overlay"]').exists()).toBe(true)
    })
  })

  describe('Side Filters', () => {
    it('renders three side filter buttons', () => {
      const wrapper = mountComponent()
      const buttons = wrapper.findAll('[data-testid^="side-filter-"]')
      expect(buttons).toHaveLength(3)
      expect(wrapper.find('[data-testid="side-filter-all"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="side-filter-blue"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="side-filter-red"]').exists()).toBe(true)
    })

    it('marks "all" side filter as active by default', () => {
      const wrapper = mountComponent()
      const allButton = wrapper.find('[data-testid="side-filter-all"]')
      expect(allButton.classes()).toContain('active')
    })

    it('emits update:side event when side button clicked', async () => {
      const wrapper = mountComponent()
      const blueButton = wrapper.find('[data-testid="side-filter-blue"]')
      await blueButton.trigger('click')

      expect(wrapper.emitted('update:side')).toBeTruthy()
      expect(wrapper.emitted('update:side')[0]).toEqual(['blue'])
    })

    it('does not emit if same side is clicked again', async () => {
      const wrapper = mountComponent()
      const allButton = wrapper.find('[data-testid="side-filter-all"]')
      await allButton.trigger('click')

      expect(wrapper.emitted('update:side')).toBeFalsy()
    })

    it('updates active class when side changes', async () => {
      const wrapper = mountComponent()
      const blueButton = wrapper.find('[data-testid="side-filter-blue"]')
      await blueButton.trigger('click')
      await nextTick()

      expect(blueButton.classes()).toContain('active')
      expect(wrapper.find('[data-testid="side-filter-all"]').classes()).not.toContain('active')
    })
  })

  describe('Phase Filters', () => {
    it('renders five phase filter buttons', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        phaseSummary: mockPhaseSummary
      })

      const buttons = wrapper.findAll('[data-testid^="phase-filter-"]')
      expect(buttons).toHaveLength(5)
      expect(wrapper.find('[data-testid="phase-filter-all"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="phase-filter-early"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="phase-filter-mid"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="phase-filter-late"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="phase-filter-veryLate"]').exists()).toBe(true)
    })

    it('marks "all" phase filter as active by default', () => {
      const wrapper = mountComponent({ deaths: mockDeaths })
      const allButton = wrapper.find('[data-testid="phase-filter-all"]')
      expect(allButton.classes()).toContain('active')
    })

    it('shows death counts in phase buttons', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        phaseSummary: mockPhaseSummary
      })

      const allButton = wrapper.find('[data-testid="phase-filter-all"]')
      expect(allButton.text()).toContain('(5)')

      const earlyButton = wrapper.find('[data-testid="phase-filter-early"]')
      expect(earlyButton.text()).toContain('(2)')
    })

    it('filters deaths client-side when phase selected', async () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        phaseSummary: mockPhaseSummary
      })

      const earlyButton = wrapper.find('[data-testid="phase-filter-early"]')
      await earlyButton.trigger('click')
      await nextTick()

      // Component should filter to only early deaths (2 deaths)
      // We can't directly test the internal state, but we can verify the button is active
      expect(earlyButton.classes()).toContain('active')
      expect(wrapper.find('[data-testid="phase-filter-all"]').classes()).not.toContain('active')
    })

    it('does not emit any event when phase filter changes (client-side only)', async () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        phaseSummary: mockPhaseSummary
      })

      const midButton = wrapper.find('[data-testid="phase-filter-mid"]')
      await midButton.trigger('click')

      // Should NOT emit update:side (phase filter is client-side only)
      expect(wrapper.emitted('update:side')).toBeFalsy()
    })
  })

  describe('Phase Summary Bar', () => {
    it('renders phase summary bar with segments', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        phaseSummary: mockPhaseSummary
      })

      const bar = wrapper.find('.phase-bar')
      expect(bar.exists()).toBe(true)

      const segments = wrapper.findAll('.phase-segment')
      expect(segments.length).toBeGreaterThan(0)
    })

    it('displays phase labels with counts', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        phaseSummary: mockPhaseSummary
      })

      const labels = wrapper.find('.phase-labels')
      expect(labels.exists()).toBe(true)
      expect(labels.text()).toContain('Early: 2')
      expect(labels.text()).toContain('Mid: 1')
      expect(labels.text()).toContain('Late: 1')
      expect(labels.text()).toContain('Very Late: 1')
    })

    it('calculates correct width percentages for segments', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        phaseSummary: mockPhaseSummary
      })

      const segments = wrapper.findAll('.phase-segment')
      // Early: 2/5 = 40%, Mid: 1/5 = 20%, Late: 1/5 = 20%, VeryLate: 1/5 = 20%
      expect(segments[0].attributes('style')).toContain('40%')
      expect(segments[1].attributes('style')).toContain('20%')
    })
  })

  describe('Context Text', () => {
    it('displays total deaths and matches analyzed', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        matchesAnalyzed: 10
      })

      const contextText = wrapper.find('.context-text')
      expect(contextText.exists()).toBe(true)
      expect(contextText.text()).toContain('5 deaths')
      expect(contextText.text()).toContain('10 matches')
    })
  })

  describe('Phase Filter Reset on Side Change', () => {
    it('resets phase filter to "all" when side changes', async () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        phaseSummary: mockPhaseSummary
      })

      // Select a specific phase
      const earlyButton = wrapper.find('[data-testid="phase-filter-early"]')
      await earlyButton.trigger('click')
      await nextTick()

      expect(earlyButton.classes()).toContain('active')

      // Change side filter
      const blueButton = wrapper.find('[data-testid="side-filter-blue"]')
      await blueButton.trigger('click')
      await nextTick()

      // Phase filter should reset to "all"
      const allPhaseButton = wrapper.find('[data-testid="phase-filter-all"]')
      expect(allPhaseButton.classes()).toContain('active')
      expect(earlyButton.classes()).not.toContain('active')
    })
  })

  describe('Coordinate Normalization', () => {
    it('normalizes riot coordinates to canvas coordinates', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5
      })

      const instance = wrapper.vm

      // Center of map: (7500, 7500) should map to center of canvas
      const coords = instance.riotToCanvas(7500, 7500, 512, 512)
      expect(coords.x).toBeCloseTo(256, 0)
      expect(coords.y).toBeCloseTo(256, 0)

      // Bottom-left in Riot: (0, 0) should map to bottom-left in canvas (0, 512)
      const bottomLeft = instance.riotToCanvas(0, 0, 512, 512)
      expect(bottomLeft.x).toBeCloseTo(0, 0)
      expect(bottomLeft.y).toBeCloseTo(512, 0)

      // Top-right in Riot: (15000, 15000) should map to top-right in canvas (512, 0)
      const topRight = instance.riotToCanvas(15000, 15000, 512, 512)
      expect(topRight.x).toBeCloseTo(512, 0)
      expect(topRight.y).toBeCloseTo(0, 0)
    })
  })

  describe('Map Image Loading', () => {
    it('sets canvas size when map image loads', async () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5
      })

      const img = wrapper.find('.minimap-image')
      const canvas = wrapper.find('[data-testid="heat-overlay"]')

      // Mock image dimensions
      Object.defineProperty(img.element, 'clientWidth', { value: 512, writable: false })
      Object.defineProperty(img.element, 'clientHeight', { value: 512, writable: false })

      await img.trigger('load')
      await nextTick()

      expect(canvas.element.width).toBe(512)
      expect(canvas.element.height).toBe(512)
    })
  })

  describe('Props Validation', () => {
    it('accepts all required props', () => {
      const wrapper = mountComponent({
        deaths: mockDeaths,
        totalDeaths: 5,
        matchesAnalyzed: 10,
        phaseSummary: mockPhaseSummary,
        loading: false,
        error: null,
        queueType: 'ranked_solo',
        timeRange: '1m'
      })

      expect(wrapper.props('deaths')).toEqual(mockDeaths)
      expect(wrapper.props('totalDeaths')).toBe(5)
      expect(wrapper.props('matchesAnalyzed')).toBe(10)
      expect(wrapper.props('phaseSummary')).toEqual(mockPhaseSummary)
      expect(wrapper.props('loading')).toBe(false)
      expect(wrapper.props('error')).toBeNull()
      expect(wrapper.props('queueType')).toBe('ranked_solo')
      expect(wrapper.props('timeRange')).toBe('1m')
    })

    it('uses default prop values', () => {
      const wrapper = mount(DangerZonesMap)

      expect(wrapper.props('deaths')).toEqual([])
      expect(wrapper.props('totalDeaths')).toBe(0)
      expect(wrapper.props('matchesAnalyzed')).toBe(0)
      expect(wrapper.props('phaseSummary')).toEqual({ early: 0, mid: 0, late: 0, veryLate: 0 })
      expect(wrapper.props('loading')).toBe(false)
      expect(wrapper.props('error')).toBeNull()
      expect(wrapper.props('queueType')).toBe('all')
      expect(wrapper.props('timeRange')).toBeNull()
    })
  })
})
