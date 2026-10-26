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
      <BaseDeathMap :zones="data.zones" :selected-key="shownKey" :focus-key="focusKey" :description="mapDescription" />

      <ul class="death-zones__list" aria-label="Zones" data-testid="solo-death-zones-list">
        <li v-for="zone in data.zones" :key="zone.key">
          <button
            type="button"
            class="death-zones__zone"
            :aria-pressed="zone.key === shownKey"
            :aria-label="`${zoneLabel(zone.key)}, ${zone.deaths} deaths${zone.key === focusKey ? ', fix first' : ''}`"
            :data-testid="`death-zone-${zone.key}`"
            @click="select(zone.key)"
          >
            <span class="death-zones__dot" :class="{ 'death-zones__dot--focus': zone.key === focusKey }" aria-hidden="true" />
            <span class="death-zones__zone-text" aria-hidden="true">
              <span class="death-zones__zone-head">
                <span class="death-zones__zone-name">{{ zoneLabel(zone.key) }}</span>
                <span v-if="zone.key === focusKey" class="mp-chip mp-chip--pattern" data-testid="death-zone-fix-first">Fix first</span>
                <span class="death-zones__zone-count">{{ zone.deaths }}</span>
              </span>
              <span class="death-zones__zone-bar" :data-testid="`death-zone-bar-${zone.key}`">
                <span :style="{ width: zoneBarWidth(zone) }" />
              </span>
            </span>
          </button>
        </li>
      </ul>

      <section
        v-if="shownZone && focus"
        class="death-zones__focus"
        aria-live="polite"
        aria-labelledby="solo-death-zones-focus-zone"
        data-testid="solo-death-zones-focus"
      >
        <p class="death-zones__focus-zone">
          <span id="solo-death-zones-focus-zone" class="mp-eyebrow" data-testid="solo-death-zones-filter">{{ zoneLabel(shownKey) }}</span>
        </p>
        <h3 class="death-zones__pattern" data-testid="solo-death-zones-pattern">{{ focus.pattern }}</h3>

        <ul class="death-zones__facts">
          <li v-for="fact in focus.facts" :key="fact.key" class="death-zones__fact" :data-testid="`death-zones-fact-${fact.key}`">
            <span class="death-zones__fact-value">{{ fact.value }}</span>
            <span class="death-zones__fact-label">{{ fact.label }}</span>
            <span v-if="fact.share !== null" class="death-zones__fact-track" aria-hidden="true">
              <span :style="{ width: `${fact.share * 100}%` }" />
            </span>
          </li>
        </ul>

        <div class="death-zones__tip" data-testid="solo-death-zones-tip">
          <BaseIcon :name="focus.tip.icon" :size="20" class="death-zones__tip-icon" />
          <p><strong>Next match:</strong> {{ focus.tip.text }}</p>
        </div>

        <button
          type="button"
          class="death-zones__more"
          :aria-expanded="showNumbers"
          aria-controls="solo-death-zones-numbers"
          data-testid="solo-death-zones-more"
          @click="showNumbers = !showNumbers"
        >
          {{ showNumbers ? 'Hide numbers' : 'Show all numbers' }}
          <BaseIcon name="chevron-down" :size="16" :class="{ 'death-zones__more-icon--open': showNumbers }" />
        </button>

        <div v-if="showNumbers" id="solo-death-zones-numbers" class="death-zones__breakdowns">
          <section v-for="group in groups" :key="group.key" class="death-zones__group" :aria-label="group.title">
            <h4>{{ group.title }}</h4>
            <ul>
              <li v-for="row in group.rows" :key="row.key" class="death-zones__row" :data-testid="`death-zones-${group.key}-${row.key}`">
                <span class="death-zones__row-label">{{ row.label }}</span>
                <span class="death-zones__row-track" aria-hidden="true">
                  <span :style="{ width: row.width }" />
                </span>
                <span class="death-zones__row-value">{{ row.value }}</span>
              </li>
            </ul>
          </section>
        </div>
      </section>
    </div>
  </SoloCard>
</template>

