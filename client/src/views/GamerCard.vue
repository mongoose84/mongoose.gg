<template>
  <div class="gamer-card" :aria-label="`${gamer.gamerName} #${gamer.tagline}`">
    <div class="icon-wrap">
      <img
        class="icon"
        :src="gamer.iconUrl"
        :alt="`${gamer.gamerName} profile icon`"
        width="72"
        height="72"
        loading="lazy"
      />
    </div>

    <div class="level">Level {{ gamer.level }}</div>

    <div class="name">
      {{ gamer.gamerName }}<span class="tag">#{{ gamer.tagline }}</span>
    </div>

    <div
      class="chart"
      role="img"
      :aria-label="`Wins ${wins}, Losses ${losses}`"
      :style="{ width: size + 'px', height: size + 'px' }"
    >
      <svg :width="size" :height="size" :viewBox="`0 0 ${size} ${size}`">
        <g :transform="`rotate(-90 ${center} ${center})`">
          <!-- Losses ring (background) -->
          <circle
            :cx="center" :cy="center" :r="radius"
            :stroke="lossColor" :stroke-width="strokeWidth"
            fill="none"
          />
          <!-- Wins arc -->
          <circle
            :cx="center" :cy="center" :r="radius"
            :stroke="winColor" :stroke-width="strokeWidth"
            fill="none"
            :stroke-dasharray="dashArray"
            stroke-linecap="butt"
          />
        </g>
      </svg>
      <div class="chart-label">{{ winRate }}%</div>
    </div>

    <div class="game-info">{{ total }}G {{ wins }}W {{ losses }}L</div>
    <div class="kda">
      <span :class="killsClass">{{ avgKills }}</span>
      <span class="kda-sep">/</span>
      <span :class="deathsClass">{{ avgDeaths }}</span>
      <span class="kda-sep">/</span>
      <span :class="assistsClass">{{ avgAssists }}</span>
    </div>
    <div class="per-minute" role="contentinfo" :aria-label="`${csPerMin} CS/min,  ${goldPerMin} G/min`">
      <span class="pm-item">{{ csPerMin }} CS/min </span>
      <span class="pm-sep">•</span>
      <span class="pm-item">{{ goldPerMin }} G/min</span>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  gamer: {
    type: Object,
    required: true
  }
})

const size = 120
const strokeWidth = 12
const radius = (size - strokeWidth) / 2
const center = size / 2

const stats = computed(() => props.gamer?.stats ?? {})

const total = computed(() => Number(stats.value?.totalMatches ?? 0))

const circumference = 2 * Math.PI * radius
const wins = computed(() => Number(stats.value?.wins ?? 0))
const losses = computed(() => Math.max(0, total.value - wins.value))

const winRate = computed(() => {
  if (!total.value) return 0
  return Math.round((wins.value / total.value) * 100)
})

const dashArray = computed(() => {
  if (!total.value) return `0 ${circumference}`
  const winLen = (wins.value / total.value) * circumference
  const rest = Math.max(0, circumference - winLen)
  return `${winLen} ${rest}`
})

const winColor = 'var(--color-success)'
const lossColor = 'var(--color-danger)'

const avgKills = computed(() => {
  const tk = Number(stats.value?.totalKills ?? 0)
  if (!total.value) return '0.0'
  return (tk / total.value).toFixed(1)
})

const avgDeaths = computed(() => {
  const td = Number(stats.value?.totalDeaths ?? 0)
  if (!total.value) return '0.0'
  return (td / total.value).toFixed(1)
})

const avgAssists = computed(() => {
  const ta = Number(stats.value?.totalAssists ?? 0)
  if (!total.value) return '0.0'
  return (ta / total.value).toFixed(1)
})

// Per-minute metrics based on total playtime in seconds
const csPerMin = computed(() => {
  const cs = Number(stats.value?.totalCreepScore ?? 0)
  const secs = Number(stats.value?.totalDurationPlayedSeconds ?? 0)
  if (!secs) return '0.0'
  return ((cs * 60) / secs).toFixed(1)
})

const goldPerMin = computed(() => {
  const gold = Number(stats.value?.totalGoldEarned ?? 0)
  const secs = Number(stats.value?.totalDurationPlayedSeconds ?? 0)
  if (!secs) return '0.0'
  return ((gold * 60) / secs).toFixed(1)
})

// Numeric averages for classification
const avgKillsNum = computed(() => {
  const tk = Number(stats.value?.totalKills ?? 0)
  if (!total.value) return 0
  return tk / total.value
})

const avgDeathsNum = computed(() => {
  const td = Number(stats.value?.totalDeaths ?? 0)
  if (!total.value) return 0
  return td / total.value
})

const avgAssistsNum = computed(() => {
  const ta = Number(stats.value?.totalAssists ?? 0)
  if (!total.value) return 0
  return ta / total.value
})

// KDA color classes
const killsClass = computed(() => {
  if (avgKillsNum.value > 8) return 'kda-good'
  if (avgKillsNum.value > 4) return 'kda-warn'
  return 'kda-bad'
})

const deathsClass = computed(() => {
  if (avgDeathsNum.value < 6) return 'kda-good'
  if (avgDeathsNum.value < 8) return 'kda-warn'
  return 'kda-bad'
})

const assistsClass = computed(() => {
  return avgAssistsNum.value > 10 ? 'kda-good' : ''
})
</script>

<style scoped>
.gamer-card {
  width: 260px;
  aspect-ratio: 64 / 104; /* longer card */
  box-sizing: border-box;
  padding: 0.75rem 0.75rem 1.35rem; /* a bit more bottom padding */
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background-color: var(--color-bg-elev);
  color: var(--color-text);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  transition: background-color 0.15s ease;
}

.gamer-card:hover {
  background-color: var(--color-bg-hover);
}

.icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
}

.icon {
  width: 72px;
  height: 72px;
  border-radius: 8px;
  border: 1px solid var(--color-border);
  object-fit: cover;
  background: var(--color-bg);
}

.level {
  margin-top: 0.25rem;
  font-size: 0.95rem;
  opacity: 0.9;
}

.name {
  margin-top: 0.2rem;
  font-weight: 600;
}

.tag {
  margin-left: 0.35rem;
  opacity: 0.8;
  font-weight: 500;
}

.chart {
  margin-top: 0.4rem;
  position: relative;
}

.chart-label {
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  font-size: 0.85rem;
  color: var(--color-text);
  text-align: center;
  pointer-events: none;
}

.game-info {
  margin-top: 0.6rem; /* larger vertical gap */
  font-size: 0.9rem;
  opacity: 0.9;
}

.kda {
  margin-top: 1.1rem; /* moved down by +0.5rem */
  font-size: 1.1rem; /* slightly bigger */
  font-weight: 500;
}

.kda-sep {
  margin: 0 0.25rem;
  opacity: 0.7;
}

.kda-good { color: var(--color-success); }
.kda-bad { color: var(--color-danger); }
.kda-warn { color: #e0b000; }

.per-minute {
  margin-top: 1.15rem; /* moved down by +0.5rem */
  font-size: 0.9rem;
  opacity: 0.95;
}

.pm-sep {
  margin: 0 0.35rem;
  opacity: 0.6;
}
</style>