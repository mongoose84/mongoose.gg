<template>
  <SoloCard
    test-id="solo-death-zones"
    title-id="solo-death-zones-title"
    :title="title"
    :caption="caption"
    error-title="Where your deaths cost you"
    loading-label="Loading your death zones"
    :loading="loading"
    :error="error"
    :empty="empty"
    @retry="$emit('retry')"
  >
    <template #skeleton>
      <BaseSkeleton variant="title" />
      <div class="death-zones__layout">
        <BaseSkeleton variant="block" height="18rem" />
        <div class="death-zones__skeleton-list">
          <BaseSkeleton v-for="n in 4" :key="n" variant="block" height="3.5rem" />
        </div>
        <BaseSkeleton variant="block" height="14rem" />
      </div>
    </template>

    <template #empty-action>
      <BaseButton
        v-if="empty?.showAllQueues"
        variant="secondary"
        data-testid="solo-death-zones-show-all"
        @click="$emit('show-all-queues')"
      >Show all queues</BaseButton>
      <BaseButton v-else :disabled="syncing" data-testid="solo-death-zones-sync" @click="$emit('sync')">
        <template #icon-left><BaseIcon name="refresh-cw" :size="20" /></template>
        Sync matches
      </BaseButton>
    </template>

    <!-- FR 39: the backfill fills older matches in; its progress takes the card's slot -->
    <div v-if="progress" class="death-zones__backfill" data-testid="solo-death-zones-backfill">
      <p class="death-zones__backfill-title" aria-live="polite">{{ progress.title }}</p>
      <div
        class="death-zones__bar"
        :class="{ 'death-zones__bar--indeterminate': !progress.determinate }"
        role="progressbar"
        aria-label="Adding detail to your older matches"
        aria-valuemin="0"
        :aria-valuemax="progress.determinate ? progress.total : undefined"
        :aria-valuenow="progress.determinate ? progress.done : undefined"
      >
        <span :style="progress.determinate ? { width: `${(progress.done / progress.total) * 100}%` } : null" />
      </div>
      <p v-if="progress.line" class="death-zones__backfill-line" data-testid="solo-death-zones-backfill-line">{{ progress.line }}</p>
    </div>

    <div v-else class="death-zones__layout">
      <BaseDeathMap :zones="data.zones" :selected-key="selectedKey" :description="mapDescription" />

      <ul class="death-zones__list" aria-label="Zones" data-testid="solo-death-zones-list">
        <li v-for="zone in data.zones" :key="zone.key">
          <button
            type="button"
            class="death-zones__zone"
            :aria-pressed="zone.key === selectedKey"
            :data-testid="`death-zone-${zone.key}`"
            @click="toggle(zone.key)"
          >
            <span class="death-zones__dot" :class="{ 'death-zones__dot--costly': zone.costly }" aria-hidden="true" />
            <span class="death-zones__zone-text">
              <span class="death-zones__zone-name">{{ zoneLabel(zone.key) }}</span>
              <span class="death-zones__zone-meta">{{ zoneSummary(zone) }}</span>
              <span class="death-zones__zone-meta">{{ zoneTimingNote(zone.timing) }}</span>
            </span>
          </button>
        </li>
      </ul>

      <div class="death-zones__breakdowns">
        <h3 class="death-zones__filter" aria-live="polite" data-testid="solo-death-zones-filter">{{ filterLabel }}</h3>
        <section v-for="group in groups" :key="group.key" class="death-zones__group" :aria-label="group.title">
          <h4>{{ group.title }}</h4>
          <ul>
            <li v-for="row in group.rows" :key="row.key" class="death-zones__row" :data-testid="`death-zones-${group.key}-${row.key}`">
              <span class="death-zones__row-label">{{ row.label }}</span>
              <span class="death-zones__row-track" aria-hidden="true">
                <span :class="{ 'death-zones__row-bar--warn': group.key === 'cost' }" :style="{ width: row.width }" />
              </span>
              <span class="death-zones__row-value">{{ row.value }}</span>
            </li>
          </ul>
        </section>
      </div>
    </div>
  </SoloCard>
</template>

<script setup>
/**
 * "Where your deaths cost you" (features/solo-trends.spec.md FR 31–39): the map with zone circles,
 * the zone list that filters the breakdowns (phase, how, what it cost), and the death detail
 * backfill's progress while older matches are still being read.
 */
import { computed, ref, watch } from 'vue'
import SoloCard from './SoloCard.vue'
import BaseDeathMap from '../base/BaseDeathMap.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseButton from '../base/BaseButton.vue'
import BaseIcon from '../base/BaseIcon.vue'
import {
  COST_LABELS,
  HOW_LABELS,
  PHASE_LABELS,
  buildBackfillProgress,
  buildDeathZonesCaption,
  buildDeathZonesEmpty,
  buildDeathZonesTitle,
  buildNoMatchesEmpty,
  describeDeathMap,
  zoneLabel,
  zoneSummary,
  zoneTimingNote
} from '@/utils/soloSummary'

const props = defineProps({
  /** death-zones response, or null before the first load */
  data: { type: Object, default: null },
  loading: { type: Boolean, default: false },
  error: { type: Boolean, default: false },
  queue: { type: String, default: null },
  syncing: { type: Boolean, default: false }
})

defineEmits(['retry', 'show-all-queues', 'sync'])

