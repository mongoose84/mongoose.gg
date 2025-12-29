<template>
  <section class="dashboard">
    <!-- Smaller brand header -->
    <header class="brand brand--compact" aria-labelledby="app-title-duo">
      <router-link to="/" class="brand-link">
        <div class="brand-inner">
          <span class="logo compact" aria-hidden="true">
            <AppLogo :size="56" />
          </span>
          <div class="titles">
            <h1 id="app-title-duo" class="title compact">Do End</h1>
            <p class="subtitle compact">Cross Account LoL Statistics</p>
          </div>
        </div>
      </router-link>
    </header>

    <div class="dashboard-container">
      <h2 v-if="hasUser">{{ userName }} <span class="dashboard-type">Duo</span></h2>
      <h2 v-else>Missing dashboard details</h2>

      <div v-if="!hasUser">Please navigate via the dashboard list.</div>
      <div v-else-if="loading">Loading…</div>
      <div v-else-if="error">{{ error }}</div>

      <template v-else>
        <!-- Duo Identity Header -->
        <div v-if="gamers.length === 2" class="duo-identity">
          <div class="duo-cards-container">
            <GamerCard :gamer="gamers[0]" />
            <div class="duo-separator">
              <span class="handshake-icon" aria-label="Duo partners">🤝</span>
            </div>
            <GamerCard :gamer="gamers[1]" />
          </div>

          <!-- Context Stats -->
          <div class="duo-context">
            <span class="context-stat">{{ duoStats.gamesPlayed }} games together</span>
            <span class="context-separator">|</span>
            <span class="context-stat" :class="winRateClass">{{ duoStats.winRate }}% WR</span>
            <span class="context-separator">|</span>
            <span class="context-stat">{{ duoStats.queueType }}</span>
          </div>
          <!-- Latest Game Together -->
          <LatestGameTogether :latestGame="latestGameData" />
        </div>

        <!-- Fallback: Show regular gamer cards list if not exactly 2 gamers -->
        <GamerCardsList v-else :gamers="gamers" />

        <!-- Duo Features Container -->
        <div v-if="gamers.length === 2" class="duo-features-container">
          <!-- Trend Analysis Section (Top, Single Row) -->
          <div class="duo-features-grid-3">
            <DuoWinRateTrend :userId="userId" />
            <DuoStreak :userId="userId" />
            <DuoPerformanceRadar :userId="userId" />
          </div>

          <!-- Champion Synergy & Duo vs Enemy (Two-column layout) -->
          <div class="duo-features-grid">
            <ChampionSynergyMatrix :userId="userId" :gamers="gamers" />
            <DuoVsEnemyMatrix :userId="userId" :gamers="gamers" />
          </div>

          <!-- Match Duration & Side Win Rate (Two-column layout) -->
          <div class="duo-features-grid">
            <DuoMatchDuration :userId="userId" />
            <SideWinRate :userId="userId" mode="duo" />
          </div>

          <!-- Kill Analysis Section -->
          <div class="duo-features-grid-4">
            <DuoMultiKillShowcase :userId="userId" />
            <DuoKillParticipation :userId="userId" />
            <DuoKillsByPhase :userId="userId" />
            <DuoKillsTrend :userId="userId" />
          </div>

          <!-- Death Analysis Section -->
          <div class="duo-features-grid-3-auto">
            <DuoDeathTimerImpact :userId="userId" />
            <DuoDeathsByDuration :userId="userId" />
            <DuoDeathsTrend :userId="userId" />
          </div>

          <!-- Improvement Summary (Full width) -->
          <div class="duo-summary-section">
            <DuoImprovementSummary :userId="userId" />
          </div>
        </div>
      </template>
    </div>
  </section>

