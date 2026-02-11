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
import { computed, ref, onMounted } from 'vue'
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

// Register Chart.js components and annotation plugin globally
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

// Calculate dynamic Y-axis max based on absolute LP data
const yAxisMax = computed(() => {
  if (!hasData.value) return 2800 // Default max (Challenger range)
  const maxAbsoluteLp = Math.max(...props.data.map(point => point.absoluteLp))
  // Add 10% padding above the max value for better visualization
  return Math.ceil(maxAbsoluteLp * 1.1)
})

const yAxisMin = computed(() => {
  if (!hasData.value) return 0
  const minAbsoluteLp = Math.min(...props.data.map(point => point.absoluteLp))
  // Add 10% padding below the min value for better visualization
  return Math.floor(minAbsoluteLp * 0.9)
})

function formatDate(timestamp) {
  const date = new Date(timestamp)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }

  const labels = props.data.map(point => formatDate(point.timestamp))
  // Use absoluteLp for Y-axis positioning (handles promotions/demotions correctly)
  const data = props.data.map(point => point.absoluteLp)

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
      pointBackgroundColor: '#6d28d9',
      pointBorderColor: '#6d28d9',
      pointHoverRadius: 8,
      pointHoverBackgroundColor: '#6d28d9',
      pointHoverBorderColor: '#ffffff',
      pointHoverBorderWidth: 2
    }]
  }
})

// Tier and division boundaries for horizontal rank lines (absolute LP values)
const RANK_BOUNDARIES = [
  // Iron
  { name: 'Iron IV', tier: 'iron', division: 'IV', absoluteLp: 0, color: 'rgba(139, 69, 19, 0.4)', isTier: true },
  { name: 'Iron III', tier: 'iron', division: 'III', absoluteLp: 100, color: 'rgba(139, 69, 19, 0.2)', isTier: false },
  { name: 'Iron II', tier: 'iron', division: 'II', absoluteLp: 200, color: 'rgba(139, 69, 19, 0.2)', isTier: false },
  { name: 'Iron I', tier: 'iron', division: 'I', absoluteLp: 300, color: 'rgba(139, 69, 19, 0.2)', isTier: false },
  // Bronze
  { name: 'Bronze IV', tier: 'bronze', division: 'IV', absoluteLp: 400, color: 'rgba(205, 127, 50, 0.4)', isTier: true },
  { name: 'Bronze III', tier: 'bronze', division: 'III', absoluteLp: 500, color: 'rgba(205, 127, 50, 0.2)', isTier: false },
  { name: 'Bronze II', tier: 'bronze', division: 'II', absoluteLp: 600, color: 'rgba(205, 127, 50, 0.2)', isTier: false },
  { name: 'Bronze I', tier: 'bronze', division: 'I', absoluteLp: 700, color: 'rgba(205, 127, 50, 0.2)', isTier: false },
  // Silver
  { name: 'Silver IV', tier: 'silver', division: 'IV', absoluteLp: 800, color: 'rgba(192, 192, 192, 0.4)', isTier: true },
  { name: 'Silver III', tier: 'silver', division: 'III', absoluteLp: 900, color: 'rgba(192, 192, 192, 0.2)', isTier: false },
  { name: 'Silver II', tier: 'silver', division: 'II', absoluteLp: 1000, color: 'rgba(192, 192, 192, 0.2)', isTier: false },
  { name: 'Silver I', tier: 'silver', division: 'I', absoluteLp: 1100, color: 'rgba(192, 192, 192, 0.2)', isTier: false },
  // Gold
  { name: 'Gold IV', tier: 'gold', division: 'IV', absoluteLp: 1200, color: 'rgba(255, 215, 0, 0.4)', isTier: true },
  { name: 'Gold III', tier: 'gold', division: 'III', absoluteLp: 1300, color: 'rgba(255, 215, 0, 0.2)', isTier: false },
  { name: 'Gold II', tier: 'gold', division: 'II', absoluteLp: 1400, color: 'rgba(255, 215, 0, 0.2)', isTier: false },
  { name: 'Gold I', tier: 'gold', division: 'I', absoluteLp: 1500, color: 'rgba(255, 215, 0, 0.2)', isTier: false },
  // Platinum
  { name: 'Platinum IV', tier: 'platinum', division: 'IV', absoluteLp: 1600, color: 'rgba(64, 224, 208, 0.4)', isTier: true },
  { name: 'Platinum III', tier: 'platinum', division: 'III', absoluteLp: 1700, color: 'rgba(64, 224, 208, 0.2)', isTier: false },
  { name: 'Platinum II', tier: 'platinum', division: 'II', absoluteLp: 1800, color: 'rgba(64, 224, 208, 0.2)', isTier: false },
  { name: 'Platinum I', tier: 'platinum', division: 'I', absoluteLp: 1900, color: 'rgba(64, 224, 208, 0.2)', isTier: false },
  // Emerald
  { name: 'Emerald IV', tier: 'emerald', division: 'IV', absoluteLp: 2000, color: 'rgba(80, 200, 120, 0.4)', isTier: true },
  { name: 'Emerald III', tier: 'emerald', division: 'III', absoluteLp: 2100, color: 'rgba(80, 200, 120, 0.2)', isTier: false },
  { name: 'Emerald II', tier: 'emerald', division: 'II', absoluteLp: 2200, color: 'rgba(80, 200, 120, 0.2)', isTier: false },
  { name: 'Emerald I', tier: 'emerald', division: 'I', absoluteLp: 2300, color: 'rgba(80, 200, 120, 0.2)', isTier: false },
  // Diamond
  { name: 'Diamond IV', tier: 'diamond', division: 'IV', absoluteLp: 2400, color: 'rgba(185, 242, 255, 0.4)', isTier: true },
  { name: 'Diamond III', tier: 'diamond', division: 'III', absoluteLp: 2500, color: 'rgba(185, 242, 255, 0.2)', isTier: false },
  { name: 'Diamond II', tier: 'diamond', division: 'II', absoluteLp: 2600, color: 'rgba(185, 242, 255, 0.2)', isTier: false },
  { name: 'Diamond I', tier: 'diamond', division: 'I', absoluteLp: 2700, color: 'rgba(185, 242, 255, 0.2)', isTier: false },
  // Master+
  { name: 'Master', tier: 'master', division: '', absoluteLp: 2800, color: 'rgba(147, 51, 234, 0.4)', isTier: true }
]

