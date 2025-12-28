<template>
  <div class="duo-kill-efficiency-container">
    <div v-if="loading" class="efficiency-loading">Loading kill efficiency data…</div>
    <div v-else-if="error" class="efficiency-error">{{ error }}</div>
    <div v-else-if="!hasData" class="efficiency-empty">No kill efficiency data available.</div>

    <ChartCard v-else title="Kill & Death Efficiency">
      <div class="efficiency-content">
        <div class="charts-wrapper">
          <!-- Kill Participation Chart -->
          <div class="chart-section">
            <h5 class="chart-title">Kill Participation</h5>
            <div class="bar-chart">
              <div
                v-for="player in efficiencyData.players"
                :key="player.playerName + '-kills'"
                class="bar-row"
              >
                <div class="player-label">{{ player.playerName }}</div>
                <div class="bar-container">
                  <div
                    class="bar"
                    :style="{
                      width: player.killParticipation + '%',
                      backgroundColor: getPlayerColor(player.playerName)
                    }"
                  >
                    <span class="bar-value">{{ player.killParticipation.toFixed(0) }}%</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Death Share in Losses Chart -->
          <div class="chart-section">
            <h5 class="chart-title">Deaths in Losses</h5>
            <div class="bar-chart">
              <div
                v-for="player in efficiencyData.players"
                :key="player.playerName + '-deaths'"
                class="bar-row"
              >
                <div class="player-label">{{ player.playerName }}</div>
                <div class="bar-container">
                  <div
                    class="bar"
                    :style="{
                      width: player.deathShareInLosses + '%',
                      backgroundColor: getPlayerColor(player.playerName)
                    }"
                  >
                    <span class="bar-value">{{ player.deathShareInLosses.toFixed(0) }}%</span>
                  </div>
                </div>
              </div>
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
import getDuoKillEfficiency from '@/assets/getDuoKillEfficiency.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const efficiencyData = ref(null);

const hasData = computed(() => efficiencyData.value?.players?.length > 0);

function getPlayerColor(playerName) {
  const allPlayers = efficiencyData.value?.players?.map(p => p.playerName) || [];
  const index = allPlayers.indexOf(playerName);
  
  const colors = [
    'var(--color-primary)',      // Purple - First player
    'var(--color-success)',      // Green - Second player
  ];
  return colors[index] || 'var(--color-text)';
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    efficiencyData.value = await getDuoKillEfficiency(props.userId);
  } catch (e) {
    console.error('Error loading duo kill efficiency data:', e);
    error.value = e?.message || 'Failed to load kill efficiency data.';
    efficiencyData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.duo-kill-efficiency-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

.duo-kill-efficiency-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 250px;
}

.efficiency-loading,
.efficiency-error,
.efficiency-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.efficiency-error {
  color: var(--color-danger);
}

.efficiency-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 0.5rem 0;
  padding-top: 1.5rem;
}

.charts-wrapper {
  display: flex;
  gap: 2rem;
  align-items: flex-start;
  justify-content: center;
  width: 100%;
}

.chart-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.chart-title {
  margin: 0;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-text);
  text-align: center;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.bar-chart {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.bar-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.player-label {
  min-width: 80px;
  font-size: 0.75rem;
  font-weight: 500;
  color: var(--color-text);
  text-align: right;
}

.bar-container {
  flex: 1;
  height: 28px;
  background-color: var(--color-bg-elev);
  border-radius: 4px;
  overflow: hidden;
  position: relative;
}

.bar {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 0.5rem;
  transition: width 0.3s ease;
  border-radius: 4px;
  min-width: 40px;
}

.bar-value {
  font-size: 0.75rem;
  font-weight: 600;
  color: white;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}

@media (max-width: 768px) {
  .charts-wrapper {
    flex-direction: column;
  }
}
</style>

