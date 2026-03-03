<template>
  <section class="overview-account-cards" data-testid="overview-account-cards">
    <div class="header-row">
      <h2 class="section-title">Your Accounts</h2>
      <p class="section-subtitle">Rank tier colors · Activity indicator</p>
    </div>
    <div class="account-cards-container">
      <div
        v-for="account in normalizedAccounts"
        :key="account.accountId"
        :data-testid="`account-card-${account.accountId}`"
        class="account-card"
        :style="{ borderLeftColor: getTierColor(account.rank) }"
      >
        <!-- Game Name + Tag -->
        <div class="account-name">
          <span class="game-name">{{ account.gameName }}</span>
          <span v-if="account.tagLine" class="tag-line">#{{ account.tagLine }}</span>
        </div>

        <!-- Region + Rank -->
        <div class="account-region-rank">
          <span class="region">{{ account.region }}</span>
          <span v-if="account.rank" class="separator">·</span>
          <span v-if="account.rank" class="rank">{{ account.rank }}</span>
        </div>

        <!-- LP -->
        <div v-if="account.lp !== null && account.lp !== undefined" class="account-lp">
          <span class="lp-value">{{ account.lp }} LP</span>
        </div>

        <!-- Games Today -->
        <div class="games-today" :class="{ 'has-games': account.gamesToday > 0 }">
          <span v-if="account.gamesToday > 0" class="activity-dot"></span>
          <span class="games-count">{{ account.gamesToday }} {{ account.gamesToday === 1 ? 'game' : 'games' }} today</span>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  accounts: {
    type: Array,
    required: true,
    default: () => []
  },
  activeAccountPuuid: {
    type: String,
    default: null
  }
})

const emit = defineEmits(['select'])

const tierColorMap = {
  iron: '#6b7280',      // Gray
  bronze: '#d97706',    // Orange
  silver: '#9ca3af',    // Light gray
  gold: '#f59e0b',      // Yellow
  platinum: '#06b6d4',  // Cyan
  diamond: '#a855f7',   // Purple
  master: '#fbbf24',    // Gold
  grandmaster: '#fbbf24', // Gold
  challenger: '#fbbf24' // Gold
}

function getTierColor(rankString) {
  if (!rankString) return 'var(--color-border)'
  const tier = rankString.toLowerCase().split(' ')[0]
  return tierColorMap[tier] || 'var(--color-border)'
}

const normalizedAccounts = computed(() => {
  return (props.accounts || []).map((account, index) => {
    const accountId = account.accountId || account.puuid || account.id || `account-${index}`
    const gameName = account.gameName || account.summonerName || account.name || 'Unknown Account'
    const tagLine = account.tagLine || account.tag || ''
    const region = (account.region || account.server || 'Unknown').toUpperCase()
    const gamesToday = Number.isFinite(account.gamesToday) ? account.gamesToday : 0

    return {
      ...account,
      accountId,
      gameName,
      tagLine,
      region,
      gamesToday
    }
  })
})
</script>

<style scoped>
.overview-account-cards {
  width: 100%;
}

.header-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
}

.section-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.section-subtitle {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.account-cards-container {
  display: flex;
  gap: var(--spacing-md);
  overflow-x: auto;
  padding-bottom: var(--spacing-xs);
  scrollbar-width: thin;
  scrollbar-color: var(--color-border) transparent;
}

.account-card {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  min-width: 160px;
  min-height: 132px;
  padding: var(--spacing-lg);
  background: linear-gradient(135deg, 
    rgba(255, 255, 255, 0.05) 0%,
    var(--color-surface) 100%);
  border: 1px solid var(--color-border);
  border-left: 4px solid;
  border-radius: var(--radius-lg);
  text-align: left;
  flex-shrink: 0;
  box-shadow: var(--shadow-sm);
  transition: all 0.2s ease;
}

.account-name {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: var(--spacing-xs);
  min-width: 0;
}

.game-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tag-line {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.account-region-rank {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.region {
  text-transform: uppercase;
  font-weight: var(--font-weight-medium);
}

.separator {
  color: var(--color-text-tertiary);
}

.rank {
  font-weight: var(--font-weight-medium);
}

.rank-muted {
  color: var(--color-text-secondary);
}

.account-lp {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.games-today {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.games-today.has-games {
  color: var(--color-success);
  font-weight: 500;
}

.activity-dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background-color: var(--color-success);
  margin-right: var(--spacing-xs);
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

/* Scrollbar styling */
.account-cards-container::-webkit-scrollbar {
  height: 6px;
}

.account-cards-container::-webkit-scrollbar-track {
  background: var(--color-background);
  border-radius: 3px;
}

.account-cards-container::-webkit-scrollbar-thumb {
  background: var(--color-border);
  border-radius: 3px;
}

.account-cards-container::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-tertiary);
}

/* Mobile responsive */
@media (max-width: 640px) {
  .header-row {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-xs);
  }

  .account-card {
    min-width: 140px;
    padding: var(--spacing-md);
  }

  .game-name {
    font-size: var(--font-size-xs);
  }
}
</style>
