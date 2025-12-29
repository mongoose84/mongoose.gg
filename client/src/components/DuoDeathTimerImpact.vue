<template>
  <div class="death-timer-container">
    <div v-if="loading" class="loading">Loading death timer data…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No death timer data available.</div>

    <ChartCard v-else title="⏱️ Death Timer Impact">
      <div class="timer-content">
        <div class="duo-summary">
          <div class="summary-item wins">
            <span class="label">Avg Time Dead in Wins</span>
            <span class="value">{{ formatSeconds(data.duoAvgTimeDeadWins) }}</span>
          </div>
          <div class="summary-item losses">
            <span class="label">Avg Time Dead in Losses</span>
            <span class="value">{{ formatSeconds(data.duoAvgTimeDeadLosses) }}</span>
          </div>
        </div>

        <div class="player-list">
          <div v-for="player in data.players" :key="player.playerName" class="player-row">
            <div class="player-name">{{ getShortName(player.playerName) }}</div>
            <div class="player-bars">
              <div class="bar-group">
                <span class="bar-label">W</span>
                <div class="bar win-bar" :style="{ width: getBarWidth(player.avgTimeDeadWins) + '%' }">
                  <span class="bar-value">{{ formatSeconds(player.avgTimeDeadWins) }}</span>
                </div>
              </div>
              <div class="bar-group">
                <span class="bar-label">L</span>
                <div class="bar loss-bar" :style="{ width: getBarWidth(player.avgTimeDeadLosses) + '%' }">
                  <span class="bar-value">{{ formatSeconds(player.avgTimeDeadLosses) }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <p class="insight">
          💡 Higher death timers in losses often indicate dying at critical late-game moments.
        </p>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoDeathTimerImpact from '@/assets/getDuoDeathTimerImpact.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const data = ref(null);

const hasData = computed(() => data.value?.players?.length > 0);

const maxTimeDead = computed(() => {
  if (!data.value?.players) return 300;
  const values = data.value.players.flatMap(p => [p.avgTimeDeadWins, p.avgTimeDeadLosses]);
  return Math.max(...values, 60);
});

function getBarWidth(value) {
  return Math.min(100, (value / maxTimeDead.value) * 100);
}

function formatSeconds(seconds) {
  if (!seconds) return '0s';
  const mins = Math.floor(seconds / 60);
  const secs = Math.round(seconds % 60);
  return mins > 0 ? `${mins}m ${secs}s` : `${secs}s`;
}

function getShortName(fullName) {
  return fullName?.split('#')[0] || fullName;
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    data.value = await getDuoDeathTimerImpact(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load death timer data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.death-timer-container { width: 100%; }
.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }
.timer-content { padding: 1rem 0; }
.duo-summary { display: flex; gap: 2rem; margin-bottom: 1.5rem; padding-bottom: 1rem; border-bottom: 1px solid var(--color-border); }
.summary-item { display: flex; flex-direction: column; }
.summary-item .label { font-size: 0.8rem; color: var(--color-text-muted); }
.summary-item .value { font-size: 1.25rem; font-weight: 600; }
.summary-item.wins .value { color: var(--color-success); }
.summary-item.losses .value { color: var(--color-danger); }
.player-list { display: flex; flex-direction: column; gap: 0.75rem; }
.player-row { display: flex; align-items: center; gap: 1rem; }
.player-name { width: 80px; font-size: 0.85rem; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.player-bars { flex: 1; display: flex; flex-direction: column; gap: 0.25rem; }
.bar-group { display: flex; align-items: center; gap: 0.5rem; }
.bar-label { width: 16px; font-size: 0.7rem; color: var(--color-text-muted); }
.bar { height: 18px; border-radius: 3px; display: flex; align-items: center; padding-left: 0.5rem; min-width: 40px; }
.win-bar { background: var(--color-success); }
.loss-bar { background: var(--color-danger); }
.bar-value { font-size: 0.7rem; color: white; font-weight: 500; }
.insight { margin-top: 1rem; font-size: 0.8rem; color: var(--color-text-muted); font-style: italic; }
</style>

