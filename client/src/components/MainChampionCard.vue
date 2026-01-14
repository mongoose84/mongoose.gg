<template>
  <section class="main-champion-card">
    <header class="header">
      <h2 class="title">Main Champions by Role</h2>
      <p class="subtitle">Top picks based on your performance in the selected queue and time range.</p>
    </header>

    <div v-if="hasData" class="content">
      <div class="role-tabs" role="tablist">
        <button
          v-for="role in roles"
          :key="role"
          type="button"
          class="role-pill"
          :class="{ active: role === selectedRole }"
          @click="selectRole(role)"
          role="tab"
          :aria-selected="role === selectedRole"
        >
          {{ roleLabel(role) }}
        </button>
      </div>

      <Transition name="fade-slide">
        <div
          v-if="selectedRole"
          :key="selectedRole"
          class="champion-cards"
          role="tabpanel"
        >
          <article
            v-for="(champion, index) in championsForSelectedRole"
            :key="champion.championId"
            :class="['champion-card', { recommended: index === 0 }]"
          >
            <div class="champion-header">
              <div class="champion-main">
                <img
                  class="champion-icon"
                  :src="getChampionIconUrl(champion.championName)"
                  :alt="`${champion.championName} icon`"
                />
                <div class="champion-meta">
                  <div class="champion-name-row">
                    <span class="champion-name">{{ champion.championName }}</span>
                    <span class="role-badge">{{ roleLabel(selectedRole) }}</span>
                  </div>
                  <span v-if="index === 0" class="recommended-badge">Recommended</span>
                </div>
              </div>
            </div>
            <div class="champion-stats">
              <div class="stat">
                <span class="stat-value">{{ formatWinRate(champion.winRate) }}</span>
                <span class="stat-label">Win Rate</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ formatLpPerGame(champion.lpPerGame) }}</span>
                <span class="stat-label">LP / game</span>
              </div>
              <div class="stat">
                <span class="stat-value">{{ champion.wins }}-{{ champion.losses }}</span>
                <span class="stat-label">{{ champion.gamesPlayed }} games</span>
              </div>
            </div>
          </article>
        </div>
      </Transition>
    </div>

    <div v-else class="empty-state">
      <p>No champion data yet for this filter. Play some games to see your best picks.</p>
    </div>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'

const props = defineProps({
  mainChampions: {
    type: Array,
    default: () => []
  }
})

const selectedRole = ref(null)

const roles = computed(() => props.mainChampions.map((role) => role.role))

const hasData = computed(() => props.mainChampions && props.mainChampions.length > 0)

watch(
  () => props.mainChampions,
  (newVal) => {
    if (!newVal || newVal.length === 0) {
      selectedRole.value = null
      return
    }

    // Keep the current selection if it still exists
    if (selectedRole.value && newVal.some((r) => r.role === selectedRole.value)) {
      return
    }

    // Default to the most-played role (by total games across its top champs)
    let bestRole = newVal[0].role
    let bestGames = -1

    for (const roleEntry of newVal) {
      const totalGames = (roleEntry.champions || []).reduce(
        (sum, c) => sum + (c.gamesPlayed || 0),
        0
      )
      if (totalGames > bestGames) {
        bestGames = totalGames
        bestRole = roleEntry.role
      }
    }

    selectedRole.value = bestRole
  },
  { immediate: true, deep: true }
)

function selectRole(role) {
  selectedRole.value = role
}

// Data Dragon version for champion icons (kept in sync with ProfileHeaderCard)
const ddVersion = '16.1.1'

function normalizeChampionName(name) {
  if (!name) return ''
  // Remove spaces, punctuation, etc. (e.g., "Cho'Gath" -> "ChoGath")
  return name.replace(/[^A-Za-z0-9]/g, '')
}

function getChampionIconUrl(name) {
  const normalized = normalizeChampionName(name)
  return `https://ddragon.leagueoflegends.com/cdn/${ddVersion}/img/champion/${normalized}.png`
}

function roleLabel(role) {
  const map = {
    TOP: 'Top',
    JUNGLE: 'Jungle',
    MIDDLE: 'Mid',
    BOTTOM: 'ADC',
    UTILITY: 'Support'
  }
  return map[role] || role
}

const championsForSelectedRole = computed(() => {
  if (!selectedRole.value) return []
  const entry = props.mainChampions.find((r) => r.role === selectedRole.value)
  return entry ? entry.champions || [] : []
})

function formatWinRate(value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  return `${value.toFixed(1)}%`
}

function formatLpPerGame(value) {
  if (value === null || value === undefined || Number.isNaN(value)) return '--'
  const rounded = value.toFixed(1)
  const sign = value > 0 ? '+' : ''
  return `${sign}${rounded}`
}
</script>

<style scoped>
.main-champion-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
}

.header {
  margin-bottom: var(--spacing-md);
}

.title {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.subtitle {
  margin: 4px 0 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.role-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-md);
}

.role-pill {
  padding: 6px 12px;
  border-radius: 999px;
  border: 1px solid var(--color-border);
  background: var(--color-elevated);
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: all 0.15s ease;
}

.role-pill:hover {
  border-color: var(--color-primary);
  color: var(--color-text);
}

.role-pill.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: #fff;
}

.champion-cards {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-md);
}

.champion-card {
  flex: 1 1 220px;
  min-width: 0;
  padding: var(--spacing-md);
  border-radius: var(--radius-md);
  background: var(--color-elevated);
  border: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
}

.champion-card.recommended {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-sm);
  transform: translateY(-1px);
}

.champion-card.recommended:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.champion-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.champion-main {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.champion-icon {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-sm);
  object-fit: cover;
}

.champion-meta {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.champion-name-row {
  display: flex;
  align-items: center;
  gap: 6px;
}

.champion-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.role-badge {
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  padding: 2px 6px;
  border-radius: var(--radius-sm);
  background: rgba(148, 163, 184, 0.2);
  color: var(--color-text-secondary);
}

.recommended-badge {
  font-size: var(--font-size-2xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-primary);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

.champion-stats {
  display: flex;
  justify-content: space-between;
  gap: var(--spacing-md);
  margin-top: var(--spacing-sm);
}

.stat {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.stat-value {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.stat-label {
  font-size: var(--font-size-2xs);
  color: var(--color-text-secondary);
}

.empty-state {
  padding-top: var(--spacing-sm);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Tab switching animation */
.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-slide-enter-from,
.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(4px);
}

@media (max-width: 768px) {
  .champion-cards {
    flex-direction: column;
  }

  .champion-card {
    flex: 1 1 auto;
  }
}
</style>
