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
/* Mongoose.gg design system Button: pill, flat, 150ms colour change, shadow-focus */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  border: 0;
  border-radius: 999px;
  font-family: var(--font-body);
  font-weight: 700;
  line-height: 1;
  text-decoration: none;
  white-space: nowrap;
  cursor: pointer;
  transition: background-color 150ms ease-out, color 150ms ease-out;
}

.btn:focus-visible {
  outline: none;
  box-shadow: var(--shadow-focus);
}

/* Sizes: 48px default for main actions, 36px only in dense rows */
.btn--sm {
  height: 2.25rem;
  padding: 0 1rem;
  font-size: 0.875rem;
}

.btn--md {
  height: 2.75rem;
  padding: 0 1.25rem;
  font-size: 0.875rem;
}

.btn--lg {
  height: 3rem;
  padding: 0 1.5rem;
  font-size: 0.9375rem;
}

/* Variants */
.btn--primary {
  background: var(--color-primary);
  color: var(--color-on-primary);
}

.btn--primary:hover:not(.btn--disabled):not(.btn--loading) {
  background: var(--color-primary-light);
}

.btn--secondary {
  background: var(--color-surface-selected);
  color: var(--color-text);
}

.btn--secondary:hover:not(.btn--disabled):not(.btn--loading) {
  background: var(--color-surface-selected-hover);
}

.btn--ghost {
  background: transparent;
  color: var(--color-positive-text);
}

.btn--ghost:hover:not(.btn--disabled):not(.btn--loading) {
  color: var(--color-positive-text-strong);
}

/* Destructive is a system state, so it keeps the error colour */
.btn--destructive {
  background: var(--color-error);
  color: var(--color-on-primary);
}

.btn--destructive:hover:not(.btn--disabled):not(.btn--loading) {
  background: var(--color-error-hover);
}

/* States */
.btn--disabled,
.btn--loading {
  opacity: 0.45;
  cursor: not-allowed;
}

.btn--block {
  width: 100%;
}

/* Loading spinner */
.btn-spinner {
  display: inline-block;
  width: 1em;
  height: 1em;
  border: 2px solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

@media (prefers-reduced-motion: reduce) {
  .btn {
    transition: none;
  }

  .btn-spinner {
    animation: none;
  }
}
</style>
