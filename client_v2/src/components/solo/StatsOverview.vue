<template>
  <div class="stats-overview card">
    <div class="card-header">
      <h3 class="card-title">Performance Stats</h3>
    </div>

    <div v-if="loading" class="loading-skeleton">
      <div class="skeleton-row" v-for="i in 5" :key="i"></div>
    </div>

    <div v-else-if="stats" class="stats-content">
      <!-- Win/Loss Record -->
      <div class="stat-row">
        <span class="stat-name">Record</span>
        <span class="stat-value">
          <span class="wins">{{ stats.wins || 0 }}W</span>
          <span class="separator"> - </span>
          <span class="losses">{{ (stats.gamesPlayed || 0) - (stats.wins || 0) }}L</span>
        </span>
      </div>

      <!-- Overall Stats -->
      <div class="stat-row">
        <span class="stat-name">Win Rate</span>
        <span class="stat-value" :class="getWinrateClass(stats.winRate)">{{ formatStat(stats.winRate) }}%</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Avg KDA</span>
        <span class="stat-value">{{ formatStat(stats.avgKda) }}</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Avg Game Duration</span>
        <span class="stat-value">{{ formatDuration(stats.avgGameDurationMinutes) }}</span>
      </div>
      <div class="stat-row">
        <span class="stat-name">Champions Played</span>
        <span class="stat-value">{{ stats.uniqueChampsPlayedCount || 0 }}</span>
      </div>

      <!-- Side Stats -->
      <div v-if="stats.sideStats" class="side-stats">
        <div class="stat-row">
          <span class="stat-name">Blue Side</span>
          <span class="stat-value">
            <span class="blue-side">{{ stats.sideStats.blueWins }}/{{ stats.sideStats.blueGames }}</span>
            <span class="winrate">({{ formatStat(stats.sideStats.blueWinDistribution) }}%)</span>
          </span>
        </div>
        <div class="stat-row">
          <span class="stat-name">Red Side</span>
          <span class="stat-value">
            <span class="red-side">{{ stats.sideStats.redWins }}/{{ stats.sideStats.redGames }}</span>
            <span class="winrate">({{ formatStat(stats.sideStats.redWinDistribution) }}%)</span>
          </span>
        </div>
      </div>

      <!-- Main Champion -->
      <div v-if="stats.mainChampion" class="main-champ-section">
        <div class="stat-row">
          <span class="stat-name">Main Champion</span>
          <span class="stat-value">{{ stats.mainChampion.championName }}</span>
        </div>
        <div class="stat-row sub">
          <span class="stat-name">Games / Win Rate</span>
          <span class="stat-value">{{ stats.mainChampion.picks }} ({{ formatStat(stats.mainChampion.winRate) }}%)</span>
        </div>
      </div>

      <!-- Recent Trend indicator if available -->
      <div v-if="trends" class="trend-section">
        <div class="trend-header">Last {{ trends.games }} Games</div>
        <div class="trend-stats">
          <span class="trend-wr" :class="getWinrateClass(trends.winRate)">{{ formatStat(trends.winRate) }}% WR</span>
          <span class="trend-kda">{{ formatStat(trends.avgKda) }} KDA</span>
        </div>
      </div>
    </div>

    <div v-else class="empty-state">
      <p>No stats available</p>
    </div>
  </div>
</template>

<script setup>
defineProps({
  stats: {
    type: Object,
    default: null
  },
  trends: {
    type: Object,
    default: null
  },
  loading: {
    type: Boolean,
    default: false
  }
})

function formatStat(value) {
  if (value == null) return '0.0'
  return value.toFixed(1)
}

function formatDuration(minutes) {
  if (minutes == null) return '0:00'
  const mins = Math.floor(minutes)
  const secs = Math.round((minutes - mins) * 60)
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

function getWinrateClass(winRate) {
  if (winRate >= 55) return 'winrate-high'
  if (winRate >= 50) return 'winrate-good'
  return 'winrate-low'
}
</script>

<style scoped>
.card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
}

.card-header {
  margin-bottom: var(--spacing-lg);
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.card-title {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.stats-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.stat-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-xs) 0;
}

.stat-name {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.stat-value {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.wins { color: #22c55e; }
.losses { color: #ef4444; }
.separator { color: var(--color-text-secondary); }

.trend-section {
  margin-top: var(--spacing-md);
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-border);
}

.winrate-high { color: #22c55e; }
.winrate-good { color: #3b82f6; }
.winrate-low { color: #ef4444; }

.side-stats, .main-champ-section {
  margin-top: var(--spacing-md);
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-border);
}

.blue-side { color: #3b82f6; }
.red-side { color: #ef4444; }
.winrate { color: var(--color-text-secondary); margin-left: var(--spacing-xs); }

.stat-row.sub .stat-name {
  padding-left: var(--spacing-md);
  font-size: var(--font-size-xs);
}

.trend-section {
  margin-top: var(--spacing-md);
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-border);
}

.trend-header {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-xs);
}

.trend-stats {
  display: flex;
  gap: var(--spacing-md);
  font-weight: var(--font-weight-semibold);
}

.trend-kda {
  color: var(--color-text-secondary);
}

/* Loading skeleton */
.loading-skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.skeleton-row {
  height: 24px;
  background: linear-gradient(90deg, var(--color-surface-hover) 25%, var(--color-surface) 50%, var(--color-surface-hover) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-sm);
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.empty-state {
  text-align: center;
  padding: var(--spacing-xl);
  color: var(--color-text-secondary);
}
</style>

