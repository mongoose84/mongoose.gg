<template>
  <section class="px-4 desk:px-14 pb-16 desk:pb-28 grid grid-cols-1 desk:grid-cols-[minmax(0,1fr)_50rem] gap-7 desk:gap-16 items-center" data-testid="landing-champion-pool">
    <div class="flex flex-col gap-3 desk:gap-5">
      <span class="text-eyebrow text-text-secondary">YOUR CHAMPION POOL</span>
      <h2 class="font-display font-semibold text-[1.875rem] leading-[1.15] desk:text-headline text-text">Your champions, not a tier list</h2>
      <p class="text-body desk:text-body-lg text-text-soft max-w-[27.5rem]">
        See which champions win you LP and which cost it. Each one gets its own scores, matchups and fixes, and your Overview follows the champion you pick.
      </p>
    </div>
    <ul class="grid grid-cols-1 desk:grid-cols-3 gap-4 desk:gap-5">
      <li
        v-for="(champion, i) in example.champions"
        :key="champion.name"
        class="relative h-[15rem] desk:h-[23.75rem] rounded-xl overflow-hidden bg-background-surface border-2"
        :class="i === 0 ? 'border-primary-accent' : 'border-transparent'"
        data-testid="champion-card"
      >
        <img
          :src="getChampionCenteredUrl(champion.name)"
          alt=""
          class="absolute top-0 left-0 w-full h-full desk:h-[17.5rem] object-cover"
          :style="{ objectPosition: champion.focus }"
        />
        <div class="champion-scrim absolute inset-x-0 bottom-0 h-full desk:top-[8.75rem] desk:h-[8.75rem] desk:bottom-auto" aria-hidden="true" />
        <div class="absolute left-[1.375rem] right-[1.375rem] bottom-[1.375rem] flex flex-col gap-2.5">
          <span class="self-start inline-flex items-center gap-1 h-[1.625rem] px-2.5 rounded-pill bg-primary-soft text-positive-strong text-chip">
            <BaseIcon name="trending-up" :size="16" />
            {{ champion.tag }}
          </span>
          <div class="flex items-baseline justify-between gap-2">
            <span class="font-display text-title-lg text-text">{{ champion.name }}</span>
            <span class="font-display text-stat text-positive">{{ champion.winRate }}%</span>
          </div>
          <span class="text-caption text-text-secondary">{{ champion.matches }} matches · {{ champion.kda }} KDA</span>
        </div>
      </li>
    </ul>
  </section>
</template>

<script setup>
import { BaseIcon } from '@/components/base'
import { getChampionCenteredUrl } from '@/utils/leagueAssets'
import { exampleAccount as example } from './exampleAccount'
</script>

<style scoped>
/* Centred art fades into the card from the bottom */
.champion-scrim {
  background: linear-gradient(180deg, rgba(18, 15, 25, 0) 20%, var(--color-surface) 85%);
}

@media (min-width: 900px) {
  .champion-scrim {
    background: linear-gradient(180deg, rgba(18, 15, 25, 0) 0%, var(--color-surface) 100%);
  }
}
</style>
