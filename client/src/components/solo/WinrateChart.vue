<template>
  <div class="winrate-chart" data-testid="winrate-chart">
    <!-- Chart -->
    <div v-if="hasData" class="chart-wrapper">
      <Line :data="chartData" :options="chartOptions" />
    </div>

    <!-- Empty state -->
    <div v-else class="empty-state" data-testid="empty-state">
      <p class="empty-text">No winrate data available</p>
      <p class="empty-subtext">Play some games to see your winrate trend</p>
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
  /** Array of winrate trend data points */
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

// Get line color based on current winrate (last data point)
const lineColor = computed(() => {
  if (!hasData.value) return '#6d28d9'
  const lastWinrate = props.data[props.data.length - 1]?.winRate ?? 50
  if (lastWinrate >= 52) return '#22c55e' // Green
  if (lastWinrate < 48) return '#ef4444' // Red
  return '#6d28d9' // Purple (neutral)
})

function formatDate(timestamp) {
  const date = new Date(timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }

  const labels = props.data.map(point => formatDate(point.timestamp))
  const data = props.data.map(point => point.winRate)

  return {
    labels,
    datasets: [{
      label: 'Winrate %',
      data,
      borderColor: lineColor.value,
      backgroundColor: `${lineColor.value}1A`, // 10% opacity
      borderWidth: 2,
      fill: true,
      tension: 0.3,
      pointRadius: 0,
      pointHoverRadius: 6,
      pointHoverBackgroundColor: lineColor.value,
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
    annotation: false,
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.9)',
      titleColor: '#ffffff',
      bodyColor: '#ffffff',
      borderColor: 'rgba(109, 40, 217, 0.3)',
      borderWidth: 1,
      padding: 12,
      displayColors: false,
      callbacks: {
        title: (items) => `Game ${props.data[items[0].dataIndex]?.gameIndex}`,
        label: (context) => {
          const point = props.data[context.dataIndex]
          const date = new Date(point.timestamp).toLocaleDateString('en-US', {
            month: 'short', day: 'numeric', year: 'numeric'
          })
          return [
            `Winrate: ${point.winRate.toFixed(1)}%`,
            `Date: ${date}`
          ]
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
      min: 0,
      max: 100,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: { color: '#888888', callback: (value) => `${value}%`, stepSize: 25, font: { size: 11 } }
    }
  }
}))
</script>

<style scoped>
.winrate-chart {
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

