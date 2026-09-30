<template>
  <svg
    class="death-map"
    viewBox="-8 -8 116 116"
    role="img"
    :aria-label="description"
    data-testid="death-map"
  >
    <rect class="death-map__ground" x="0" y="0" width="100" height="100" rx="6" />
    <!-- Bases, lanes and river, the player's base bottom left -->
    <path class="death-map__base" d="M0 82 L0 100 L18 100 L18 82 Z" />
    <path class="death-map__base" d="M82 0 L100 0 L100 18 L82 18 Z" />
    <polyline class="death-map__lane" points="7,92 7,7 92,7" />
    <polyline class="death-map__lane" points="8,93 93,93 93,8" />
    <line class="death-map__lane" x1="10" y1="90" x2="90" y2="10" />
    <line class="death-map__river" x1="4" y1="4" x2="96" y2="96" />

    <circle
      v-for="zone in placed"
      :key="zone.key"
      class="death-map__zone"
      :class="{ 'death-map__zone--costly': zone.costly, 'death-map__zone--selected': zone.key === selectedKey }"
      :cx="zone.x"
      :cy="zone.y"
      :r="zone.r"
      :data-testid="`death-map-zone-${zone.key}`"
    />
  </svg>
</template>

<script setup>
/**
 * DeathMap (design-system pattern, Solo death zones): an outline of the map with one circle per
 * zone at its fixed anchor, sized by deaths, in warn when 30% or more cost an objective. Anchors
 * come with v up, so v is flipped for SVG. role="img" with every zone in the text alternative;
 * the zone list next to it is the interactive part.
 */
import { computed } from 'vue'

const MIN_RADIUS = 4
// The view box leaves 8 units around the map, so an edge circle is never clipped
const MAX_RADIUS = 8

const props = defineProps({
  /** [{ key, deaths, costly, anchor: { u, v } }] */
  zones: { type: Array, required: true },
  /** Key of the zone the breakdowns are filtered to */
  selectedKey: { type: String, default: null },
  /** Every zone said in words */
  description: { type: String, required: true }
})

const placed = computed(() => {
  const most = Math.max(1, ...props.zones.map((z) => z.deaths))
  return props.zones.map((z) => ({
    key: z.key,
    costly: z.costly,
    x: Math.round(z.anchor.u * 1000) / 10,
    y: Math.round((1 - z.anchor.v) * 1000) / 10,
    // Area, not radius, follows deaths
    r: Math.round((MIN_RADIUS + (MAX_RADIUS - MIN_RADIUS) * Math.sqrt(z.deaths / most)) * 10) / 10
  }))
})
</script>

<style scoped>
.death-map {
  display: block;
  width: 100%;
  max-width: 21.25rem;
  aspect-ratio: 1;
}

.death-map__ground {
  fill: var(--color-elevated);
}

.death-map__base {
  fill: var(--color-track);
}

.death-map__lane {
  fill: none;
  stroke: var(--color-track-strong);
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.death-map__river {
  stroke: var(--color-border);
  stroke-width: 3;
  stroke-dasharray: 2 2;
}

.death-map__zone {
  fill: var(--color-primary-accent);
  fill-opacity: 0.55;
  stroke: var(--color-primary-accent);
  stroke-width: 0.6;
}

.death-map__zone--costly {
  fill: var(--color-warn);
  stroke: var(--color-warn);
}

.death-map__zone--selected {
  fill-opacity: 0.9;
  stroke: var(--color-text);
  stroke-width: 1;
}
</style>
