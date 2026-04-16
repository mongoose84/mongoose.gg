<template>
  <AnalysisLayout page-title="Solo Dashboard" data-testid="solo-dashboard">
    <!-- Zone 1: Context Bar -->
    <template #context-bar>
      <!-- Queue Toggle Bar (centered) -->
      <BaseQueueToggle v-model="queueFilter" />

      <!-- Time Range Filter (positioned right) -->
      <div class="absolute right-0">
        <BaseTimeRangeSelect v-model="timeRange" />
      </div>
    </template>

    <!-- Zone 2: Summary Stats -->
    <template #summary>
      <SummaryStatsCard
        :games-played="dashboardData?.gamesPlayed ?? 0"
        :win-rate="dashboardData?.winRate ?? null"
        :overall-win-rate="dashboardData?.overallWinRate ?? null"
        :avg-kda="dashboardData?.avgKda ?? null"
        :avg-kills="dashboardData?.avgKills ?? null"
        :avg-deaths="dashboardData?.avgDeaths ?? null"
        :avg-assists="dashboardData?.avgAssists ?? null"
        :overall-avg-kills="dashboardData?.overallAvgKills ?? null"
        :overall-avg-deaths="dashboardData?.overallAvgDeaths ?? null"
        :overall-avg-assists="dashboardData?.overallAvgAssists ?? null"
        :overall-avg-kda="dashboardData?.overallAvgKda ?? null"
        :solo-duo-rank="dashboardData?.rankInfo?.soloDuoRank ?? null"
        :flex-rank="dashboardData?.rankInfo?.flexRank ?? null"
        :queue-filter="queueFilter"
        :loading="isLoading"
        :account-count="summaryAccountCount"
        :ranks="dashboardData?.allAccountRanks ?? null"
      />
    </template>

    <!-- Zone 3: Trend Charts -->
    <template #trend-charts>
      <!-- Winrate Trend Chart — "How am I doing overall?" -->
      <TrendChartCard
        title="Winrate Over Time"
        subtitle="Rolling 20-game average"
        :loading="winrateLoading"
        test-id="winrate-trend-card"
        @toggle-expand="handleWinrateExpand"
      >
        <template #default>
          <WinrateChart
            :data="winrateTrendData"
            :overall-win-rate="dashboardData?.overallWinRate ?? null"
            :chart-mode="chartMode"
            :accounts="chartAccounts"
          />
        </template>
      </TrendChartCard>

      <!-- Deaths Over Time Chart — "What's the #1 thing I can fix?" -->
      <TrendChartCard
        title="Deaths Over Time"
        subtitle="Most actionable metric for improvement"
        :loading="deathsLoading"
        test-id="deaths-trend-card"
        @toggle-expand="handleDeathsExpand"
      >
        <template #default>
          <DeathsChart
            :data="deathsTrendData"
            :overall-average="deathsSummary.overallAverage"
            :trend="deathsSummary.trend"
            :chart-mode="chartMode"
            :accounts="chartAccounts"
          />
        </template>
      </TrendChartCard>

      <!-- Dragon Participation Trend Chart — "Am I showing up for objectives?" -->
      <TrendChartCard
        title="Dragon Participation"
        subtitle="First Dragon = 70.69% win rate correlation"
        :loading="dragonParticipationLoading"
        test-id="dragon-participation-trend-card"
        @toggle-expand="handleDragonParticipationExpand"
      >
        <template #default>
          <DragonParticipationChart
            :data="dragonParticipationTrendData"
            :overall-average="dragonParticipationSummary.overallAverage"
            :trend="dragonParticipationSummary.trend"
            :chart-mode="chartMode"
            :accounts="chartAccounts"
          />
        </template>
      </TrendChartCard>

      <!-- Vision Score Trend Chart — "Am I giving myself information?" -->
      <TrendChartCard
        title="Vision Score Over Time"
        subtitle="Key metric for map awareness and objective control"
        :loading="visionScoreLoading"
        test-id="vision-score-trend-card"
        @toggle-expand="handleVisionScoreExpand"
      >
        <template #default>
          <VisionChart
            :data="visionScoreTrendData"
            :overall-average="visionScoreSummary.overallAverage"
            :role-target="visionScoreSummary.roleTarget"
            :trend="visionScoreSummary.trend"
            :chart-mode="chartMode"
            :accounts="chartAccounts"
          />
        </template>
      </TrendChartCard>

      <!-- Gold at 15 Trend Chart — "Am I winning my lane?" -->
      <TrendChartCard
        title="Gold at 15 Minutes"
        subtitle="Most predictive metric for winning"
        :loading="goldAt15Loading"
        test-id="gold-at-15-trend-card"
        @toggle-expand="handleGoldAt15Expand"
      >
        <template #default>
          <GoldAt15Chart
            :data="goldAt15TrendData"
            :chart-mode="chartMode"
            :accounts="chartAccounts"
          />
        </template>
      </TrendChartCard>

      <!-- CS Per Minute Trend Chart — "Am I farming efficiently?" -->
      <TrendChartCard
        title="CS Per Minute"
        subtitle="Farming efficiency over time"
        :loading="csPerMinuteLoading"
        test-id="cs-per-minute-trend-card"
        @toggle-expand="handleCsPerMinuteExpand"
      >
        <template #default>
          <CsPerMinuteChart
            :data="csPerMinuteTrendData"
            :chart-mode="chartMode"
            :accounts="chartAccounts"
          />
        </template>
      </TrendChartCard>
    </template>

    <!-- Zone 4: Deep Analysis -->
    <template #deep-analysis>
      <div class="deep-analysis-grid" data-testid="deep-analysis-grid">
        <BaseCard
          title="Performance Profile"
          subtitle="Your strengths and weaknesses across 6 dimensions"
          data-testid="radar-chart-card"
        >
          <RadarChart
            :axes="radarChartData?.axes ?? []"
            :games-analyzed="radarChartData?.gamesAnalyzed ?? 0"
            :loading="radarChartLoading"
          />
        </BaseCard>

        <BaseCard title="Danger Zones" subtitle="Where you die most on the map" data-testid="danger-zones-card">
          <DangerZonesMap
            :deaths="deathPositionsData?.deaths ?? []"
            :total-deaths="deathPositionsData?.totalDeaths ?? 0"
            :matches-analyzed="deathPositionsData?.matchesAnalyzed ?? 0"
            :phase-summary="deathPositionsData?.phaseSummary ?? { early: 0, mid: 0, late: 0, veryLate: 0 }"
            :loading="deathPositionsLoading"
            :error="deathPositionsError"
            :queue-type="queueFilter"
            :time-range="timeRange"
            @update:side="onSideFilterChange"
          />
        </BaseCard>

        <BaseCard title="Match Activity" data-testid="match-activity-card">
          <MatchActivityHeatmap
            v-if="matchActivityData"
            :daily-match-counts="matchActivityData.dailyMatchCounts"
            :start-date="matchActivityData.startDate"
            :end-date="matchActivityData.endDate"
            :total-matches="matchActivityData.totalMatches"
          />
          <div
            v-else-if="matchActivityLoading"
            class="empty-state"
            data-testid="match-activity-loading-state"
            aria-live="polite"
          >
            Loading match activity...
          </div>
          <div v-else class="empty-state">No match activity data</div>
        </BaseCard>
      </div>
    </template>

    <!-- Zone 5: Not rendered in v1 -->
  </AnalysisLayout>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSoloDashboardData } from '../composables/useSoloDashboardData'
