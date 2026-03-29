<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
    :chart-mode="chartMode"
    :accounts="accounts"
    empty-text="No winrate data available"
    empty-subtext="Play some games to see your winrate trend"
    test-id="winrate-chart"
  />
</template>

<script setup>
import { computed } from 'vue'
import TrendLineChart from './TrendLineChart.vue'
import { winrateConfig } from '../../utils/chartConfigs.js'

const props = defineProps({
  /** Array of winrate trend data points */
  data: {
    type: Array,
    default: () => []
  },
  /** Match ID to highlight (optional) */
  highlightMatchId: {
    type: String,
    default: null
  },
  /** Overall winrate to show as reference line */
  overallWinRate: {
    type: Number,
    default: null
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
const chartConfig = computed(() => winrateConfig({
  overallWinRate: props.overallWinRate
}))
</script>

