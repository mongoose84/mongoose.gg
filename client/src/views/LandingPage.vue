<template>
  <div class="min-h-screen bg-background text-text font-body tabular-nums">
    <NavBar />

    <main class="pt-14 desk:pt-20">
      <div class="max-w-[1440px] mx-auto">
        <LandingHero :proof-count="proofCount" />
        <LandingFeatures v-reveal-on-view />
        <LandingChampionPool v-reveal-on-view />
        <LandingSteps v-reveal-on-view />
        <LandingCallToAction v-reveal-on-view />
      </div>
    </main>

    <LandingFooter />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import NavBar from '../components/NavBar.vue';
import LandingHero from '../components/landing/LandingHero.vue';
import LandingFeatures from '../components/landing/LandingFeatures.vue';
import LandingChampionPool from '../components/landing/LandingChampionPool.vue';
import LandingSteps from '../components/landing/LandingSteps.vue';
import LandingCallToAction from '../components/landing/LandingCallToAction.vue';
import LandingFooter from '../components/landing/LandingFooter.vue';
import { vRevealOnView } from '../composables/useRevealOnView';
import { useAsyncData } from '../composables/useAsyncData';
import { getPublicStats } from '../services/publicApi';
import { formatProofCount } from '../utils/formatters';

/**
 * Proof sits next to the main button only once the real count helps
 * (design system: "when the count is small, leave it out").
 */
const PROOF_MIN_MATCHES = 10_000;

const totalMatches = ref(null);
const proofCount = computed(() => {
  if (typeof totalMatches.value !== 'number' || totalMatches.value < PROOF_MIN_MATCHES) {
    return '';
  }
  return formatProofCount(totalMatches.value);
});

const { execute: executePublicStatsFetch } = useAsyncData(async () => {
  return await getPublicStats();
}, { immediate: false, errorMessage: 'Failed to load public stats' });

onMounted(async () => {
  try {
    const stats = await executePublicStatsFetch();
    if (stats && typeof stats.totalMatches === 'number') {
      totalMatches.value = stats.totalMatches;
    }
  } catch (error) {
    // Proof is optional: the page reads the same without it
    console.error('Failed to load public stats', error);
  }
});
</script>
