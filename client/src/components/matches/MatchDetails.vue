<template>
  <div class="match-details">
    <!-- Empty state when no match selected -->
    <div v-if="!match" class="empty-state">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="empty-icon">
        <path fill-rule="evenodd" d="M2.25 12c0-5.385 4.365-9.75 9.75-9.75s9.75 4.365 9.75 9.75-4.365 9.75-9.75 9.75S2.25 17.385 2.25 12zm8.706-1.442c1.146-.573 2.437.463 2.126 1.706l-.709 2.836.042-.02a.75.75 0 01.67 1.34l-.04.022c-1.147.573-2.438-.463-2.127-1.706l.71-2.836-.042.02a.75.75 0 11-.671-1.34l.041-.022zM12 9a.75.75 0 100-1.5.75.75 0 000 1.5z" clip-rule="evenodd" />
      </svg>
      <span class="empty-text">Select a match to view details</span>
    </div>

    <!-- Match details content -->
    <div v-else class="details-content">
      <MatchHeader :match="match" />

      <div class="details-sections">
        <StatSnapshot :match="match" :baseline="baseline" />
        <MatchActions />
      </div>
    </div>
  </div>
</template>

<script setup>
import MatchHeader from './MatchHeader.vue'
import StatSnapshot from './StatSnapshot.vue'
import MatchActions from './MatchActions.vue'

defineProps({
  match: {
    type: Object,
    default: null
  },
  baseline: {
    type: Object,
    default: null
    // Expected: RoleBaseline for the selected match's role
  }
})
</script>

<style scoped>
.match-details {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}

.details-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
  overflow-y: auto;
  padding-right: var(--spacing-xs);
}

.details-sections {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xl);
}

/* Empty State */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-md);
  height: 100%;
  min-height: 300px;
  text-align: center;
  padding: var(--spacing-2xl);
}

.empty-icon {
  width: 48px;
  height: 48px;
  color: var(--color-text-secondary);
  opacity: 0.3;
}

.empty-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}
</style>

