<template>
  <figure class="death-map-figure">
    <svg
      class="death-map"
      viewBox="-8 -8 116 116"
      role="img"
      :aria-label="description"
      data-testid="death-map"
    >
      <defs>
        <clipPath id="death-map-clip">
          <rect x="0" y="0" width="100" height="100" rx="6" />
        </clipPath>
      </defs>
      <!-- Summoner's Rift minimap, the player's base bottom left -->
      <image
        class="death-map__ground"
        :href="MAP_IMAGE"
        x="0"
        y="0"
        width="100"
        height="100"
        preserveAspectRatio="none"
        clip-path="url(#death-map-clip)"
        data-testid="death-map-image"
      />

      <g v-for="zone in placed" :key="zone.key">
        <circle
          class="death-map__zone"
          :class="{ 'death-map__zone--costly': zone.costly, 'death-map__zone--selected': zone.key === selectedKey }"
          :cx="zone.x"
          :cy="zone.y"
          :r="zone.r"
          :data-testid="`death-map-zone-${zone.key}`"
        />
        <text
          class="death-map__count"
          :x="zone.x"
          :y="zone.y"
          :data-testid="`death-map-count-${zone.key}`"
        >{{ zone.deaths }}</text>
      </g>
    </svg>

    <figcaption class="death-map__key" data-testid="death-map-key">
      <span class="death-map__key-item">
        <span class="death-map__swatch" aria-hidden="true" />
        Deaths
      </span>
      <span class="death-map__key-item">
        <span class="death-map__swatch death-map__swatch--costly" aria-hidden="true" />
        Often cost an objective
      </span>
    </figcaption>
  </figure>
</template>

<script setup>
/**
 * DeathMap (design-system pattern, Solo death zones): the Summoner's Rift minimap with one circle
 * per zone at its fixed anchor, sized by deaths and labelled with the count. Circles are neutral,
 * and warn only when 30% or more of the zone's deaths cost an objective; the key under the map says
 * so. Anchors come with v up, so v is flipped for SVG. role="img" with every zone in the text
 * alternative; the zone list next to it is the interactive part.
 */
import { computed } from 'vue'

const MAP_IMAGE = '/assets/images/summoners-rift-minimap.png'
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
    deaths: z.deaths,
    costly: z.costly,
    x: Math.round(z.anchor.u * 1000) / 10,
    y: Math.round((1 - z.anchor.v) * 1000) / 10,
    // Area, not radius, follows deaths
    r: Math.round((MIN_RADIUS + (MAX_RADIUS - MIN_RADIUS) * Math.sqrt(z.deaths / most)) * 10) / 10
  }))
})
</script>

<style scoped>
.death-map-figure {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  width: 100%;
  max-width: 21.25rem;
  margin: 0;
}

.death-map {
  display: block;
  width: 100%;
  aspect-ratio: 1;
}

/* The minimap is always dark, so circles and counts use fixed light ink, not the theme's */
.death-map__zone {
  fill: var(--color-on-primary);
  fill-opacity: 0.3;
  stroke: var(--color-on-primary);
  stroke-width: 0.6;
}

.death-map__zone--costly {
  fill: var(--color-warn);
  fill-opacity: 0.7;
  stroke: var(--color-warn);
}

.death-map__zone--selected {
  fill-opacity: 0.85;
  stroke: var(--color-on-primary);
  stroke-width: 1.2;
}

.death-map__count {
  fill: var(--color-on-primary);
  stroke: var(--color-bg);
  stroke-width: 0.8;
  paint-order: stroke;
  font-family: var(--font-display);
  font-size: 4.5px;
  font-weight: 700;
  text-anchor: middle;
  dominant-baseline: central;
  pointer-events: none;
}

.death-map__key {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem 1rem;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.death-map__key-item {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
}

.death-map__swatch {
  width: 0.75rem;
  height: 0.75rem;
  border: 1px solid var(--color-text);
  border-radius: 999px;
  background: var(--color-ink-faint);
}

.death-map__swatch--costly {
  border-color: var(--color-warn);
  background: var(--color-warn);
}
</style>
