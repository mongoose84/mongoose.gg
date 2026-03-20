<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
    :chart-mode="chartMode"
    :accounts="accounts"
    empty-text="No gold at 15 data available"
    empty-subtext="Play some games to see your gold economy trend"
    test-id="gold-at-15-chart"
  />
</template>

<script setup>
import { computed } from 'vue'
import TrendLineChart from './TrendLineChart.vue'
import { goldAt15Config } from '../../utils/chartConfigs.js'

const props = defineProps({
  /** Array of gold at 15 trend data points */
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

// Build chart config from props
const chartConfig = computed(() => goldAt15Config())
</script>
