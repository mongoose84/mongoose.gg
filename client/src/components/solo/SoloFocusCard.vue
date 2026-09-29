<template>
  <section
    v-if="loading"
    class="mp-card mp-card--highlight focus-card focus-card--loading"
    aria-busy="true"
    data-testid="solo-focus-loading"
  >
    <span class="visually-hidden">Loading your focus</span>
    <BaseSkeleton width="30%" />
    <BaseSkeleton variant="title" width="80%" />
    <BaseSkeleton width="100%" />
    <BaseSkeleton variant="block" height="1.25rem" />
  </section>

  <section
    v-else-if="focus"
    class="mp-card mp-card--highlight focus-card"
    aria-labelledby="solo-focus-finding"
    data-testid="solo-focus"
  >
    <p class="mp-eyebrow focus-card__eyebrow">
      <BaseIcon name="target" :size="16" />
      Your focus
    </p>
    <h2 id="solo-focus-finding" class="focus-card__finding" data-testid="solo-focus-finding">{{ finding }}</h2>
    <p class="focus-card__evidence" data-testid="solo-focus-evidence">{{ evidence }}</p>

    <div class="focus-card__strip">
      <p class="focus-card__strip-caption" data-testid="solo-focus-strip-caption">{{ stripCaption }}</p>
      <BaseGoalStrip :results="focus.last20" :mark-label="markLabel" />
    </div>

    <div v-if="fix" class="focus-card__fix" data-testid="solo-focus-fix">
      <BaseIcon :name="fix.icon" :size="20" class="focus-card__fix-icon" />
      <p><strong>Next match:</strong> {{ fix.text }}</p>
    </div>
  </section>
</template>

<script setup>
/**
 * "Your focus" (features/solo-trends.spec.md FR 18–20, FR 29): the page's one highlight card.
 * The finding, the evidence from the player's own matches, the last 20 matches against the mark,
 * and one "Next match" fix. Left out when the server picks no focus. No goal button until Goals
 * ship (decision 2).
 */
import { computed } from 'vue'
import BaseGoalStrip from '../base/BaseGoalStrip.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseIcon from '../base/BaseIcon.vue'
import {
  buildFocusEvidence,
  buildFocusFinding,
  buildFocusFix,
  buildFocusStripCaption,
  focusMarkLabel
} from '@/utils/soloSummary'

const props = defineProps({
  /** The focus from stat-trends, or null */
  focus: { type: Object, default: null },
  /** The focus stat's verdict (slipping reads differently) */
  verdict: { type: String, default: null },
  /** Show the skeleton */
  loading: { type: Boolean, default: false }
})

const finding = computed(() => (props.focus ? buildFocusFinding(props.focus, props.verdict) : ''))
const evidence = computed(() => (props.focus ? buildFocusEvidence(props.focus) : ''))
const fix = computed(() => buildFocusFix(props.focus))
const markLabel = computed(() => focusMarkLabel(props.focus))
const stripCaption = computed(() => (props.focus ? buildFocusStripCaption(props.focus) : ''))
</script>

<style scoped>
.focus-card {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  min-width: 0;
}

.focus-card--loading {
  animation: focus-card-appear 0s linear 300ms both;
}

@keyframes focus-card-appear {
  from { visibility: hidden; }
  to { visibility: visible; }
}

.focus-card__eyebrow {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  margin: 0;
}

.focus-card__finding {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.5rem;
  font-weight: 600;
  line-height: 1.25;
  color: var(--color-text);
}

.focus-card__evidence {
  margin: 0;
  font-size: 0.9375rem;
  line-height: 1.55;
  color: var(--color-ink-soft);
}

.focus-card__strip {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.focus-card__strip-caption {
  margin: 0;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.focus-card__fix {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  margin-top: auto;
  padding: 1rem 1.25rem;
  border-radius: 1rem;
  background: var(--color-surface);
}

.focus-card__fix p {
  margin: 0;
  font-size: 0.9375rem;
  line-height: 1.5;
  color: var(--color-text);
}

.focus-card__fix-icon {
  flex-shrink: 0;
  margin-top: 0.125rem;
  color: var(--color-text-secondary);
}
</style>
