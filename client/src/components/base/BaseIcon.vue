<template>
  <span
    class="base-icon"
    :style="{ width: `${size}px`, height: `${size}px` }"
    aria-hidden="true"
    data-testid="base-icon"
    :data-icon="name"
    v-html="svg"
  />
</template>

<script setup>
/**
 * Lucide icon from the design-system vocabulary (src/assets/icons, ISC licence).
 * Rendered inline so the stroke follows the surrounding text colour (currentColor).
 */
import { computed } from 'vue'

const files = import.meta.glob('@/assets/icons/*.svg', { query: '?raw', import: 'default', eager: true })

const icons = Object.fromEntries(
  Object.entries(files).map(([path, raw]) => [path.split('/').pop().replace('.svg', ''), raw])
)

const props = defineProps({
  /** Lucide icon name, e.g. "swords" */
  name: {
    type: String,
    required: true
  },
  /** 16 (chips, dense rows), 20 (buttons, rows) or 24 (navigation, empty states) */
  size: {
    type: Number,
    default: 20,
    validator: (value) => [16, 20, 24].includes(value)
  }
})

const svg = computed(() => {
  const raw = icons[props.name]
  if (!raw) {
    console.warn(`BaseIcon: unknown icon "${props.name}"`)
    return ''
  }
  return raw
    .replace(/\swidth="[^"]*"/, '')
    .replace(/\sheight="[^"]*"/, '')
    .replace('<svg', `<svg width="${props.size}" height="${props.size}" aria-hidden="true" focusable="false"`)
})
</script>

<style scoped>
.base-icon {
  display: inline-flex;
  flex-shrink: 0;
}

.base-icon :deep(svg) {
  display: block;
}
</style>
