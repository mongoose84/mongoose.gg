<template>
  <div class="duo-match-duration-container">
    <div v-if="loading" class="duration-loading">Loading match duration data…</div>
    <div v-else-if="error" class="duration-error">{{ error }}</div>
    <div v-else-if="!hasData" class="duration-empty">No match duration data available.</div>

    <ChartCard v-else title="Match Duration Performance">
      <div class="duration-content">
        <div class="duration-chart">
          <div
            v-for="bucket in durationData.buckets"
            :key="bucket.durationRange"
            class="bucket-col"
          >
            <div class="bar-container">
              <div
                class="bar"
                :style="{
                  height: bucket.winrate + '%',
                  backgroundColor: getWinRateColor(bucket.winrate)
                }"
                :title="`${bucket.winrate.toFixed(1)}% WR (${bucket.gamesPlayed} games)`"
              >
                <span class="bar-value">{{ bucket.winrate.toFixed(0) }}%</span>
              </div>
            </div>
            <div class="bucket-label">{{ bucket.durationRange }}</div>
            <div class="games-count">{{ bucket.gamesPlayed }} games</div>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoMatchDuration from '@/assets/getDuoMatchDuration.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const durationData = ref(null);

const hasData = computed(() => durationData.value?.buckets?.length > 0);

function getWinRateColor(winrate) {
  if (winrate >= 55) return 'var(--color-success)';
  if (winrate >= 50) return 'var(--color-text-muted)';
  return 'var(--color-danger)';
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    durationData.value = await getDuoMatchDuration(props.userId);
  } catch (e) {
    console.error('Error loading duo match duration data:', e);
    error.value = e?.message || 'Failed to load match duration data.';
    durationData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.duo-match-duration-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

.duo-match-duration-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 250px;
}

.duration-loading,
.duration-error,
.duration-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.duration-error {
  color: var(--color-danger);
}

.duration-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.5rem 0 0.5rem 0;
  height: 100%;
}

.duration-chart {
  display: flex;
  flex-direction: row;
  justify-content: space-around;
  align-items: flex-end;
  gap: 0.5rem;
  flex: 1;
  min-height: 180px;
}

.bucket-col {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
}

.bucket-label {
  font-size: 0.7rem;
  font-weight: 600;
  color: var(--color-text);
  text-align: center;
}

.bar-container {
  width: 100%;
  max-width: 50px;
  height: 150px;
  background-color: var(--color-bg);
  border-radius: 4px;
  overflow: hidden;
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
}

.bar {
  width: 100%;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding-top: 0.4rem;
  transition: all 0.3s ease;
  border-radius: 4px 4px 0 0;
  min-height: 25px;
}

.bar:hover {
  opacity: 0.8;
  transform: scaleX(1.05);
}

.bar-value {
  font-size: 0.7rem;
  font-weight: 600;
  color: white;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}

.games-count {
  font-size: 0.65rem;
  color: var(--color-text-muted);
  text-align: center;
}
</style>

