<template>
  <section class="px-4 desk:px-14 pt-7 desk:pt-14 pb-16 desk:pb-28 grid grid-cols-1 desk:grid-cols-[minmax(0,1fr)_41.25rem] gap-10 desk:gap-16 items-center" data-testid="landing-hero">
    <div class="flex flex-col gap-5 desk:gap-7">
      <span class="hidden desk:inline-flex self-start items-center gap-2 h-8 px-3.5 rounded-pill bg-background-elevated text-caption font-medium text-text-soft">
        <span class="w-2 h-2 rounded-pill bg-primary-accent" aria-hidden="true" />
        A coach built from your own matches
      </span>
      <h1 class="font-display text-[2.375rem] leading-[1.08] tracking-[-0.01em] font-semibold desk:text-display text-text max-w-[37.5rem]" data-testid="landing-headline">
        Climb faster with coaching from your own match history
      </h1>
      <p class="text-body-lg desk:text-lg text-text-soft max-w-[33.75rem]">
        Champion picks for your pool, a readiness score before you queue, and one fix after every match.
      </p>
      <div class="flex flex-col desk:flex-row desk:items-center gap-3">
        <router-link to="/auth?mode=signup" class="mp-btn mp-btn--primary h-[3.25rem] text-base" data-testid="hero-signup">
          Create free account
          <BaseIcon name="arrow-right" :size="20" />
        </router-link>
        <router-link to="/auth?mode=login" class="mp-btn mp-btn--secondary h-[3.25rem] text-base hidden desk:inline-flex" data-testid="hero-login">
          Log in
        </router-link>
      </div>
      <ul class="flex flex-wrap justify-center desk:justify-start gap-x-5 gap-y-2 text-caption desk:text-body-sm text-text-secondary" data-testid="hero-trust">
        <li v-for="item in trustItems" :key="item" class="flex items-center gap-2">
          <BaseIcon name="check" :size="16" class="text-positive" />
          {{ item }}
        </li>
      </ul>
    </div>

    <!-- Product preview: real components filled with the example account -->
    <div class="flex flex-col gap-5" aria-label="Example of your Overview" data-testid="hero-preview">
      <div class="hero-card relative h-[21.25rem] desk:h-[23.75rem] rounded-xl desk:rounded-2xl overflow-hidden bg-background">
        <img
          :src="splashUrl"
          :alt="`${example.main.name} splash art`"
          class="hero-card__art absolute top-0 right-0 h-full object-cover"
        />
        <div class="hero-card__scrim absolute inset-0" aria-hidden="true" />
        <span class="absolute top-4 right-4 desk:top-5 desk:right-5 h-7 inline-flex items-center px-3 rounded-pill bg-glass text-chip text-text" data-testid="example-label">Example</span>
        <div class="absolute left-5 right-5 bottom-5 desk:left-9 desk:right-auto desk:top-9 desk:bottom-auto desk:w-[22.5rem] flex flex-col gap-2.5 desk:gap-3.5">
          <span class="text-caption text-text-secondary">{{ example.riotId }} · {{ example.main.name }} · {{ example.rank }} · {{ example.lp }} LP</span>
          <span class="font-display font-semibold text-[1.375rem] desk:text-[1.875rem] leading-[1.15] text-text">{{ example.main.headline }}</span>
          <span class="hidden desk:block text-body-sm text-text-soft">{{ example.main.sub }}</span>
        </div>
        <ul class="hidden desk:flex absolute left-9 bottom-8 gap-2">
          <li
            v-for="stat in example.main.stats"
            :key="stat.label"
            class="inline-flex items-center gap-1.5 h-8 px-3.5 rounded-pill bg-glass text-caption text-text-soft"
          >
            <b class="font-display text-body font-bold text-text">{{ stat.value }}</b>{{ stat.label }}
          </li>
        </ul>
      </div>

      <div class="grid grid-cols-1 desk:grid-cols-[25rem_minmax(0,1fr)] gap-5">
        <div class="p-4 desk:p-6 rounded-xl bg-background-surface grid grid-cols-3 gap-3">
          <div v-for="score in example.scores" :key="score.label" class="flex flex-col items-center gap-2">
            <ScoreRing :value="score.value" :label="score.label" :size="76" />
            <span class="text-caption font-bold text-text">{{ score.label }}</span>
            <span class="text-chip" :class="score.delta >= 0 ? 'text-positive' : 'text-warn-text'">
              {{ score.delta >= 0 ? '▲' : '▼' }} {{ Math.abs(score.delta) }}
            </span>
          </div>
        </div>
        <div class="hidden desk:flex p-[1.375rem] rounded-card-sm bg-background-surface flex-col gap-2.5">
          <span class="self-start inline-flex items-center gap-1.5 h-[1.625rem] px-2.5 rounded-pill bg-warn-soft text-warn-text text-chip">
            <BaseIcon name="repeat" :size="16" />
            Pattern
          </span>
          <span class="font-display text-insight text-text">{{ example.insight.title }}</span>
          <span class="text-caption text-text-soft">{{ example.insight.body }}</span>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed } from 'vue'
import { BaseIcon, ScoreRing } from '@/components/base'
import { getChampionSplashUrl } from '@/utils/leagueAssets'
import { exampleAccount as example } from './exampleAccount'

const props = defineProps({
  /** Rounded proof count such as "3.2M+", shown only when it is large enough to help */
  proofCount: {
    type: String,
    default: ''
  }
})

const splashUrl = getChampionSplashUrl(example.main.name)

const trustItems = computed(() => {
  const items = ['Free', "Built on Riot's official API"]
  if (props.proofCount) {
    items.push(`${props.proofCount} matches analysed`)
  }
  return items
})
</script>

<style scoped>
/* Phone: art fills the card behind the text with a full-width scrim */
.hero-card__art {
  width: 100%;
  object-position: 72% 25%;
}

.hero-card__scrim {
  background: linear-gradient(180deg, rgba(10, 8, 16, 0.1) 0%, rgba(10, 8, 16, 0.75) 45%, var(--color-bg) 80%);
}

/* Desktop: art anchored right, faded into bg from the left so text sits on solid ground */
@media (min-width: 900px) {
  .hero-card__art {
    width: 32.5rem;
    object-position: 70% 30%;
  }

  .hero-card__scrim {
    background:
      linear-gradient(90deg, var(--color-bg) 30%, rgba(10, 8, 16, 0) 64%),
      linear-gradient(0deg, rgba(10, 8, 16, 0.85) 0%, rgba(10, 8, 16, 0) 32%);
  }
}
</style>
