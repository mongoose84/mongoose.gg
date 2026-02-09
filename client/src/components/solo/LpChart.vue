<template>
  <div class="lp-chart" data-testid="lp-chart">
    <!-- Chart -->
    <div v-if="hasData" class="chart-wrapper">
      <Line :data="chartData" :options="chartOptions" />
    </div>

    <!-- Empty state -->
    <div v-else class="empty-state" data-testid="empty-state">
      <p class="empty-text">No LP data available yet</p>
      <p class="empty-subtext">LP tracking starts after your next ranked game</p>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js'

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
)

const props = defineProps({
  /** Array of LP trend data points */
  data: {
    type: Array,
    default: () => []
  },
  /** Match ID to highlight (optional) */
  highlightMatchId: {
    type: String,
    default: null
  }
})

const hasData = computed(() => props.data && props.data.length > 0)

// Check if any data point is from Master+ tiers (LP can exceed 100)
const isMasterPlus = computed(() => {
  if (!hasData.value) return false
  const masterPlusTiers = ['master', 'grandmaster', 'challenger']
  return props.data.some(point =>
    masterPlusTiers.includes(point.rank?.split(' ')[0]?.toLowerCase())
  )
})

// Calculate dynamic Y-axis max based on data
const yAxisMax = computed(() => {
  if (!hasData.value) return 100
  const maxLp = Math.max(...props.data.map(point => point.currentLp))
  if (isMasterPlus.value || maxLp > 100) {
    return Math.max(100, Math.ceil(maxLp / 100) * 100)
  }
  return 100
})

const yAxisStepSize = computed(() => {
  const max = yAxisMax.value
  if (max <= 100) return 25
  if (max <= 500) return 100
  return 200
})

function formatDate(timestamp) {
  const date = new Date(timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

// Get point colors based on LP change (green = gained, red = lost, purple = neutral/first)
function getPointColors() {
  return props.data.map(point => {
    if (point.lpGain === null) return '#6d28d9' // First point or unknown
    if (point.lpGain > 0) return '#22c55e' // Gained LP
    if (point.lpGain < 0) return '#ef4444' // Lost LP
    return '#6d28d9' // No change
  })
}

const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }

  const labels = props.data.map(point => formatDate(point.timestamp))
  const data = props.data.map(point => point.currentLp)
  const pointColors = getPointColors()

  return {
    labels,
    datasets: [{
      label: 'LP',
      data,
      borderColor: '#6d28d9',
      backgroundColor: 'rgba(109, 40, 217, 0.1)',
      borderWidth: 2,
      fill: true,
      tension: 0.3,
      pointRadius: 4,
      pointBackgroundColor: pointColors,
      pointBorderColor: pointColors,
      pointHoverRadius: 8,
      pointHoverBackgroundColor: pointColors,
      pointHoverBorderColor: '#ffffff',
      pointHoverBorderWidth: 2
    }]
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index', intersect: false },
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.9)',
      titleColor: '#ffffff',
      bodyColor: '#ffffff',
      borderColor: 'rgba(109, 40, 217, 0.3)',
      borderWidth: 1,
      padding: 12,
      displayColors: false,
      callbacks: {
        title: (items) => `Snapshot ${props.data[items[0].dataIndex]?.gameIndex}`,
        label: (context) => {
          const point = props.data[context.dataIndex]
          const date = new Date(point.timestamp).toLocaleDateString('en-US', {
            month: 'short', day: 'numeric', year: 'numeric'
          })
          const lines = [`${point.rank} - ${point.currentLp} LP`]
          // Show LP change if available (not first snapshot)
          if (point.lpGain !== null) {
            const changeText = point.lpGain >= 0 ? `+${point.lpGain}` : `${point.lpGain}`
            lines.push(`Change: ${changeText} LP`)
          }
          lines.push(`Date: ${date}`)
          if (point.isPromotion) lines.push('🎉 Promoted!')
          if (point.isDemotion) lines.push('📉 Demoted')
          return lines
        }
      }
    }
  },
  scales: {
    x: { display: true, grid: { color: 'rgba(255,255,255,0.05)' }, ticks: { color: '#888', maxTicksLimit: 6, font: { size: 11 } } },
    y: { display: true, min: 0, max: yAxisMax.value, grid: { color: 'rgba(255,255,255,0.05)' }, ticks: { color: '#888', callback: (v) => `${v} LP`, stepSize: yAxisStepSize.value, font: { size: 11 } } }
  }
}))
</script>

<style scoped>
.lp-chart { height: 100%; display: flex; flex-direction: column; }
.chart-wrapper { flex: 1; min-height: 180px; position: relative; }
.empty-state { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: var(--spacing-xs); }
.empty-text { margin: 0; color: var(--text-secondary); font-size: var(--font-size-sm); }
.empty-subtext { margin: 0; color: var(--text-secondary); font-size: var(--font-size-xs); opacity: 0.7; }
</style>

