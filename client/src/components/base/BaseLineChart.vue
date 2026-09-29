<template>
  <div
    class="line-chart"
    role="img"
    :aria-label="ariaLabel"
    :style="{ height }"
    data-testid="line-chart"
  >
    <svg
      class="line-chart__svg"
      :viewBox="`0 0 ${WIDTH} ${HEIGHT}`"
      preserveAspectRatio="none"
      aria-hidden="true"
      focusable="false"
    >
      <line
        v-for="guide in placedGuides"
        :key="`guide-${guide.y}`"
        class="line-chart__guide"
        x1="0"
        :x2="WIDTH"
        :y1="guide.py"
        :y2="guide.py"
        vector-effect="non-scaling-stroke"
        data-testid="line-chart-guide"
      />
      <polyline
        v-if="linePoints"
        class="line-chart__line"
        :points="linePoints"
        vector-effect="non-scaling-stroke"
        data-testid="line-chart-line"
      />
      <!-- Zero-length round-capped paths stay circles when the chart stretches -->
      <path
        v-if="lastPoint"
        class="line-chart__last"
        :d="`M${lastPoint.px} ${lastPoint.py}h0`"
        vector-effect="non-scaling-stroke"
      />
      <path
        v-for="marker in placedMarkers"
        :key="`marker-${marker.x}`"
        class="line-chart__marker"
        :class="{ 'line-chart__marker--warn': marker.tone === 'warn' }"
        :d="`M${marker.px} ${marker.py}h0`"
        vector-effect="non-scaling-stroke"
        data-testid="line-chart-marker"
      />
    </svg>

    <span
      v-for="guide in placedGuides"
      :key="`guide-label-${guide.y}`"
      class="line-chart__guide-label"
      :style="{ top: percentY(guide.py) }"
      aria-hidden="true"
    >{{ guide.label }}</span>

    <span
      v-for="marker in labelledMarkers"
      :key="`marker-label-${marker.x}`"
      class="line-chart__marker-label"
      :class="{
        'line-chart__marker-label--warn': marker.tone === 'warn',
        'line-chart__marker-label--below': marker.py < HEIGHT * 0.3,
        'line-chart__marker-label--end': marker.px > WIDTH * 0.85,
        'line-chart__marker-label--start': marker.px < WIDTH * 0.15
      }"
      :style="{ left: percentX(marker.px), top: percentY(marker.py) }"
      aria-hidden="true"
      data-testid="line-chart-marker-label"
    >{{ marker.label }}</span>
  </div>
</template>

<script setup>
/**
 * Line chart (design system "Charts"): one primary line with its last point highlighted,
 * divider guide lines labelled in ink-faint, and labelled moments on the line (warn for a
 * drop). The x axis is the match index, so a match without a value keeps its place.
 * role="img" with the text alternative in ariaLabel; the numbers belong in the card around it.
 */
import { computed } from 'vue'

const WIDTH = 1000
const HEIGHT = 200
const PAD_Y = 14

const props = defineProps({
  /** [{ x, y }]: x is the match index in range, oldest first */
  points: { type: Array, required: true },
  /** Matches in range: the width of the x axis */
  length: { type: Number, required: true },
  /** Scale bounds; default to the data with some room */
  yMin: { type: Number, default: null },
  yMax: { type: Number, default: null },
  /** [{ y, label }] horizontal guides (divisions, 50%) */
  guides: { type: Array, default: () => [] },
  /** [{ x, y, label, tone: 'primary' | 'warn' }] moments on the line; an empty label draws only the dot */
  markers: { type: Array, default: () => [] },
  /** The chart's text alternative */
  ariaLabel: { type: String, required: true },
  /** CSS height of the chart */
  height: { type: String, default: '12.5rem' }
})

const scale = computed(() => {
  const values = [...props.points.map((p) => p.y), ...props.markers.map((m) => m.y)]
  const dataMin = values.length ? Math.min(...values) : 0
  const dataMax = values.length ? Math.max(...values) : 1
  const room = (dataMax - dataMin || 1) * 0.1
  const min = props.yMin ?? dataMin - room
  const max = props.yMax ?? dataMax + room
  return { min, max: max === min ? min + 1 : max }
})

function px(x) {
  return (x / Math.max(props.length - 1, 1)) * WIDTH
}

function py(y) {
  const { min, max } = scale.value
  return PAD_Y + (1 - (y - min) / (max - min)) * (HEIGHT - PAD_Y * 2)
}

function round(n) {
  return Math.round(n * 10) / 10
}

const placed = computed(() => props.points.map((p) => ({ ...p, px: round(px(p.x)), py: round(py(p.y)) })))

const linePoints = computed(() =>
  placed.value.length > 1 ? placed.value.map((p) => `${p.px},${p.py}`).join(' ') : null
)

const lastPoint = computed(() => placed.value[placed.value.length - 1] ?? null)

// Guides outside the scale are left out rather than drawn at the edge
const placedGuides = computed(() =>
  props.guides
    .filter((g) => g.y >= scale.value.min && g.y <= scale.value.max)
    .map((g) => ({ ...g, py: round(py(g.y)) }))
)

const placedMarkers = computed(() => props.markers.map((m) => ({ ...m, px: round(px(m.x)), py: round(py(m.y)) })))
const labelledMarkers = computed(() => placedMarkers.value.filter((m) => m.label))

function percentX(value) {
  return `${(value / WIDTH) * 100}%`
}

function percentY(value) {
  return `${(value / HEIGHT) * 100}%`
}
</script>

<style scoped>
.line-chart {
  position: relative;
  width: 100%;
}

.line-chart__svg {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  overflow: visible;
}

.line-chart__guide {
  stroke: var(--color-border);
  stroke-width: 1px;
}

.line-chart__line {
  fill: none;
  stroke: var(--color-primary-accent);
  stroke-width: 3px;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.line-chart__last {
  stroke: var(--color-primary-accent);
  stroke-width: 10px;
  stroke-linecap: round;
}

.line-chart__marker {
  stroke: var(--color-primary-accent);
  stroke-width: 8px;
  stroke-linecap: round;
}

.line-chart__marker--warn {
  stroke: var(--color-warn);
}

.line-chart__guide-label {
  position: absolute;
  left: 0;
  transform: translateY(-100%);
  padding-bottom: 0.125rem;
  font-size: 0.75rem;
  line-height: 1.2;
  color: var(--color-ink-faint);
  pointer-events: none;
}

.line-chart__marker-label {
  position: absolute;
  transform: translate(-50%, calc(-100% - 0.5rem));
  font-size: 0.75rem;
  font-weight: 700;
  line-height: 1.2;
  white-space: nowrap;
  color: var(--color-positive-text);
  pointer-events: none;
}

.line-chart__marker-label--warn {
  color: var(--color-warn-text);
}

.line-chart__marker-label--below {
  transform: translate(-50%, 0.5rem);
}

.line-chart__marker-label--end {
  transform: translate(-100%, calc(-100% - 0.5rem));
}

.line-chart__marker-label--end.line-chart__marker-label--below {
  transform: translate(-100%, 0.5rem);
}

.line-chart__marker-label--start {
  transform: translate(0, calc(-100% - 0.5rem));
}

.line-chart__marker-label--start.line-chart__marker-label--below {
  transform: translate(0, 0.5rem);
}
</style>
