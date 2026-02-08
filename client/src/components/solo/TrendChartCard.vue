<template>
  <BaseCard class="trend-chart-card" :data-testid="testId">
    <div class="card-header">
      <div class="header-left">
        <h3 class="chart-title">{{ title }}</h3>
        <p v-if="subtitle" class="chart-subtitle">{{ subtitle }}</p>
      </div>
      <button
        type="button"
        class="expand-toggle"
        :aria-label="isExpanded ? 'Show last 20 games' : 'Show full season'"
        :aria-pressed="isExpanded"
        @click="toggleExpand"
      >
        <span class="toggle-text">{{ isExpanded ? 'Last 20' : 'Full Season' }}</span>
        <svg
          class="toggle-icon"
          :class="{ 'icon-rotated': isExpanded }"
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 20 20"
          fill="currentColor"
          aria-hidden="true"
        >
          <path
            fill-rule="evenodd"
            d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z"
            clip-rule="evenodd"
          />
        </svg>
      </button>
    </div>

    <div class="chart-container">
      <!-- Loading state -->
      <div v-if="loading" class="loading-state" data-testid="loading-state">
        <div class="skeleton skeleton-chart" />
      </div>

      <!-- Chart slot (receives isExpanded state) -->
      <slot v-else :is-expanded="isExpanded" :data-limit="dataLimit" />
    </div>
  </BaseCard>
</template>

<script setup>
import { ref, computed } from 'vue'
import { BaseCard } from '../base'

const props = defineProps({
  /** Chart title */
  title: {
    type: String,
    required: true
  },
  /** Optional subtitle */
  subtitle: {
    type: String,
    default: null
  },
  /** Loading state */
  loading: {
    type: Boolean,
    default: false
  },
  /** Test ID for the card */
  testId: {
    type: String,
    default: 'trend-chart-card'
  },
  /** Default expanded state */
  defaultExpanded: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['toggle-expand'])

const isExpanded = ref(props.defaultExpanded)

// Data limit: 20 for collapsed (last 20 games), 500 for expanded (full season)
const dataLimit = computed(() => isExpanded.value ? 500 : 20)

function toggleExpand() {
  isExpanded.value = !isExpanded.value
  emit('toggle-expand', isExpanded.value)
}
</script>

<style scoped>
.trend-chart-card {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 280px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-md);
}

.header-left {
  flex: 1;
}

.chart-title {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--text);
}

.chart-subtitle {
  margin: var(--spacing-xs) 0 0 0;
  font-size: var(--font-size-xs);
  color: var(--text-secondary);
}

.expand-toggle {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-xs) var(--spacing-sm);
  background: var(--background-elevated);
  border: 1px solid var(--border);
  border-radius: var(--border-radius-sm);
  color: var(--text-secondary);
  font-size: var(--font-size-xs);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.expand-toggle:hover {
  background: var(--background-hover);
  color: var(--text);
  border-color: var(--border-hover);
}

.toggle-icon {
  width: 14px;
  height: 14px;
  transition: transform var(--transition-fast);
}

.icon-rotated {
  transform: rotate(180deg);
}

.chart-container {
  flex: 1;
  min-height: 200px;
  position: relative;
}

/* Loading skeleton */
.loading-state {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.skeleton {
  background: linear-gradient(
    90deg,
    var(--background-elevated) 25%,
    var(--background-hover) 50%,
    var(--background-elevated) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--border-radius-sm);
}

.skeleton-chart {
  width: 100%;
  height: 180px;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
</style>