import { useChartDisplayMode } from '../composables/useChartDisplayMode'
import { ACCOUNT_COLORS } from '../utils/chartConfigs.js'
import { BaseQueueToggle, BaseTimeRangeSelect, BaseCard } from '../components/base'
import AnalysisLayout from '../components/shared/AnalysisLayout.vue'
import SummaryStatsCard from '../components/solo/SummaryStatsCard.vue'
import TrendChartCard from '../components/solo/TrendChartCard.vue'
import WinrateChart from '../components/solo/WinrateChart.vue'
import GoldAt15Chart from '../components/solo/GoldAt15Chart.vue'
import CsPerMinuteChart from '../components/solo/CsPerMinuteChart.vue'
import DeathsChart from '../components/solo/DeathsChart.vue'
import DragonParticipationChart from '../components/solo/DragonParticipationChart.vue'
import VisionChart from '../components/solo/VisionChart.vue'
import DangerZonesMap from '../components/solo/DangerZonesMap.vue'
import RadarChart from '../components/solo/RadarChart.vue'
import MatchActivityHeatmap from '../components/overview/MatchActivityHeatmap.vue'

const authStore = useAuthStore()
const { chartMode } = useChartDisplayMode()

const {
  queueFilter,
  timeRange,
  dashboardData,
  isLoading,
  error,
  winrateTrendData,
  winrateLoading,
  goldAt15TrendData,
  goldAt15Loading,
  csPerMinuteTrendData,
  csPerMinuteLoading,
  deathsTrendData,
  deathsLoading,
  deathsSummary,
  dragonParticipationTrendData,
  dragonParticipationLoading,
  dragonParticipationSummary,
  visionScoreTrendData,
  visionScoreLoading,
  visionScoreSummary,
  radarChartData,
  radarChartLoading,
  deathPositionsData,
  deathPositionsLoading,
  deathPositionsError,
  matchActivityData,
  matchActivityLoading,
  handleWinrateExpand,
  handleGoldAt15Expand,
  handleCsPerMinuteExpand,
  handleDeathsExpand,
  handleDragonParticipationExpand,
  handleVisionScoreExpand,
  onSideFilterChange,
  fetchAllData
} = useSoloDashboardData()

