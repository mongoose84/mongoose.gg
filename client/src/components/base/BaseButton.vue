<template>
  <component
    :is="componentType"
    :to="to"
    :href="href"
    :type="isButton ? type : undefined"
    :disabled="isButton ? (disabled || loading) : undefined"
    :class="buttonClasses"
    v-bind="$attrs"
  >
    <!-- Loading spinner -->
    <span v-if="loading" class="btn-spinner" aria-hidden="true"></span>
    
    <!-- Icon slot (left) -->
    <slot name="icon-left"></slot>
    
    <!-- Default slot for button text -->
    <slot></slot>
    
    <!-- Icon slot (right) -->
    <slot name="icon-right"></slot>
  </component>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  /** Button variant: primary, secondary, ghost, destructive */
  variant: {
    type: String,
    default: 'primary',
    validator: (v) => ['primary', 'secondary', 'ghost', 'destructive'].includes(v)
  },
  /** Button size: sm, md, lg */
  size: {
    type: String,
    default: 'md',
    validator: (v) => ['sm', 'md', 'lg'].includes(v)
  },
  /** Whether the button is in a loading state */
  loading: {
    type: Boolean,
    default: false
  },
  /** Whether the button is disabled */
  disabled: {
    type: Boolean,
    default: false
  },
  /** Button type attribute (for native buttons) */
  type: {
    type: String,
    default: 'button'
  },
  /** Router-link destination (makes it a router-link) */
  to: {
    type: [String, Object],
    default: null
  },
  /** External link href (makes it an anchor) */
  href: {
    type: String,
    default: null
  },
  /** Full width button */
  block: {
    type: Boolean,
    default: false
  }
})

// Determine component type based on props
const componentType = computed(() => {
  if (props.to) return 'router-link'
  if (props.href) return 'a'
  return 'button'
})

const isButton = computed(() => componentType.value === 'button')

// Build class list
const buttonClasses = computed(() => {
  const classes = ['btn', `btn--${props.variant}`, `btn--${props.size}`]
  
  if (props.loading) classes.push('btn--loading')
  if (props.disabled && !props.loading) classes.push('btn--disabled')
  if (props.block) classes.push('btn--block')
  
  return classes
})
</script>

<style scoped>
/* Base button styles */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-xs);
  font-weight: 600;
  text-decoration: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.2s ease;
  border: 1px solid transparent;
  letter-spacing: var(--letter-spacing);
  white-space: nowrap;
}

.btn:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

/* Sizes */
.btn--sm {
  padding: var(--spacing-xs) var(--spacing-sm);
  font-size: var(--font-size-sm);
}

.btn--md {
  padding: var(--spacing-sm) var(--spacing-lg);
  font-size: var(--font-size-sm);
}

.btn--lg {
  padding: var(--spacing-md) var(--spacing-xl);
  font-size: var(--font-size-md);
}

/* Variants */
.btn--primary {
  background: var(--color-primary);
  color: white;
  box-shadow: var(--shadow-sm);
}

.btn--primary:hover:not(.btn--disabled):not(.btn--loading) {
  box-shadow: var(--shadow-md);
  transform: translateY(-1px);
}

.btn--secondary {
  background: transparent;
  color: var(--color-text-secondary);
  border-color: var(--color-border);
}

.btn--secondary:hover:not(.btn--disabled):not(.btn--loading) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.btn--ghost {
  background: transparent;
  color: var(--color-primary);
  border-color: transparent;
}

.btn--ghost:hover:not(.btn--disabled):not(.btn--loading) {
  background: var(--color-primary-soft);
}

.btn--destructive {
  background: var(--color-error);
  color: white;
}

.btn--destructive:hover:not(.btn--disabled):not(.btn--loading) {
  background: var(--color-error);
  filter: brightness(0.85);
  box-shadow: var(--shadow-sm);
}

/* States */
.btn--disabled,
.btn--loading {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none !important;
}

.btn--block {
  width: 100%;
}

/* Loading spinner */
.btn-spinner {
  display: inline-block;
  width: 1em;
  height: 1em;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: currentColor;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>

