<template>
  <div
    class="mp-usual"
    role="meter"
    :aria-label="label"
    :aria-valuemin="min"
    :aria-valuemax="max"
    :aria-valuenow="now"
    :aria-valuetext="valueText"
  >
    <div class="mp-usual__head">
      <span class="mp-usual__label">{{ label }}</span>
      <span class="mp-usual__value">{{ value }}</span>
    </div>
    <div class="mp-usual__track">
      <span class="mp-usual__fill" :class="{ 'mp-usual__fill--warn': isBehindUsual }" :style="{ width: `${fillPercent}%` }" />
      <span class="mp-usual__tick" :style="{ left: `${tickPercent}%` }" />
    </div>
  </div>
</template>

<script setup>
/**
 * UsualMeter (design system): one stat of a match against the player's own usual — a bar for this
 * match, a tick for what they normally do. The fill's colour follows whether this match beat the
 * usual (by `better`'s direction), not the bar's length: a longer "worse" bar (e.g. more deaths)
 * still shows warn.
 */
import { computed } from 'vue'

const props = defineProps({
  /** Sentence-case stat label, e.g. "Gold lead at 10" */
  label: { type: String, required: true },
  /** This match's value, already formatted for display, e.g. "+1,240" */
  value: { type: [String, Number], required: true },
  /** This match's raw numeric value, for the fill position */
  now: { type: Number, required: true },
  min: { type: Number, required: true },
  max: { type: Number, required: true },
  /** The player's usual (raw numeric average), for the tick position */
  usual: { type: Number, required: true },
  /** Which direction is better for this stat */
  better: { type: String, default: 'higher', validator: (value) => ['higher', 'lower'].includes(value) },
  /** aria-valuetext stating both numbers, e.g. "+1,240 gold, your usual is +180" */
  valueText: { type: String, required: true }
})

const range = computed(() => (props.max - props.min) || 1)

function toPercent(rawValue) {
  const clamped = Math.min(props.max, Math.max(props.min, rawValue))
  const percent = ((clamped - props.min) / range.value) * 100
  // Round away binary floating-point noise (e.g. 54.50000000000001) before it hits the DOM
  return Math.round(percent * 100) / 100
}

const fillPercent = computed(() => toPercent(props.now))
const tickPercent = computed(() => toPercent(props.usual))

const isBehindUsual = computed(() =>
  props.better === 'lower' ? props.now > props.usual : props.now < props.usual
)
</script>