// Derived account list for per-account chart mode
const chartAccounts = computed(() =>
  authStore.riotAccounts.map((account, index) => ({
    gameName: `${account.gameName}#${account.tagLine}`,
    color: ACCOUNT_COLORS[index % ACCOUNT_COLORS.length]
  }))
)

// Account count for SummaryStatsCard label — sourced from the API response so it
// stays in sync with the server's visibility/tier logic, not the client-side store filter.
const summaryAccountCount = computed(() => dashboardData.value?.accountCount ?? 1)

onMounted(() => { fetchAllData() })

</script>

<style scoped>
.deep-analysis-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  grid-template-rows: auto auto;
  gap: var(--spacing-lg);
}

/* Performance Profile: top-left */
.deep-analysis-grid > :nth-child(1) {
  grid-column: 1;
  grid-row: 1;
}

/* Danger Zones: spans full right column */
.deep-analysis-grid > :nth-child(2) {
  grid-column: 2;
  grid-row: 1 / -1;
}

/* Match Activity: bottom-left, sizes to content */
.deep-analysis-grid > :nth-child(3) {
  grid-column: 1;
  grid-row: 2;
  align-self: start;
}

@media (max-width: 768px) {
  .deep-analysis-grid {
    grid-template-columns: 1fr;
    grid-template-rows: auto;
  }

  /* On mobile, reset all explicit placement so items stack in order:
     Performance Profile → Match Activity → Danger Zones */
  .deep-analysis-grid > :nth-child(1),
  .deep-analysis-grid > :nth-child(2),
  .deep-analysis-grid > :nth-child(3) {
    grid-column: 1;
    grid-row: auto;
  }

  /* Danger Zones last on mobile */
  .deep-analysis-grid > :nth-child(2) {
    order: 3;
  }

  .deep-analysis-grid > :nth-child(3) {
    order: 2;
  }
}
</style>


