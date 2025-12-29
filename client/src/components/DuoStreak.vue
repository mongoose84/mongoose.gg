<template>
  <div class="streak-container">
    <div v-if="loading" class="loading">Loading streak data…</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else-if="!hasData" class="empty">No streak data available.</div>

    <ChartCard v-else title="🔥 Win/Loss Streak">
      <div class="streak-content">
        <!-- Current Streak Display -->
        <div class="current-streak" :class="data.isWinStreak ? 'win' : 'loss'">
          <div class="streak-number">{{ data.currentStreak }}</div>
          <div class="streak-type">{{ data.isWinStreak ? 'Win Streak' : 'Loss Streak' }}</div>
        </div>

        <!-- Streak Message -->
        <div class="streak-message">{{ data.streakMessage }}</div>

        <!-- Historical Streaks -->
        <div class="streak-history">
          <div class="history-item">
            <span class="history-label">Longest Win Streak</span>
            <span class="history-value win">{{ data.longestWinStreak }}</span>
          </div>
          <div class="history-item">
            <span class="history-label">Longest Loss Streak</span>
            <span class="history-value loss">{{ data.longestLossStreak }}</span>
          </div>
        </div>

        <!-- Visual Streak Indicator -->
        <div class="streak-visual">
          <div v-for="i in Math.min(data.currentStreak, 10)" :key="i" 
               class="streak-dot" :class="data.isWinStreak ? 'win' : 'loss'">
          </div>
          <span v-if="data.currentStreak > 10" class="more-indicator">+{{ data.currentStreak - 10 }}</span>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoStreak from '@/assets/getDuoStreak.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const data = ref(null);

const hasData = computed(() => data.value !== null);

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    data.value = await getDuoStreak(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load streak data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.streak-container { width: 100%; height: 100%; }
.loading, .error, .empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.error { color: var(--color-danger); }

.streak-content { padding: 1rem 0; display: flex; flex-direction: column; align-items: center; gap: 0.75rem; }

.current-streak {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 1.5rem 2rem;
  border-radius: 12px;
  background: var(--color-bg-elev);
}

.current-streak.win { border: 2px solid var(--color-success); }
.current-streak.loss { border: 2px solid var(--color-danger); }

.streak-number {
  font-size: 3rem;
  font-weight: 700;
  line-height: 1;
}

.current-streak.win .streak-number { color: var(--color-success); }
.current-streak.loss .streak-number { color: var(--color-danger); }

.streak-type {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--color-text-muted);
  margin-top: 0.25rem;
}

.streak-message {
  font-size: 1rem;
  text-align: center;
  padding: 0.75rem 1rem;
  background: var(--color-bg);
  border-radius: 8px;
  max-width: 300px;
}

.streak-history {
  display: flex;
  gap: 2rem;
  padding: 1rem;
  background: var(--color-bg);
  border-radius: 8px;
}

.history-item { display: flex; flex-direction: column; align-items: center; }
.history-label { font-size: 0.75rem; color: var(--color-text-muted); }
.history-value { font-size: 1.5rem; font-weight: 700; }
.history-value.win { color: var(--color-success); }
.history-value.loss { color: var(--color-danger); }

.streak-visual {
  display: flex;
  gap: 0.4rem;
  align-items: center;
  flex-wrap: wrap;
  justify-content: center;
}

.streak-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
}

.streak-dot.win { background: var(--color-success); }
.streak-dot.loss { background: var(--color-danger); }

.more-indicator {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--color-text-muted);
  margin-left: 0.25rem;
}
</style>

