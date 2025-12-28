<template>
  <div class="team-performance-container">
    <div v-if="loading" class="perf-loading">Loading team performance data…</div>
    <div v-else-if="error" class="perf-error">{{ error }}</div>
    <div v-else-if="!hasData" class="perf-empty">No team performance data available.</div>

    <ChartCard v-else title="📊 Individual Performance">
      <div class="perf-content">
        <div class="perf-table">
          <div class="perf-header">
            <span class="col-name">Player</span>
            <span class="col-stat">KDA</span>
            <span class="col-stat">WR%</span>
            <span class="col-stat">KP%</span>
            <span class="col-stat">GPM</span>
          </div>
          <div 
            v-for="player in performanceData.players" 
            :key="player.playerName"
            class="perf-row"
          >
            <span class="col-name">{{ getShortName(player.playerName) }}</span>
            <span class="col-stat" :class="getKdaClass(player.kda)">{{ player.kda.toFixed(2) }}</span>
            <span class="col-stat" :class="getWinRateClass(player.winRate)">{{ player.winRate }}%</span>
            <span class="col-stat">{{ player.killParticipation }}%</span>
            <span class="col-stat">{{ Math.round(player.goldPerMin) }}</span>
          </div>
        </div>

        <!-- Team Totals -->
        <div v-if="performanceData.teamTotals" class="team-totals">
          <span class="totals-label">Team Avg:</span>
          <span class="totals-stat">{{ performanceData.teamTotals.avgTeamKda.toFixed(2) }} KDA</span>
          <span class="totals-stat">{{ Math.round(performanceData.teamTotals.avgTeamGoldPerMin) }} GPM</span>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamPerformance from '@/assets/getTeamPerformance.js';

const props = defineProps({
  userId: { type: [String, Number], required: true },
});

const loading = ref(false);
const error = ref(null);
const performanceData = ref(null);

const hasData = computed(() => performanceData.value?.players?.length > 0);

function getShortName(fullName) { return fullName?.split('#')[0] || fullName; }

function getKdaClass(kda) {
  if (kda >= 3) return 'high';
  if (kda >= 2) return 'medium';
  return 'low';
}

function getWinRateClass(winRate) {
  if (winRate >= 55) return 'high';
  if (winRate >= 45) return 'medium';
  return 'low';
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    performanceData.value = await getTeamPerformance(props.userId);
  } catch (e) {
    console.error('Error loading team performance:', e);
    error.value = e?.message || 'Failed to load team performance.';
    performanceData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.team-performance-container { width: 100%; max-width: 100%; height: 100%; }
.team-performance-container :deep(.chart-card) { max-width: 100%; height: 100%; min-height: 280px; }

.perf-loading, .perf-error, .perf-empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.perf-error { color: var(--color-danger); }

.perf-content { display: flex; flex-direction: column; gap: 0.75rem; padding: 0.5rem 0; padding-top: 1.5rem; }

.perf-table { display: flex; flex-direction: column; gap: 0.3rem; }

.perf-header, .perf-row {
  display: grid;
  grid-template-columns: 1.5fr repeat(4, 1fr);
  gap: 0.5rem;
  padding: 0.4rem 0.5rem;
  align-items: center;
}

.perf-header {
  font-size: 0.7rem; font-weight: 600; color: var(--color-text-muted);
  border-bottom: 1px solid var(--color-border);
}

.perf-row {
  font-size: 0.8rem; background: var(--color-bg-elev); border-radius: 4px;
}

.col-name { font-weight: 500; color: var(--color-text); text-align: left; }
.col-stat { text-align: center; }

.col-stat.high { color: var(--color-success); font-weight: 600; }
.col-stat.medium { color: var(--color-warning, #f59e0b); }
.col-stat.low { color: var(--color-danger); }

.team-totals {
  display: flex; gap: 1rem; align-items: center;
  padding: 0.5rem; background: var(--color-bg); border-radius: 4px;
  font-size: 0.75rem;
}

.totals-label { font-weight: 600; color: var(--color-text-muted); }
.totals-stat { color: var(--color-text); }
</style>

