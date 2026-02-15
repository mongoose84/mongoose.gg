<template>
  <div class="cs-chart" data-testid="cs-per-minute-chart">
    <!-- Chart -->
    <div v-if="hasData" class="chart-wrapper">
      <Line :data="chartData" :options="chartOptions" />
    </div>

    <!-- Empty state -->
    <div v-else class="empty-state" data-testid="empty-state">
      <p class="empty-text">No CS per minute data available</p>
      <p class="empty-subtext">Play some games to see your farming efficiency trend</p>
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
import annotationPlugin from 'chartjs-plugin-annotation'

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
  annotationPlugin
)

const props = defineProps({
  /** Array of CS per minute trend data points */
  data: {
    type: Array,
    default: () => []
  },
  /** Role-specific target CS/min (optional) */
  roleTarget: {
    type: Number,
    default: null
  }
})

const hasData = computed(() => props.data && props.data.length > 0)

// Calculate average CS per minute
const averageCsPerMin = computed(() => {
  if (!hasData.value) return 0
  const sum = props.data.reduce((acc, p) => acc + p.csPerMinute, 0)
  return sum / props.data.length
})

// Get line color based on average CS per minute
const lineColor = computed(() => {
  const avg = averageCsPerMin.value
  // Good: >= 6 CS/min, Needs work: < 5 CS/min
  if (avg >= 6) return '#22c55e' // Green
  if (avg < 5) return '#ef4444' // Red
  return '#6d28d9' // Purple (neutral)
})

function formatDate(timestamp) {
  const date = new Date(timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }

  const labels = props.data.map(point => formatDate(point.timestamp))
  const data = props.data.map(point => point.csPerMinute)

  return {
    labels,
    datasets: [{
      label: 'CS/min',
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

// Build annotation config for role target reference line (if provided)
const annotationConfig = computed(() => {
  if (props.roleTarget === null || props.roleTarget === undefined) {
    return {}
  }
  return {
    annotations: {
      targetLine: {
        type: 'line',
        yMin: props.roleTarget,
        yMax: props.roleTarget,
        borderColor: 'rgba(255, 255, 255, 0.4)',
        borderWidth: 1,
        borderDash: [5, 5],
        label: {
          display: true,
          content: `Target: ${props.roleTarget.toFixed(1)} CS/min`,
          position: 'end',
          backgroundColor: 'rgba(0, 0, 0, 0.7)',
          color: 'rgba(255, 255, 255, 0.8)',
          font: { size: 10 },
          padding: 4
        }
      }
    }
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index', intersect: false },
  plugins: {
    legend: { display: false },
    annotation: annotationConfig.value,
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
          return `CS/min: ${point.csPerMinute.toFixed(1)}`
        },
        footer: (items) => {
          const point = props.data[items[0].dataIndex]
          const footerLines = []
          footerLines.push(`Total CS: ${point.totalCs}`)
          footerLines.push(`Duration: ${point.gameDurationMinutes.toFixed(1)} min`)
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
      min: 0,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: { 
        color: '#888888', 
        callback: (value) => `${value.toFixed(1)}`,
        font: { size: 11 } 
      }
    }
  }
}))
</script>

<style scoped>
.cs-chart {
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
