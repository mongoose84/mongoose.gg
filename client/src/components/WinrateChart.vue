<template>
  <section class="winrate-chart-card">
    <header class="header">
      <h2 class="title">Winrate Over Time</h2>
      <p class="subtitle">Rolling average winrate</p>
    </header>

    <div v-if="hasData" class="chart-container">
      <Line
        :data="chartData"
        :options="chartOptions"
      />
    </div>

    <div v-else class="empty-state">
      <p>No data for selected time range</p>
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
  winrateTrend: {
    type: Array,
    default: () => []
  }
})

const hasData = computed(() => props.winrateTrend && props.winrateTrend.length > 0)

// Format date for display
function formatDate(timestamp) {
  const date = new Date(timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

// Prepare chart data
const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }

  const labels = props.winrateTrend.map(point => formatDate(point.timestamp))
  const data = props.winrateTrend.map(point => point.winRate)

  return {
    labels,
    datasets: [
      {
        label: 'Winrate %',
        data,
        borderColor: '#6d28d9',
        backgroundColor: 'rgba(109, 40, 217, 0.1)',
        borderWidth: 2,
        fill: true,
        tension: 0.3,
        pointRadius: 0,
        pointHoverRadius: 6,
        pointHoverBackgroundColor: '#6d28d9',
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
          const point = props.winrateTrend[index]
          return `Game ${point.gameIndex}`
        },
        label: (context) => {
          const index = context.dataIndex
          const point = props.winrateTrend[index]
          const date = new Date(point.timestamp)
          const formattedDate = date.toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric'
          })
          return [
            `Winrate: ${point.winRate.toFixed(1)}%`,
            `Date: ${formattedDate}`
          ]
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
      max: 100,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: {
        color: '#888888',
        callback: (value) => `${value}%`,
        stepSize: 25,
        font: { size: 11 }
      }
    }
  }
}))
</script>

<style scoped>
.winrate-chart-card {
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
  align-items: center;
  justify-content: center;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.empty-state p {
  margin: 0;
}
</style>

