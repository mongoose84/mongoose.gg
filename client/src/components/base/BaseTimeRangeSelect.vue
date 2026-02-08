<template>
  <div class="time-range-select-wrapper">
    <label 
      v-if="showLabel" 
      :for="selectId" 
      class="time-range-label"
    >
      {{ label }}
    </label>
    <select
      :id="selectId"
      :value="modelValue"
      :aria-label="ariaLabel"
      class="time-range-select"
      @change="$emit('update:modelValue', $event.target.value)"
    >
      <option 
        v-for="option in options" 
        :key="option.value" 
        :value="option.value"
      >
        {{ option.label }}
      </option>
    </select>
  </div>
</template>

<script setup>
import { computed, useId } from 'vue'

/**
 * BaseTimeRangeSelect - A reusable select component for filtering by time range.
 *
 * @example
 * <BaseTimeRangeSelect v-model="timeRange" />
 *
 * @example with custom options
 * <BaseTimeRangeSelect
 *   v-model="timeRange"
 *   :options="[{ value: '1w', label: 'Last Week' }, { value: '1m', label: 'Last Month' }]"
 * />
 *
 * @example with visible label
 * <BaseTimeRangeSelect v-model="timeRange" show-label label="Time Period" />
 */

const props = defineProps({
  /** Currently selected time range value (v-model) */
  modelValue: {
    type: String,
    required: true
  },
  /** Time range options array with { value, label } objects */
  options: {
    type: Array,
    default: () => [
      { value: 'current_season', label: 'Current Season' },
      { value: '1w', label: 'Last Week' },
      { value: '1m', label: 'Last Month' },
      { value: '3m', label: 'Last 3 Months' },
      { value: '6m', label: 'Last 6 Months' },
      { value: 'all', label: 'All Time' }
    ],
    validator: (options) => Array.isArray(options) && options.every(o => 'value' in o && 'label' in o)
  },
  /** Accessible label for the select element */
  ariaLabel: {
    type: String,
    default: 'Filter matches by time range'
  },
  /** Whether to show a visible label above the select */
  showLabel: {
    type: Boolean,
    default: false
  },
  /** Visible label text (only shown if showLabel is true) */
  label: {
    type: String,
    default: 'Time Range'
  },
  /** Optional ID for the select element (auto-generated unique ID if not provided) */
  id: {
    type: String,
    default: null
  }
})

defineEmits(['update:modelValue'])

// Generate unique ID for label association
const generatedId = useId()
const selectId = computed(() => props.id || `time-range-${generatedId}`)
</script>

<style scoped>
.time-range-select-wrapper {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.time-range-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  font-weight: 500;
}

.time-range-select {
  padding: var(--spacing-sm) var(--spacing-md);
  background-color: #020617;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.time-range-select:hover {
  border-color: var(--color-primary);
}

.time-range-select:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(147, 51, 234, 0.1);
}

/* Style the dropdown options */
.time-range-select option {
  background-color: #020617;
  color: var(--color-text);
}
</style>

