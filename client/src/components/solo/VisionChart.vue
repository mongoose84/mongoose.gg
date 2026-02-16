<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
    empty-text="No vision score data available"
    empty-subtext="Play some games to see your vision score trends"
    test-id="vision-chart"
  />
</template>

<script setup>
import { computed } from 'vue'
import TrendLineChart from './TrendLineChart.vue'
import { visionScoreConfig } from '../../utils/chartConfigs.js'

const props = defineProps({
  /** Array of vision score trend data points */
  data: {
    type: Array,
    default: () => []
  },
  /** Overall average vision per minute to show as reference line */
  overallAverage: {
    type: Number,
    default: null
  },
  /** Role-specific target (Support: 2.0, others: 1.0) */
  roleTarget: {
    type: Number,
    default: 1.0
  },
  /** Trend direction: 'improving', 'worsening', 'neutral' */
  trend: {
    type: String,
    default: 'neutral'
  }
})

// Build chart config from props
const chartConfig = computed(() => visionScoreConfig({
  overallAverage: props.overallAverage,
  roleTarget: props.roleTarget,
  trend: props.trend
}))
</script>
