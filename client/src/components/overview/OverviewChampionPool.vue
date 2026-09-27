<template>
  <section
    class="champion-pool"
    aria-labelledby="champion-pool-title"
    data-testid="overview-champion-pool"
  >
    <header class="champion-pool__header">
      <h2 id="champion-pool-title" class="mp-card-title">Your champions</h2>
      <p class="champion-pool__caption">Ranked this season</p>
    </header>

    <div
      v-if="champions.length"
      class="champion-pool__grid"
      :class="{ 'champion-pool__grid--with-list': alsoPlayed.length }"
    >
      <ChampionCard
        v-for="champion in champions"
        :key="champion.championId"
        :champion-name="champion.championName"
        :win-rate="champion.winRate"
        :matches="champion.matches"
        :avg-kda="champion.avgKda"
        :strength-tag="champion.strengthTag"
      />

      <div v-if="alsoPlayed.length" class="champion-pool__also" data-testid="champion-pool-also-played">
        <h3 class="champion-pool__also-title">Also played</h3>
        <ul class="champion-pool__list">
          <li
            v-for="champion in alsoPlayed"
            :key="champion.championId"
            class="champion-pool__item"
            data-testid="champion-pool-also-played-item"
          >
            <img
              v-if="!failedIcons.has(champion.championName)"
              :src="getChampionIconUrl(champion.championName)"
              alt=""
              class="champion-pool__icon"
              @error="markIconFailed(champion.championName)"
            />
            <span v-else class="champion-pool__icon" aria-hidden="true" />
            <span class="champion-pool__name">
              <span class="champion-pool__champion">{{ champion.championName }}</span>
              <span class="champion-pool__matches">{{ formatMatches(champion.matches) }}</span>
            </span>
            <span
              class="champion-pool__winrate"
              :class="{ 'mp-down': champion.winRate < 50 }"
              data-testid="champion-pool-also-played-winrate"
            >{{ Math.round(champion.winRate) }}%<span class="visually-hidden"> win rate</span></span>
          </li>
        </ul>
      </div>
    </div>

    <BaseEmptyState
      v-else
      title="No ranked matches this season"
      description="Play ranked and your best champions show up here."
      data-testid="champion-pool-empty"
    />
  </section>
</template>

<script setup>
/**
 * "Your champions" on the Overview: the top three ranked champions this season as ChampionCards
 * (ordered by M-Score server-side), the next three in a compact "Also played" list.
 */
import { reactive } from 'vue'
import ChampionCard from '../base/ChampionCard.vue'
import BaseEmptyState from '../base/BaseEmptyState.vue'
import { getChampionIconUrl } from '@/utils/leagueAssets'

defineProps({
  /** Up to three champions shown as cards */
  champions: {
    type: Array,
    default: () => []
  },
  /** Up to three more champions for the "Also played" list */
  alsoPlayed: {
    type: Array,
    default: () => []
  }
})

const failedIcons = reactive(new Set())

function markIconFailed(championName) {
  failedIcons.add(championName)
}

function formatMatches(count) {
  return `${count} ${count === 1 ? 'match' : 'matches'}`
}
</script>

<style scoped>
.champion-pool {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.champion-pool__header {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.5rem 1rem;
}

.champion-pool__caption {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.champion-pool__grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1.25rem;
}

.champion-pool__grid--with-list {
  grid-template-columns: repeat(3, minmax(0, 1fr)) 18.75rem;
}

.champion-pool__also {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 1.25rem;
  border-radius: 1.5rem;
  background: var(--color-surface);
}

.champion-pool__also-title {
  padding-bottom: 0.5rem;
  font-size: 0.8125rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--color-text-secondary);
}

.champion-pool__list {
  list-style: none;
}

.champion-pool__item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  height: 4rem;
  border-top: 1px solid var(--color-border);
}

.champion-pool__icon {
  width: 2.75rem;
  height: 2.75rem;
  flex-shrink: 0;
  border-radius: 0.75rem;
  background: var(--color-track);
}

.champion-pool__name {
  display: flex;
  flex-direction: column;
  flex-grow: 1;
  min-width: 0;
}

.champion-pool__champion {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text);
}

.champion-pool__matches {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
}

.champion-pool__winrate {
  font-family: var(--font-display);
  font-size: 1.0625rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

.champion-pool__winrate.mp-down {
  color: var(--color-warn-text);
}

/* Narrow desktops: keep three cards in a row and move "Also played" under them */
@media (max-width: 1199px) {
  .champion-pool__grid--with-list {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  .champion-pool__also {
    grid-column: 1 / -1;
  }
}

@media (max-width: 899px) {
  .champion-pool__grid,
  .champion-pool__grid--with-list {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
