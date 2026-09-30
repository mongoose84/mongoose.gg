<template>
  <div class="diverging-bar" role="img" :aria-label="description" data-testid="diverging-bar">
    <span class="mp-lane-bar__behind">
      <span v-if="value < 0" :style="{ width: share }" data-testid="diverging-bar-behind" />
    </span>
    <span class="mp-lane-bar__axis" />
    <span class="mp-lane-bar__ahead">
      <span v-if="value > 0" :style="{ width: share }" data-testid="diverging-bar-ahead" />
    </span>
  </div>
</template>

<script setup>
/**
 * Diverging bar (LaneBar shape without icons): a value around zero, growing right in primary
 * when positive and left in warn when negative, scaled to a shared maximum so rows compare.
 * The row around it writes the number; the bar is role="img" with the row said in words.
 */
import { computed } from 'vue'

const props = defineProps({
  /** The signed value (LP, net wins) */
  value: { type: Number, required: true },
  /** The largest absolute value among the rows: a full half */
  max: { type: Number, required: true },
  /** The row said in words */
  description: { type: String, required: true }
})

const share = computed(() => {
  const ratio = props.max > 0 ? Math.min(Math.abs(props.value) / props.max, 1) : 0
  return `${(ratio * 100).toFixed(1)}%`
})
</script>

<style scoped>
.diverging-bar {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 2px minmax(0, 1fr);
  align-items: center;
  height: 1.5rem;
}

/* A 0 still shows a sliver so the row never looks empty */
.mp-lane-bar__behind > span,
.mp-lane-bar__ahead > span {
  min-width: 3px;
}
</style>