const selectedKey = ref(null)

// A zone that is no longer listed (new range or queue) returns the filter to all zones
watch(() => props.data?.zones, (zones) => {
  if (selectedKey.value && !zones?.some((z) => z.key === selectedKey.value)) selectedKey.value = null
})

const progress = computed(() => (props.data && !props.data.ready ? buildBackfillProgress(props.data.backfill) : null))

const empty = computed(() => {
  if (!props.data) return null
  if (props.data.matches === 0) return buildNoMatchesEmpty(props.queue)
  if (!props.data.ready && !props.data.backfill) return buildDeathZonesEmpty(props.data)
  return null
})

const title = computed(() => {
  if (!props.data) return ''
  if (progress.value) return 'Where your deaths cost you'
  return buildDeathZonesTitle(props.data.zones) ?? 'Your deaths are spread across the map'
})
const caption = computed(() => (props.data && !progress.value ? buildDeathZonesCaption(props.data) : null))
const mapDescription = computed(() => (props.data?.zones ? describeDeathMap(props.data) : ''))

function toggle(key) {
  selectedKey.value = selectedKey.value === key ? null : key
}

const filterLabel = computed(() => (selectedKey.value ? zoneLabel(selectedKey.value) : 'All zones'))

function rows(counts, labels) {
  const most = Math.max(1, ...Object.values(counts))
  return Object.entries(labels).map(([key, label]) => ({
    key,
    label,
    value: counts[key] ?? 0,
    width: `${((counts[key] ?? 0) / most) * 100}%`
  }))
}

const groups = computed(() => {
  const breakdowns = props.data?.breakdowns
  if (!breakdowns) return []
  const shown = (selectedKey.value && breakdowns.byZone[selectedKey.value]) || breakdowns.all
  return [
    { key: 'phase', title: 'When', rows: rows(shown.phase, PHASE_LABELS) },
    { key: 'how', title: 'How', rows: rows(shown.how, HOW_LABELS) },
    { key: 'cost', title: 'What it cost', rows: rows(shown.cost, COST_LABELS) }
  ]
})
</script>

<style scoped>
.death-zones__layout {
  display: grid;
  grid-template-columns: minmax(0, 21.25rem) minmax(0, 21.25rem) minmax(0, 1fr);
  gap: 1.5rem;
  align-items: start;
}

.death-zones__skeleton-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.death-zones__list {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.death-zones__zone {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  width: 100%;
  min-height: 2.75rem;
  padding: 0.625rem 0.75rem;
  border: 0;
  border-radius: 0.75rem;
  background: transparent;
  color: var(--color-text);
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.death-zones__zone:hover {
  background: var(--color-elevated);
}

.death-zones__zone[aria-pressed="true"] {
  background: var(--color-surface-selected);
}

.death-zones__zone:focus-visible {
  outline: none;
  box-shadow: var(--shadow-focus);
}

.death-zones__dot {
  flex-shrink: 0;
  width: 0.75rem;
  height: 0.75rem;
  margin-top: 0.25rem;
  border-radius: 999px;
  background: var(--color-primary-accent);
}

.death-zones__dot--costly {
  background: var(--color-warn);
}

.death-zones__zone-text {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
  min-width: 0;
}

.death-zones__zone-name {
  font-weight: 700;
  font-size: 0.9375rem;
}

.death-zones__zone-meta {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.death-zones__breakdowns {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  min-width: 0;
}

.death-zones__filter {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text);
}

.death-zones__group h4 {
  margin: 0 0 0.375rem;
  font-size: 0.8125rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

.death-zones__group ul {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.death-zones__row {
  display: grid;
  grid-template-columns: minmax(0, 10rem) minmax(0, 1fr) 2rem;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.8125rem;
  color: var(--color-ink-soft);
}

.death-zones__row-track {
  height: 0.5rem;
  border-radius: 999px;
  background: var(--color-track);
}

.death-zones__row-track > span {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--color-primary-accent);
}

.death-zones__row-track > span.death-zones__row-bar--warn {
  background: var(--color-warn);
}

.death-zones__row-value {
  font-family: var(--font-display);
  font-weight: 600;
  text-align: right;
  color: var(--color-text);
  font-variant-numeric: tabular-nums;
}

.death-zones__backfill {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
  max-width: 36rem;
}

.death-zones__backfill-title {
  margin: 0;
  font-weight: 700;
  color: var(--color-text);
}

.death-zones__backfill-line {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-ink-soft);
}

.death-zones__bar {
  position: relative;
  height: 0.5rem;
  overflow: hidden;
  border-radius: 999px;
  background: var(--color-track);
}

.death-zones__bar > span {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--color-primary-accent);
}

.death-zones__bar--indeterminate > span {
  width: 30%;
  animation: death-zones-indeterminate 1.6s ease-in-out infinite;
}

@keyframes death-zones-indeterminate {
  from { transform: translateX(-100%); }
  to { transform: translateX(340%); }
}

@media (prefers-reduced-motion: reduce) {
  .death-zones__bar--indeterminate > span {
    animation: none;
  }
}

@media (max-width: 1099px) {
  .death-zones__layout {
    grid-template-columns: minmax(0, 21.25rem) minmax(0, 1fr);
  }

  .death-zones__breakdowns {
    grid-column: 1 / -1;
  }
}

@media (max-width: 899px) {
  .death-zones__layout {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
