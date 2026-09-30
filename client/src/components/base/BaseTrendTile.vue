<template>
  <li class="mp-stat trend-tile" role="group" :aria-label="description" data-testid="trend-tile">
    <div class="trend-tile__text">
      <span class="mp-stat__label" data-testid="trend-tile-label">{{ label }}</span>
      <span class="mp-stat__value" data-testid="trend-tile-value">
        {{ value }}<span v-if="unit" class="trend-tile__unit">{{ `\u00a0${unit}` }}</span>
      </span>
      <span class="mp-stat__note" :class="toneClass" data-testid="trend-tile-verdict">
        <span v-if="arrow" aria-hidden="true">{{ `${arrow}\u00a0` }}</span>{{ verdict }}<span v-if="was" class="trend-tile__was">{{ ` · was ${was}` }}</span>
      </span>
    </div>

    <svg
      v-if="hasChart"
      class="trend-tile__chart"
      :viewBox="`0 0 ${WIDTH} ${HEIGHT}`"
      preserveAspectRatio="none"
      aria-hidden="true"
      focusable="false"
      data-testid="trend-tile-chart"
    >
      <line
        v-if="benchmarkY !== null"
        class="trend-tile__benchmark"
        x1="0"
        :x2="WIDTH"
        :y1="benchmarkY"
        :y2="benchmarkY"
        vector-effect="non-scaling-stroke"
        data-testid="trend-tile-benchmark"
      />
      <!-- Zero-length round-capped paths stay circles when the chart stretches -->
      <path
        v-for="dot in dots"
        :key="`dot-${dot.index}`"
        class="trend-tile__dot"
        :d="`M${dot.x} ${dot.y}h0`"
        vector-effect="non-scaling-stroke"
        data-testid="trend-tile-dot"
      />
      <polyline
        v-if="linePoints"
        class="trend-tile__line"
        :points="linePoints"
        vector-effect="non-scaling-stroke"
        data-testid="trend-tile-line"
      />
      <path
        v-if="lastPoint"
        class="trend-tile__last"
        :d="`M${lastPoint.x} ${lastPoint.y}h0`"
        vector-effect="non-scaling-stroke"
      />
    </svg>
  </li>
</template>

<script setup>
/**
 * TrendTile (design-system pattern, Solo page): one stat's trend over the range. The number is
 * the current 10-match average, the note the verdict with the number's direction, and the
 * sparkline shows each match as a dot, the rolling average as the line and the benchmark as a
 * dashed line. "Lower is better" stats flip the verdict, never the chart.
 */
import { computed } from 'vue'

const WIDTH = 200
const HEIGHT = 56
const PAD = 5

const props = defineProps({
  /** Stat label in sentence case ("Gold lead at 15") */
  label: { type: String, required: true },
  /** The current average, formatted ("4.1") */
  value: { type: String, required: true },
  /** Unit after the value ("per match") */
  unit: { type: String, default: '' },
  /** The earlier average, formatted; null without a verdict */
  was: { type: String, default: null },
  /** Verdict word ("Improving", "Needs 20 matches") */
  verdict: { type: String, required: true },
  /** up (good), down (needs work) or neutral */
  tone: {
    type: String,
    default: 'neutral',
    validator: (value) => ['up', 'down', 'neutral'].includes(value)
  },
  /** ▲ or ▼ for the number's direction, or '' */
  arrow: { type: String, default: '' },
  /** Per-match values in range order (null where excluded); null when sampled */
  values: { type: Array, default: null },
  /** Rolling average points: [{ index, value }] */
  rolling: { type: Array, default: () => [] },
  /** Matches in range: the width of the x axis */
  length: { type: Number, required: true },
  /** Benchmark value for the dashed line, or null */
  benchmark: { type: Number, default: null },
  /** The tile's text alternative (range, change, benchmark) */
  description: { type: String, required: true }
})

const presentValues = computed(() =>
  (props.values ?? [])
    .map((value, index) => ({ index, value }))
    .filter((p) => typeof p.value === 'number')
)

const hasChart = computed(() => props.length > 1 && (presentValues.value.length > 0 || props.rolling.length > 0))

// The y scale covers dots, line and benchmark, so none of them is clipped
const scale = computed(() => {
  const all = [
    ...presentValues.value.map((p) => p.value),
    ...props.rolling.map((p) => p.value),
    ...(props.benchmark !== null ? [props.benchmark] : [])
  ]
  const min = Math.min(...all)
  const max = Math.max(...all)
  const span = max - min || Math.abs(max) || 1
  return { min: min - span * 0.1, max: max + span * 0.1 }
})

function x(index) {
  return PAD + (index / Math.max(props.length - 1, 1)) * (WIDTH - PAD * 2)
}

function y(value) {
  const { min, max } = scale.value
  return PAD + (1 - (value - min) / (max - min)) * (HEIGHT - PAD * 2)
}

function round(n) {
  return Math.round(n * 10) / 10
}

const dots = computed(() => presentValues.value.map((p) => ({ index: p.index, x: round(x(p.index)), y: round(y(p.value)) })))

const linePoints = computed(() => {
  if (props.rolling.length < 2) return null
  return props.rolling.map((p) => `${round(x(p.index))},${round(y(p.value))}`).join(' ')
})

const lastPoint = computed(() => {
  const last = props.rolling[props.rolling.length - 1]
  return last ? { x: round(x(last.index)), y: round(y(last.value)) } : null
})

const benchmarkY = computed(() => (props.benchmark !== null ? round(y(props.benchmark)) : null))

const toneClass = computed(() => ({ 'mp-up': props.tone === 'up', 'mp-down': props.tone === 'down' }))
</script>

<style scoped>
.trend-tile {
  gap: 0.75rem;
}

.trend-tile__text {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.trend-tile__unit {
  font-family: var(--font-body);
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

.trend-tile__was {
  color: var(--color-text-secondary);
  font-weight: 400;
}

.trend-tile__chart {
  display: block;
  width: 100%;
  height: 3.5rem;
  overflow: visible;
}

.trend-tile__dot {
  stroke: var(--color-track-strong);
  stroke-width: 4px;
  stroke-linecap: round;
}

.trend-tile__line {
  fill: none;
  stroke: var(--color-primary-accent);
  stroke-width: 2.5px;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.trend-tile__last {
  stroke: var(--color-primary-accent);
  stroke-width: 8px;
  stroke-linecap: round;
}

.trend-tile__benchmark {
  stroke: var(--color-text-secondary);
  stroke-width: 1px;
  stroke-dasharray: 4 4;
}

/* Phones: tiles become rows, text left and the sparkline right */
@media (max-width: 899px) {
  .trend-tile {
    display: grid;
    grid-template-columns: minmax(0, 1fr) minmax(0, 9rem);
    align-items: center;
  }
}
</style>
