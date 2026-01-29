<template>
  <div class="highlight-tile" :class="typeClass">
    <!-- Icon -->
    <div class="tile-icon">
      <component :is="iconComponent" class="icon" />
    </div>

    <!-- Content -->
    <div class="tile-content">
      <span class="stat-name">{{ statName }}</span>
      <span class="insight-text">{{ insightText }}</span>
    </div>

    <!-- Trend indicator -->
    <div class="trend-indicator" :class="typeClass">
      <span v-if="type === 'positive'">↑</span>
      <span v-else-if="type === 'negative'">↓</span>
      <span v-else>→</span>
    </div>
  </div>
</template>

<script setup>
import { computed, h } from 'vue'

const props = defineProps({
  statName: {
    type: String,
    required: true
  },
  insightText: {
    type: String,
    required: true
  },
  type: {
    type: String,
    default: 'neutral',
    validator: (val) => ['positive', 'negative', 'neutral'].includes(val)
  },
  icon: {
    type: String,
    default: 'chart'
  }
})

const typeClass = computed(() => `trend-${props.type}`)

// Simple inline SVG icons
const icons = {
  damage: h('svg', { xmlns: 'http://www.w3.org/2000/svg', viewBox: '0 0 20 20', fill: 'currentColor' }, [
    h('path', { 'fill-rule': 'evenodd', d: 'M13.5 4.938a7 7 0 11-9.006 1.737c.202-.257.59-.218.793.039.278.352.594.672.943.954.332.269.786-.049.786-.478V6.3a.5.5 0 01.15-.356l1.5-1.5a.5.5 0 01.707 0l2.5 2.5a.5.5 0 01-.707.707l-1.5-1.5-.146.146v3.097l.293-.293a.5.5 0 01.707.707L8.5 11.21v1.353a.5.5 0 01-.146.353l-.75.75V15a.5.5 0 01-1 0v-1.5a.5.5 0 01.146-.354l.854-.853v-1.5a.5.5 0 01.146-.354l1.061-1.06-1.06-1.061a.5.5 0 01.707-.707L10 8.668V6.3c0-.282.152-.543.4-.684a5 5 0 103.1-.678z', 'clip-rule': 'evenodd' })
  ]),
  kda: h('svg', { xmlns: 'http://www.w3.org/2000/svg', viewBox: '0 0 20 20', fill: 'currentColor' }, [
    h('path', { d: 'M10 9a3 3 0 100-6 3 3 0 000 6zM6 8a2 2 0 11-4 0 2 2 0 014 0zM1.49 15.326a.78.78 0 01-.358-.442 3 3 0 014.308-3.516 6.484 6.484 0 00-1.905 3.959c-.023.222-.014.442.025.654a4.97 4.97 0 01-2.07-.655zM16.44 15.98a4.97 4.97 0 002.07-.654.78.78 0 00.357-.442 3 3 0 00-4.308-3.517 6.484 6.484 0 011.907 3.96 2.32 2.32 0 01-.026.654zM18 8a2 2 0 11-4 0 2 2 0 014 0zM5.304 16.19a.844.844 0 01-.277-.71 5 5 0 019.947 0 .843.843 0 01-.277.71A6.975 6.975 0 0110 18a6.974 6.974 0 01-4.696-1.81z' })
  ]),
  cs: h('svg', { xmlns: 'http://www.w3.org/2000/svg', viewBox: '0 0 20 20', fill: 'currentColor' }, [
    h('path', { 'fill-rule': 'evenodd', d: 'M10 18a8 8 0 100-16 8 8 0 000 16zm.75-11.25a.75.75 0 00-1.5 0v2.5h-2.5a.75.75 0 000 1.5h2.5v2.5a.75.75 0 001.5 0v-2.5h2.5a.75.75 0 000-1.5h-2.5v-2.5z', 'clip-rule': 'evenodd' })
  ]),
  vision: h('svg', { xmlns: 'http://www.w3.org/2000/svg', viewBox: '0 0 20 20', fill: 'currentColor' }, [
    h('path', { d: 'M10 12.5a2.5 2.5 0 100-5 2.5 2.5 0 000 5z' }),
    h('path', { 'fill-rule': 'evenodd', d: 'M.664 10.59a1.651 1.651 0 010-1.186A10.004 10.004 0 0110 3c4.257 0 7.893 2.66 9.336 6.41.147.381.146.804 0 1.186A10.004 10.004 0 0110 17c-4.257 0-7.893-2.66-9.336-6.41zM14 10a4 4 0 11-8 0 4 4 0 018 0z', 'clip-rule': 'evenodd' })
  ]),
  chart: h('svg', { xmlns: 'http://www.w3.org/2000/svg', viewBox: '0 0 20 20', fill: 'currentColor' }, [
    h('path', { d: 'M15.5 2A1.5 1.5 0 0014 3.5v13a1.5 1.5 0 001.5 1.5h1a1.5 1.5 0 001.5-1.5v-13A1.5 1.5 0 0016.5 2h-1zM9.5 6A1.5 1.5 0 008 7.5v9A1.5 1.5 0 009.5 18h1a1.5 1.5 0 001.5-1.5v-9A1.5 1.5 0 0010.5 6h-1zM3.5 10A1.5 1.5 0 002 11.5v5A1.5 1.5 0 003.5 18h1A1.5 1.5 0 006 16.5v-5A1.5 1.5 0 004.5 10h-1z' })
  ])
}

const iconComponent = computed(() => icons[props.icon] || icons.chart)
</script>

<style scoped>
.highlight-tile {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
}

.tile-icon {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-sm);
  background: var(--color-elevated);
}

.tile-icon .icon {
  width: 18px;
  height: 18px;
  color: var(--color-text-secondary);
}

.tile-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.stat-name {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.insight-text {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.trend-indicator {
  width: 24px;
  height: 24px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-bold);
}

.trend-indicator.trend-positive {
  background: var(--color-success-soft);
  color: var(--color-success);
}

.trend-indicator.trend-negative {
  background: var(--color-error-soft);
  color: var(--color-error);
}

.trend-indicator.trend-neutral {
  background: var(--color-elevated);
  color: var(--color-text-secondary);
}
</style>

