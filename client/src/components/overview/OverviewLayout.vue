<template>
  <div class="overview-layout">
    <div class="overview-container">
      <!-- Loading State -->
      <BaseCard v-if="isLoading" class="state-card loading-state">
        <div class="loading-spinner"></div>
        <p class="text-text-secondary text-sm">Loading overview...</p>
      </BaseCard>

      <!-- Error State -->
      <BaseCard v-else-if="error" class="state-card error-state">
        <div class="error-icon text-error">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-8 h-8">
            <path fill-rule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.752-2.5-2.598-4.5L9.4 3.003zM12 8.25a.75.75 0 01.75.75v3.75a.75.75 0 01-1.5 0V9a.75.75 0 01.75-.75zm0 8.25a.75.75 0 100-1.5.75.75 0 000 1.5z" clip-rule="evenodd" />
          </svg>
        </div>
        <p class="error-message text-error">{{ error }}</p>
        <BaseButton variant="primary" @click="$emit('retry')">
          Try Again
        </BaseButton>
      </BaseCard>

      <!-- Empty State (no linked accounts) -->
      <BaseCard v-else-if="isEmpty" class="state-card empty-state">
        <div class="empty-icon">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-12 h-12">
            <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
          </svg>
        </div>
        <h2 class="empty-title">No Linked Account</h2>
        <p class="empty-description">Link a Riot account to see your overview.</p>
        <slot name="empty-action"></slot>
      </BaseCard>

      <!-- Content: Vertical section-based layout -->
      <div v-else class="overview-content">
        <!-- Player Header Section (full width) -->
        <section v-if="$slots.header" class="overview-section">
          <slot name="header"></slot>
        </section>

        <!-- Today at a glance Section -->
        <section v-if="$slots['glance-left'] || $slots['glance-right']" class="overview-section">
          <h2 class="section-title">Today at a glance</h2>
          <div class="section-row">
            <div class="section-col section-col--primary">
              <slot name="glance-left"></slot>
            </div>
            <div class="section-col section-col--secondary">
              <slot name="glance-right"></slot>
            </div>
          </div>
        </section>

        <!-- Recent games Section -->
        <section v-if="$slots['recent-left'] || $slots['recent-right']" class="overview-section">
          <h2 class="section-title">Recent games</h2>
          <div class="section-row">
            <div class="section-col section-col--primary">
              <slot name="recent-left"></slot>
            </div>
            <div class="section-col section-col--secondary">
              <slot name="recent-right"></slot>
            </div>
          </div>
        </section>

        <!-- Latest Match Section (full width) -->
        <section v-if="$slots['latest-match']" class="overview-section">
          <h2 class="section-title">Latest match</h2>
          <slot name="latest-match"></slot>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup>
import { BaseCard, BaseButton } from '@/components/base'

defineProps({
  isLoading: {
    type: Boolean,
    default: false
  },
  error: {
    type: String,
    default: null
  },
  isEmpty: {
    type: Boolean,
    default: false
  }
})

defineEmits(['retry'])
</script>

<style scoped>
.overview-layout {
  min-height: 100vh;
  padding: var(--spacing-lg);
}

@media (min-width: 768px) {
  .overview-layout {
    padding: var(--spacing-2xl);
  }
}

.overview-container {
  max-width: 80%;
  margin: 0 auto;
}

.overview-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

/* Section styles */
.overview-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.section-title {
  margin: 0;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

/* Section row: two columns on desktop, stacked on mobile */
.section-row {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

@media (min-width: 768px) {
  .section-row {
    flex-direction: row;
    align-items: stretch;
  }
}

/* Column sizing */
.section-col {
  min-width: 0;
}

.section-col--primary {
  flex: 1 1 60%;
}

.section-col--secondary {
  flex: 1 1 40%;
}

/* State cards */
.state-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-2xl);
  backdrop-filter: blur(10px);
  text-align: center;
}

/* Loading state */
.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-2xl);
}

.loading-spinner {
  width: 32px;
  height: 32px;
  border: 3px solid var(--color-border);
  border-radius: 50%;
  border-top-color: var(--color-primary);
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Error state */
.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-md);
}

.error-message {
  margin: 0;
}

/* Empty state */
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-sm);
}

.empty-icon {
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-sm);
}

.empty-title {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
}

.empty-description {
  margin: 0;
  color: var(--color-text-secondary);
}
</style>

