<template>
  <section id="features" class="scroll-mt-14 desk:scroll-mt-20 px-4 desk:px-14 pb-16 desk:pb-28 flex flex-col gap-5 desk:gap-10" data-testid="landing-features">
    <div class="flex flex-col desk:flex-row desk:items-end desk:justify-between gap-3 desk:gap-10">
      <div class="flex flex-col gap-2.5 desk:gap-3 max-w-[40rem]">
        <span class="text-eyebrow text-text-secondary">WHAT YOU GET</span>
        <h2 class="font-display font-semibold text-[1.875rem] leading-[1.15] desk:text-headline text-text">Everything comes from your own matches</h2>
      </div>
      <p class="hidden desk:block max-w-[25rem] text-body-lg text-text-soft">
        Not the meta, not someone else's build. Each part of the app answers one question about how you play.
      </p>
    </div>

    <div class="grid grid-cols-1 desk:grid-cols-12 gap-5">
      <!-- Overview -->
      <LandingFeatureCard
        class="desk:col-span-7"
        icon="house"
        page="Overview"
        title="Know when to queue again"
        description="A readiness score from your last matches and how long you have played today, next to your three scores."
      >
        <div class="grid grid-cols-1 desk:grid-cols-[minmax(0,21.25rem)_minmax(0,1fr)] gap-5">
          <div class="p-5 desk:p-6 rounded-xl bg-background-highlight border border-border-highlight flex flex-col gap-3.5">
            <span class="text-eyebrow text-text-secondary">QUEUE READINESS</span>
            <div class="flex items-baseline gap-3">
              <span class="font-display text-[2.75rem] desk:text-score-xl text-text">{{ readiness.value }}</span>
              <span class="text-body font-bold text-positive">{{ readiness.verdict }}</span>
            </div>
            <div
              class="grid grid-cols-[repeat(20,minmax(0,1fr))] gap-[3px]"
              role="meter"
              aria-label="Queue readiness"
              aria-valuemin="0"
              aria-valuemax="100"
              :aria-valuenow="readiness.value"
              :aria-valuetext="`${readiness.value} out of 100, ${readiness.verdict.toLowerCase()}`"
              data-testid="readiness-meter"
            >
              <span
                v-for="(on, i) in readinessSegments"
                :key="i"
                class="h-[1.125rem] rounded-[3px]"
                :class="on ? 'bg-primary-accent' : 'bg-track-strong'"
              />
            </div>
            <span class="hidden desk:block text-caption text-text-soft">{{ readiness.advice }}</span>
          </div>
          <ul class="hidden desk:flex flex-col">
            <li
              v-for="(score, i) in example.scores"
              :key="score.label"
              class="flex flex-col gap-2 py-3.5"
              :class="{ 'border-b border-border': i < example.scores.length - 1 }"
            >
              <div class="flex items-baseline gap-2">
                <span class="flex-grow text-body-sm font-medium text-text-soft">{{ score.label }}</span>
                <span class="font-display text-title font-bold text-text">{{ score.value }}</span>
                <span class="w-11 text-right text-chip" :class="score.delta >= 0 ? 'text-positive' : 'text-warn-text'">
                  {{ score.delta >= 0 ? '▲' : '▼' }} {{ Math.abs(score.delta) }}
                </span>
              </div>
              <div class="h-1.5 rounded-[2px] bg-track">
                <div class="h-1.5 rounded-[2px]" :class="score.value >= 70 ? 'bg-primary-accent' : 'bg-warn'" :style="{ width: `${score.value}%` }" />
              </div>
            </li>
          </ul>
        </div>
      </LandingFeatureCard>

      <!-- Champion Select -->
      <LandingFeatureCard
        class="desk:col-span-5"
        icon="shield"
        page="Champion Select"
        title="One pick into your opponent"
        description="Tell us who you are facing. We pick the champion from your own pool that wins that lane most."
      >
        <div class="flex flex-col gap-3">
          <div class="hidden desk:flex items-center gap-3 text-caption text-text-secondary">
            <img :src="iconUrl(pick.opponent)" alt="" class="w-8 h-8 rounded-pill" />
            Facing {{ pick.opponent }} mid
          </div>
          <div class="p-3.5 desk:p-[1.125rem] rounded-card-sm bg-background border border-border flex items-center gap-3 desk:gap-4">
            <img :src="iconUrl(pick.champion)" alt="" class="w-12 h-12 desk:w-14 desk:h-14 rounded-[12px] border-2 border-primary-accent" />
            <div class="flex flex-col gap-1 flex-grow min-w-0">
              <span class="font-display font-semibold text-[1.0625rem] desk:text-title text-text">Pick {{ pick.champion }}</span>
              <span class="text-caption text-text-soft">You won {{ pick.wins }} of {{ pick.matches }} into {{ pick.opponent }}</span>
            </div>
            <div class="flex flex-col items-end gap-0.5">
              <span class="font-display text-stat text-positive">{{ pick.winRate }}%</span>
              <span class="hidden desk:block text-chip text-text-secondary">High confidence</span>
            </div>
          </div>
          <ul class="hidden desk:grid grid-cols-2 gap-3">
            <li v-for="alt in pick.alternatives" :key="alt.name" class="py-3 px-3.5 rounded-[1rem] bg-background flex items-center gap-2.5">
              <img :src="iconUrl(alt.name)" alt="" class="w-9 h-9 rounded-[10px]" />
              <span class="flex-grow text-body-sm font-bold text-text">{{ alt.name }}</span>
              <span class="font-display text-body font-bold" :class="alt.winRate >= 50 ? 'text-text-soft' : 'text-warn-text'">{{ alt.winRate }}%</span>
            </li>
          </ul>
        </div>
      </LandingFeatureCard>

      <!-- Matches -->
      <LandingFeatureCard
        class="desk:col-span-5"
        icon="swords"
        page="Matches"
        title="One fix after every match"
        description="Every match broken down, with the one thing that would have changed it most."
      >
        <ul class="rounded-card-sm bg-background flex flex-col">
          <li
            v-for="(match, i) in example.matches"
            :key="match.champion"
            class="flex-col gap-3 py-3.5 px-3.5 desk:px-[1.125rem]"
            :class="[i < example.matches.length - 1 ? 'hidden desk:flex border-b border-border' : 'flex']"
          >
            <div class="grid grid-cols-[2.75rem_minmax(0,1fr)_auto] desk:grid-cols-[3rem_minmax(0,1fr)_auto] gap-3 desk:gap-3.5 items-center">
              <img
                :src="iconUrl(match.champion)"
                alt=""
                class="w-11 h-11 desk:w-12 desk:h-12 rounded-pill border-2"
                :class="match.win ? 'border-primary-accent' : 'border-warn'"
              />
              <span class="flex flex-col gap-0.5 min-w-0">
                <span class="text-body desk:text-base font-bold text-text">
                  {{ match.champion }}
                  <span class="text-caption desk:text-body-sm font-bold" :class="match.win ? 'text-positive' : 'text-warn-text'">{{ match.win ? 'Victory' : 'Defeat' }}</span>
                </span>
                <span class="text-caption text-text-secondary">{{ match.meta }}</span>
              </span>
              <span class="font-display font-semibold text-body desk:text-base" :class="match.win ? 'text-positive' : 'text-warn-text'">{{ formatSignedLp(match.lp) }}</span>
            </div>
            <p v-if="match.fix" class="py-3.5 px-4 rounded-[1rem] bg-background-surface flex gap-3 text-body-sm text-text-soft">
              <BaseIcon name="skull" :size="20" class="text-warn-text" />
              <span><b class="text-text">{{ match.fixLead }}</b> {{ match.fix }}</span>
            </p>
          </li>
        </ul>
      </LandingFeatureCard>

      <!-- Solo -->
      <LandingFeatureCard
        class="desk:col-span-7"
        icon="chart-line"
        page="Solo"
        title="See your trends, match by match"
        description="Win rate, deaths and gold at 15 over your last 20 or 50 matches, or the whole season."
      >
        <div class="p-4 desk:py-[1.375rem] desk:px-6 rounded-card-sm bg-background flex flex-col gap-4">
          <div class="flex flex-col desk:flex-row desk:justify-between desk:items-center gap-3 desk:gap-4">
            <div class="flex flex-col gap-0.5">
              <span class="font-display font-semibold text-base desk:text-lg text-text" data-testid="trend-title">{{ trendTitle }}</span>
              <span class="text-caption text-text-secondary">Deaths per match</span>
            </div>
            <div class="self-start desk:self-auto inline-flex gap-1 p-1 rounded-pill bg-background-elevated" role="group" aria-label="Range">
              <button
                v-for="range in ranges"
                :key="range.id"
                type="button"
                class="range-button mp-focusable"
                :aria-pressed="selectedRange === range.id"
                :data-testid="`trend-range-${range.id}`"
                @click="selectedRange = range.id"
              >{{ range.label }}</button>
            </div>
          </div>
          <svg viewBox="0 0 660 150" class="w-full h-auto" role="img" :aria-label="trendAriaLabel">
            <line v-for="y in [20, 70, 120]" :key="y" x1="0" :y1="y" x2="660" :y2="y" class="trend-grid" />
            <text x="0" y="14" class="trend-axis">6.5</text>
            <text x="0" y="64" class="trend-axis">5.5</text>
            <text x="0" y="114" class="trend-axis">4.5</text>
            <polyline :points="trendPoints" class="trend-line" />
            <circle :cx="trendEnd[0]" :cy="trendEnd[1]" r="6" class="trend-end" />
            <text x="40" y="144" class="trend-axis">{{ activeRange.start }}</text>
            <text x="660" y="144" text-anchor="end" class="trend-axis">Last match</text>
          </svg>
        </div>
      </LandingFeatureCard>
    </div>
  </section>
