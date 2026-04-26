<template>
  <section class="analysis-layout" data-testid="analysis-layout">
    <h1 class="sr-only">{{ pageTitle }}</h1>

    <!-- Zone 1: Context Bar (filters, time range) -->
    <header
      v-if="$slots['context-bar']"
      class="zone zone-context-bar"
      data-testid="zone-context-bar"
    >
      <slot name="context-bar"></slot>
    </header>

    <!-- Zone 2: Summary Stats -->
    <div
      v-if="$slots.summary"
      class="zone zone-summary"
      data-testid="zone-summary"
    >
      <slot name="summary"></slot>
    </div>

    <!-- Zone 3: Trend Charts (3-column grid - 1/3 width each) -->
    <div
      v-if="$slots['trend-charts']"
      class="zone zone-trend-charts"
      data-testid="zone-trend-charts"
    >
      <slot name="trend-charts"></slot>
    </div>

    <!-- Zone 4: Deep Analysis (conditional - only renders when content provided) -->
    <div
      v-if="$slots['deep-analysis']"
      class="zone zone-deep-analysis"
      data-testid="zone-deep-analysis"
    >
      <slot name="deep-analysis"></slot>
    </div>

    <!-- Zone 5: Goals (conditional - only renders when content provided) -->
    <div
      v-if="$slots.goals"
      class="zone zone-goals"
      data-testid="zone-goals"
    >
      <slot name="goals"></slot>
    </div>
  </section>
</template>

<script setup>
/**
 * AnalysisLayout - Zone-based layout for Solo and Team analysis pages
 *
 * Zones:
 * 1. context-bar: Filters, time range selector
 * 2. summary: Summary stats (games, winrate, KDA)
 * 3. trend-charts: 2-column grid for LP (Solo/Flex) and Winrate charts
 * 4. deep-analysis: Danger zones, champion matrix (v2)
 * 5. goals: Active goals with progress (v2)
 *
 * Zones 4 and 5 only render when slot content is provided.
 */

defineProps({
  /** Page title for screen readers */
  pageTitle: {
    type: String,
    default: 'Analysis Dashboard'
  },
  /** Match ID for "View Analysis" highlight mode */
  matchId: {
    type: String,
    default: null
  }
})
</script>

<style scoped>
.analysis-layout {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg) var(--spacing-2xl);
}

/* Zone base styles */
.zone {
  width: 100%;
}

/* Zone 1: Context Bar */
.zone-context-bar {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: var(--spacing-sm);
}

/* Zone 2: Summary */
.zone-summary {
  /* Summary stats card fills width */
}

/* Zone 3: Trend Charts - 2-column equal-width grid */
.zone-trend-charts {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--spacing-lg);
}

/* Responsive: stack on mobile */
@media (max-width: 768px) {
  .analysis-layout {
    padding: var(--spacing-md) var(--spacing-lg);
  }

  .zone-trend-charts {
    grid-template-columns: 1fr;
  }
}

/* Zone 4: Deep Analysis — full-width */
.zone-deep-analysis {
  width: 100%;
}

@media (max-width: 768px) {
  .zone-deep-analysis {
    max-width: 100%;
  }
}

/* Zone 5: Goals */
.zone-goals {
  /* Future: styling for goals section */
}
</style>

