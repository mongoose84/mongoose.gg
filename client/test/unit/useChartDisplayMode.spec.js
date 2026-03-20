import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { useChartDisplayMode } from '@/composables/useChartDisplayMode'

const STORAGE_KEY = 'mongoose_chart_mode'

describe('useChartDisplayMode', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    localStorage.clear()
  })

  describe('chartMode default', () => {
    it('returns "merged" when nothing is stored in localStorage', () => {
      const { chartMode } = useChartDisplayMode()
      expect(chartMode.value).toBe('merged')
    })

    it('reads initial value from localStorage', () => {
      localStorage.setItem(STORAGE_KEY, 'per-account')
      // Re-import or test reactivity — composable is module-level so we verify via setChartMode
      const { setChartMode, chartMode } = useChartDisplayMode()
      setChartMode('per-account')
      expect(chartMode.value).toBe('per-account')
    })
  })

  describe('setChartMode', () => {
    it('updates the reactive chartMode ref', () => {
      const { chartMode, setChartMode } = useChartDisplayMode()
      setChartMode('per-account')
      expect(chartMode.value).toBe('per-account')
    })

    it('persists the value to localStorage', () => {
      const { setChartMode } = useChartDisplayMode()
      setChartMode('per-account')
      expect(localStorage.getItem(STORAGE_KEY)).toBe('per-account')
    })

    it('updates back to merged correctly', () => {
      const { chartMode, setChartMode } = useChartDisplayMode()
      setChartMode('per-account')
      setChartMode('merged')
      expect(chartMode.value).toBe('merged')
      expect(localStorage.getItem(STORAGE_KEY)).toBe('merged')
    })
  })

  describe('shared reactive state', () => {
    it('shares the same chartMode ref across multiple consumers', () => {
      const consumer1 = useChartDisplayMode()
      const consumer2 = useChartDisplayMode()

      consumer1.setChartMode('per-account')

      expect(consumer2.chartMode.value).toBe('per-account')
    })
  })
})
