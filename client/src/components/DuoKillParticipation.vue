<template>
  <div class="kill-participation-container">
    <div v-if="loading" class="loading">Loading kill participation data…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No kill participation data available.</div>

    <ChartCard v-else title="🗡️ Kill Share">
      <div class="share-content">
        <div class="total-header">
          <span class="total-label">Total Duo Kills:</span>
          <span class="total-value">{{ data.totalDuoKills }}</span>
        </div>

        <div class="player-list">
          <div v-for="(player, i) in data.players" :key="player.playerName" class="player-row">
            <div class="player-info">
              <span class="player-name">{{ getShortName(player.playerName) }}</span>
              <span class="player-avg">{{ player.avgKillsPerGame }} K/game</span>
            </div>
            <div class="bar-wrapper">
              <div class="bar" :style="{ width: Math.min(player.killParticipation, 100) + '%', backgroundColor: getColor(i) }">
              </div>
              <span class="percent-label">{{ player.killParticipation.toFixed(0) }}%</span>
            </div>
          </div>
        </div>

        <p v-if="lowestPlayer" class="insight">
          <span v-if="isLowestSignificant">
            ⚠️ {{ getShortName(lowestPlayer.playerName) }} has low kill share ({{ lowestPlayer.killParticipation.toFixed(0) }}%)
          </span>
          <span v-else>
            ✓ Balanced kill distribution across the duo
          </span>
        </p>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoKillParticipation from '@/assets/getDuoKillParticipation.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const data = ref(null);

const hasData = computed(() => data.value?.players?.length > 0);

const lowestPlayer = computed(() => {
  if (!data.value?.players?.length) return null;
  return data.value.players[data.value.players.length - 1];
});

const isLowestSignificant = computed(() => {
  if (!lowestPlayer.value) return false;
  return lowestPlayer.value.killParticipation < 40;
});

function getShortName(fullName) {
  return fullName?.split('#')[0] || fullName;
}

const colors = ['var(--color-success)', 'var(--color-primary)'];

function getColor(index) {
  return colors[index % colors.length];
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    data.value = await getDuoKillParticipation(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load kill participation data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.kill-participation-container { width: 100%; height: 100%; display: flex; flex-direction: column; }
.kill-participation-container :deep(.chart-card) { flex: 1; display: flex; flex-direction: column; }
.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }

.share-content { padding: 1rem 0; flex: 1; display: flex; flex-direction: column; }

.total-header { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 1rem; padding-bottom: 0.75rem; border-bottom: 1px solid var(--color-border); }
.total-label { font-size: 0.85rem; color: var(--color-text-muted); }
.total-value { font-size: 1.1rem; font-weight: 600; color: var(--color-success); }

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

