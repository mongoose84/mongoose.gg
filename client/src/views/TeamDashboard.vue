<template>
  <section class="dashboard">
    <!-- Smaller brand header -->
    <header class="brand brand--compact" aria-labelledby="app-title-team">
      <router-link to="/" class="brand-link">
        <div class="brand-inner">
          <span class="logo compact" aria-hidden="true">
            <AppLogo :size="56" />
          </span>
          <div class="titles">
            <h1 id="app-title-team" class="title compact">Do End</h1>
            <p class="subtitle compact">Cross Account LoL Statistics</p>
          </div>
        </div>
      </router-link>
    </header>

    <div class="dashboard-container">
      <h2 v-if="hasUser">{{ userName }} <span class="dashboard-type">Team</span></h2>
      <h2 v-else>Missing dashboard details</h2>

      <div v-if="!hasUser">Please navigate via the dashboard list.</div>
      <div v-else-if="loading">Loading…</div>
      <div v-else-if="error">{{ error }}</div>

      <template v-else>
        <!-- Team Identity Header -->
        <div v-if="gamers.length >= 3" class="team-identity">
          <div class="team-cards-container">
            <GamerCard v-for="gamer in gamers" :key="gamer.puuid" :gamer="gamer" />
          </div>

          <!-- Context Stats -->
          <div class="team-context">
            <span class="context-stat">{{ teamStats.gamesPlayed }} games together</span>
            <span class="context-separator">|</span>
            <span class="context-stat" :class="winRateClass">{{ teamStats.winRate }}% WR</span>
            <span class="context-separator">|</span>
            <span class="context-stat">{{ teamStats.queueType }}</span>
            <span class="context-separator">|</span>
            <span class="context-stat">{{ teamStats.playerCount }} players</span>
          </div>
        </div>

        <!-- Fallback: Show regular gamer cards list if less than 3 gamers -->
        <GamerCardsList v-else :gamers="gamers" />

        <!-- Team Features Container -->
        <div v-if="gamers.length >= 3" class="team-features-container">
          <!-- Win Rate Trend, Side Win Rate & Duration Analysis (Three-column layout) -->
          <div class="team-features-grid-3">
            <TeamWinRateTrend :userId="userId" />
            <SideWinRate :userId="userId" mode="team" />
            <TeamDurationAnalysis :userId="userId" />
          </div>

          <!-- Synergy, Matrix & Champion Combos (Three-column layout) -->
          <div class="team-features-grid-3">
            <TeamSynergyMatrix :userId="userId" />
            <TeamRolePairEffectiveness :userId="userId" />
            <TeamChampionCombos :userId="userId" />
          </div>

          <!-- Kill Analysis (Four-column layout) -->
          <div class="team-features-grid-4">
            <TeamMultiKillShowcase :userId="userId" />
            <TeamKillsByPhase :userId="userId" />
            <TeamKillParticipation :userId="userId" />
            <TeamKillsTrend :userId="userId" />
          </div>

          <!-- Death Analysis (Four-column layout) -->
          <div class="team-features-grid-4">
            <TeamDeathTimerImpact :userId="userId" />
            <TeamDeathsByDuration :userId="userId" />
            <TeamDeathShare :userId="userId" />
            <TeamDeathsTrend :userId="userId" />
          </div>

        </div>
      </template>
    </div>
  </section>

</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { useGamers } from '@/composables/useGamers.js';
import GamerCardsList from '@/components/GamerCardsList.vue';
import GamerCard from '@/views/GamerCard.vue';
import AppLogo from '@/components/AppLogo.vue';
import getTeamStats from '@/assets/getTeamStats.js';
import TeamSynergyMatrix from '@/components/TeamSynergyMatrix.vue';
import TeamWinRateTrend from '@/components/TeamWinRateTrend.vue';
import TeamDurationAnalysis from '@/components/TeamDurationAnalysis.vue';
import TeamChampionCombos from '@/components/TeamChampionCombos.vue';
import TeamRolePairEffectiveness from '@/components/TeamRolePairEffectiveness.vue';
import TeamDeathTimerImpact from '@/components/TeamDeathTimerImpact.vue';
import TeamDeathsByDuration from '@/components/TeamDeathsByDuration.vue';
import TeamDeathShare from '@/components/TeamDeathShare.vue';
import TeamDeathsTrend from '@/components/TeamDeathsTrend.vue';
import TeamKillParticipation from '@/components/TeamKillParticipation.vue';
import TeamKillsByPhase from '@/components/TeamKillsByPhase.vue';
import TeamKillsTrend from '@/components/TeamKillsTrend.vue';
import TeamMultiKillShowcase from '@/components/TeamMultiKillShowcase.vue';
import SideWinRate from '@/components/SideWinRate.vue';

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

