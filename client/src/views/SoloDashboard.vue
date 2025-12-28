<template>
  <section class="userview">
    <!-- Smaller brand header -->
    <header class="brand brand--compact" aria-labelledby="app-title-user">
      <router-link to="/" class="brand-link">
        <div class="brand-inner">
          <span class="logo compact" aria-hidden="true">
            <AppLogo :size="56" />
          </span>
          <div class="titles">
            <h1 id="app-title-user" class="title compact">{{ appTitle }}</h1>
            <p class="subtitle compact">{{ appSubtitle }}</p>
          </div>
        </div>
      </router-link>
    </header>

    <div class="user-container">
      <h2 v-if="hasUser">{{ userName }}</h2>
      <h2 v-else>Missing user details</h2>

      <div v-if="!hasUser">Please navigate via the Users list.</div>
      <div v-else-if="loading">Loading…</div>
      <div v-else-if="error">{{ error }}</div>
      
      <!-- Gamers list -->
      <GamerCardsList v-else :gamers="gamers">
        <template #after-cards>
          <div class="comparison-strip-wrap">
            <ComparisonStrip :userId="userId" />
          </div>

          <PerformanceCharts :userId="userId" />

          <!-- Radar Chart and Champion Performance Section -->
          <div class="radar-champion-section">
            <div class="radar-wrapper">
              <RadarChart :userId="userId" />
            </div>
            <div class="champion-wrapper">
              <ChampionPerformanceSplit :userId="userId" />
            </div>
          </div>

          <!-- New Row: Role Distribution, Death Efficiency, and Match Duration -->
          <div class="new-cards-section">
            <RoleDistribution :userId="userId" />
            <DeathEfficiency :userId="userId" />
            <MatchDuration :userId="userId" />
          </div>

          <!-- Summary Insights Panel -->
          <SummaryInsights :userId="userId" />
        </template>
      </GamerCardsList>
    </div>
  </section>

</template>

<script setup>
import { useGamers } from '@/composables/useGamers.js';
import GamerCardsList from '@/components/GamerCardsList.vue';
import PerformanceCharts from '@/components/PerformanceCharts.vue';
import ComparisonStrip from './ComparisonStrip.vue';
import RadarChart from '@/components/RadarChart.vue';
import ChampionPerformanceSplit from '@/components/ChampionPerformanceSplit.vue';
import RoleDistribution from '@/components/RoleDistribution.vue';
import DeathEfficiency from '@/components/DeathEfficiency.vue';
import MatchDuration from '@/components/MatchDuration.vue';
import SummaryInsights from '@/components/SummaryInsights.vue';
import AppLogo from '@/components/AppLogo.vue';

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

// App branding - could be moved to a config file
const appTitle = 'Do End';
const appSubtitle = 'Cross Account LoL Statistics';

const { loading, error, gamers, hasUser, load } = useGamers(() => ({
  userName: props.userName,
  userId: props.userId,
}));

// (Optional) expose `load` so a parent could call it manually
defineExpose({ load });
</script>

<style scoped>
.userview {
  width: 100%;
  max-width: 100vw; /* Prevent horizontal overflow */
  overflow-x: hidden; /* Prevent horizontal scroll */
  /* full width; keep slight page padding */
  margin: 2rem 0;
  padding: 0 1rem;
  box-sizing: border-box;
}

.brand--compact {
  width: 100%;
  /* keep header left-aligned, avoid auto-centering */
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
  justify-content: flex-start; /* left-align logo and titles */
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

.user-container {
  max-width: 100%;
  overflow-x: hidden; /* Prevent child overflow */
}

.user-container h2 { text-align: left; margin-left: 1rem; }

.comparison-strip-wrap {
  margin-top: 1.2rem;
  /* Remove any side margins to align with cards */
  margin-left: 0;
  margin-right: 0;
}

.radar-champion-section {
  margin-top: 1.2rem;
  display: flex;
  gap: 1.2rem;
  width: 100%;
  align-items: flex-start;
}

.radar-wrapper,
.champion-wrapper {
  flex: 1;
  min-width: 0; /* Allow flex items to shrink below content size */
}

/* New cards section - three cards per row */
.new-cards-section {
  margin-top: 1.2rem;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.2rem;
  width: 100%;
  align-items: stretch;
}

/* Ensure all grid items have the same height */
.new-cards-section > * {
  height: 100%;
}

/* Responsive: stack vertically on smaller screens */
@media (max-width: 1200px) {
  .radar-champion-section {
    flex-direction: column;
  }

  .radar-wrapper,
  .champion-wrapper {
    width: 100%;
  }

  .new-cards-section {
    grid-template-columns: 1fr;
  }
}

/* Medium screens: 2 cards per row */
@media (min-width: 768px) and (max-width: 1200px) {
  .new-cards-section {
    grid-template-columns: repeat(2, 1fr);
  }
}
</style>