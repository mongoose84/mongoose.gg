<template>
  <div class="goal-strip" data-testid="goal-strip">
    <div class="goal-strip__cells" role="img" :aria-label="ariaLabel" data-testid="goal-strip-cells">
      <span
        v-for="(result, index) in results"
        :key="index"
        class="goal-strip__cell"
        :class="{ 'is-hit': result === 'hit', 'is-miss': result === 'miss', 'is-none': result !== 'hit' && result !== 'miss' }"
        data-testid="goal-strip-cell"
      />
    </div>
    <div class="mp-form-scale" aria-hidden="true">
      <span>{{ results.length }} matches ago</span>
      <span>Latest</span>
    </div>
  </div>
</template>

<script setup>
/**
 * Goal strip (design-system pattern, Solo focus): one cell per match, oldest on the left, for
 * whether the player hit a mark. Hit is a filled primary cell, a miss a warn ring, and a match
 * where the mark doesn't apply a small track stub, so shape carries the result too.
 */
import { computed } from 'vue'

const props = defineProps({
  /** 'hit', 'miss' or null per match, oldest first */
  results: { type: Array, required: true },
  /** What the mark is, for the text alternative ("0.9+ vision per minute") */
  markLabel: { type: String, required: true }
})

const ariaLabel = computed(() => {
  const words = props.results.map((r) => (r === 'hit' ? 'hit' : r === 'miss' ? 'missed' : 'not counted'))
  const hits = props.results.filter((r) => r === 'hit').length
  const counted = props.results.filter((r) => r === 'hit' || r === 'miss').length
  return `${props.markLabel}, last ${props.results.length} matches, oldest to newest: ${words.join(', ')}. Hit in ${hits} of ${counted}.`
})
</script>

<style scoped>
.goal-strip {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.goal-strip__cells {
  display: grid;
  grid-template-columns: repeat(20, minmax(0, 1fr));
  align-items: center;
  gap: 3px;
  height: 1.25rem;
}

.goal-strip__cell {
  height: 1.25rem;
  border-radius: 3px;
}

.goal-strip__cell.is-hit {
  background: var(--color-primary-accent);
}

.goal-strip__cell.is-miss {
  border: 2px solid var(--color-warn);
}

.goal-strip__cell.is-none {
  height: 0.375rem;
  background: var(--color-track-strong);
}
</style>
