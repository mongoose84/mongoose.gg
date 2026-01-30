<template>
  <div class="match-header" :class="{ 'win': match.win, 'loss': !match.win }">
    <!-- Champion Icon with KDA -->
    <div class="champion-section">
      <div class="champion-icon-wrapper">
        <img
          v-if="match.championIconUrl"
          :src="match.championIconUrl"
          :alt="`${match.championName} icon`"
          class="champion-icon"
        />
      </div>
      <div class="kda-display">
        <span class="kda-kills">{{ match.kills }}</span>
        <span class="kda-separator">/</span>
        <span class="kda-deaths">{{ match.deaths }}</span>
        <span class="kda-separator">/</span>
        <span class="kda-assists">{{ match.assists }}</span>
      </div>
    </div>

    <!-- Match Info -->
    <div class="match-info">
      <div class="primary-row">
        <span class="champion-name">{{ match.championName }}</span>
        <span class="result-badge" :class="{ 'win': match.win, 'loss': !match.win }">
          {{ match.win ? 'Victory' : 'Defeat' }}
        </span>
      </div>
      <div class="game-result-row">
        <span class="team-kills" :class="{ 'win': match.win }">{{ match.teamKills }}</span>
        <span class="result-separator">-</span>
        <span class="team-kills" :class="{ 'loss': !match.win }">{{ match.enemyTeamKills }}</span>
        <span class="result-label">Game Result</span>
      </div>
      <div class="secondary-row">
        <template v-if="match.role && match.role !== 'UNKNOWN'">
          <span class="role">{{ formatRole(match.role) }}</span>
          <span class="separator">·</span>
        </template>
        <span class="queue">{{ match.queueType }}</span>
        <span class="separator">·</span>
        <span class="duration">{{ formatDuration(match.gameDurationSec) }}</span>
        <span class="separator">·</span>
        <span class="timestamp">{{ relativeTime }}</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { formatRole, formatDuration, formatRelativeTime } from '@/utils/formatters'

const props = defineProps({
  match: {
    type: Object,
    required: true
  }
})

const relativeTime = computed(() => formatRelativeTime(props.match.gameStartTime))
</script>

<style scoped>
.match-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-xs);
  background: var(--color-elevated);
  border-radius: var(--radius-md);
}

.match-header.win {
  border-left: 4px solid var(--color-success);
}

.match-header.loss {
  border-left: 4px solid var(--color-error);
}

.champion-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-xs);
  flex-shrink: 0;
}

.champion-icon-wrapper {
  width: 64px;
  height: 64px;
  border-radius: var(--radius-md);
  overflow: hidden;
  background: var(--color-elevated);
}

.kda-display {
  display: flex;
  align-items: center;
  gap: 2px;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.kda-kills {
  color: var(--color-success);
}

.kda-deaths {
  color: var(--color-error);
}

.kda-assists {
  color: var(--color-info);
}

.kda-separator {
  color: var(--color-text-secondary);
  opacity: 0.5;
}

.champion-icon {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.match-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.primary-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.champion-name {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.result-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  padding: 3px 10px;
  border-radius: var(--radius-sm);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.result-badge.win {
  background: var(--color-success-soft);
  color: var(--color-success);
}

.result-badge.loss {
  background: var(--color-error-soft);
  color: var(--color-error);
}

.game-result-row {
  display: flex;
  align-items: baseline;
  gap: 4px;
}

.team-kills {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.team-kills.win {
  color: var(--color-success);
}

.team-kills.loss {
  color: var(--color-error);
}

.result-separator {
  font-size: var(--font-size-lg);
  color: var(--color-text-secondary);
  margin: 0 4px;
}

.result-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin-left: var(--spacing-sm);
}

.secondary-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.separator {
  opacity: 0.5;
}
</style>

