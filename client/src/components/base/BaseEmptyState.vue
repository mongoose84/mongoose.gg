<template>
  <div class="mp-empty" data-testid="base-empty-state">
    <component :is="headingTag" class="mp-empty__title" data-testid="base-empty-state-title">{{ title }}</component>
    <p v-if="description" data-testid="base-empty-state-description">{{ description }}</p>
    <div v-if="$slots.action" class="mp-empty__action">
      <slot name="action" />
    </div>
  </div>
</template>

<script setup>
/**
 * EmptyState (design system): what a card shows when there is nothing yet, written as the
 * next step. Title says what is missing, description what happens next, and the action slot
 * holds the one button that fixes it.
 */
import { computed } from 'vue'

const props = defineProps({
  /** What is missing, in the player's words ("No matches yet") */
  title: {
    type: String,
    required: true
  },
  /** One line of why or what happens next */
  description: {
    type: String,
    default: null
  },
  /** Heading level that fits the surrounding outline */
  headingLevel: {
    type: Number,
    default: 3,
    validator: (value) => value >= 2 && value <= 4
  }
})

const headingTag = computed(() => `h${props.headingLevel}`)
</script>

<style scoped>
/* Keep the component class styling whatever heading level is used */
.mp-empty__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.25rem;
  font-weight: 600;
  line-height: 1.3;
  color: var(--color-text);
}

.mp-empty__action {
  margin-top: 0.25rem;
}
</style>
