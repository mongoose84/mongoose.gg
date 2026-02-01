<template>
  <Transition name="slide-down">
    <div v-if="authStore.sessionExpired" class="session-expired-banner">
      <div class="flex items-center justify-center gap-4 max-w-[1400px] mx-auto px-6 py-3">
        <!-- Lock icon -->
        <LockClosedIcon class="w-5 h-5 text-primary flex-shrink-0" />

        <!-- Message -->
        <span class="text-sm font-medium text-white">
          Your session has expired. Please log in again to continue.
        </span>

        <!-- Login button -->
        <BaseButton
          variant="primary"
          size="sm"
          @click="goToLogin"
        >
          Log In
        </BaseButton>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import { LockClosedIcon } from '@heroicons/vue/24/solid'
import BaseButton from './base/BaseButton.vue'

const router = useRouter()
const authStore = useAuthStore()

function goToLogin() {
  // Preserve current route for redirect after login
  const currentPath = router.currentRoute.value.fullPath
  authStore.clearSessionExpired()
  router.push(`/auth?mode=login&redirect=${encodeURIComponent(currentPath)}`)
}
</script>

<style scoped>
.session-expired-banner {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 400; /* Toast level from z-index scale */
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
  backdrop-filter: blur(10px);
}

/* Slide down transition */
.slide-down-enter-active,
.slide-down-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.slide-down-enter-from,
.slide-down-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}

.slide-down-enter-to,
.slide-down-leave-from {
  transform: translateY(0);
  opacity: 1;
}

/* Responsive adjustments */
@media (max-width: 640px) {
  .session-expired-banner > div {
    flex-wrap: wrap;
    gap: 0.75rem;
  }

  .session-expired-banner span {
    flex: 1 1 100%;
    text-align: center;
    order: -1;
  }
}
</style>

