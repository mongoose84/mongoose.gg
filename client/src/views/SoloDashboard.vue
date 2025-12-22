<template>
  <section class="userview">
    <!-- Smaller brand header -->
    <header class="brand brand--compact" aria-labelledby="app-title-user">
      <div class="brand-inner">
        <span class="logo compact" aria-hidden="true">
          <AppLogo :size="56" />
        </span>
        <div class="titles">
          <h1 id="app-title-user" class="title compact">Do End</h1>
          <p class="subtitle compact">Cross Account LoL Statistics</p>
        </div>
      </div>
    </header>

    <div class="user-container">
      <h2 v-if="hasUser">{{ userName }}</h2>
      <h2 v-else>Missing user details</h2>

      <div v-if="!hasUser">Please navigate via the Users list.</div>
      <div v-else-if="loading">Loading…</div>
      <div v-else-if="error">{{ error }}</div>
      
      <!-- Gamers list -->
      <section v-else class="gamers-list">
        <template v-if="gamers && gamers.length">
          <div class="gamer-cards">
            <GamerCard v-for="g in gamers" :key="g.puuid || g.iconUrl" :gamer="g" />
          </div>
          <div class="comparison-strip-wrap">
            <ComparisonStrip :userId="userId" />
          </div>
        </template>
        <div v-else class="empty-state">No linked gamers yet.</div>
      </section>
    </div>
  </section>
  
</template>

<script setup>
import { onMounted, watch, computed, ref } from 'vue';
import getGamers from '@/assets/getGamers.js';
import GamerCard from './GamerCard.vue';
import ComparisonStrip from './ComparisonStrip.vue';
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

const loading = ref(false);
const error = ref(null);
const gamers = ref([]);

const hasUser = computed(() => !!props.userName && props.userId !== undefined && props.userId !== '' );

async function load() {
  if (!hasUser.value) return;
  loading.value = true;
  error.value = null;
  try {
    const list = await getGamers(props.userId);
    gamers.value = Array.isArray(list) ? list : [];
  } catch (e) {
    error.value = e?.message || 'Failed to load gamers.';
    gamers.value = [];
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  load();
});

watch(
  () => [props.userName, props.userId],
  () => {
    load();
  }
);

// (Optional) expose `load` so a parent could call it manually
defineExpose({ load });
</script>

<style scoped>
.userview {
  width: 100%;
  /* full width; keep slight page padding */
  margin: 2rem 0;
  padding: 0 1rem;
}

.brand--compact {
  width: 100%;
  /* keep header left-aligned, avoid auto-centering */
  margin: 0 0 0.5rem 0;
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

.user-container h2 { text-align: left; margin-left: 1rem; }

.gamers-list {
  /* center the cards section on the page */
  /* Limit to 5 columns worth of width, center horizontally */
  --card-width: 260px;
  --card-gap: 1.2rem; /* keep existing visual spacing */
  --max-cols: 5;
  max-width: calc(var(--max-cols) * var(--card-width) + (var(--max-cols) - 1) * var(--card-gap));
  margin: 0.25rem auto 0;
}

.gamers-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.gamer-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(var(--card-width), var(--card-width)));
  justify-content: center;
  gap: var(--card-gap);
  margin: 0;
  padding: 0;
}

.comparison-strip-wrap {
  /* Full width within the same centered container as the grid */
  margin-top: var(--card-gap);
}
</style>