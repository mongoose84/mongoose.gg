<template>
  <div class="comparison-strip" role="region" aria-label="Quick comparison stats">
    <div v-if="loading" class="strip-loading">Loading comparison…</div>
    <div v-else-if="error" class="strip-error">{{ error }}</div>
    <div v-else-if="hasData" class="strip-content">
      <div 
        class="stat-item" 
        :class="getColorClass(stats.winrate.difference)"
        :title="buildTooltip('Winrate', data.winrate, '%', 1)"
      >
        <span class="stat-label">Winrate</span>
        <span class="stat-value">{{ stats.winrate.bestGamer }} +{{ formatNumber(stats.winrate.difference, 1) }}%</span>
      </div>

      <div class="separator">|</div>

      <div 
        class="stat-item"
        :class="getColorClass(stats.kda.difference)"
        :title="buildTooltip('KDA', data.kda, ' KDA', 2)"
      >
        <span class="stat-label">KDA</span>
        <span class="stat-value">{{ stats.kda.bestGamer }} +{{ formatNumber(stats.kda.difference, 2) }} KDA</span>
      </div>

      <div class="separator">|</div>

      <div 
        class="stat-item"
        :class="getColorClass(stats.csPrMin.difference)"
        :title="buildTooltip('CS/min', data.csPrMin, ' CS/min', 1)"
      >
        <span class="stat-label">CS/min</span>
        <span class="stat-value">{{ stats.csPrMin.bestGamer }} +{{ formatNumber(stats.csPrMin.difference, 1) }} CS/min</span>
      </div>

      <div class="separator">|</div>

      <div 
        class="stat-item"
        :class="getColorClass(stats.goldPrMin.difference)"
        :title="buildTooltip('Gold/min', data.goldPrMin, ' G/min', 0)"
      >
        <span class="stat-label">Gold/min</span>
        <span class="stat-value">{{ stats.goldPrMin.bestGamer }} +{{ formatNumber(stats.goldPrMin.difference, 0) }} G/min</span>
      </div>

      <div class="separator">|</div>

      <div 
        class="stat-item"
        :class="getColorClass(stats.gamesPlayed.difference)"
        :title="buildTooltip('Games', data.gamesPlayed, ' games', 0)"
      >
        <span class="stat-label">Games</span>
        <span class="stat-value">{{ stats.gamesPlayed.bestGamer }} +{{ stats.gamesPlayed.difference }} games</span>
      </div>
    </div>
    <div v-else class="strip-empty">No comparison data available.</div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import getComparison from '@/assets/getComparison.js'

const props = defineProps({
  userId: { type: [String, Number], required: true }
})

const loading = ref(false)
const error = ref(null)
const data = ref(null)

async function load() {
  if (props.userId === undefined || props.userId === null || props.userId === '') return
  loading.value = true
  error.value = null
  try {
    data.value = await getComparison(props.userId)
  } catch (e) {
    error.value = e?.message || 'Failed to load comparison data'
    data.value = null
  } finally {
    loading.value = false
  }
}

onMounted(load)
watch(() => props.userId, load)

const hasData = computed(() => {
  return data.value && (
    data.value.winrate?.length ||
    data.value.kda?.length ||
    data.value.csPrMin?.length ||
    data.value.goldPrMin?.length ||
    data.value.gamesPlayed?.length
  )
})

// Calculate stats from the new array format
const stats = computed(() => {
  const calcStat = (arr) => {
    if (!arr || arr.length === 0) return { difference: 0, bestGamer: '' }
    const sorted = [...arr].sort((a, b) => b.value - a.value)
    const highest = sorted[0]
    const lowest = sorted[sorted.length - 1]
    return {
      difference: highest.value - lowest.value,
      bestGamer: highest.gamerName || ''
    }
  }

  return {
    winrate: calcStat(data.value?.winrate),
    kda: calcStat(data.value?.kda),
    csPrMin: calcStat(data.value?.csPrMin),
    goldPrMin: calcStat(data.value?.goldPrMin),
    gamesPlayed: calcStat(data.value?.gamesPlayed)
  }
})

function formatNumber(value, decimals = 1) {
  if (value === undefined || value === null) return '0'
  return Number(value).toFixed(decimals)
}

function buildTooltip(label, arr, suffix, decimals) {
  if (!arr || arr.length === 0) return `${label}: No data`
  const sorted = [...arr].sort((a, b) => b.value - a.value)
  const lines = sorted.map((item, idx) => {
    const prefix = idx === 0 ? '🏆 ' : '   '
    return `${prefix}${item.gamerName}: ${formatNumber(item.value, decimals)}${suffix}`
  })
  return `${label}\n${lines.join('\n')}`
}

function getColorClass(difference) {
  if (difference === undefined || difference === null || difference === 0) return 'neutral'
  return difference > 0 ? 'positive' : 'negative'
}
</script>

<style scoped>
.comparison-strip {
  width: 100%;
  padding: 0.6rem 1rem;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background-color: var(--color-bg-elev);
  color: var(--color-text);
}

.strip-content {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem 0.5rem;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0.25rem 0.5rem;
  cursor: default;
  transition: opacity 0.15s ease;
  flex: 1;
  min-width: 120px;
}

.stat-item:hover {
  opacity: 0.85;
}

.stat-label {
  font-size: 0.7rem;
  text-transform: uppercase;
  opacity: 0.7;
  letter-spacing: 0.03em;
}

.stat-value {
  font-size: 0.9rem;
  font-weight: 600;
  white-space: nowrap;
}

.separator {
  color: var(--color-border);
  opacity: 0.5;
  font-size: 1rem;
  user-select: none;
  flex-shrink: 0;
}

/* Color coding */
.positive .stat-value {
  color: var(--color-text);
}

.negative .stat-value {
  color: var(--color-danger, #ef4444);
}

.neutral .stat-value {
  color: var(--color-text);
  opacity: 0.7;
}

.strip-loading,
.strip-error,
.strip-empty {
  text-align: center;
  font-size: 0.85rem;
  opacity: 0.8;
  padding: 0.25rem;
}

.strip-error {
  color: var(--color-danger, #ef4444);
}

/* Responsive: stack vertically on small screens */
@media (max-width: 768px) {
  .strip-content {
    flex-direction: column;
    gap: 0.4rem;
  }
  
  .separator {
    display: none;
  }
  
  .stat-item {
    flex-direction: row;
    gap: 0.5rem;
    width: 100%;
    justify-content: space-between;
    padding: 0.3rem 0.5rem;
    border-bottom: 1px solid var(--color-border);
  }
  
  .stat-item:last-child {
    border-bottom: none;
  }
}
</style>
