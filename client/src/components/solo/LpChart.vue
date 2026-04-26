<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
    :chart-mode="chartMode"
    :accounts="accounts"
    empty-text="No LP data available"
    empty-subtext="Play ranked games to track your LP progress"
    test-id="lp-chart"
  />
</template>

<script setup>
import { computed } from 'vue'
import TrendLineChart from './TrendLineChart.vue'

const props = defineProps({
  /** Array of LP data points, each with { gameNumber, value } */
  data: {
    type: Array,
    default: () => []
  },
  /** Chart display mode: 'merged' | 'per-account' */
  chartMode: {
    type: String,
    default: 'merged'
  },
  /** Accounts for per-account mode: [{ gameName, color }] */
  accounts: {
    type: Array,
    default: () => []
  }
})

const chartConfig = computed(() => ({
  label: 'LP',
  borderColor: '#6d28d9',
  backgroundColor: 'rgba(109, 40, 217, 0.1)',
  tension: 0.3,
  yAxisLabel: 'LP',
  tooltipSuffix: ' LP'
}))
</script>
