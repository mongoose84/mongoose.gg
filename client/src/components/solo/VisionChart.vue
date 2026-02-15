<template>
  <div class="vision-chart" data-testid="vision-chart">
    <!-- Chart -->
    <div v-if="hasData" class="chart-wrapper">
      <Line :data="chartData" :options="chartOptions" />
    </div>

    <!-- Empty state -->
    <div v-else class="empty-state" data-testid="empty-state">
      <p class="empty-text">No vision score data available</p>
      <p class="empty-subtext">Play some games to see your vision score trends</p>
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

const hasData = computed(() => props.data && props.data.length > 0)

// Get line color based on trend and target
const lineColor = computed(() => {
  if (!hasData.value) return '#6d28d9'
  
  // Calculate average vision per minute from recent games
  const recentCount = Math.min(10, props.data.length)
  const recentGames = props.data.slice(-recentCount)
  const recentAvg = recentGames.reduce((sum, point) => sum + point.visionScorePerMinute, 0) / recentCount
  
  // Color based on performance relative to target
  if (recentAvg >= props.roleTarget) return '#22c55e' // Green - meeting target
  if (recentAvg >= props.roleTarget * 0.8) return '#eab308' // Yellow - approaching target
  return '#ef4444' // Red - below target
})

function formatDate(timestamp) {
  const date = new Date(timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }

  const labels = props.data.map(point => formatDate(point.timestamp))
  const visionData = props.data.map(point => point.visionScorePerMinute)
  const rollingAvgData = props.data.map(point => point.rollingAverage)

  return {
    labels,
    datasets: [
      {
        label: 'Vision/Min',
        data: visionData,
        borderColor: `${lineColor.value}80`, // 50% opacity
        backgroundColor: `${lineColor.value}1A`, // 10% opacity
        borderWidth: 1,
        fill: false,
        tension: 0,
        pointRadius: 3,
        pointHoverRadius: 6,
        pointBackgroundColor: lineColor.value,
        pointHoverBackgroundColor: lineColor.value,
        pointHoverBorderColor: '#ffffff',
        pointHoverBorderWidth: 2
      },
      {
        label: 'Rolling Average',
        data: rollingAvgData,
        borderColor: lineColor.value,
        backgroundColor: 'transparent',
        borderWidth: 2,
        fill: false,
        tension: 0.3,
        pointRadius: 0,
        pointHoverRadius: 0
      }
    ]
  }
})

// Build annotation config for target line and overall average reference line
const annotationConfig = computed(() => {
  const annotations = {}

  // Role-appropriate target line
  const targetLabel = props.roleTarget >= 2.0 
    ? 'Support Target: 2.0/min' 
    : 'Target: 1.0/min'
  
  annotations.targetLine = {
    type: 'line',
    yMin: props.roleTarget,
    yMax: props.roleTarget,
    borderColor: 'rgba(34, 197, 94, 0.5)', // Green with opacity
    borderWidth: 2,
    borderDash: [5, 5],
    label: {
      display: true,
      content: targetLabel,
      position: 'start',
      backgroundColor: 'rgba(34, 197, 94, 0.7)',
      color: '#ffffff',
      font: { size: 10 },
      padding: 4
    }
  }

  // Overall average line (if provided)
  if (props.overallAverage !== null && props.overallAverage !== undefined) {
    annotations.overallLine = {
      type: 'line',
      yMin: props.overallAverage,
      yMax: props.overallAverage,
      borderColor: 'rgba(255, 255, 255, 0.4)',
      borderWidth: 1,
      borderDash: [5, 5],
      label: {
        display: true,
        content: `Overall: ${props.overallAverage.toFixed(2)}/min`,
        position: 'end',
        backgroundColor: 'rgba(0, 0, 0, 0.7)',
        color: 'rgba(255, 255, 255, 0.8)',
        font: { size: 10 },
        padding: 4
      }
    }
  }

  return { annotations }
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
        color: '#888888',
        font: { size: 11 },
        usePointStyle: true,
        boxWidth: 6,
        boxHeight: 6,
        padding: 10
      }
    },
    annotation: annotationConfig.value,
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.9)',
      titleColor: '#ffffff',
      bodyColor: '#ffffff',
      borderColor: 'rgba(109, 40, 217, 0.3)',
      borderWidth: 1,
      padding: 12,
      displayColors: false,
      filter: (tooltipItem) => {
        // Only show tooltip for the first dataset (Vision/Min), not the rolling average
        return tooltipItem.datasetIndex === 0
      },
      callbacks: {
        title: (items) => {
          const point = props.data[items[0].dataIndex]
          return `Game ${point.gameIndex} - ${point.championName}`
        },
        label: (context) => {
          const point = props.data[context.dataIndex]
          const date = new Date(point.timestamp).toLocaleDateString('en-US', {
            month: 'short', day: 'numeric', year: 'numeric'
          })
          return [
            `Vision/Min: ${point.visionScorePerMinute.toFixed(2)}`,
            `Rolling Avg: ${point.rollingAverage.toFixed(2)}`,
            `Vision Score: ${point.visionScore}`,
            `Wards Placed: ${point.wardsPlaced}`,
            `Wards Destroyed: ${point.wardsDestroyed}`,
            `Game Duration: ${point.gameDurationMinutes.toFixed(1)} min`,
            point.role ? `Role: ${point.role}` : null,
            `Date: ${date}`
          ].filter(line => line !== null)
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
      suggestedMax: Math.max(props.roleTarget * 1.5, 3.0),
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: { 
        color: '#888888', 
        font: { size: 11 },
        callback: (value) => value.toFixed(1)
      }
    }
  }
}))
</script>

<style scoped>
.vision-chart {
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