// Load rank emblem images
const rankImages = ref({})
onMounted(() => {
  const tiers = ['iron', 'bronze', 'silver', 'gold', 'platinum', 'emerald', 'diamond', 'master']
  tiers.forEach(tier => {
    const img = new Image()
    img.src = `/assets/ranked/emblem-${tier}.png`
    rankImages.value[tier] = img
  })
})

// Build annotations for promotions, demotions, and rank boundaries
const annotations = computed(() => {
  if (!hasData.value) return {}

  const result = {}

  // Add horizontal rank boundary lines (tiers and divisions)
  const minAbsoluteLp = Math.min(...props.data.map(point => point.absoluteLp))
  const maxAbsoluteLp = Math.max(...props.data.map(point => point.absoluteLp))

  RANK_BOUNDARIES.forEach(rank => {
    // Only show rank lines that are within the data range
    if (rank.absoluteLp >= minAbsoluteLp && rank.absoluteLp <= maxAbsoluteLp) {
      const img = rankImages.value[rank.tier]

      // Add horizontal line
      result[`rank-line-${rank.name}`] = {
        type: 'line',
        scaleID: 'y',
        value: rank.absoluteLp,
        borderColor: rank.color,
        borderWidth: rank.isTier ? 5 : 3,
        borderDash: rank.isTier ? [3, 3] : [2, 2],
        label: {
          display: true,
          content: rank.isTier ? rank.tier.toUpperCase() : rank.division,
          position: 'start',
          backgroundColor: rank.isTier ? 'rgba(0, 0, 0, 0.8)' : 'rgba(0, 0, 0, 0.6)',
          color: '#ffffff',
          font: { size: rank.isTier ? 10 : 8, weight: rank.isTier ? 'bold' : 'normal' },
          padding: rank.isTier ? 4 : 2,
          xAdjust: rank.isTier && img && img.complete ? 20 : 0 // Make room for icon
        }
      }
    }
  })

  return result
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index', intersect: false },
  plugins: {
    legend: { display: false },
    annotation: {
      annotations: annotations.value
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
        title: (items) => `Game ${props.data[items[0].dataIndex]?.gameIndex}`,
        label: (context) => {
          const point = props.data[context.dataIndex]
          const date = new Date(point.timestamp).toLocaleDateString('en-US', {
            month: 'short', day: 'numeric', year: 'numeric'
          })
          const lines = [`${point.rank} - ${point.currentLp} LP`]
          // Show LP change if available (not first game)
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
    // Y-axis hidden but still used for positioning (absoluteLp values)
    y: { display: false, min: yAxisMin.value, max: yAxisMax.value, grid: { display: false } }
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

