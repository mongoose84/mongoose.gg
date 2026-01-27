<template>
  <div class="match-list">
    <!-- Loading State -->
    <div v-if="loading" class="loading-state">
      <div class="loading-spinner"></div>
      <span class="loading-text">Loading matches...</span>
    </div>

    <!-- Empty State -->
    <div v-else-if="!matches || matches.length === 0" class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="empty-icon">
        <path fill-rule="evenodd" d="M4.5 5.653c0-1.426 1.529-2.33 2.779-1.643l11.54 6.348c1.295.712 1.295 2.573 0 3.285L7.28 19.991c-1.25.687-2.779-.217-2.779-1.643V5.653z" clip-rule="evenodd" />
      </svg>
      <span class="empty-text">No matches found</span>
      <span class="empty-subtext">Play some games to see your match history</span>
    </div>

    <!-- Match List -->
    <div v-else class="matches-container">
      <MatchRow
        v-for="match in matches"
        :key="match.matchId"
        :match="match"
        :selected="selectedMatchId === match.matchId"
        @select="handleSelect"
      />
    </div>
  </div>
</template>

<script setup>
import MatchRow from './MatchRow.vue'

const props = defineProps({
  matches: {
    type: Array,
    default: () => []
  },
  selectedMatchId: {
    type: String,
    default: null
  },
  loading: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['select'])

function handleSelect(matchId) {
  emit('select', matchId)
}
</script>

<style scoped>
.match-list {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  overflow: hidden;
}

.matches-container {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  flex: 1;
  overflow-y: auto;
  padding-right: var(--spacing-xs);
}

/* Loading State */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-md);
  padding: var(--spacing-2xl);
  color: var(--color-text-secondary);
}

.loading-spinner {
  width: 32px;
  height: 32px;
  border: 3px solid var(--color-border);
  border-top-color: var(--color-primary);
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.loading-text {
  font-size: var(--font-size-sm);
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-2xl);
  text-align: center;
}

.empty-icon {
  width: 48px;
  height: 48px;
  color: var(--color-text-secondary);
  opacity: 0.3;
}

.empty-text {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
}

.empty-subtext {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  opacity: 0.7;
}
</style>

