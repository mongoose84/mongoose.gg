<template>
  <section class="mp-card match-actions" aria-labelledby="match-actions-title" data-testid="match-actions">
    <div class="match-actions__text">
      <h3 id="match-actions-title" class="mp-card-title">Is this match a one-off?</h3>
      <p>Your trends show whether these numbers repeat across your recent matches.</p>
    </div>
    <BaseButton variant="secondary" :to="{ name: 'app-solo' }" data-testid="match-actions-trends">
      <template #icon-left><BaseIcon name="chart-line" :size="20" /></template>
      See your trends
    </BaseButton>
  </section>
</template>

<script setup>
/**
 * The next step after reading a match: compare it with the player's trends on Solo.
 */
import BaseButton from '../base/BaseButton.vue'
import BaseIcon from '../base/BaseIcon.vue'

defineProps({
  match: {
    type: Object,
    default: null
  }
})
</script>

<style scoped>
.match-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 1rem 1.5rem;
}

.match-actions__text {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  max-width: 40.625rem;
}

.match-actions__text p {
  font-size: 0.875rem;
  color: var(--color-ink-soft);
}
</style>
