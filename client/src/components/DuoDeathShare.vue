<template>
  <div class="death-share-container">
    <div v-if="loading" class="loading">Loading death share data…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No death share data available.</div>

    <ChartCard v-else title="🎯 Death Share Distribution">
      <div class="share-content">
        <div class="total-header">
          <span class="total-label">Total Duo Deaths:</span>
          <span class="total-value">{{ data.totalDuoDeaths }}</span>
        </div>

        <div class="player-list">
          <div v-for="(player, i) in data.players" :key="player.playerName" class="player-row">
            <div class="player-info">
              <span class="player-name">{{ getShortName(player.playerName) }}</span>
              <span class="player-avg">{{ player.avgDeathsPerGame }} avg/game</span>
            </div>
            <div class="bar-wrapper">
              <div class="bar" :style="{ width: player.deathSharePercent + '%', backgroundColor: getColor(i) }">
              </div>
              <span class="percent-label">{{ player.deathSharePercent.toFixed(0) }}%</span>
            </div>
          </div>
        </div>

        <p v-if="highestPlayer" class="insight">
          <span v-if="isHighestSignificant">
            ⚠️ {{ getShortName(highestPlayer.playerName) }} accounts for {{ highestPlayer.deathSharePercent.toFixed(0) }}% of duo deaths
          </span>
          <span v-else>
            ✓ Deaths are relatively balanced between the duo
          </span>
        </p>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoDeathShare from '@/assets/getDuoDeathShare.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const data = ref(null);

const hasData = computed(() => data.value?.players?.length > 0);

const highestPlayer = computed(() => {
  if (!data.value?.players?.length) return null;
  return data.value.players[0];
});

const isHighestSignificant = computed(() => {
  if (!highestPlayer.value) return false;
  return highestPlayer.value.deathSharePercent > 65;
});

function getShortName(fullName) {
  return fullName?.split('#')[0] || fullName;
}

const colors = ['var(--color-danger)', 'var(--color-primary)'];

function getColor(index) {
  return colors[index % colors.length];
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    data.value = await getDuoDeathShare(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load death share data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.death-share-container { width: 100%; height: 100%; display: flex; flex-direction: column; }
.death-share-container :deep(.chart-card) { flex: 1; display: flex; flex-direction: column; }
.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }
.share-content { padding: 1rem 0; flex: 1; display: flex; flex-direction: column; }
.total-header { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 1rem; padding-bottom: 0.75rem; border-bottom: 1px solid var(--color-border); }
.total-label { font-size: 0.85rem; color: var(--color-text-muted); }
.total-value { font-size: 1.1rem; font-weight: 600; color: var(--color-danger); }
.player-list { display: flex; flex-direction: column; gap: 0.6rem; flex: 1; }
.player-row { display: flex; align-items: center; gap: 1rem; }
.player-info { width: 100px; display: flex; flex-direction: column; }
.player-name { font-size: 0.85rem; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.player-avg { font-size: 0.7rem; color: var(--color-text-muted); }
.bar-wrapper { flex: 1; display: flex; align-items: center; gap: 0.5rem; }
.bar { height: 20px; border-radius: 4px; min-width: 4px; }
.percent-label { font-size: 0.8rem; font-weight: 600; min-width: 35px; }
.insight { margin-top: auto; padding-top: 1rem; font-size: 0.8rem; padding: 0.5rem; background: var(--color-bg-elev); border-radius: 6px; }
</style>

