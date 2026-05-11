<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
    :chart-mode="chartMode"
    :accounts="accounts"
    empty-text="No damage per minute data available"
    empty-subtext="Play some games to see your damage output trend"
    test-id="dpm-chart"
  />
</template>

<script setup>
import { computed } from 'vue'
import TrendLineChart from './TrendLineChart.vue'
import { dpmConfig } from '../../utils/chartConfigs.js'

const props = defineProps({
  /** Array of DPM trend data points */
  data: {
    type: Array,
    default: () => []
  },
  /** Overall average DPM to show as reference line */
  overallAverage: {
    type: Number,
    default: null
  },
  /** Trend direction: 'improving', 'worsening', 'neutral' */
  trend: {
    type: String,
    default: 'neutral'
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

const chartConfig = computed(() => dpmConfig({
  overallAverage: props.overallAverage,
  trend: props.trend
}))
</script>
