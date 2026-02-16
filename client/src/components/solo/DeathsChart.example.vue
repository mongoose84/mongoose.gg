<!-- 
  EXAMPLE: DeathsChart using TrendLineChart base component
  
  This shows how to migrate from the old standalone component to the new base component.
  
  BEFORE (DeathsChart.vue - 231 lines):
  - Full Chart.js setup
  - Duplicate template/styling
  - Custom computed properties
  
  AFTER (using TrendLineChart - ~20 lines):
  - Import base component
  - Import config helper
  - Pass props through config
-->

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

<!-- 
  Migration steps for other charts:
  
  1. WinrateChart.vue (215 lines → ~25 lines):
     <TrendLineChart :data="data" :config="winrateConfig({ overallWinRate })" ... />
  
  2. DragonParticipationChart.vue (250 lines → ~25 lines):
     <TrendLineChart :data="data" :config="dragonParticipationConfig({ overallAverage, trend })" ... />
  
  3. VisionChart.vue (250 lines → ~25 lines):
     <TrendLineChart :data="data" :config="visionScoreConfig({ overallAverage, roleTarget, trend })" ... />
  
  4. GoldAt15Chart.vue (254 lines → ~20 lines):
     <TrendLineChart :data="data" :config="goldAt15Config()" ... />
  
  5. CsPerMinuteChart.vue (238 lines → ~25 lines):
     <TrendLineChart :data="data" :config="csPerMinuteConfig({ roleTarget })" ... />
  
  Total reduction: ~1,438 lines → ~140 lines = 90% reduction
-->