</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { useGamers } from '@/composables/useGamers.js';
import { getDuoStats, getDuoLatestGame } from '@/api/duo.js';
// Shared components
import GamerCardsList from '@/components/shared/GamerCardsList.vue';
import SideWinRate from '@/components/shared/SideWinRate.vue';
import AppLogo from '@/components/shared/AppLogo.vue';
import LatestGameTogether from '@/components/shared/LatestGameTogether.vue';
// Duo components
import ChampionSynergyMatrix from '@/components/duo/ChampionSynergyMatrix.vue';
import DuoVsEnemyMatrix from '@/components/duo/DuoVsEnemyMatrix.vue';
import DuoMatchDuration from '@/components/duo/DuoMatchDuration.vue';
import DuoImprovementSummary from '@/components/duo/DuoImprovementSummary.vue';
import DuoMultiKillShowcase from '@/components/duo/DuoMultiKillShowcase.vue';
import DuoKillsByPhase from '@/components/duo/DuoKillsByPhase.vue';
import DuoKillParticipation from '@/components/duo/DuoKillParticipation.vue';
import DuoKillsTrend from '@/components/duo/DuoKillsTrend.vue';
import DuoDeathTimerImpact from '@/components/duo/DuoDeathTimerImpact.vue';
import DuoDeathsByDuration from '@/components/duo/DuoDeathsByDuration.vue';
import DuoDeathShare from '@/components/duo/DuoDeathShare.vue';
import DuoDeathsTrend from '@/components/duo/DuoDeathsTrend.vue';
import DuoWinRateTrend from '@/components/duo/DuoWinRateTrend.vue';
import DuoPerformanceRadar from '@/components/duo/DuoPerformanceRadar.vue';
import DuoStreak from '@/components/duo/DuoStreak.vue';
// Local views
import GamerCard from '@/views/GamerCard.vue';

// ----- Props coming from the parent (router, other component, etc.) -----
const props = defineProps({
  userName: {
    type: String,
    required: true,
  },
  userId: {
    type: [String, Number],
    required: true,
  },
});

const { loading, error, gamers, hasUser, load } = useGamers(() => ({
  userName: props.userName,
  userId: props.userId,
}));

// Duo statistics state
const duoStatsData = ref(null);
const duoStatsLoading = ref(false);
const duoStatsError = ref(null);

// Latest game together state
const latestGameData = ref(null);

// Fetch duo statistics
async function loadDuoStats() {
  if (!hasUser.value) return;

  duoStatsLoading.value = true;
  duoStatsError.value = null;

  try {
    const data = await getDuoStats(props.userId);
    duoStatsData.value = data;
  } catch (e) {
    duoStatsError.value = e?.message || 'Failed to load duo statistics.';
    console.error('Error loading duo stats:', e);
  } finally {
    duoStatsLoading.value = false;
  }
}

// Fetch latest game together
async function loadLatestGame() {
  if (!hasUser.value) return;

  try {
    const data = await getDuoLatestGame(props.userId);
    latestGameData.value = data;
  } catch (e) {
    console.error('Error loading latest game together:', e);
  }
}

// Computed duo statistics with fallback
const duoStats = computed(() => {
  if (duoStatsData.value) {
    return duoStatsData.value;
  }

  // Fallback while loading or on error
  return {
    gamesPlayed: 0,
    winRate: 0,
    queueType: 'Loading...'
  };
});

// Win rate color class
const winRateClass = computed(() => {
  const wr = duoStats.value.winRate;
  if (wr >= 55) return 'wr-good';
  if (wr >= 50) return 'wr-neutral';
  return 'wr-bad';
});

// Load duo stats and latest game when component mounts
onMounted(() => {
  loadDuoStats();
  loadLatestGame();
});

// Watch for userId changes and reload duo stats
watch(
  () => props.userId,
  () => {
    loadDuoStats();
    loadLatestGame();
  }
);

// (Optional) expose `load` so a parent could call it manually
defineExpose({ load, loadDuoStats, loadLatestGame });
</script>

