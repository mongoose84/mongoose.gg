<template>
  <div class="min-h-screen p-2xl">
    <div class="max-w-[800px] mx-auto">
      <div class="mb-2xl">
        <h1 class="text-2xl font-bold text-text tracking-tight">User Settings</h1>
      </div>

      <div class="flex flex-col gap-2xl">
        <!-- Account Section -->
        <div class="flex flex-col gap-md">
          <h2 class="text-lg font-semibold text-text tracking-tight">Account</h2>
          <div class="bg-background-surface border border-border rounded-lg p-xl">
            <div class="py-md border-b border-border">
              <div class="flex justify-between items-center">
                <span class="text-sm font-medium text-text-secondary">Username</span>
                <span class="text-sm text-text">{{ username }}</span>
              </div>
            </div>
            <div class="py-md border-b border-border">
              <div class="flex justify-between items-center">
                <span class="text-sm font-medium text-text-secondary">Email</span>
                <span class="text-sm text-text">{{ email }}</span>
              </div>
            </div>
            <div class="py-md">
              <div class="flex justify-between items-center">
                <span class="text-sm font-medium text-text-secondary">Tier</span>
                <span
                  class="text-sm px-3 py-1 rounded-sm font-semibold uppercase text-xs tracking-wide"
                  :class="{
                    'bg-[rgba(136,136,136,0.2)] text-[#888888]': tier === 'free',
                    'bg-[rgba(59,130,246,0.2)] text-[#3b82f6]': tier === 'premium',
                    'bg-primary-soft text-primary': tier === 'pro'
                  }"
                >{{ tierLabel }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Logout Section -->
        <div class="flex flex-col gap-md">
          <h2 class="text-lg font-semibold text-text tracking-tight">Session</h2>
          <div class="bg-background-surface border border-border rounded-lg p-xl">
            <button
              @click="handleLogout"
              :disabled="isLoggingOut"
              class="flex items-center gap-sm py-md px-lg bg-transparent border border-[rgba(239,68,68,0.3)] rounded-md text-[#ef4444] text-sm font-semibold cursor-pointer transition-all duration-200 hover:bg-[rgba(239,68,68,0.1)] hover:border-[#ef4444] disabled:opacity-60 disabled:cursor-not-allowed"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
                <path fill-rule="evenodd" d="M3 4.25A2.25 2.25 0 015.25 2h5.5A2.25 2.25 0 0113 4.25v2a.75.75 0 01-1.5 0v-2a.75.75 0 00-.75-.75h-5.5a.75.75 0 00-.75.75v11.5c0 .414.336.75.75.75h5.5a.75.75 0 00.75-.75v-2a.75.75 0 011.5 0v2A2.25 2.25 0 0110.75 18h-5.5A2.25 2.25 0 013 15.75V4.25z" clip-rule="evenodd" />
                <path fill-rule="evenodd" d="M19 10a.75.75 0 00-.75-.75H8.704l1.048-.943a.75.75 0 10-1.004-1.114l-2.5 2.25a.75.75 0 000 1.114l2.5 2.25a.75.75 0 101.004-1.114l-1.048-.943h9.546A.75.75 0 0019 10z" clip-rule="evenodd" />
              </svg>
              {{ isLoggingOut ? 'Logging out...' : 'Logout' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { trackAuth } from '../services/analyticsApi';

const router = useRouter();
const authStore = useAuthStore();

const isLoggingOut = ref(false);

const username = computed(() => authStore.username || 'User');
const email = computed(() => authStore.email || 'Not set');
const tier = computed(() => authStore.tier || 'free');

const tierLabel = computed(() => {
  const t = tier.value;
  if (t === 'pro') return 'Pro';
  if (t === 'premium') return 'Premium';
  return 'Free';
});

async function handleLogout() {
  isLoggingOut.value = true;
  try {
    await authStore.logout();
    trackAuth('logout', true);
    router.push('/');
  } catch (e) {
    console.error('Logout failed:', e);
    trackAuth('logout', false);
    // Still redirect even if logout fails
    router.push('/');
  } finally {
    isLoggingOut.value = false;
  }
}
</script>

