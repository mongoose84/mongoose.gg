<template>
  <section class="gamers-list">
    <template v-if="gamers && gamers.length">
      <div class="gamer-cards">
        <GamerCard v-for="g in gamers" :key="g.puuid || g.iconUrl" :gamer="g" />
      </div>
      <!-- Latest Games Cards (shown if any gamer has latest game data) -->
      <LatestGamesCards v-if="showLatestGames" :gamers="gamers" />
      <slot name="after-cards"></slot>
    </template>
    <div v-else class="empty-state">No linked gamers yet.</div>
  </section>
</template>

<script setup>
import GamerCard from '@/views/GamerCard.vue';
import LatestGamesCards from '@/components/shared/LatestGamesCards.vue';

defineProps({
  gamers: {
    type: Array,
    required: true,
  },
  showLatestGames: {
    type: Boolean,
    default: true,
  },
});
</script>

<style scoped>
.gamers-list {
  --card-width: 260px;
  --card-gap: 1.2rem;
  --max-cols: 5;
  max-width: calc(var(--max-cols) * var(--card-width) + (var(--max-cols) - 1) * var(--card-gap));
  margin: 0.25rem auto 0;
  width: 100%;
  box-sizing: border-box;
}

.gamer-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(var(--card-width), var(--card-width)));
  justify-content: center;
  gap: var(--card-gap);
  margin: 0;
  padding: 0;
}

.empty-state {
  text-align: center;
  opacity: 0.7;
  padding: 2rem;
}
</style>
