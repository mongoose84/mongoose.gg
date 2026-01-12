<template>
  <div class="role-breakdown card">
    <div class="card-header">
      <h3 class="card-title">Role Breakdown</h3>
    </div>

    <div v-if="loading" class="loading-skeleton">
      <div class="skeleton-role" v-for="i in 5" :key="i"></div>
    </div>

    <div v-else-if="roles && roles.length" class="roles-content">
      <div
        v-for="role in sortedRoles"
        :key="role.role"
        class="role-item"
      >
        <div class="role-header">
          <span class="role-icon">{{ getRoleIcon(role.role) }}</span>
          <span class="role-name">{{ getRoleName(role.role) }}</span>
          <span class="role-games">{{ role.gamesPlayed }} games</span>
        </div>
        
        <div class="role-bar-container">
          <div
            class="role-bar"
            :style="{ width: getBarWidth(role.gamesPlayed) + '%' }"
            :class="getRoleClass(role.role)"
          ></div>
        </div>

        <div class="role-stats">
          <span class="role-winrate" :class="getWinrateClass(role.winRate)">
            {{ formatPercent(role.winRate) }} WR
          </span>
          <span class="role-kda">{{ formatKDA(role.avgKda) }} KDA</span>
        </div>

        <!-- Top Champions for this role -->
        <div v-if="role.topChampions?.length" class="role-champions">
          <div 
            v-for="champ in role.topChampions.slice(0, 3)" 
            :key="champ.championId"
            class="champion-mini"
            :title="`${champ.championName}: ${champ.games} games, ${formatPercent(champ.winRate)} WR`"
          >
            <img 
              :src="getChampionIcon(champ.championId)"
              :alt="champ.championName"
              class="champion-icon"
            />
          </div>
        </div>
      </div>
    </div>

    <div v-else class="empty-state">
      <p>No role data available</p>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  roles: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  }
})

const sortedRoles = computed(() => {
  if (!props.roles) return []
  return [...props.roles].sort((a, b) => b.gamesPlayed - a.gamesPlayed)
})

const maxGames = computed(() => {
  if (!sortedRoles.value.length) return 1
  return Math.max(...sortedRoles.value.map(r => r.gamesPlayed))
})

const roleIcons = {
  TOP: '🛡️',
  JUNGLE: '🌲',
  MIDDLE: '⚡',
  BOTTOM: '🏹',
  UTILITY: '💚'
}

const roleNames = {
  TOP: 'Top',
  JUNGLE: 'Jungle',
  MIDDLE: 'Mid',
  BOTTOM: 'ADC',
  UTILITY: 'Support'
}

function getRoleIcon(role) {
  return roleIcons[role] || '❓'
}

function getRoleName(role) {
  return roleNames[role] || role
}

function getRoleClass(role) {
  return `role-${role?.toLowerCase()}`
}

function getBarWidth(gamesPlayed) {
  return (gamesPlayed / maxGames.value) * 100
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

function getChampionIcon(championId) {
  // Use Data Dragon for champion icons
  // Note: This requires champion name, not ID. For now use placeholder
  return `https://ddragon.leagueoflegends.com/cdn/14.24.1/img/champion/placeholder.png`
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

.roles-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.role-item {
  padding: var(--spacing-sm);
  background: var(--color-surface-hover);
  border-radius: var(--radius-md);
}

.role-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-xs);
}

.role-icon {
  font-size: var(--font-size-md);
}

.role-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
  flex: 1;
}

.role-games {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.role-bar-container {
  height: 4px;
  background: var(--color-border);
  border-radius: 2px;
  margin-bottom: var(--spacing-xs);
  overflow: hidden;
}

.role-bar {
  height: 100%;
  border-radius: 2px;
  transition: width 0.3s ease;
}

.role-top { background: #f59e0b; }
.role-jungle { background: #22c55e; }
.role-middle { background: #3b82f6; }
.role-bottom { background: #ef4444; }
.role-utility { background: #a855f7; }

.role-stats {
  display: flex;
  gap: var(--spacing-md);
  font-size: var(--font-size-xs);
}

.role-winrate {
  font-weight: var(--font-weight-medium);
}

.role-kda {
  color: var(--color-text-secondary);
}

.winrate-high { color: #22c55e; }
.winrate-good { color: #3b82f6; }
.winrate-low { color: #ef4444; }

.role-champions {
  display: flex;
  gap: 4px;
  margin-top: var(--spacing-xs);
}

.champion-mini {
  width: 24px;
  height: 24px;
  border-radius: var(--radius-sm);
  overflow: hidden;
  border: 1px solid var(--color-border);
}

.champion-icon {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* Loading skeleton */
.loading-skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.skeleton-role {
  height: 60px;
  background: linear-gradient(90deg, var(--color-surface-hover) 25%, var(--color-surface) 50%, var(--color-surface-hover) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-md);
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

