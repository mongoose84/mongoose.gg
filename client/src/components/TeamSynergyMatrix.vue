<template>
  <div class="team-synergy-container">
    <div v-if="loading" class="synergy-loading">Loading team synergy data…</div>
    <div v-else-if="error" class="synergy-error">{{ error }}</div>
    <div v-else-if="!hasData" class="synergy-empty">No team synergy data available.</div>

    <ChartCard v-else title="🤝 Player Pair Synergies">
      <div class="synergy-content">
        <div class="synergy-grid">
          <div
            v-for="pair in synergyData.playerPairs"
            :key="`${pair.player1}-${pair.player2}`"
            class="synergy-pair"
            :class="getWinRateClass(pair.winRate)"
          >
            <div class="pair-names">
              <div class="player-with-role">
                <span class="player-name">{{ getShortName(pair.player1) }}</span>
                <span class="player-role">{{ getRoleShort(pair.player1Role) }}</span>
              </div>
              <span class="pair-separator">+</span>
              <div class="player-with-role">
                <span class="player-name">{{ getShortName(pair.player2) }}</span>
                <span class="player-role">{{ getRoleShort(pair.player2Role) }}</span>
              </div>
            </div>
            <div class="pair-stats">
              <span class="win-rate" :class="getWinRateClass(pair.winRate)">{{ pair.winRate }}%</span>
              <span class="games-count">{{ pair.gamesPlayed }} games</span>
            </div>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamSynergy from '@/assets/getTeamSynergy.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const synergyData = ref(null);

const hasData = computed(() => synergyData.value?.playerPairs?.length > 0);

function getShortName(fullName) {
  return fullName?.split('#')[0] || fullName;
}

function getRoleShort(role) {
  const roleMap = {
    'Top': 'TOP',
    'Jungle': 'JNG',
    'Mid': 'MID',
    'Bot': 'BOT',
    'Support': 'SUP'
  };
  return roleMap[role] || role;
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
    synergyData.value = await getTeamSynergy(props.userId);
  } catch (e) {
    console.error('Error loading team synergy data:', e);
    error.value = e?.message || 'Failed to load team synergy data.';
    synergyData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.team-synergy-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

.team-synergy-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 280px;
}

.synergy-loading, .synergy-error, .synergy-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.synergy-error { color: var(--color-danger); }

.synergy-content {
  padding: 0.5rem 0;
  padding-top: 1.5rem;
}

.synergy-grid {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.synergy-pair {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.6rem 0.8rem;
  background: var(--color-bg-elev);
  border-radius: 6px;
  border-left: 3px solid var(--color-border);
}

.synergy-pair.high { border-left-color: var(--color-success); }
.synergy-pair.medium { border-left-color: var(--color-warning, #f59e0b); }
.synergy-pair.low { border-left-color: var(--color-danger); }

.pair-names {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.player-with-role {
  display: flex;
  align-items: center;
  gap: 0.3rem;
}

.player-name { font-size: 0.85rem; font-weight: 500; color: var(--color-text); }
.player-role {
  font-size: 0.65rem;
  font-weight: 600;
  color: var(--color-text-muted);
  background: var(--color-bg);
  padding: 0.1rem 0.3rem;
  border-radius: 3px;
}
.pair-separator { color: var(--color-text-muted); font-size: 0.8rem; }

.pair-stats {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.win-rate { font-weight: 600; font-size: 0.9rem; }
.win-rate.high { color: var(--color-success); }
.win-rate.medium { color: var(--color-warning, #f59e0b); }
.win-rate.low { color: var(--color-danger); }

.games-count { font-size: 0.75rem; color: var(--color-text-muted); }
</style>

