<template>
  <component
    :is="componentType"
    :to="to"
    :href="href"
    :class="cardClasses"
    v-bind="$attrs"
  >
    <!-- Header slot -->
    <div v-if="$slots.header || title" class="card-header">
      <slot name="header">
        <h3 v-if="title" class="card-title">{{ title }}</h3>
        <p v-if="subtitle" class="card-subtitle">{{ subtitle }}</p>
      </slot>
    </div>
    
    <!-- Default content slot -->
    <div class="card-body" :class="{ 'card-body--no-padding': noPadding }">
      <slot></slot>
    </div>
    
    <!-- Footer slot -->
    <div v-if="$slots.footer" class="card-footer">
      <slot name="footer"></slot>
    </div>
  </component>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  /** Card variant: default, interactive, highlighted, elevated */
  variant: {
    type: String,
    default: 'default',
    validator: (v) => ['default', 'interactive', 'highlighted', 'elevated'].includes(v)
  },
  /** Card title (optional, can use header slot instead) */
  title: {
    type: String,
    default: null
  },
  /** Card subtitle */
  subtitle: {
    type: String,
    default: null
  },
  /** Remove body padding */
  noPadding: {
    type: Boolean,
    default: false
  },
  /** Router-link destination (makes card clickable) */
  to: {
    type: [String, Object],
    default: null
  },
  /** External link href (makes card clickable) */
  href: {
    type: String,
    default: null
  }
})

// Determine component type
const componentType = computed(() => {
  if (props.to) return 'router-link'
  if (props.href) return 'a'
  return 'div'
})

const isClickable = computed(() => !!props.to || !!props.href)

// Build class list
const cardClasses = computed(() => {
  const classes = ['card', `card--${props.variant}`]
  
  if (isClickable.value) {
    classes.push('card--clickable')
  }
  
  return classes
})
</script>

<style scoped>
.card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
  overflow: hidden;
  text-decoration: none;
  color: inherit;
}

/* Variants */
.card--default {
  /* Base styles already applied */
}

.card--interactive {
  transition: all 0.2s ease;
}

.card--interactive:hover {
  border-color: var(--color-primary);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.card--highlighted {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-lg);
}

.card--elevated {
  background: var(--color-elevated);
  box-shadow: var(--shadow-sm);
}

/* Clickable state */
.card--clickable {
  cursor: pointer;
  transition: all 0.2s ease;
}

.card--clickable:hover {
  border-color: var(--color-primary);
  background: var(--color-elevated);
}

/* Header */
.card-header {
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.card-title {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: 600;
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.card-subtitle {
  margin: var(--spacing-xs) 0 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Body */
.card-body {
  padding: var(--spacing-lg);
}

.card-body--no-padding {
  padding: 0;
}

/* Footer */
.card-footer {
  padding: var(--spacing-lg);
  border-top: 1px solid var(--color-border);
}
</style>