</template>

<script setup>
import { ref, computed } from 'vue'
import { BaseIcon } from '@/components/base'
import { getChampionIconUrl } from '@/utils/leagueAssets'
import LandingFeatureCard from './LandingFeatureCard.vue'
import { exampleAccount as example, formatSignedLp } from './exampleAccount'

const readiness = example.readiness
const pick = example.pick
const iconUrl = getChampionIconUrl

// 20 segments, round(value / 5) switched on
const readinessSegments = Array.from({ length: 20 }, (_, i) => i < Math.round(readiness.value / 5))

// Example trend per range; ranges count matches, never days
const ranges = [
  { id: 'last20', label: 'Last 20', start: '20 matches ago', values: example.deathsTrend },
  { id: 'last50', label: 'Last 50', start: '50 matches ago', values: [6.6, 6.8, 6.4, 6.5, 6.3, 6.4, 6.1, 6.2, 6.0, 6.1, 5.9, 6.0, 5.8, 5.9, 5.7, 5.6, 5.8, 5.5, 5.4, 5.2] },
  { id: 'season', label: 'Season', start: 'Season start', values: [6.4, 6.3, 6.5, 6.2, 6.1, 6.2, 6.0, 5.9, 6.0, 5.8, 5.7, 5.8, 5.6, 5.5, 5.6, 5.4, 5.3, 5.4, 5.3, 5.2] }
]
const selectedRange = ref('last20')
const activeRange = computed(() => ranges.find((r) => r.id === selectedRange.value))

