<template>
  <div class="team-combos-container">
    <div v-if="loading" class="combos-loading">Loading champion combinations…</div>
    <div v-else-if="error" class="combos-error">{{ error }}</div>
    <div v-else-if="!hasData" class="combos-empty">
      Not enough team games to show champion combinations.
      <span class="requirement-hint">(Minimum: 1 game where all team members played together)</span>
    </div>

    <ChartCard v-else title="🏆 Best Champion Combinations">
      <div class="combos-content">
        <div class="combos-list">
          <div v-for="(combo, i) in topCombos" :key="i" class="combo-row" :class="getWinRateClass(combo.winRate)">
            <span class="combo-rank">#{{ i + 1 }}</span>
            <div class="combo-champions">
              <div v-for="(champ, j) in combo.champions" :key="j" class="champion-pick">
                <img 
                  :src="getChampionIcon(champ.championName)" 
                  :alt="champ.championName"
                  class="champion-icon"
                  @error="handleImageError"
                />
                <span class="champion-name">{{ champ.championName }}</span>
              </div>
            </div>
            <div class="combo-stats">
              <span class="combo-winrate" :class="getWinRateClass(combo.winRate)">{{ combo.winRate }}%</span>
              <span class="combo-games">{{ combo.gamesPlayed }}G / {{ combo.wins }}W</span>
            </div>
          </div>
        </div>
        <div class="combos-footer">
          <span>Top 5 combinations</span>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamChampionCombos from '@/assets/getTeamChampionCombos.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const combosData = ref(null);

const hasData = computed(() => combosData.value?.combos?.length > 0);

const topCombos = computed(() => {
  if (!combosData.value?.combos) return [];
  return combosData.value.combos.slice(0, 5);
});

function getChampionIcon(championName) {
  const formatted = championName.replace(/[^a-zA-Z]/g, '');
  return `https://ddragon.leagueoflegends.com/cdn/14.10.1/img/champion/${formatted}.png`;
}

function handleImageError(e) {
  e.target.style.display = 'none';
}

function getWinRateClass(winRate) {
  if (winRate >= 60) return 'wr-high';
  if (winRate >= 50) return 'wr-medium';
  return 'wr-low';
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    combosData.value = await getTeamChampionCombos(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load champion combinations.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.team-combos-container {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.team-combos-container :deep(.chart-card) {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.combos-loading, .combos-error, .combos-empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.combos-error { color: var(--color-danger); }
.combos-empty .requirement-hint { display: block; margin-top: 0.5rem; font-size: 0.85rem; opacity: 0.7; }

.combos-content {
  padding: 1rem 0 0.5rem 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.combos-list { display: flex; flex-direction: column; gap: 0.5rem; }
.combo-row { display: flex; align-items: center; gap: 1rem; padding: 0.75rem; background: var(--color-bg-elev); border-radius: 6px; border-left: 3px solid var(--color-border); }
.combo-row.wr-high { border-left-color: var(--color-success); }
.combo-row.wr-medium { border-left-color: var(--color-warning, #f59e0b); }
.combo-row.wr-low { border-left-color: var(--color-danger); }
.combo-rank { font-weight: 600; color: var(--color-text-muted); min-width: 2rem; }
.combo-champions { display: flex; flex-wrap: wrap; gap: 0.5rem; flex: 1; }
.champion-pick { display: flex; align-items: center; gap: 0.25rem; }
.champion-icon { width: 24px; height: 24px; border-radius: 4px; }
.champion-name { font-size: 0.85rem; }
.combo-stats { display: flex; flex-direction: column; align-items: flex-end; min-width: 70px; }
.combo-winrate { font-weight: 600; font-size: 1rem; }
.combo-winrate.wr-high { color: var(--color-success); }
.combo-winrate.wr-medium { color: var(--color-warning, #f59e0b); }
.combo-winrate.wr-low { color: var(--color-danger); }
.combo-games { font-size: 0.75rem; color: var(--color-text-muted); }
.combos-footer { margin-top: 1rem; text-align: center; font-size: 0.85rem; color: var(--color-text-muted); }
</style>

