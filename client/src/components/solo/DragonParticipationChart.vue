<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
    :chart-mode="chartMode"
    :accounts="accounts"
    empty-text="No dragon participation data available"
    empty-subtext="Play some games to see your dragon participation trends"
    test-id="dragon-participation-chart"
  />
</template>

<script setup>
import { computed } from 'vue'
import TrendLineChart from './TrendLineChart.vue'
import { dragonParticipationConfig } from '../../utils/chartConfigs.js'

const props = defineProps({
  /** Array of dragon participation trend data points */
  data: {
    type: Array,
    default: () => []
  },
  /** Overall average participation rate to show as reference line */
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

// Build chart config from props
const chartConfig = computed(() => dragonParticipationConfig({
  overallAverage: props.overallAverage,
  trend: props.trend
}))
</script>