const trendCoords = computed(() => {
  const values = activeRange.value.values
  return values.map((v, i) => [40 + (i * 600) / (values.length - 1), 20 + (6.5 - v) * 50])
})
const trendPoints = computed(() => trendCoords.value.map(([x, y]) => `${x.toFixed(1)},${y.toFixed(1)}`).join(' '))
const trendEnd = computed(() => trendCoords.value[trendCoords.value.length - 1])

const trendTitle = computed(() => {
  const values = activeRange.value.values
  return `Deaths down from ${values[0].toFixed(1)} to ${values[values.length - 1].toFixed(1)}`
})
const trendAriaLabel = computed(() => {
  const range = activeRange.value
  return `Deaths per match, ${range.label.toLowerCase()}, from ${range.values[0].toFixed(1)} to ${range.values[range.values.length - 1].toFixed(1)}`
})
</script>

<style scoped>
.range-button {
  height: 2.25rem;
  padding: 0 0.875rem;
  border: 0;
  border-radius: 999px;
  background: transparent;
  color: var(--color-text-secondary);
  font-family: var(--font-body);
  font-size: 0.75rem;
  font-weight: 500;
  cursor: pointer;
}

.range-button[aria-pressed='true'] {
  background: var(--color-surface-selected);
  color: var(--color-text);
  font-weight: 700;
}

.trend-grid {
  stroke: var(--color-border);
}

.trend-axis {
  fill: var(--color-ink-faint);
  font-family: var(--font-body);
  font-size: 12px;
}

.trend-line {
  fill: none;
  stroke: var(--color-primary-accent);
  stroke-width: 3;
  stroke-linejoin: round;
  stroke-linecap: round;
}

.trend-end {
  fill: var(--color-primary-accent);
  stroke: var(--color-bg);
  stroke-width: 3;
}
</style>
