<template>
  <section
    class="mp-hero champion-hero"
    :class="{ 'champion-hero--plain': !hasArt }"
    aria-labelledby="champion-hero-headline"
    data-testid="champion-hero"
  >
    <template v-if="hasArt">
      <img
        :src="splashUrl"
        :alt="`${championName} splash art`"
        class="mp-hero-art"
        data-testid="champion-hero-art"
        @error="artFailed = true"
      />
      <div class="mp-hero-scrim" aria-hidden="true" />
    </template>

    <div class="mp-hero-body">
      <div class="champion-hero__text">
        <p v-if="playerLine" class="champion-hero__player" data-testid="champion-hero-player">{{ playerLine }}</p>
        <h1 id="champion-hero-headline" data-testid="champion-hero-headline">{{ headline }}</h1>
        <p v-if="text" data-testid="champion-hero-text">{{ text }}</p>
      </div>

      <div v-if="$slots.action || visibleChips.length" class="champion-hero__footer">
        <slot name="action" />
        <ul v-if="visibleChips.length" class="champion-hero__chips" aria-label="This week">
          <li
            v-for="chip in visibleChips"
            :key="chip"
            class="mp-chip mp-chip--glass"
            data-testid="champion-hero-chip"
          >{{ chip }}</li>
        </ul>
      </div>
    </div>
  </section>
</template>

<script setup>
/**
 * ChampionHero (design system): the page opener — the player's main champion splash with one
 * headline about them. Without a champion (or when the art fails) it falls back to a plain
 * surface card with the same text. One hero per page.
 */
import { computed, ref, watch } from 'vue'
import { getChampionSplashUrl } from '@/utils/leagueAssets'

const MAX_CHIPS = 2

const props = defineProps({
  /** The one sentence about the player (page headline) */
  headline: {
    type: String,
    required: true
  },
  /** One supporting sentence */
  text: {
    type: String,
    default: null
  },
  /** Riot ID · champion main · rank · LP */
  playerLine: {
    type: String,
    default: null
  },
  /** Champion whose splash art fills the right side */
  championName: {
    type: String,
    default: null
  },
  /** Up to two short stats shown as glass chips ("64% win rate") */
  chips: {
    type: Array,
    default: () => []
  }
})

const artFailed = ref(false)
watch(() => props.championName, () => { artFailed.value = false })

const splashUrl = computed(() => getChampionSplashUrl(props.championName))
const hasArt = computed(() => Boolean(splashUrl.value) && !artFailed.value)
const visibleChips = computed(() => props.chips.filter(Boolean).slice(0, MAX_CHIPS))
</script>

<style scoped>
.champion-hero__text {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.champion-hero__player {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

.champion-hero__footer {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
}

.champion-hero__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  list-style: none;
}

/* No champion: a plain surface card, no fixed art height */
.champion-hero--plain,
.champion-hero--plain .mp-hero-body {
  min-height: 0;
}

/* Below 900px the art moves behind the text with a full-width scrim */
@media (max-width: 899px) {
  .champion-hero .mp-hero-art {
    width: 100%;
  }

  .champion-hero .mp-hero-scrim {
    width: 100%;
    background: linear-gradient(0deg, var(--color-bg) 0%, var(--color-bg) 35%, rgba(10, 8, 16, 0.8) 70%, rgba(10, 8, 16, 0.55) 100%);
  }

  .champion-hero .mp-hero-body {
    padding: 1.75rem 1.25rem;
  }

  .champion-hero .mp-hero-body h1 {
    font-size: 2rem;
  }
}
</style>
