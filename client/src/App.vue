<template>
  <div class="app-container">
    <RouterView />
    <VersionBadge v-if="!isInApp" />
    <!-- Global session expired modal -->
    <SessionExpiredModal />
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue';
import { RouterView, useRoute } from 'vue-router';
import VersionBadge from './components/VersionBadge.vue';
import SessionExpiredModal from './components/SessionExpiredModal.vue';
import { useAuthStore } from './stores/authStore';

const route = useRoute();
const authStore = useAuthStore();
const isInApp = computed(() => route.path.startsWith('/app'));

// Initialize the session expiry handler on app mount
onMounted(() => {
  authStore.initializeSessionHandler();
});
</script>

<style scoped>
.app-container {
  min-height: 100vh;
}
</style>