<style scoped>
.dashboard {
  width: 100%;
  margin: 2rem 0;
  padding: 0 1rem;
}

.brand--compact {
  width: 100%;
  margin: 0 0 0.5rem 0;
}

.brand-link {
  text-decoration: none;
  color: inherit;
  display: block;
  transition: opacity 0.2s ease;
}

.brand-link:hover {
  opacity: 0.8;
  cursor: pointer;
}

.brand-inner {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 0.75rem;
}

.logo.compact {
  display: inline-flex;
  width: 56px;
  height: 56px;
  color: var(--color-primary);
}

.titles .title.compact {
  margin: 0;
  font-size: clamp(1.4rem, 3vw, 2rem);
  line-height: 1.1;
}

.titles .subtitle.compact {
  margin: 0.1rem 0 0;
  font-size: clamp(0.8rem, 1.4vw, 1rem);
  opacity: 0.85;
}

.titles { text-align: left; }

.dashboard-container h2 { text-align: left; margin-left: 1rem; }

.dashboard-type {
  font-size: 0.75em;
  opacity: 0.7;
  font-weight: normal;
  margin-left: 0.5rem;
}

/* Duo Identity Section */
.duo-identity {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
  margin: 2rem auto;
  max-width: 800px;
}

.duo-cards-container {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 2rem;
  flex-wrap: wrap;
}

.duo-separator {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 60px;
}

.handshake-icon {
  font-size: 2.5rem;
  opacity: 0.8;
  transition: transform 0.2s ease;
}

.duo-separator:hover .handshake-icon {
  transform: scale(1.1);
}

/* Context Stats */
.duo-context {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem 2rem;
  background: var(--color-bg-elev);
  border: 1px solid var(--color-border);
  border-radius: 8px;
  font-size: 1rem;
  color: var(--color-text);
}

.context-stat {
  font-weight: 500;
}

.context-separator {
  opacity: 0.5;
  font-weight: 300;
}

/* Win rate colors */
.wr-good {
  color: var(--color-success);
  font-weight: 600;
}

.wr-neutral {
  color: var(--color-text);
  font-weight: 600;
}

.wr-bad {
  color: var(--color-danger);
  font-weight: 600;
}

/* Duo Features Container - Consistent max-width for all duo features */
.duo-features-container {
  max-width: 1400px;
  margin: 2rem auto 0;
}

/* Section Titles */
.section-title {
  margin: 2.5rem 0 0.5rem 0;
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-text);
  padding-bottom: 0.5rem;
  border-bottom: 2px solid var(--color-border);
}

/* Duo Features Grid (Two-column layout) */
.duo-features-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
  gap: 1.5rem;
  margin-top: 1.5rem;
}

/* Duo Features Grid (Three-column layout for Trend Analysis - compact height) */
.duo-features-grid-3 {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
  margin-top: 1.5rem;
}

.duo-features-grid-3 :deep(.chart-card) {
  min-height: 360px;
  height: 360px;
}

/* Duo Features Grid (Four-column layout for Kill Analysis) */
.duo-features-grid-4 {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1.5rem;
  margin-top: 1.5rem;
}

/* Duo Features Grid (Three-column layout for Death Analysis) */
.duo-features-grid-3-auto {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
  margin-top: 1.5rem;
}

/* Duo Summary Section (Full width) */
.duo-summary-section {
  margin-top: 1.5rem;
  width: 100%;
}

@media (max-width: 1400px) {
  .duo-features-grid-4 {
    grid-template-columns: repeat(2, 1fr);
  }
  .duo-features-grid-3-auto {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 1400px) {
  .duo-features-grid-3 {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 1100px) {
  .duo-features-grid {
    grid-template-columns: 1fr;
  }
  .duo-features-grid-4 {
    grid-template-columns: 1fr;
  }
  .duo-features-grid-3-auto {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 900px) {
  .duo-features-grid-3 {
    grid-template-columns: 1fr;
  }
}
</style>
