<template>
  <!-- Loading: the card's skeleton, shown after 300ms so fast loads never flash it -->
  <section
    v-if="loading"
    class="mp-card solo-card solo-card--loading"
    aria-busy="true"
    :data-testid="`${testId}-loading`"
  >
    <span class="visually-hidden">{{ loadingLabel }}</span>
    <slot name="skeleton" />
  </section>

  <section
    v-else-if="error"
    class="mp-card solo-card"
    :aria-labelledby="titleId"
    :data-testid="`${testId}-error`"
  >
    <h2 :id="titleId" class="mp-card-title">{{ errorTitle }}</h2>
    <div class="mp-message mp-message--error" role="alert">
      <BaseIcon name="triangle-alert" :size="20" />
      <div class="mp-message__body">
        <p>We couldn't load this card. Try again in a minute.</p>
        <button type="button" class="mp-message__action" :data-testid="`${testId}-retry`" @click="$emit('retry')">Try again</button>
      </div>
    </div>
  </section>

  <BaseEmptyState
    v-else-if="empty"
    :title="empty.title"
    :description="empty.description"
    :heading-level="2"
    :data-testid="`${testId}-empty`"
  >
    <template v-if="$slots['empty-action']" #action>
      <slot name="empty-action" />
    </template>
  </BaseEmptyState>

  <section
    v-else
    class="mp-card solo-card"
    :aria-labelledby="titleId"
    :data-testid="testId"
  >
    <header class="solo-card__header">
      <div class="solo-card__heading">
        <h2 :id="titleId" class="mp-card-title" :data-testid="`${testId}-title`">{{ title }}</h2>
        <p v-if="caption" class="solo-card__caption" :data-testid="`${testId}-caption`">{{ caption }}</p>
      </div>
      <slot name="aside" />
    </header>
    <slot />
  </section>
</template>

<script setup>
/**
 * Shell for a Solo page card: the design system's four states in one place. Skeleton while
 * loading (after 300ms), an inline error with "Try again", the EmptyState in the card's slot,
 * or the content under a takeaway title and caption.
 */
import BaseIcon from '../base/BaseIcon.vue'
import BaseEmptyState from '../base/BaseEmptyState.vue'

defineProps({
  /** Base data-testid; states add -loading, -error, -empty */
  testId: { type: String, required: true },
  /** id of the card title, for aria-labelledby */
  titleId: { type: String, required: true },
  /** Takeaway title of the content */
  title: { type: String, default: '' },
  /** Measure and range under the title */
  caption: { type: String, default: null },
  /** Title shown above the error message (the card's subject) */
  errorTitle: { type: String, required: true },
  /** Visually hidden label while loading ("Loading your stat trends") */
  loadingLabel: { type: String, required: true },
  /** Show the skeleton (only when there is no earlier content to keep) */
  loading: { type: Boolean, default: false },
  /** The request failed */
  error: { type: Boolean, default: false },
  /** { title, description } for the EmptyState, or null */
  empty: { type: Object, default: null }
})

defineEmits(['retry'])
</script>

<style scoped>
.solo-card {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  min-width: 0;
}

.solo-card--loading {
  animation: solo-card-appear 0s linear 300ms both;
}

@keyframes solo-card-appear {
  from { visibility: hidden; }
  to { visibility: visible; }
}

.solo-card__header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 0.75rem 1.5rem;
}

.solo-card__heading {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.solo-card__caption {
  margin: 0;
  font-size: 0.875rem;
  line-height: 1.5;
  color: var(--color-text-secondary);
}
</style>
