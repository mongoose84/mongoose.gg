<template>
  <div class="survival-check-card" data-testid="survival-check-card">
    <div class="survival-placeholder-content">
      <span class="survival-label">SURVIVAL CHECK</span>
      <p class="survival-placeholder-text">Coming soon</p>
    </div>
  </div>
</template>

<script setup>
// Placeholder for Task 5 — SurvivalCheckCard implementation.
// Props will be wired up when Task 5 is implemented.
defineProps({
  survivalStats: { type: Object, default: null },
  loading: { type: Boolean, default: false }
})
</script>

<style scoped>
.survival-check-card {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
  min-height: 140px;
}

.survival-placeholder-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-sm);
}

.survival-label {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: var(--color-text-secondary);
}

.survival-placeholder-text {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  font-style: italic;
}
</style>
