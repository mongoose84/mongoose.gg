<template>
  <div class="input-wrapper">
    <!-- Label -->
    <label v-if="label" :for="inputId" class="input-label">
      {{ label }}
      <span v-if="required" class="input-required" aria-hidden="true">*</span>
    </label>
    
    <!-- Input container for icon support -->
    <div class="input-container" :class="containerClasses">
      <!-- Left icon slot -->
      <span v-if="$slots['icon-left']" class="input-icon input-icon--left">
        <slot name="icon-left"></slot>
      </span>
      
      <!-- Input element -->
      <input
        :id="inputId"
        ref="inputRef"
        v-model="modelValue"
        :type="type"
        :placeholder="placeholder"
        :disabled="disabled"
        :required="required"
        :minlength="minlength"
        :maxlength="maxlength"
        :autocomplete="autocomplete"
        :aria-invalid="!!error"
        :aria-describedby="error ? errorId : undefined"
        class="input-field"
        :class="{ 'has-icon-left': $slots['icon-left'], 'has-icon-right': $slots['icon-right'] }"
        v-bind="$attrs"
      />
      
      <!-- Right icon slot -->
      <span v-if="$slots['icon-right']" class="input-icon input-icon--right">
        <slot name="icon-right"></slot>
      </span>
    </div>
    
    <!-- Error message -->
    <span v-if="error" :id="errorId" class="input-error" role="alert">
      {{ error }}
    </span>
    
    <!-- Hint text -->
    <span v-else-if="hint" class="input-hint">
      {{ hint }}
    </span>
  </div>
</template>

<script setup>
import { computed, ref, useId } from 'vue'

const props = defineProps({
  /** v-model binding */
  modelValue: {
    type: [String, Number],
    default: ''
  },
  /** Input label */
  label: {
    type: String,
    default: null
  },
  /** Input type */
  type: {
    type: String,
    default: 'text'
  },
  /** Placeholder text */
  placeholder: {
    type: String,
    default: ''
  },
  /** Error message (also sets error state) */
  error: {
    type: String,
    default: null
  },
  /** Hint text shown below input */
  hint: {
    type: String,
    default: null
  },
  /** Disabled state */
  disabled: {
    type: Boolean,
    default: false
  },
  /** Required field */
  required: {
    type: Boolean,
    default: false
  },
  /** Minimum length */
  minlength: {
    type: [String, Number],
    default: null
  },
  /** Maximum length */
  maxlength: {
    type: [String, Number],
    default: null
  },
  /** Autocomplete attribute */
  autocomplete: {
    type: String,
    default: 'off'
  },
  /** Custom id (auto-generated if not provided) */
  id: {
    type: String,
    default: null
  }
})

const emit = defineEmits(['update:modelValue'])

const inputRef = ref(null)

const focus = () => inputRef.value?.focus()
const blur = () => inputRef.value?.blur()

// Generate unique IDs
const generatedId = useId()
const inputId = computed(() => props.id || `input-${generatedId}`)
const errorId = computed(() => `${inputId.value}-error`)

// Two-way binding
const modelValue = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

// Container classes
const containerClasses = computed(() => ({
  'input-container--error': !!props.error,
  'input-container--disabled': props.disabled
}))

// Expose input ref for programmatic focus
defineExpose({
  focus,
  blur,
  inputRef
})
</script>

<style scoped>
/* Mongoose.gg design system text field: pill, bg ground, divider border, shadow-focus */
.input-wrapper {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.input-label {
  font-size: 0.875rem;
  font-weight: 500;
  line-height: 1.5;
  color: var(--color-ink-soft);
}

.input-required {
  color: var(--color-text-secondary);
  margin-left: 2px;
}

/* Input container */
.input-container {
  position: relative;
  display: flex;
  align-items: center;
}

.input-field {
  width: 100%;
  height: 3rem;
  box-sizing: border-box;
  padding: 0 1.25rem;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  font-family: var(--font-body);
  font-size: 1rem;
  font-weight: 400;
  color: var(--color-text);
  transition: border-color 150ms ease-out;
}

.input-field::placeholder {
  color: var(--color-ink-faint);
}

.input-field:hover:not(:disabled) {
  border-color: var(--color-border-highlight);
}

.input-field:focus-visible {
  outline: none;
  border-color: var(--color-primary-accent);
  box-shadow: var(--shadow-focus);
}

.input-field:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

/* Icon support */
.input-icon {
  position: absolute;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-secondary);
  pointer-events: none;
}

.input-icon--left {
  left: 1.25rem;
}

.input-icon--right {
  right: 1.25rem;
}

.input-field.has-icon-left {
  padding-left: 3.25rem;
}

.input-field.has-icon-right {
  padding-right: 3.25rem;
}

/* Error state (form validation is a system state: error colour) */
.input-container--error .input-field {
  border-color: var(--color-error);
}

.input-error {
  font-size: 0.8125rem;
  line-height: 1.4;
  color: var(--color-error);
}

.input-hint {
  font-size: 0.8125rem;
  line-height: 1.4;
  color: var(--color-text-secondary);
}

@media (prefers-reduced-motion: reduce) {
  .input-field {
    transition: none;
  }
}
</style>
