<template>
  <span
    class="mp-skeleton"
    :class="variantClass"
    :style="sizeStyle"
    aria-hidden="true"
    data-testid="base-skeleton"
  />
</template>

<script setup>
/**
 * Skeleton (design system): one track-coloured shape standing in for content that is loading.
 * Compose several in the exact layout of the coming content; the container that holds them
 * sets aria-busy and a visually hidden "Loading …" label.
 */
import { computed } from 'vue'

const props = defineProps({
  /** text (one line), title, ring (score ring), portrait (48px round) or block (any box) */
  variant: {
    type: String,
    default: 'text',
    validator: (value) => ['text', 'title', 'ring', 'portrait', 'block'].includes(value)
  },
  /** CSS width, e.g. "60%" or "12rem" */
  width: {
    type: String,
    default: null
  },
  /** CSS height, e.g. "18.75rem" (mainly for block) */
  height: {
    type: String,
    default: null
  }
})

const variantClass = computed(() => (props.variant === 'block' ? null : `mp-skeleton--${props.variant}`))

const sizeStyle = computed(() => ({
  ...(props.width ? { width: props.width } : {}),
  ...(props.height ? { height: props.height } : {})
}))
</script>
