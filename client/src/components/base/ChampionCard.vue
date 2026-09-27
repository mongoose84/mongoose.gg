<template>
  <article class="mp-champ-card champion-card" data-testid="champion-card">
    <img
      v-if="artUrl && !artFailed"
      :src="artUrl"
      alt=""
      data-testid="champion-card-art"
      @error="artFailed = true"
    />
    <div class="mp-champ-card-scrim" aria-hidden="true" />

    <div class="mp-champ-card-body">
      <div class="champion-card__row champion-card__row--title">
        <h3 class="mp-champ-name" data-testid="champion-card-name">{{ championName }}</h3>
        <span class="champion-card__winrate" data-testid="champion-card-winrate">
          {{ winRateLabel }}<span class="visually-hidden"> win rate</span>
        </span>
      </div>
      <div class="champion-card__row champion-card__meta">
        <span data-testid="champion-card-meta">{{ metaLabel }}</span>
        <span v-if="strengthTag" data-testid="champion-card-tag">{{ strengthTag }}</span>
      </div>
      <div class="mp-bar" aria-hidden="true">
        <span :style="{ width: barWidth }" data-testid="champion-card-bar" />
      </div>
    </div>
  </article>
</template>

<script setup>
/**
 * ChampionCard (design system): one champion in the player's pool — centred art, win rate,
 * matches, KDA and one strength tag. Static here: the Overview has no champion focus, so it is
 * an article, not a button, and gets no spotlight hover.
 */
import { computed, ref, watch } from 'vue'
import { getChampionCenteredUrl } from '@/utils/leagueAssets'

const props = defineProps({
  championName: {
    type: String,
    required: true
  },
  /** Win rate in percent (0–100) */
  winRate: {
    type: Number,
    required: true
  },
  matches: {
    type: Number,
    required: true
  },
  avgKda: {
    type: Number,
    required: true
  },
  /** What this champion does best in the pool ("Best laning"), if anything */
  strengthTag: {
    type: String,
    default: null
  }
})

const artFailed = ref(false)
watch(() => props.championName, () => { artFailed.value = false })

const artUrl = computed(() => getChampionCenteredUrl(props.championName))
const clampedWinRate = computed(() => Math.min(100, Math.max(0, props.winRate)))
const winRateLabel = computed(() => `${Math.round(clampedWinRate.value)}%`)
const barWidth = computed(() => `${clampedWinRate.value}%`)
const metaLabel = computed(() => {
  const matchWord = props.matches === 1 ? 'match' : 'matches'
  return `${props.matches} ${matchWord} · KDA ${props.avgKda.toFixed(1)}`
})
</script>

<style scoped>
.champion-card__row {
  display: flex;
  justify-content: space-between;
  gap: 0.75rem;
}

.champion-card__row--title {
  align-items: baseline;
}

.champion-card__row--title .mp-champ-name {
  margin: 0;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.champion-card__winrate {
  font-family: var(--font-display);
  font-size: 1.375rem;
  font-weight: 700;
  line-height: 1.2;
  font-variant-numeric: tabular-nums;
}

.champion-card__meta {
  font-size: 0.8125rem;
  color: var(--color-ink-soft);
}

@media (max-width: 899px) {
  .champion-card {
    height: 13.75rem;
  }
}
</style>
