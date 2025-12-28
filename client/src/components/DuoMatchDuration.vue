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
            class="bucket-row"
          >
            <div class="bucket-label">{{ bucket.durationRange }} min</div>
            <div class="bar-container">
              <div 
                class="bar"
                :style="{ 
                  width: bucket.winrate + '%',
                  backgroundColor: getWinRateColor(bucket.winrate)
                }"
                :title="`${bucket.winrate.toFixed(1)}% WR (${bucket.gamesPlayed} games)`"
              >
                <span v-if="bucket.winrate >= 10" class="bar-value">
                  {{ bucket.winrate.toFixed(0) }}%
                </span>
              </div>
            </div>
            <div class="games-count">{{ bucket.gamesPlayed }}</div>
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
  padding: 0.5rem 0;
  padding-top: 1.5rem;
}

.duration-chart {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.bucket-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.bucket-label {
  min-width: 80px;
  font-size: 0.75rem;
  font-weight: 600;
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
  transition: all 0.3s ease;
  border-radius: 4px;
  min-width: 30px;
}

.bar:hover {
  opacity: 0.8;
  transform: scaleY(1.05);
}

.bar-value {
  font-size: 0.75rem;
  font-weight: 600;
  color: white;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}

.games-count {
  min-width: 40px;
  text-align: right;
  font-size: 0.75rem;
  color: var(--color-text-muted);
}
</style>

