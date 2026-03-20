import { ref } from 'vue'

const CHART_MODE_KEY = 'mongoose_chart_mode'

// Module-level ref so all consumers share the same reactive state
const chartMode = ref(localStorage.getItem(CHART_MODE_KEY) || 'merged')

export function useChartDisplayMode() {
  function setChartMode(mode) {
    chartMode.value = mode
    localStorage.setItem(CHART_MODE_KEY, mode)
  }

  return { chartMode, setChartMode }
}
