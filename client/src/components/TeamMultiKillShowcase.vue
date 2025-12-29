<template>
  <div class="multi-kill-container">
    <div v-if="loading" class="loading">Loading multi-kill data…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No multi-kill data available.</div>

    <ChartCard v-else title="🔥 Multi-Kill Showcase">
      <div class="showcase-content">
        <!-- Team Totals -->
        <div class="totals-row">
          <div class="total-item">
            <span class="total-count">{{ data.teamTotals.pentaKills }}</span>
            <span class="total-label">Pentas</span>
          </div>
          <div class="total-item">
            <span class="total-count">{{ data.teamTotals.quadraKills }}</span>
            <span class="total-label">Quadras</span>
          </div>
          <div class="total-item">
            <span class="total-count">{{ data.teamTotals.tripleKills }}</span>
            <span class="total-label">Triples</span>
          </div>
          <div class="total-item">
            <span class="total-count">{{ data.teamTotals.doubleKills }}</span>
            <span class="total-label">Doubles</span>
          </div>
        </div>

        <!-- Player Breakdown -->
        <div class="player-list">
          <div v-for="player in data.players" :key="player.playerName" class="player-row">
            <span class="player-name">{{ getShortName(player.playerName) }}</span>
            <div class="kill-badges">
              <span v-if="player.pentaKills > 0" class="badge penta">{{ player.pentaKills }}P</span>
              <span v-if="player.quadraKills > 0" class="badge quadra">{{ player.quadraKills }}Q</span>
              <span v-if="player.tripleKills > 0" class="badge triple">{{ player.tripleKills }}T</span>
              <span v-if="player.doubleKills > 0" class="badge double">{{ player.doubleKills }}D</span>
              <span v-if="player.totalMultiKills === 0" class="no-multi">—</span>
            </div>
            <span class="total-multi">{{ player.totalMultiKills }}</span>
          </div>
        </div>

        <!-- MVP Highlight -->
        <p v-if="mvpPlayer" class="insight">
          🏆 {{ getShortName(mvpPlayer.playerName) }} leads with {{ mvpPlayer.totalMultiKills }} multi-kills
          <span v-if="mvpPlayer.pentaKills > 0"> including {{ mvpPlayer.pentaKills }} penta{{ mvpPlayer.pentaKills > 1 ? 's' : '' }}!</span>
        </p>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamMultiKills from '@/assets/getTeamMultiKills.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const data = ref(null);

const hasData = computed(() => data.value?.players?.length > 0);

const mvpPlayer = computed(() => {
  if (!data.value?.players?.length) return null;
  return data.value.players[0]; // Already sorted by multi-kills
});

function getShortName(fullName) {
  return fullName?.split('#')[0] || fullName;
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    data.value = await getTeamMultiKills(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load multi-kill data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.multi-kill-container { width: 100%; height: 100%; display: flex; flex-direction: column; }
.multi-kill-container :deep(.chart-card) { flex: 1; display: flex; flex-direction: column; }
.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }

.showcase-content { padding: 1rem 0; flex: 1; display: flex; flex-direction: column; }

.totals-row { display: flex; justify-content: space-around; margin-bottom: 1rem; padding-bottom: 0.75rem; border-bottom: 1px solid var(--color-border); }
.total-item { display: flex; flex-direction: column; align-items: center; }
.total-count { font-size: 1.5rem; font-weight: 700; color: var(--color-primary); }
.total-label { font-size: 0.7rem; color: var(--color-text-muted); }

.player-list { display: flex; flex-direction: column; gap: 0.5rem; flex: 1; }
.player-row { display: flex; align-items: center; gap: 0.75rem; padding: 0.3rem 0; }
.player-name { width: 80px; font-size: 0.85rem; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.kill-badges { flex: 1; display: flex; gap: 0.3rem; flex-wrap: wrap; }
.badge { padding: 0.15rem 0.4rem; border-radius: 4px; font-size: 0.7rem; font-weight: 600; }
.badge.penta { background: linear-gradient(135deg, #f59e0b, #ef4444); color: white; }
.badge.quadra { background: #8b5cf6; color: white; }
.badge.triple { background: var(--color-primary); color: white; }
.badge.double { background: #a78bfa; color: white; }
.no-multi { color: var(--color-text-muted); font-size: 0.8rem; }
.total-multi { font-size: 0.85rem; font-weight: 600; min-width: 25px; text-align: right; }

.insight { margin-top: auto; padding-top: 1rem; font-size: 0.8rem; padding: 0.5rem; background: var(--color-bg-elev); border-radius: 6px; }
</style>

