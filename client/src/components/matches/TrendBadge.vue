<template>
  <span v-if="badge" class="trend-badge" :class="typeClass">
    <span class="badge-icon" v-if="type === 'positive'">↑</span>
    <span class="badge-icon" v-else-if="type === 'negative'">↓</span>
    <span class="badge-text">{{ badge.text }}</span>
  </span>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  badge: {
    type: Object,
    default: null
    // Expected: { text: string, type: 'positive'|'neutral'|'negative', stat: string }
  }
})

const type = computed(() => props.badge?.type || 'neutral')

const typeClass = computed(() => {
  switch (type.value) {
    case 'positive': return 'trend-positive'
    case 'negative': return 'trend-negative'
    default: return 'trend-neutral'
  }
})
</script>

<style scoped>
.trend-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 8px;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  white-space: nowrap;
}

.badge-icon {
  font-size: 10px;
  font-weight: var(--font-weight-bold);
}

.badge-text {
  line-height: 1.2;
}

/* Positive - subtle green */
.trend-positive {
  background: var(--color-success-soft);
  color: var(--color-success);
}

/* Negative - subtle red */
.trend-negative {
  background: var(--color-error-soft);
  color: var(--color-error);
}

/* Neutral - subtle gray */
.trend-neutral {
  background: var(--color-elevated);
  color: var(--color-text-secondary);
}
</style>

