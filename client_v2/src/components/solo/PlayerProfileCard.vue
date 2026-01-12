<template>
  <div class="profile-card">
    <div class="profile-header">
      <!-- Summoner Icon -->
      <div class="summoner-icon">
        <img 
          v-if="account?.profileIconId"
          :src="`https://ddragon.leagueoflegends.com/cdn/14.24.1/img/profileicon/${account.profileIconId}.png`"
          :alt="account.gameName"
          class="icon-img"
        />
        <div v-else class="icon-placeholder">
          {{ account?.gameName?.charAt(0) || '?' }}
        </div>
      </div>

      <!-- Player Info -->
      <div class="player-info">
        <h2 class="player-name">
          {{ account?.gameName }}<span class="tag-line">#{{ account?.tagLine }}</span>
        </h2>
        <div class="player-meta">
          <span class="region-badge">{{ getRegionLabel(account?.region) }}</span>
          <span v-if="stats?.rank" class="rank-badge" :class="getRankClass(stats.rank)">
            {{ stats.rank }}
          </span>
        </div>
      </div>
    </div>

    <!-- Quick Stats -->
    <div class="quick-stats" v-if="stats">
      <div class="stat-item">
        <span class="stat-value">{{ stats.gamesPlayed || 0 }}</span>
        <span class="stat-label">Games</span>
      </div>
      <div class="stat-item">
        <span class="stat-value" :class="getWinrateClass(stats.winRate)">
          {{ formatPercent(stats.winRate) }}
        </span>
        <span class="stat-label">Win Rate</span>
      </div>
      <div class="stat-item">
        <span class="stat-value">{{ formatKDA(stats.avgKda) }}</span>
        <span class="stat-label">KDA</span>
      </div>
      <div class="stat-item">
        <span class="stat-value">{{ formatDuration(stats.avgGameDurationMinutes) }}</span>
        <span class="stat-label">Avg Duration</span>
      </div>
    </div>

    <!-- Loading skeleton -->
    <div v-else-if="loading" class="quick-stats skeleton">
      <div class="stat-item skeleton-item" v-for="i in 4" :key="i"></div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  account: {
    type: Object,
    default: null
  },
  stats: {
    type: Object,
    default: null
  },
  loading: {
    type: Boolean,
    default: false
  }
})

const regionLabels = {
  euw1: 'EUW', eun1: 'EUNE', na1: 'NA', kr: 'KR', jp1: 'JP',
  br1: 'BR', la1: 'LAN', la2: 'LAS', oc1: 'OCE', tr1: 'TR', ru: 'RU'
}

function getRegionLabel(region) {
  return regionLabels[region] || region?.toUpperCase() || 'Unknown'
}

function getRankClass(rank) {
  if (!rank) return ''
  const tier = rank.split(' ')[0].toLowerCase()
  return `rank-${tier}`
}

function getWinrateClass(winRate) {
  if (winRate >= 55) return 'winrate-high'
  if (winRate >= 50) return 'winrate-good'
  return 'winrate-low'
}

function formatPercent(value) {
  if (value == null) return '0%'
  return `${Math.round(value)}%`
}

function formatKDA(kda) {
  if (kda == null) return '0.00'
  return kda.toFixed(2)
}

function formatDuration(minutes) {
  if (minutes == null) return '0:00'
  const mins = Math.floor(minutes)
  const secs = Math.round((minutes - mins) * 60)
  return `${mins}:${secs.toString().padStart(2, '0')}`
}
</script>

<style scoped>
.profile-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-xl);
  margin-bottom: var(--spacing-xl);
}

.profile-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
}

.summoner-icon {
  width: 80px;
  height: 80px;
  border-radius: var(--radius-lg);
  overflow: hidden;
  border: 3px solid var(--color-border);
}

.icon-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.icon-placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-surface-hover);
  color: var(--color-text);
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
}

.player-name {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  margin-bottom: var(--spacing-xs);
}

.tag-line {
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-normal);
}

.player-meta {
  display: flex;
  gap: var(--spacing-sm);
}

.region-badge, .rank-badge {
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
}

.region-badge {
  background: var(--color-surface-hover);
  color: var(--color-text-secondary);
}

.rank-badge {
  background: rgba(168, 85, 247, 0.2);
  color: #a855f7;
}

.rank-iron { background: rgba(139, 90, 43, 0.2); color: #8b5a2b; }
.rank-bronze { background: rgba(205, 127, 50, 0.2); color: #cd7f32; }
.rank-silver { background: rgba(192, 192, 192, 0.2); color: #c0c0c0; }
.rank-gold { background: rgba(255, 215, 0, 0.2); color: #ffd700; }
.rank-platinum { background: rgba(0, 168, 168, 0.2); color: #00a8a8; }
.rank-emerald { background: rgba(80, 200, 120, 0.2); color: #50c878; }
.rank-diamond { background: rgba(185, 242, 255, 0.2); color: #b9f2ff; }
.rank-master { background: rgba(148, 0, 211, 0.2); color: #9400d3; }
.rank-grandmaster { background: rgba(255, 69, 0, 0.2); color: #ff4500; }
.rank-challenger { background: rgba(0, 191, 255, 0.2); color: #00bfff; }

.quick-stats {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-md);
  padding-top: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
}

.stat-item {
  text-align: center;
}

.stat-value {
  display: block;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  margin-bottom: 2px;
}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.winrate-high { color: #22c55e; }
.winrate-good { color: #3b82f6; }
.winrate-low { color: #ef4444; }

/* Skeleton loading */
.skeleton .skeleton-item {
  height: 48px;
  background: linear-gradient(90deg, var(--color-surface-hover) 25%, var(--color-surface) 50%, var(--color-surface-hover) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-sm);
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

@media (max-width: 600px) {
  .quick-stats {
    grid-template-columns: repeat(2, 1fr);
  }

  .profile-header {
    flex-direction: column;
    text-align: center;
  }
}
</style>

