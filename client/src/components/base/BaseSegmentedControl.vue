<template>
  <div class="mp-seg" role="group" :aria-label="ariaLabel" data-testid="segmented-control">
    <button
      v-for="option in options"
      :key="option.value"
      type="button"
      :aria-pressed="modelValue === option.value"
      :data-testid="`${testIdPrefix}-${option.value}`"
      @click="$emit('update:modelValue', option.value)"
    >
      {{ option.label }}
    </button>
  </div>
</template>

<script setup>
/**
 * SegmentedControl (design system): switches the range or view of one card or page.
 * 2–4 short options; the active one has aria-pressed="true". Not for page navigation.
 */
defineProps({
  /** The selected option value (v-model) */
  modelValue: {
    type: String,
    default: null
  },
  /** Options as { value, label } */
  options: {
    type: Array,
    required: true,
    validator: (options) => options.every((o) => 'value' in o && 'label' in o)
  },
  /** What the group switches ("Queue", "Role") */
  ariaLabel: {
    type: String,
    required: true
  },
  /** Prefix for each button's data-testid (`<prefix>-<value>`) */
  testIdPrefix: {
    type: String,
    default: 'segment'
  }
})

defineEmits(['update:modelValue'])
</script>
