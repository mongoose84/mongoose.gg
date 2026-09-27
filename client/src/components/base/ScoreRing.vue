<template>
  <div
    class="score-ring"
    :class="{ 'score-ring--warn': isWarn }"
    :style="{ width: `${size}px`, height: `${size}px` }"
    role="meter"
    aria-valuemin="0"
    aria-valuemax="100"
    :aria-valuenow="clampedValue"
    :aria-valuetext="valueText"
    :aria-label="label"
    data-testid="score-ring"
  >
    <svg :width="size" :height="size" :viewBox="`0 0 ${size} ${size}`" aria-hidden="true">
      <circle class="score-ring__track" :cx="center" :cy="center" :r="radius" fill="none" :stroke-width="strokeWidth" />
      <circle
        class="score-ring__fill"
        :cx="center"
        :cy="center"
        :r="radius"
        fill="none"
        :stroke-width="strokeWidth"
        stroke-linecap="round"
        :stroke-dasharray="dashArray"
        :transform="`rotate(-90 ${center} ${center})`"
      />
    </svg>
    <span class="score-ring__value" :style="{ fontSize: valueFontSize }" data-testid="score-ring-value">{{ clampedValue }}</span>
  </div>
</template>

<script setup>
/**
 * ScoreRing (design system): a 0–100 score in a ring. Fill is primary at 70+, warn below.
 * The ring is a meter for assistive tech; the card around it supplies the label and delta.
 */
import { computed } from 'vue'

const props = defineProps({
  /** Score from 0 to 100 */
  value: {
    type: Number,
    required: true
  },
  /** What the score measures, e.g. "Laning" */
  label: {
    type: String,
    required: true
  },
  /** Ring diameter in px (140 in score cards, smaller in previews) */
  size: {
    type: Number,
    default: 140
  }
})

const clampedValue = computed(() => Math.round(Math.min(100, Math.max(0, props.value))))
const isWarn = computed(() => clampedValue.value < 70)

// 11px stroke on a 140px ring, scaled with the size
const strokeWidth = computed(() => Math.max(4, Math.round((props.size * 11) / 140)))
const center = computed(() => props.size / 2)
const radius = computed(() => (props.size - strokeWidth.value) / 2)
const circumference = computed(() => 2 * Math.PI * radius.value)
const dashArray = computed(() => {
  const filled = (clampedValue.value / 100) * circumference.value
  return `${filled.toFixed(1)} ${circumference.value.toFixed(1)}`
})
const valueFontSize = computed(() => `${((props.size * 0.29) / 16).toFixed(3)}rem`)

const valueText = computed(() => {
  const v = clampedValue.value
  const word = v >= 80 ? 'strong' : v >= 70 ? 'good' : v >= 50 ? 'needs work' : 'weak'
  return `${v} out of 100, ${word}`
})
</script>

<style scoped>
.score-ring {
  position: relative;
  flex-shrink: 0;
}

.score-ring svg {
  display: block;
}

.score-ring__track {
  stroke: var(--color-track);
}

.score-ring__fill {
  stroke: var(--color-primary-accent);
}

.score-ring--warn .score-ring__fill {
  stroke: var(--color-warn);
}

.score-ring__value {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--font-display);
  font-weight: 700;
  line-height: 1;
  color: var(--color-text);
  font-variant-numeric: tabular-nums;
}
</style>
