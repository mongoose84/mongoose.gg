<template>
  <TrendLineChart
    :data="data"
    :config="chartConfig"
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
  }
})

// Build chart config from props
const chartConfig = computed(() => winrateConfig({
  overallWinRate: props.overallWinRate
}))
</script>

