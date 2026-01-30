<template>
  <div 
    class="queue-toggle-group" 
    role="group" 
    :aria-label="ariaLabel"
  >
    <button
      v-for="option in options"
      :key="option.value"
      type="button"
      class="queue-toggle-btn"
      :class="{
        'queue-toggle-btn--active': modelValue === option.value,
        'queue-toggle-btn--inactive': modelValue !== option.value
      }"
      @click="$emit('update:modelValue', option.value)"
      :aria-pressed="modelValue === option.value"
    >
      {{ option.label }}
    </button>
  </div>
</template>

<script setup>
/**
 * BaseQueueToggle - A reusable toggle button group for filtering by queue type.
 * 
 * @example
 * <BaseQueueToggle v-model="queueFilter" />
 * 
 * @example with custom options
 * <BaseQueueToggle 
 *   v-model="queueFilter" 
 *   :options="[{ value: 'all', label: 'All' }, { value: 'ranked', label: 'Ranked' }]"
 * />
 */

const props = defineProps({
  /** Currently selected queue value (v-model) */
  modelValue: {
    type: String,
    required: true
  },
  /** Queue options array with { value, label } objects */
  options: {
    type: Array,
    default: () => [
      { value: 'all', label: 'All Queues' },
      { value: 'ranked_solo', label: 'Ranked Solo/Duo' },
      { value: 'ranked_flex', label: 'Ranked Flex' },
      { value: 'normal', label: 'Normal' },
      { value: 'aram', label: 'ARAM' }
    ],
    validator: (options) => options.every(o => 'value' in o && 'label' in o)
  },
  /** Accessible label for the button group */
  ariaLabel: {
    type: String,
    default: 'Filter by queue type'
  }
})

defineEmits(['update:modelValue'])
</script>

<style scoped>
/* Container */
.queue-toggle-group {
  display: flex;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  overflow: hidden;
  background: var(--color-surface);
}

/* Base button styles */
.queue-toggle-btn {
  padding: var(--spacing-sm) var(--spacing-md);
  background: transparent;
  border: none;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.queue-toggle-btn:focus {
  outline: none;
}

.queue-toggle-btn:focus-visible {
  box-shadow: inset 0 0 0 2px var(--color-primary-soft);
}

/* Dividers between buttons */
.queue-toggle-btn:not(:last-child)::after {
  content: '';
  position: absolute;
  right: 0;
  top: 25%;
  height: 50%;
  width: 1px;
  background: var(--color-border);
}

/* Active state */
.queue-toggle-btn--active {
  background-color: var(--color-primary-dark, #5b21b6);
  color: white;
}

/* Hide divider when button is active or next to active */
.queue-toggle-btn--active::after {
  display: none;
}

.queue-toggle-btn:has(+ .queue-toggle-btn--active)::after {
  display: none;
}

/* Inactive hover state */
.queue-toggle-btn--inactive:hover {
  color: var(--color-text);
  background: var(--color-elevated);
  box-shadow: inset 0 0 0 1px var(--color-primary);
}

/* Hide divider on hovered buttons */
.queue-toggle-btn--inactive:hover::after {
  display: none;
}

.queue-toggle-btn:has(+ .queue-toggle-btn--inactive:hover)::after {
  display: none;
}
</style>

