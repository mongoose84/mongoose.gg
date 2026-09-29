<template>
  <section
    v-if="loading"
    class="solo-patterns solo-patterns--loading"
    aria-busy="true"
    data-testid="solo-patterns-loading"
  >
    <span class="visually-hidden">Loading your patterns</span>
    <div class="solo-patterns__grid">
      <div v-for="n in 3" :key="n" class="mp-insight solo-patterns__card">
        <BaseSkeleton width="30%" />
        <BaseSkeleton variant="title" width="80%" />
        <BaseSkeleton variant="block" height="9.375rem" />
      </div>
    </div>
  </section>

  <section
    v-else-if="error"
    class="mp-card solo-patterns__error"
    aria-labelledby="solo-patterns-title"
    data-testid="solo-patterns-error"
  >
    <h2 id="solo-patterns-title" class="mp-card-title">Your patterns</h2>
    <div class="mp-message mp-message--error" role="alert">
      <BaseIcon name="triangle-alert" :size="20" />
      <div class="mp-message__body">
        <p>We couldn't load your patterns. Try again in a minute.</p>
        <button type="button" class="mp-message__action" data-testid="solo-patterns-retry" @click="$emit('retry')">Try again</button>
      </div>
    </div>
  </section>

  <!-- FR 28: the section is hidden when no pattern's rules are met -->
  <section
    v-else-if="cards.length"
    class="solo-patterns"
    aria-labelledby="solo-patterns-title"
    data-testid="solo-patterns"
  >
    <h2 id="solo-patterns-title" class="mp-card-title">Your patterns</h2>
    <div class="solo-patterns__grid">
      <article
        v-for="card in cards"
        :key="card.key"
        class="mp-insight solo-patterns__card"
        :data-testid="`solo-pattern-${card.key}`"
      >
        <span class="mp-chip" :class="`mp-chip--${card.kind}`" data-testid="solo-pattern-chip">
          <BaseIcon :name="CHIPS[card.kind].icon" :size="16" />
          {{ CHIPS[card.kind].label }}
        </span>
        <h3 data-testid="solo-pattern-title">{{ card.title }}</h3>
        <BaseColumnChart :groups="card.groups" :weak-key="card.weakKey" :measure="card.measure" />
        <p>{{ card.caption }}</p>
      </article>
    </div>
  </section>
</template>

<script setup>
/**
 * "Your patterns": session, after-a-loss and match-length cards (features/solo-trends.spec.md
 * FR 26–28), InsightCard-style with a chip and a ColumnChart. Each card shows only when its
 * rules are met.
 */
import { computed } from 'vue'
import BaseColumnChart from '../base/BaseColumnChart.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseIcon from '../base/BaseIcon.vue'
import { buildPatternCards } from '@/utils/soloSummary'

const CHIPS = {
  strength: { label: 'Strength', icon: 'trending-up' },
  pattern: { label: 'Pattern', icon: 'repeat' },
  trend: { label: 'Trend', icon: 'activity' }
}

const props = defineProps({
  /** win-factors response (patterns live there), or null before the first load */
  data: { type: Object, default: null },
  loading: { type: Boolean, default: false },
  error: { type: Boolean, default: false }
})

defineEmits(['retry'])

const cards = computed(() =>
  props.data ? buildPatternCards(props.data.patterns, props.data.range, props.data.matches) : []
)
</script>

<style scoped>
.solo-patterns {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.solo-patterns--loading {
  animation: solo-patterns-appear 0s linear 300ms both;
}

@keyframes solo-patterns-appear {
  from { visibility: hidden; }
  to { visibility: visible; }
}

.solo-patterns__error {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.solo-patterns__grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1.25rem;
}

.solo-patterns__card {
  flex-direction: column;
  align-items: flex-start;
  gap: 0.75rem;
}

.solo-patterns__card :deep(.column-chart) {
  width: 100%;
}

@media (max-width: 899px) {
  .solo-patterns__grid {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
