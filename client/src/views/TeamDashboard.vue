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
      
      <!-- Gamers list -->
      <GamerCardsList v-else :gamers="gamers" />
    </div>
  </section>
  
</template>

<script setup>
import { useGamers } from '@/composables/useGamers.js';
import GamerCardsList from '@/components/GamerCardsList.vue';
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

const { loading, error, gamers, hasUser, load } = useGamers(() => ({
  userName: props.userName,
  userId: props.userId,
}));

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
</style>
