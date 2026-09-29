<template>
  <div
    class="mp-columns column-chart"
    role="img"
    :aria-label="ariaLabel"
    data-testid="column-chart"
  >
    <div v-for="group in visibleGroups" :key="group.key" data-testid="column-chart-group">
      <span class="mp-columns__value" :class="{ 'column-chart__value--weak': group.key === weakKey }">{{ formatValue(group.value) }}</span>
      <span
        class="mp-columns__bar"
        :class="{ 'mp-columns__bar--warn': group.key === weakKey }"
        :style="{ height: barHeight(group.value) }"
        data-testid="column-chart-bar"
      />
      <span class="mp-columns__label">{{ group.label }}</span>
    </div>
  </div>
</template>

<script setup>
/**
 * ColumnChart (design system): two to four groups compared on one measure. Columns are
 * primary; the weak spot the card title is about is warn. Values sit on top in Clash Display,
 * labels under them. No axis, grid lines or legend; the aria-label lists every value.
 */
import { computed } from 'vue'

const MAX_COLUMNS = 4

const props = defineProps({
  /** [{ key, label, value }] with value on a 0–max scale */
  groups: {
    type: Array,
    required: true
  },
  /** The top of the scale (100 for a percentage) */
  max: {
    type: Number,
    default: 100
  },
  /** Unit written after each value */
  unit: {
    type: String,
    default: '%'
  },
  /** Key of the group the finding is about, drawn in warn */
  weakKey: {
    type: String,
    default: null
  },
  /** What is measured, first words of the text alternative ("Win rate by start time") */
  measure: {
    type: String,
    required: true
  }
})

const visibleGroups = computed(() => props.groups.slice(0, MAX_COLUMNS))

function formatValue(value) {
  return `${value}${props.unit}`
}

// Leave room above and below the bar for the value and the label
function barHeight(value) {
  const share = Math.max(0, Math.min(value / props.max, 1))
  return `calc((100% - 3.25rem) * ${share.toFixed(3)})`
}

const ariaLabel = computed(() => {
  const unitWord = props.unit === '%' ? ' percent' : props.unit
  const parts = visibleGroups.value.map((g) => `${g.label.toLowerCase()} ${g.value}${unitWord}`)
  return `${props.measure}: ${parts.join(', ')}.`
})
</script>

<style scoped>
.column-chart__value--weak {
  color: var(--color-warn-text);
}

/* A column never disappears entirely at 0 */
.mp-columns__bar {
  min-height: 3px;
}
</style>
