<template>
  <section class="lp-trend-card">
    <header class="header">
      <h2 class="title">LP Progression</h2>
      <p class="subtitle">Track your ranked LP over time</p>
    </header>

    <div v-if="hasData" class="chart-container">
      <Line
        :data="chartData"
        :options="chartOptions"
      />
    </div>

    <div v-else class="empty-state">
      <p>No LP data available yet</p>
      <p class="empty-hint">LP tracking starts after your next ranked game</p>
    </div>
  </section>
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
  lpTrend: {
    type: Array,
    default: () => []
  }
})

const hasData = computed(() => props.lpTrend && props.lpTrend.length > 0)

// Check if any data point is from Master+ tiers (LP can exceed 100)
const isMasterPlus = computed(() => {
  if (!hasData.value) return false
  const masterPlusTiers = ['master', 'grandmaster', 'challenger']
  return props.lpTrend.some(point =>
    masterPlusTiers.includes(point.rank?.split(' ')[0]?.toLowerCase())
  )
})

// Calculate dynamic Y-axis max based on data
const yAxisMax = computed(() => {
  if (!hasData.value) return 100
  const maxLp = Math.max(...props.lpTrend.map(point => point.currentLp))

  // For Master+ or if any LP exceeds 100, calculate a nice round max
  if (isMasterPlus.value || maxLp > 100) {
    // Round up to nearest 100, with minimum of 100
    return Math.max(100, Math.ceil(maxLp / 100) * 100)
  }

  // For regular tiers, cap at 100
  return 100
})

// Calculate appropriate step size for Y-axis ticks
const yAxisStepSize = computed(() => {
  const max = yAxisMax.value
  if (max <= 100) return 25
  if (max <= 500) return 100
  return 200
})

// Format date for display
function formatDate(timestamp) {
  const date = new Date(timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

// Get point color based on win/loss
function getPointColors() {
  return props.lpTrend.map(point => point.win ? '#22c55e' : '#ef4444')
}

// Prepare chart data
const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }

  const labels = props.lpTrend.map(point => formatDate(point.timestamp))
  const data = props.lpTrend.map(point => point.currentLp)
  const pointColors = getPointColors()

  return {
    labels,
    datasets: [
      {
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
      }
    ]
  }
})

// Chart.js options
const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    mode: 'index',
    intersect: false
  },
  plugins: {
    legend: {
      display: false
    },
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.9)',
      titleColor: '#ffffff',
      bodyColor: '#ffffff',
      borderColor: 'rgba(109, 40, 217, 0.3)',
      borderWidth: 1,
      padding: 12,
      displayColors: false,
      callbacks: {
        title: (tooltipItems) => {
          const index = tooltipItems[0].dataIndex
          const point = props.lpTrend[index]
          return `Game ${point.gameIndex}`
        },
        label: (context) => {
          const index = context.dataIndex
          const point = props.lpTrend[index]
          const date = new Date(point.timestamp)
          const formattedDate = date.toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric'
          })
          const result = point.win ? '✓ Win' : '✗ Loss'
          const lpChange = point.lpGain !== null 
            ? (point.lpGain >= 0 ? `+${point.lpGain}` : `${point.lpGain}`) 
            : ''
          const lines = [
            `${point.rank} - ${point.currentLp} LP`,
            `${result}${lpChange ? ` (${lpChange} LP)` : ''}`,
            `Date: ${formattedDate}`
          ]
          if (point.isPromotion) lines.push('🎉 Promoted!')
          if (point.isDemotion) lines.push('📉 Demoted')
          return lines
        }
      }
    }
  },
  scales: {
    x: {
      display: true,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: {
        color: '#888888',
        maxTicksLimit: 6,
        font: { size: 11 }
      }
    },
    y: {
      display: true,
      min: 0,
      max: yAxisMax.value,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: {
        color: '#888888',
        callback: (value) => `${value} LP`,
        stepSize: yAxisStepSize.value,
        font: { size: 11 }
      }
    }
  }
}))
</script>

<style scoped>
.lp-trend-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  height: 100%;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
}

.header {
  margin-bottom: var(--spacing-md);
}

.title {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.subtitle {
  margin: 4px 0 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.chart-container {
  flex: 1;
  min-height: 200px;
  position: relative;
}

.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  gap: var(--spacing-xs);
}

.empty-state p {
  margin: 0;
}

.empty-hint {
  font-size: var(--font-size-xs);
  opacity: 0.7;
}
</style>

