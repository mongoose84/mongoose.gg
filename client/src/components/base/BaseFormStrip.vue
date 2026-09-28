<template>
  <div class="form-strip" data-testid="form-strip">
    <div
      class="mp-form"
      :style="{ gridTemplateColumns: `repeat(${bars.length}, minmax(0, 1fr))` }"
      role="img"
      :aria-label="ariaLabel"
      data-testid="form-strip-bars"
    >
      <span
        v-for="(result, index) in bars"
        :key="index"
        :class="barClass(result)"
        data-testid="form-strip-bar"
        :data-result="result"
      />
    </div>
    <div v-if="bars.length > 1" class="mp-form-scale" aria-hidden="true">
      <span>{{ oldestLabel }}</span>
      <span>Latest</span>
    </div>
  </div>
</template>

<script setup>
/**
 * FormStrip (design system): the player's recent results at a glance, one bar per match,
 * oldest on the left. Wins are tall purple bars, losses short orange bars, remakes a stub,
 * so height carries the result as well as colour. Not clickable (the rows are the links).
 */
import { computed } from 'vue'

const MAX_BARS = 20

const props = defineProps({
  /** Results oldest first: 'win', 'loss' or 'remake' */
  results: {
    type: Array,
    required: true
  }
})

// The newest matches when there are more than the strip holds
const bars = computed(() => props.results.slice(-MAX_BARS))

const oldestLabel = computed(() => `${bars.value.length} matches ago`)

function count(result) {
  return bars.value.filter((r) => r === result).length
}

function plural(n, word) {
  return `${n} ${word}${n === 1 ? '' : word.endsWith('s') ? 'es' : 's'}`
}

const ariaLabel = computed(() => {
  const n = bars.value.length
  const totals = [plural(count('win'), 'win'), plural(count('loss'), 'loss')]
  if (count('remake')) totals.push(plural(count('remake'), 'remake'))
  return `Last ${n} ${n === 1 ? 'match' : 'matches'}, oldest to newest: ${bars.value.join(', ')}. ${totals.join(', ')}.`
})

function barClass(result) {
  if (result === 'win') return 'is-win'
  if (result === 'remake') return 'is-remake'
  return null
}
</script>

<style scoped>
.form-strip {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}
</style>
