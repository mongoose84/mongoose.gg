<template>
  <div class="radar-chart" data-testid="radar-chart">
    <div v-if="loading" class="loading-state" data-testid="radar-loading">
      <div class="skeleton-radar" aria-hidden="true"></div>
    </div>

    <div v-else-if="!hasData" class="empty-state" data-testid="radar-empty">
      <p class="empty-text">No performance data available</p>
      <p class="empty-subtext">Play some games to see your performance profile</p>
    </div>

    <div v-else class="chart-container" data-testid="radar-content">
      <div class="chart-wrapper" role="img" :aria-label="chartAriaLabel">
        <Radar :data="chartData" :options="chartOptions" />
      </div>
      <p class="games-context" data-testid="radar-games-context">Based on {{ gamesAnalyzed }} games</p>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { Radar } from 'vue-chartjs'

const props = defineProps({
  axes: {
    type: Array,
    default: () => []
  },
  gamesAnalyzed: {
    type: Number,
    default: 0
  },
  loading: {
    type: Boolean,
    default: false
  }
})

const hasData = computed(() => props.axes.length > 0 && props.gamesAnalyzed > 0)

const chartAriaLabel = computed(() => {
  if (!hasData.value) {
    return 'Performance radar chart unavailable'
  }

  const values = props.axes
    .map((axis) => `${axis.label} ${formatScore(axis.value)}`)
    .join(', ')
  return `Performance radar chart based on ${props.gamesAnalyzed} games: ${values}`
})

const themeColors = computed(() => {
  if (typeof window === 'undefined') {
    return {
      primary: '#6d28d9',
      primarySoft: 'rgba(109, 40, 217, 0.1)',
      text: '#ffffff',
      textSecondary: '#888888',
      border: 'rgba(109, 40, 217, 0.15)'
    }
  }

  const rootStyles = getComputedStyle(document.documentElement)
  return {
    primary: rootStyles.getPropertyValue('--color-primary').trim() || '#6d28d9',
    primarySoft: rootStyles.getPropertyValue('--color-primary-soft').trim() || 'rgba(109, 40, 217, 0.1)',
    text: rootStyles.getPropertyValue('--color-text').trim() || '#ffffff',
    textSecondary: rootStyles.getPropertyValue('--color-text-secondary').trim() || '#888888',
    border: rootStyles.getPropertyValue('--color-border').trim() || 'rgba(109, 40, 217, 0.15)'
  }
})

const chartData = computed(() => ({
  labels: props.axes.map((axis) => axis.label),
  datasets: [
    {
      label: 'Performance',
      data: props.axes.map((axis) => axis.value),
      backgroundColor: themeColors.value.primarySoft,
      borderColor: themeColors.value.primary,
      borderWidth: 2,
      pointBackgroundColor: themeColors.value.primary,
      pointBorderColor: themeColors.value.text,
      pointBorderWidth: 1,
      pointRadius: 4,
      pointHoverRadius: 6
    }
  ]
}))

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: true,
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (context) => {
          const axis = props.axes[context.dataIndex]
          if (!axis) return ''
          const rawMetric = axis.rawUnit || 'Raw value'
          const rawValue = axis.rawValue ?? 'N/A'
          return `${axis.label}: ${formatScore(axis.value)} — ${rawMetric}: ${rawValue}`
        }
      }
    }
  },
  scales: {
    r: {
      min: 0,
      max: 100,
      ticks: {
        stepSize: 20,
        color: themeColors.value.textSecondary,
        backdropColor: 'transparent'
      },
      grid: {
        color: themeColors.value.border
      },
      angleLines: {
        color: themeColors.value.border
      },
      pointLabels: {
        color: themeColors.value.text,
        font: {
          size: 12
        }
      }
    }
  }
}))

function formatScore(score) {
  if (typeof score !== 'number') return '0.0'
  return score.toFixed(1)
}
</script>

<style scoped>
.radar-chart {
  min-height: 400px;
}

.loading-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 400px;
  gap: var(--spacing-xs);
}

.skeleton-radar {
  width: min(400px, 100%);
  height: 400px;
  border-radius: 50%;
  background: linear-gradient(
    90deg,
    var(--color-surface) 0%,
    var(--color-elevated) 50%,
    var(--color-surface) 100%
  );
  background-size: 200% 100%;
  animation: radar-shimmer 1.5s infinite;
}

.empty-text {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.empty-subtext {
  margin: 0;
  color: var(--color-text-secondary);
  opacity: 0.8;
  font-size: var(--font-size-xs);
}

.chart-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-sm);
}

.chart-wrapper {
  width: min(500px, 100%);
  height: 380px;
}

.chart-wrapper :deep(canvas) {
  transform: translateX(40px);
}

.games-context {
  width: min(500px, 100%);
  margin: 0;
  text-align: center;
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
}

@keyframes radar-shimmer {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}
</style>