// Team statistics state
const teamStatsData = ref(null);
const teamStatsLoading = ref(false);
const teamStatsError = ref(null);

// Fetch team statistics
async function loadTeamStats() {
  if (!hasUser.value) return;

  teamStatsLoading.value = true;
  teamStatsError.value = null;

  try {
    const data = await getTeamStats(props.userId);
    teamStatsData.value = data;
  } catch (e) {
    teamStatsError.value = e?.message || 'Failed to load team statistics.';
    console.error('Error loading team stats:', e);
  } finally {
    teamStatsLoading.value = false;
  }
}

// Computed team statistics with fallback
const teamStats = computed(() => {
  if (teamStatsData.value) {
    return teamStatsData.value;
  }

  // Fallback while loading or on error
  return {
    gamesPlayed: 0,
    winRate: 0,
    queueType: 'Loading...',
    playerCount: 0
  };
});

// Win rate styling class
const winRateClass = computed(() => {
  const wr = teamStats.value.winRate;
  if (wr >= 55) return 'win-rate-high';
  if (wr >= 45) return 'win-rate-medium';
  return 'win-rate-low';
});

// Load team stats when gamers are loaded
watch(gamers, (newGamers) => {
  if (newGamers?.length >= 3) {
    loadTeamStats();
  }
});

onMounted(() => {
  if (gamers.value?.length >= 3) {
    loadTeamStats();
  }
});

// (Optional) expose `load` so a parent could call it manually
defineExpose({ load });
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

/* Team Identity Section */
.team-identity {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  margin: 1.5rem 0;
  padding: 1.5rem;
  background: var(--color-bg-elev);
  border-radius: 12px;
  border: 1px solid var(--color-border);
}

.team-cards-container {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 1rem;
}

.team-context {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
  justify-content: center;
}

.context-stat {
  font-size: 0.9rem;
  color: var(--color-text);
}

.context-separator {
  color: var(--color-text-muted);
  opacity: 0.5;
}

.win-rate-high { color: var(--color-success); font-weight: 600; }
.win-rate-medium { color: var(--color-warning, #f59e0b); }
.win-rate-low { color: var(--color-danger); }

/* Team Features Container */
.team-features-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  margin-top: 1.5rem;
}

.team-features-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
  align-items: stretch;
}

.team-features-grid > * {
  height: 100%;
}

.team-features-grid :deep(.chart-card) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.team-features-grid :deep(.chart-card > div:last-child) {
  flex: 1;
}

.team-features-grid-3 {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
  align-items: stretch;
}

.team-features-grid-3 > * {
  height: 100%;
}

.team-features-grid-3 :deep(.chart-card) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.team-features-grid-3 :deep(.chart-card > div:last-child) {
  flex: 1;
}

.team-features-grid-4 {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1.5rem;
  align-items: stretch;
}

.team-features-grid-4 > * {
  height: 100%;
}

.team-features-grid-4 :deep(.chart-card) {
  height: 100%;
  display: flex;
  flex-direction: column;
}

.team-features-grid-4 :deep(.chart-card > div:last-child) {
  flex: 1;
}

.team-performance-section,
.team-summary-section {
  width: 100%;
}

.team-performance-section :deep(.chart-card),
.team-summary-section :deep(.chart-card) {
  max-width: 100%;
}

/* Mobile responsiveness */
@media (max-width: 1400px) {
  .team-features-grid-4 {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 1200px) {
  .team-features-grid-3 {
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 900px) {
  .team-features-grid {
    grid-template-columns: 1fr;
  }
  .team-features-grid-3 {
    grid-template-columns: 1fr;
  }
  .team-features-grid-4 {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 600px) {
  .team-identity {
    padding: 1rem;
  }

  .team-cards-container {
    gap: 0.75rem;
  }

  .context-stat {
    font-size: 0.8rem;
  }
}

/* Compact performance chart when in grid */
.compact-performance :deep(.performance-content) {
  flex: 1;
  overflow-y: auto;
}

.compact-performance :deep(.player-row) {
  padding: 0.4rem 0;
}

.compact-performance :deep(.metric-bars) {
  gap: 0.2rem;
}
</style>
