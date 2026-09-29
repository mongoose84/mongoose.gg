<template>
  <div
    v-if="state === 'failed'"
    class="mp-message mp-message--error"
    role="alert"
    data-testid="sync-progress-error"
  >
    <BaseIcon name="triangle-alert" :size="20" />
    <div class="mp-message__body">
      <p>We couldn't finish syncing your matches. Try again in a minute.</p>
      <button
        type="button"
        class="mp-message__action"
        data-testid="sync-progress-retry"
        @click="$emit('retry')"
      >Try again</button>
    </div>
  </div>

  <section
    v-else
    class="sync-progress"
    aria-label="Match sync"
    data-testid="sync-progress"
    :data-state="state"
  >
    <p class="sync-progress__title" data-testid="sync-progress-title">{{ title }}</p>

    <div
      v-if="state !== 'done'"
      class="sync-progress__bar"
      :class="{ 'sync-progress__bar--indeterminate': !isDeterminate }"
      role="progressbar"
      :aria-label="isDeterminate ? 'Matches synced' : 'Looking for new matches'"
      aria-valuemin="0"
      :aria-valuemax="isDeterminate ? total : undefined"
      :aria-valuenow="isDeterminate ? clampedCurrent : undefined"
      data-testid="sync-progress-bar"
    >
      <span :style="isDeterminate ? { width: `${percent}%` } : null" />
    </div>

    <p v-if="detail" class="sync-progress__detail" data-testid="sync-progress-detail">{{ detail }}</p>

    <!-- Polite updates, at most every few matches -->
    <span class="visually-hidden" aria-live="polite" data-testid="sync-progress-live">{{ announcement }}</span>
  </section>
</template>

<script setup>
/**
 * SyncProgress (design system): new matches coming in from Riot, shown inline at the top of the
 * page content. Determinate when the total is known, indeterminate before that. Never blocks
 * the page; everything already loaded stays usable underneath.
 */
import { computed } from 'vue'
import BaseIcon from './BaseIcon.vue'

const ANNOUNCE_EVERY = 5

const props = defineProps({
  /** running (syncing), waiting (Riot rate limit), done (just finished) or failed */
  state: {
    type: String,
    required: true,
    validator: (value) => ['running', 'waiting', 'done', 'failed'].includes(value)
  },
  /** Matches synced so far */
  current: {
    type: Number,
    default: 0
  },
  /** Total matches to sync; 0 while still unknown */
  total: {
    type: Number,
    default: 0
  },
  /** Matches synced in the finished run (done state) */
  syncedCount: {
    type: Number,
    default: 0
  }
})

defineEmits(['retry'])

const isDeterminate = computed(() => props.state === 'running' && props.total > 0)
const clampedCurrent = computed(() => Math.min(Math.max(props.current, 0), props.total))
const percent = computed(() => (props.total > 0 ? Math.round((clampedCurrent.value / props.total) * 100) : 0))

function matchesLabel(count) {
  return `${count} ${count === 1 ? 'match' : 'matches'}`
}

const title = computed(() => {
  if (props.state === 'done') return `Synced ${matchesLabel(props.syncedCount)} · just now`
  if (props.state === 'waiting') return 'Waiting on Riot’s servers…'
  if (isDeterminate.value) return `Syncing ${clampedCurrent.value} of ${matchesLabel(props.total)}`
  return 'Looking for new matches…'
})

const detail = computed(() => {
  if (props.state === 'done') return null
  // FR 39: reads the same as the death-zones card's backfill line
  if (props.state === 'waiting') return 'We’ll continue automatically.'
  return 'You can keep using the app. Your Overview updates when the sync is done.'
})

const announcement = computed(() => {
  if (!isDeterminate.value) return title.value
  const current = clampedCurrent.value
  const rounded = current === props.total ? current : Math.floor(current / ANNOUNCE_EVERY) * ANNOUNCE_EVERY
  return `Syncing ${rounded} of ${matchesLabel(props.total)}`
})
</script>

<style scoped>
.sync-progress {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
  padding: 1.25rem 1.75rem;
  border-radius: 1.5rem;
  background: var(--color-surface);
}

.sync-progress__title {
  margin: 0;
  font-size: 0.9375rem;
  font-weight: 700;
  line-height: 1.4;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

.sync-progress__detail {
  margin: 0;
  font-size: 0.875rem;
  line-height: 1.5;
  color: var(--color-text-secondary);
}

.sync-progress__bar {
  position: relative;
  height: 0.375rem;
  overflow: hidden;
  border-radius: 999px;
  background: var(--color-track);
}

.sync-progress__bar > span {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  border-radius: 999px;
  background: var(--color-primary-accent);
}

.sync-progress__bar--indeterminate > span {
  width: 30%;
}

@media (prefers-reduced-motion: no-preference) {
  .sync-progress__bar > span {
    transition: width 300ms ease-out;
  }

  .sync-progress__bar--indeterminate > span {
    animation: sync-progress-slide 1.6s ease-in-out infinite;
  }
}

@keyframes sync-progress-slide {
  0% { left: -30%; }
  100% { left: 100%; }
}
</style>
