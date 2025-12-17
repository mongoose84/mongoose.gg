<template>
  <section class="userview">
    <!-- Smaller brand header -->
    <header class="brand brand--compact" aria-labelledby="app-title-user">
      <div class="brand-inner">
        <span class="logo compact" aria-hidden="true">
          <AppLogo />
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
  max-width: 800px;
  margin: 4rem auto;
}

.brand--compact {
  width: 100%;
  margin: 0 auto 0.5rem;
}

.brand-inner {
  display: flex;
  align-items: center;
  justify-content: flex-start; /* move logo and title to the left */
  gap: 0.75rem;
}

.logo.compact {
  display: inline-flex;
  width: 48px; /* smaller than HomeView (64px) */
  height: 48px;
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

.gamers-list {
  margin-top: 1rem;
}

.gamers-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 0.75rem;
}

.gamer-cards {
  display: flex;
  flex-wrap: wrap;
  justify-content: center; /* center cards in the middle */
  gap: 7rem;
  margin: 0;
  padding: 0;
}
</style>