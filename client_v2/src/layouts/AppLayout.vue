<template>
  <div class="app-layout">
    <AppHeader />
    <main class="app-main">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import { onMounted, onUnmounted } from 'vue';
import AppHeader from '../components/AppHeader.vue';
import { useAuthStore } from '../stores/authStore';

const authStore = useAuthStore();

// Idle detection constants
const IDLE_THRESHOLD_MS = 30 * 60 * 1000; // 30 minutes
const LAST_ACTIVE_KEY = 'pulse_last_active_time';

/**
 * Update the last active timestamp in localStorage
 */
function updateLastActiveTime() {
  localStorage.setItem(LAST_ACTIVE_KEY, Date.now().toString());
}

/**
 * Get the last active timestamp from localStorage
 */
function getLastActiveTime() {
  const stored = localStorage.getItem(LAST_ACTIVE_KEY);
  return stored ? parseInt(stored, 10) : Date.now();
}

/**
 * Check if user has been idle for more than the threshold
 */
function hasBeenIdleTooLong() {
  const lastActive = getLastActiveTime();
  const idleDuration = Date.now() - lastActive;
  return idleDuration > IDLE_THRESHOLD_MS;
}

/**
 * Handle visibility change - check if user was idle and trigger sync check
 */
function handleVisibilityChange() {
  if (document.visibilityState === 'visible') {
    // User returned to the page
    if (hasBeenIdleTooLong() && authStore.isAuthenticated) {
      // Refresh user data which will trigger sync check on backend
      authStore.refreshUser();
    }
    // Update last active time
    updateLastActiveTime();
  }
}

// Throttle interval for activity tracking (30 seconds)
const ACTIVITY_THROTTLE_MS = 30 * 1000;
let lastActivityUpdate = 0;

/**
 * Handle user activity events with throttling to avoid excessive localStorage writes
 */
function handleUserActivity() {
  const now = Date.now();
  if (now - lastActivityUpdate >= ACTIVITY_THROTTLE_MS) {
    lastActivityUpdate = now;
    updateLastActiveTime();
  }
}

onMounted(() => {
  // Initialize last active time
  updateLastActiveTime();
  lastActivityUpdate = Date.now();

  // Listen for visibility changes
  document.addEventListener('visibilitychange', handleVisibilityChange);

  // Track user activity (throttled to avoid excessive localStorage writes)
  window.addEventListener('mousemove', handleUserActivity, { passive: true });
  window.addEventListener('keydown', handleUserActivity, { passive: true });
  window.addEventListener('click', handleUserActivity, { passive: true });
  window.addEventListener('scroll', handleUserActivity, { passive: true });
});

onUnmounted(() => {
  document.removeEventListener('visibilitychange', handleVisibilityChange);
  window.removeEventListener('mousemove', handleUserActivity);
  window.removeEventListener('keydown', handleUserActivity);
  window.removeEventListener('click', handleUserActivity);
  window.removeEventListener('scroll', handleUserActivity);
});
</script>

<style scoped>
.app-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-main {
  flex: 1;
  padding-top: 64px; /* Height of AppHeader */
}
</style>

