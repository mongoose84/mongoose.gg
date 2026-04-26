<template>
  <div v-if="highlights && highlights.length" class="match-highlights" data-testid="match-highlights">
    <div class="highlights-grid">
      <HighlightTile
        v-for="(tile, idx) in visibleHighlights"
        :key="idx"
        :icon-type="tile.iconType"
        :stat-name="tile.statName"
        :insight-text="tile.insightText"
        :badge="tile.badge ?? null"
        :data-testid="`highlight-tile-${idx}`"
      />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import HighlightTile from './HighlightTile.vue'

const props = defineProps({
  /**
   * Array of highlight tile data.
   * Each item: { iconType, statName, insightText, badge? }
   */
  highlights: {
    type: Array,
    default: () => []
  }
})

/** Show at most 4 tiles in a 2×2 grid */
const visibleHighlights = computed(() => props.highlights.slice(0, 4))
</script>

<style scoped>
.match-highlights {
  width: 100%;
}

.highlights-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--spacing-sm);
}

@media (max-width: 640px) {
  .highlights-grid {
    grid-template-columns: 1fr;
  }
}
</style>
