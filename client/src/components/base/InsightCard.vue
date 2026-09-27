<template>
  <article class="mp-insight insight-card" data-testid="insight-card">
    <img
      v-if="iconUrl && !iconFailed"
      :src="iconUrl"
      alt=""
      data-testid="insight-card-icon"
      @error="iconFailed = true"
    />
    <div class="insight-card__body">
      <span class="mp-chip" :class="`mp-chip--${kind}`" data-testid="insight-card-chip">
        <BaseIcon :name="chip.icon" :size="16" />
        {{ chip.label }}
      </span>
      <h3 data-testid="insight-card-title">{{ title }}</h3>
      <p data-testid="insight-card-text">{{ text }}</p>
    </div>
  </article>
</template>

<script setup>
/**
 * InsightCard (design system): a plain-language finding with its evidence and fix.
 * Strength chips are purple; Pattern and Trend chips are orange (things to work on).
 */
import { computed, ref } from 'vue'
import BaseIcon from './BaseIcon.vue'
import { getChampionIconUrl } from '@/utils/leagueAssets'

const CHIPS = {
  strength: { label: 'Strength', icon: 'trending-up' },
  pattern: { label: 'Pattern', icon: 'repeat' },
  trend: { label: 'Trend', icon: 'activity' }
}

const props = defineProps({
  /** strength, pattern or trend */
  kind: {
    type: String,
    required: true,
    validator: (value) => ['strength', 'pattern', 'trend'].includes(value)
  },
  /** The finding, one sentence */
  title: {
    type: String,
    required: true
  },
  /** One or two sentences of evidence and advice */
  text: {
    type: String,
    required: true
  },
  /** The champion the finding is about (the player's main if none) */
  championName: {
    type: String,
    default: null
  }
})

const iconFailed = ref(false)
const chip = computed(() => CHIPS[props.kind])
const iconUrl = computed(() => (props.championName ? getChampionIconUrl(props.championName) : null))
</script>

<style scoped>
.insight-card__body {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.5rem;
  min-width: 0;
}
</style>