<script setup>
/**
 * "Where your deaths cost you" (features/solo-trends.spec.md FR 31–39): the map with zone circles,
 * the zone list, and one zone at a time in the focus panel (the zone to fix first until another is
 * pressed): what its deaths have in common, three facts, a "Next match" tip and, on request, all
 * its numbers. Also the death detail backfill's progress while older matches are still being read.
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
  buildZoneFocus,
  describeDeathMap,
  focusZoneKey,
  zoneLabel
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
const showNumbers = ref(false)

// A zone that is no longer listed (new range or queue) returns the panel to the zone to fix first
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

const focusKey = computed(() => focusZoneKey(props.data?.zones))
// The panel shows the pressed zone, else the zone to fix first
const shownKey = computed(() => selectedKey.value ?? focusKey.value)
const shownZone = computed(() => props.data?.zones?.find((z) => z.key === shownKey.value) ?? null)
const shownBreakdown = computed(() => props.data?.breakdowns?.byZone?.[shownKey.value] ?? null)
const focus = computed(() => (shownZone.value ? buildZoneFocus(shownZone.value, shownBreakdown.value) : null))

function select(key) {
  selectedKey.value = key
}

/** Deaths scaled to the zone with most. */
function zoneBarWidth(zone) {
  const most = Math.max(1, ...props.data.zones.map((z) => z.deaths))
  return `${(zone.deaths / most) * 100}%`
}

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
  const shown = shownBreakdown.value
  if (!shown) return []
  return [
    { key: 'phase', title: 'Deaths when', rows: rows(shown.phase, PHASE_LABELS) },
    { key: 'how', title: 'Deaths how', rows: rows(shown.how, HOW_LABELS) },
    { key: 'cost', title: 'Objectives lost after your death', rows: rows(shown.cost, COST_LABELS) }
  ]
})
</script>

<style scoped>
.death-zones__layout {
  display: grid;
  grid-template-columns: minmax(0, 21.25rem) minmax(0, 18rem) minmax(0, 1fr);
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
  border: 1px solid var(--color-text);
  border-radius: 999px;
  background: var(--color-ink-faint);
}

.death-zones__dot--focus {
  border-color: var(--color-warn);
  background: var(--color-warn);
}

.death-zones__zone-head {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.death-zones__zone-count {
  margin-left: auto;
  font-family: var(--font-display);
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.death-zones__zone-bar {
  display: block;
  width: 100%;
  height: 0.375rem;
  margin-top: 0.375rem;
  border-radius: 999px;
  background: var(--color-track);
}

.death-zones__zone-bar > span {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--color-ink-faint);
}

.death-zones__focus {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  min-width: 0;
  padding: 1.25rem;
  border-radius: 1rem;
  background: var(--color-elevated);
}

.death-zones__focus-zone {
  margin: 0;
}

.death-zones__pattern {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.25rem;
  font-weight: 600;
  line-height: 1.3;
  color: var(--color-text);
}

.death-zones__facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(7rem, 1fr));
  gap: 0.75rem;
  margin: 0;
  padding: 0;
  list-style: none;
}

.death-zones__fact {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.death-zones__fact-value {
  font-family: var(--font-display);
  font-size: 1.5rem;
  font-weight: 600;
  line-height: 1;
  color: var(--color-text);
  font-variant-numeric: tabular-nums;
}

.death-zones__fact-label {
  font-size: 0.8125rem;
  line-height: 1.35;
  color: var(--color-text-secondary);
}

.death-zones__fact-track {
  height: 0.375rem;
  margin-top: auto;
  border-radius: 999px;
  background: var(--color-track);
}

.death-zones__fact-track > span {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--color-ink-faint);
}

.death-zones__tip {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem 1.25rem;
  border-radius: 1rem;
  background: var(--color-surface);
}

.death-zones__tip p {
  margin: 0;
  font-size: 0.9375rem;
  line-height: 1.5;
  color: var(--color-text);
}

.death-zones__tip-icon {
  flex-shrink: 0;
  margin-top: 0.125rem;
  color: var(--color-text-secondary);
}

.death-zones__more {
  display: inline-flex;
  align-items: center;
  align-self: flex-start;
  gap: 0.25rem;
  min-height: 2.75rem;
  padding: 0 0.25rem;
  border: 0;
  border-radius: 0.5rem;
  background: transparent;
  color: var(--color-text-secondary);
  font: inherit;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
}

.death-zones__more:hover {
  color: var(--color-text);
}

.death-zones__more:focus-visible {
  outline: none;
  box-shadow: var(--shadow-focus);
}

.death-zones__more-icon--open {
  transform: rotate(180deg);
}

.death-zones__zone-text {
  display: flex;
  flex-direction: column;
  flex: 1;
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
  background: var(--color-ink-faint);
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

  .death-zones__focus {
    grid-column: 1 / -1;
  }
}

@media (max-width: 899px) {
  .death-zones__layout {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
