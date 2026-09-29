<template>
  <li class="win-factor" data-testid="win-factor-row">
    <div class="win-factor__head">
      <span class="win-factor__label" data-testid="win-factor-label">{{ label }}</span>
      <span class="win-factor__rates" aria-hidden="true">
        <span class="win-factor__hit">{{ hitWinRate }}%</span>
        <span class="win-factor__vs">vs</span>
        <span class="win-factor__miss">{{ missWinRate }}%</span>
      </span>
    </div>
    <div class="win-factor__track" role="img" :aria-label="description" data-testid="win-factor-track">
      <span class="win-factor__span" :style="spanStyle" />
      <span class="win-factor__dot win-factor__dot--miss" :style="{ left: `${clamp(missWinRate)}%` }" data-testid="win-factor-miss" />
      <span class="win-factor__dot win-factor__dot--hit" :style="{ left: `${clamp(hitWinRate)}%` }" data-testid="win-factor-hit" />
    </div>
  </li>
</template>

<script setup>
/**
 * Win-factor row (design-system pattern, Solo page): win rate when the player hit a mark
 * (filled primary dot) against when they missed it (warn ring), on one 0–100% track, with the
 * two rates written above it. A negative gap draws the same way, the hit dot left of the miss.
 */
import { computed } from 'vue'

const props = defineProps({
  /** The mark in words ("Ahead at 15 minutes") */
  label: { type: String, required: true },
  /** Win rate (0–100) in matches that hit the mark */
  hitWinRate: { type: Number, required: true },
  /** Win rate (0–100) in matches that missed it */
  missWinRate: { type: Number, required: true },
  /** The row said in words, with both rates and match counts */
  description: { type: String, required: true }
})

function clamp(value) {
  return Math.max(0, Math.min(100, value))
}

const spanStyle = computed(() => {
  const from = clamp(Math.min(props.hitWinRate, props.missWinRate))
  const to = clamp(Math.max(props.hitWinRate, props.missWinRate))
  return { left: `${from}%`, width: `${to - from}%` }
})
</script>

<style scoped>
.win-factor {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
  padding: 0.875rem 0;
  border-top: 1px solid var(--color-border);
}

.win-factor__head {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 1rem;
}

.win-factor__label {
  font-size: 0.9375rem;
  line-height: 1.4;
  color: var(--color-text);
}

.win-factor__rates {
  display: inline-flex;
  align-items: baseline;
  gap: 0.375rem;
  font-family: var(--font-display);
  font-size: 1rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

.win-factor__hit {
  color: var(--color-positive-text);
}

.win-factor__miss {
  color: var(--color-warn-text);
}

.win-factor__vs {
  font-family: var(--font-body);
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

/* Dots sit on the track; their centres mark the rates */
.win-factor__track {
  position: relative;
  height: 0.875rem;
  margin: 0 0.4375rem;
}

.win-factor__track::before {
  content: "";
  position: absolute;
  left: 0;
  right: 0;
  top: 50%;
  height: 2px;
  transform: translateY(-50%);
  background: var(--color-track);
}

.win-factor__span {
  position: absolute;
  top: 50%;
  height: 2px;
  transform: translateY(-50%);
  background: var(--color-track-strong);
}

.win-factor__dot {
  position: absolute;
  top: 50%;
  width: 0.875rem;
  height: 0.875rem;
  border-radius: 999px;
  transform: translate(-50%, -50%);
}

.win-factor__dot--hit {
  background: var(--color-primary-accent);
}

.win-factor__dot--miss {
  background: var(--color-surface);
  border: 2px solid var(--color-warn);
}
</style>
