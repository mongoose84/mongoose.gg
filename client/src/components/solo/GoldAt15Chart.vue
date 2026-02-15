<template>
  <div class="gold-chart" data-testid="gold-at-15-chart">
    <!-- Chart -->
    <div v-if="hasData" class="chart-wrapper">
      <Line :data="chartData" :options="chartOptions" />
    </div>

    <!-- Empty state -->
    <div v-else class="empty-state" data-testid="empty-state">
      <p class="empty-text">No gold at 15 data available</p>
      <p class="empty-subtext">Play some games to see your gold economy trend</p>
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
  /** Array of gold at 15 trend data points */
  data: {
    type: Array,
    default: () => []
  }
})

const hasData = computed(() => props.data && props.data.length > 0)

// Get line color based on gold differential (positive = green, negative = red)
const getLineColor = (differential) => {
  if (differential === null || differential === undefined) return '#6d28d9' // Purple (neutral)
  if (differential >= 0) return '#22c55e' // Green
  return '#ef4444' // Red
}

// Calculate average gold differential for overall line color
const averageDifferential = computed(() => {
  if (!hasData.value) return 0
  const validDiffs = props.data.filter(p => p.goldDifferential !== null)
  if (validDiffs.length === 0) return 0
  const sum = validDiffs.reduce((acc, p) => acc + p.goldDifferential, 0)
  return sum / validDiffs.length
})

const lineColor = computed(() => getLineColor(averageDifferential.value))

function formatDate(timestamp) {
  const date = new Date(timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

function formatGold(gold) {
  return gold?.toLocaleString() ?? '--'
}

const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }

  const labels = props.data.map(point => formatDate(point.timestamp))
  const playerGoldData = props.data.map(point => point.playerGold)
  const opponentGoldData = props.data.map(point => point.opponentGold ?? null)

  const datasets = [
    {
      label: 'Your Gold',
      data: playerGoldData,
      borderColor: lineColor.value,
      backgroundColor: `${lineColor.value}1A`, // 10% opacity
      borderWidth: 2,
      fill: false,
      tension: 0.3,
      pointRadius: 0,
      pointHoverRadius: 6,
      pointHoverBackgroundColor: lineColor.value,
      pointHoverBorderColor: '#ffffff',
      pointHoverBorderWidth: 2
    }
  ]

  // Add opponent gold line if we have opponent data
  const hasOpponentData = opponentGoldData.some(gold => gold !== null)
  if (hasOpponentData) {
    datasets.push({
      label: 'Opponent Gold',
      data: opponentGoldData,
      borderColor: 'rgba(255, 255, 255, 0.4)',
      backgroundColor: 'transparent',
      borderWidth: 2,
      borderDash: [5, 5],
      fill: false,
      tension: 0.3,
      pointRadius: 0,
      pointHoverRadius: 6,
      pointHoverBackgroundColor: 'rgba(255, 255, 255, 0.6)',
      pointHoverBorderColor: '#ffffff',
      pointHoverBorderWidth: 2
    })
  }

  return {
    labels,
    datasets
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index', intersect: false },
  plugins: {
    legend: {
      display: true,
      position: 'top',
      align: 'end',
      labels: {
        color: '#ffffff',
        font: { size: 11 },
        usePointStyle: true,
        pointStyle: 'line',
        boxWidth: 30,
        padding: 10
      }
    },
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.9)',
      titleColor: '#ffffff',
      bodyColor: '#ffffff',
      footerColor: '#ffffff',
      borderColor: 'rgba(109, 40, 217, 0.3)',
      borderWidth: 1,
      padding: 12,
      displayColors: false,
      callbacks: {
        title: (items) => {
          const point = props.data[items[0].dataIndex]
          return `Game ${point.gameIndex}: ${point.championName}`
        },
        label: (context) => {
          const point = props.data[context.dataIndex]
          // Show dataset-specific value only
          if (context.datasetIndex === 0) {
            return `Your Gold: ${formatGold(point.playerGold)}`
          } else {
            return `Opponent: ${formatGold(point.opponentGold)}`
          }
        },
        footer: (items) => {
          // Show common information once in the footer
          const point = props.data[items[0].dataIndex]
          const footerLines = []
          
          if (point.opponentGold !== null) {
            const diff = point.goldDifferential
            const sign = diff >= 0 ? '+' : ''
            footerLines.push(`Differential: ${sign}${formatGold(diff)}`)
          }
          if (point.role) {
            footerLines.push(`Role: ${point.role}`)
          }
          const date = new Date(point.timestamp).toLocaleDateString('en-US', {
            month: 'short', day: 'numeric', year: 'numeric'
          })
          footerLines.push(`Date: ${date}`)
          
          return footerLines
        },
        labelColor: () => {
          return {
            borderColor: 'transparent',
            backgroundColor: 'transparent'
          }
        }
      }
    }
  },
  scales: {
    x: {
      display: true,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: { color: '#888888', maxTicksLimit: 6, font: { size: 11 } }
    },
    y: {
      display: true,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: { 
        color: '#888888', 
        callback: (value) => `${(value / 1000).toFixed(1)}k`,
        font: { size: 11 } 
      }
    }
  }
}))
</script>

<style scoped>
.gold-chart {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.chart-wrapper {
  flex: 1;
  min-height: 180px;
  position: relative;
}

.empty-state {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-xs);
}

.empty-text {
  margin: 0;
  color: var(--text-secondary);
  font-size: var(--font-size-sm);
}

.empty-subtext {
  margin: 0;
  color: var(--text-secondary);
  font-size: var(--font-size-xs);
  opacity: 0.7;
}
</style>
