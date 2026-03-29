<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
    :chart-mode="chartMode"
    :accounts="accounts"
    empty-text="No CS per minute data available"
    empty-subtext="Play some games to see your farming efficiency trend"
    test-id="cs-per-minute-chart"
  />
</template>

<script setup>
import { computed } from 'vue'
import TrendLineChart from './TrendLineChart.vue'
import { csPerMinuteConfig } from '../../utils/chartConfigs.js'

const props = defineProps({
  /** Array of CS per minute trend data points */
  data: {
    type: Array,
    default: () => []
  },
  /** Role-specific target CS/min (optional) */
  roleTarget: {
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
const chartConfig = computed(() => csPerMinuteConfig({
  roleTarget: props.roleTarget
}))
</script>
