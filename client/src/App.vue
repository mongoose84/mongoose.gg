<template>
  <div class="app-container">
    <RouterView />
    <VersionBadge v-if="!isInApp" />
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { RouterView, useRoute } from 'vue-router';
import VersionBadge from './components/VersionBadge.vue';

const route = useRoute();
const isInApp = computed(() => route.path.startsWith('/app'));
</script>

<style scoped>
.app-container {
  min-height: 100vh;
}
</style>
