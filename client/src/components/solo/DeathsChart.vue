<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
    empty-text="No deaths data available"
    empty-subtext="Play some games to see your death trends"
    test-id="deaths-chart"
  />
</template>

<script setup>
import { computed } from 'vue'
import TrendLineChart from './TrendLineChart.vue'
import { deathsConfig } from '../../utils/chartConfigs.js'

const props = defineProps({
  /** Array of deaths trend data points */
  data: {
    type: Array,
    default: () => []
  },
  /** Overall average deaths to show as reference line */
  overallAverage: {
    type: Number,
    default: null
  },
  /** Trend direction: 'improving', 'worsening', 'neutral' */
  trend: {
    type: String,
    default: 'neutral'
  }
})

// Build chart config from props
const chartConfig = computed(() => deathsConfig({
  overallAverage: props.overallAverage,
  trend: props.trend
}))
</script>
