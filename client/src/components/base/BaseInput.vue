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
  focus: () => inputRef.value?.focus(),
  blur: () => inputRef.value?.blur(),
  inputRef
})
</script>

<style scoped>
.input-wrapper {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.input-label {
  font-size: var(--font-size-sm);
  font-weight: 500;
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.input-required {
  color: var(--color-error);
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
  padding: var(--spacing-md);
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: var(--font-size-md);
  color: var(--color-text);
  transition: all 0.2s ease;
}

.input-field::placeholder {
  color: var(--color-text-secondary);
}

.input-field:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.input-field:disabled {
  opacity: 0.6;
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
  left: var(--spacing-md);
}

.input-icon--right {
  right: var(--spacing-md);
}

.input-field.has-icon-left {
  padding-left: calc(var(--spacing-md) * 2 + 1.25rem);
}

.input-field.has-icon-right {
  padding-right: calc(var(--spacing-md) * 2 + 1.25rem);
}

/* Error state */
.input-container--error .input-field {
  border-color: var(--color-error);
}

.input-container--error .input-field:focus {
  box-shadow: 0 0 0 3px var(--color-error-soft);
}

.input-error {
  font-size: var(--font-size-xs);
  color: var(--color-error);
}

.input-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}
</style>

