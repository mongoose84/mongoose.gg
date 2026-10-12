<template>
  <section class="mp-card gold-timeline" aria-labelledby="gold-timeline-title" data-testid="gold-timeline-card">
    <header class="gold-timeline__header">
      <h3 id="gold-timeline-title" class="mp-card-title" data-testid="gold-timeline-title">{{ title }}</h3>
      <p class="gold-timeline__subtitle" data-testid="gold-timeline-subtitle">{{ subtitle }}</p>
    </header>

    <BaseLineChart
      :points="chart.points"
      :length="chart.length"
      :guides="chart.guides"
      :markers="chart.markers"
      :moments="chart.moments"
      :aria-label="description"
      height="10rem"
    />

    <p v-if="chart.moments.length" class="gold-timeline__key" aria-hidden="true" data-testid="gold-timeline-key">
      D dragon · B Baron · H Herald · T tower ·
      <span class="gold-timeline__ours">your team</span> ·
      <span class="gold-timeline__theirs">enemy</span>
    </p>
    <p v-if="swingCaption" class="gold-timeline__caption" data-testid="gold-timeline-swing">{{ swingCaption }}</p>

    <div class="visually-hidden">
      <p>Gold lead</p>
      <ul>
        <li v-for="line in leadLines" :key="line">{{ line }}</li>
      </ul>
      <template v-if="momentLines.length">
        <p>Objectives</p>
        <ul>
          <li v-for="(line, i) in momentLines" :key="`moment-${i}`">{{ line }}</li>
        </ul>
      </template>
    </div>
  </section>
</template>

<script setup>
/**
 * The open match's gold lead over time (design system "Charts"): a takeaway title, one primary
 * line around an "Even" guide, the biggest swing as the one orange point, and the objectives as
 * lettered moments on the time axis with a one-line key. The numbers are repeated in a
 * screen-reader list. All copy lives in utils/goldTimeline.js.
 */
import { computed } from 'vue'
import BaseLineChart from '../base/BaseLineChart.vue'
import {
  buildGoldChart,
  buildGoldSubtitle,
  buildGoldTitle,
  buildSwingCaption,
  describeGoldTimeline,
  describeLeads,
  describeMoments
} from '../../utils/goldTimeline'

const props = defineProps({
  /** GoldTimeline from the match-details response */
  timeline: {
    type: Object,
    required: true
  },
  /** Whether the player's team won, for the title */
  win: {
    type: Boolean,
    required: true
  }
})

const title = computed(() => buildGoldTitle(props.timeline, props.win))
const subtitle = computed(() => buildGoldSubtitle(props.timeline))
const chart = computed(() => buildGoldChart(props.timeline))
const description = computed(() => describeGoldTimeline(props.timeline))
const swingCaption = computed(() => buildSwingCaption(props.timeline))
const leadLines = computed(() => describeLeads(props.timeline))
const momentLines = computed(() => describeMoments(props.timeline))
</script>

<style scoped>
.gold-timeline {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.gold-timeline__header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.gold-timeline__subtitle,
.gold-timeline__caption {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-ink-soft);
}

.gold-timeline__key {
  margin: 0;
  font-size: 0.75rem;
  color: var(--color-text-secondary);
}

.gold-timeline__ours {
  font-weight: 700;
  color: var(--color-positive-text);
}

.gold-timeline__theirs {
  font-weight: 700;
  color: var(--color-warn-text);
}
</style>
