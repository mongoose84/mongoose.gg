<template>
  <div class="trend-line-chart" :data-testid="testId">
    <!-- Chart -->
    <div v-if="hasData" class="chart-wrapper">
      <Line :data="chartData" :options="chartOptions" />
    </div>

    <!-- Empty state -->
    <div v-else class="empty-state" data-testid="empty-state">
      <p class="empty-text">{{ emptyText }}</p>
      <p v-if="emptySubtext" class="empty-subtext">{{ emptySubtext }}</p>
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
  /** Array of data points to plot */
  data: {
    type: Array,
    default: () => []
  },
  /** Configuration object for chart behavior */
  config: {
    type: Object,
    required: true,
    validator: (config) => {
      return config.dataKey && typeof config.dataKey === 'string'
    }
  },
  /** Empty state message */
  emptyText: {
    type: String,
    default: 'No data available'
  },
  /** Empty state subtext */
  emptySubtext: {
    type: String,
    default: null
  },
  /** Test ID for the chart */
  testId: {
    type: String,
    default: 'trend-line-chart'
  }
})

const hasData = computed(() => props.data && props.data.length > 0)

/**
 * Default label formatter - converts timestamp to short date
 */
function defaultLabelFormatter(dataPoint) {
  const date = new Date(dataPoint.timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

/**
 * Calculate line color based on config strategy
 */
const lineColor = computed(() => {
  if (!hasData.value) return '#6d28d9' // Default purple
  
  const colorConfig = props.config.color
  if (!colorConfig) return '#6d28d9'
  
  // Function-based color calculation
  if (typeof colorConfig === 'function') {
    return colorConfig(props.data)
  }
  
  // Trend-based coloring
  if (colorConfig.type === 'trend' && colorConfig.trend) {
    if (colorConfig.trend === 'improving') return '#22c55e' // Green
    if (colorConfig.trend === 'worsening') return '#ef4444' // Red
    return '#6d28d9' // Purple (neutral)
  }
  
  // Value-based coloring (e.g., last data point)
  if (colorConfig.type === 'value' && colorConfig.thresholds) {
    const value = props.data[props.data.length - 1]?.[props.config.dataKey]
    const thresholds = colorConfig.thresholds
    
    if (value >= thresholds.good) return '#22c55e' // Green
    if (value < thresholds.bad) return '#ef4444' // Red
    return '#6d28d9' // Purple (neutral)
  }
  
  // Static color
  if (typeof colorConfig === 'string') {
    return colorConfig
  }
  
  return '#6d28d9' // Default
})

/**
 * Build Chart.js data object
 */
const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }
  
  const labelFormatter = props.config.labelFormatter || defaultLabelFormatter
  const labels = props.data.map(labelFormatter)
  const dataValues = props.data.map(point => point[props.config.dataKey])
  
  const datasets = [{
    label: props.config.label || 'Value',
    data: dataValues,
    borderColor: lineColor.value,
    backgroundColor: `${lineColor.value}1A`, // 10% opacity
    borderWidth: 2,
    fill: props.config.fill !== false,
    tension: props.config.tension ?? 0.3,
    pointRadius: 0,
    pointHoverRadius: 6,
    pointHoverBackgroundColor: lineColor.value,
    pointHoverBorderColor: '#ffffff',
    pointHoverBorderWidth: 2
  }]
  
  // Add additional datasets if configured (e.g., opponent line for GoldAt15)
  if (props.config.additionalDatasets) {
    datasets.push(...props.config.additionalDatasets.map(dataset => ({
      label: dataset.label,
      data: props.data.map(point => point[dataset.dataKey]),
      borderColor: dataset.borderColor || 'rgba(255, 255, 255, 0.4)',
      backgroundColor: dataset.backgroundColor || 'transparent',
      borderWidth: dataset.borderWidth || 2,
      borderDash: dataset.borderDash || [5, 5],
      fill: dataset.fill !== false,
      tension: dataset.tension ?? 0.3,
      pointRadius: 0,
      pointHoverRadius: 6,
      pointHoverBackgroundColor: dataset.pointHoverBackgroundColor || 'rgba(255, 255, 255, 0.6)',
      pointHoverBorderColor: '#ffffff',
      pointHoverBorderWidth: 2
    })))
  }
  
  return { labels, datasets }
})

/**
 * Build annotations config
 */
const annotationConfig = computed(() => {
  const annotations = {}
  
  if (!props.config.annotations || props.config.annotations.length === 0) {
    return { annotations }
  }
  
  props.config.annotations.forEach((annotation, index) => {
    if (annotation.value === null || annotation.value === undefined) {
      return
    }
    
    annotations[`line${index}`] = {
      type: 'line',
      yMin: annotation.value,
      yMax: annotation.value,
      borderColor: annotation.color || 'rgba(255, 255, 255, 0.4)',
      borderWidth: annotation.width || 1,
      borderDash: annotation.dash || [5, 5],
      label: {
        display: !!annotation.label,
        content: annotation.label || '',
        position: annotation.labelPosition || 'end',
        backgroundColor: annotation.labelBackground || 'rgba(0, 0, 0, 0.7)',
        color: annotation.labelColor || 'rgba(255, 255, 255, 0.8)',
        font: { size: annotation.labelSize || 10 },
        padding: 4
      }
    }
  })
  
  return { annotations }
})

/**
 * Build chart options
 */
const chartOptions = computed(() => {
  const yAxisConfig = props.config.yAxis || {}
  const tooltipConfig = props.config.tooltip || {}
  
  return {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { mode: 'index', intersect: false },
    plugins: {
      legend: {
        display: props.config.showLegend || false,
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
            if (tooltipConfig.title) {
              return tooltipConfig.title(props.data[items[0].dataIndex], items[0].dataIndex)
            }
            const point = props.data[items[0].dataIndex]
            return `Game ${point.gameIndex || items[0].dataIndex + 1}`
          },
          label: (context) => {
            if (tooltipConfig.label) {
              return tooltipConfig.label(props.data[context.dataIndex], context)
            }
            const value = context.parsed.y
            return `${props.config.label || 'Value'}: ${value.toFixed(2)}`
          },
          footer: tooltipConfig.footer 
            ? (items) => tooltipConfig.footer(props.data[items[0].dataIndex], items[0].dataIndex)
            : undefined
        }
      }
    },
    scales: {
      x: {
        display: true,
        grid: { color: 'rgba(255, 255, 255, 0.05)' },
        ticks: { 
          color: '#888888', 
          maxTicksLimit: yAxisConfig.maxXTicks || 6,
          font: { size: 11 } 
        }
      },
      y: {
        display: true,
        min: yAxisConfig.min,
        max: yAxisConfig.max,
        suggestedMax: yAxisConfig.suggestedMax,
        grid: { color: 'rgba(255, 255, 255, 0.05)' },
        ticks: { 
          color: '#888888',
          stepSize: yAxisConfig.stepSize,
          font: { size: 11 },
          callback: yAxisConfig.formatter || ((value) => value)
        }
      }
    }
  }
})
</script>

<style scoped>
.trend-line-chart {
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
