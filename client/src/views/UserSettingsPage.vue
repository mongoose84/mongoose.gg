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
                    'bg-muted-soft text-muted': tier === 'free',
                    'bg-info-soft text-info': tier === 'premium',
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
              class="flex items-center gap-sm py-md px-lg bg-transparent border border-error-border rounded-md text-error text-sm font-semibold cursor-pointer transition-all duration-200 hover:bg-error-soft hover:border-error disabled:opacity-60 disabled:cursor-not-allowed"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
                <path fill-rule="evenodd" d="M3 4.25A2.25 2.25 0 015.25 2h5.5A2.25 2.25 0 0113 4.25v2a.75.75 0 01-1.5 0v-2a.75.75 0 00-.75-.75h-5.5a.75.75 0 00-.75.75v11.5c0 .414.336.75.75.75h5.5a.75.75 0 00.75-.75v-2a.75.75 0 011.5 0v2A2.25 2.25 0 0110.75 18h-5.5A2.25 2.25 0 013 15.75V4.25z" clip-rule="evenodd" />
                <path fill-rule="evenodd" d="M19 10a.75.75 0 00-.75-.75H8.704l1.048-.943a.75.75 0 10-1.004-1.114l-2.5 2.25a.75.75 0 000 1.114l2.5 2.25a.75.75 0 101.004-1.114l-1.048-.943h9.546A.75.75 0 0019 10z" clip-rule="evenodd" />
              </svg>
              {{ isLoggingOut ? 'Logging out...' : 'Logout' }}
            </button>
          </div>
        </div>

        <!-- Danger Zone -->
        <div class="flex flex-col gap-md">
          <h2 class="text-lg font-semibold text-error tracking-tight">Danger Zone</h2>
          <div class="bg-background-surface border border-error rounded-lg p-xl">
            <div class="flex flex-col gap-md sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 class="text-sm font-semibold text-text">Delete Account</h3>
                <p class="text-xs text-text-secondary mt-xs">
                  Permanently delete your account and all associated data. This action cannot be undone.
                </p>
              </div>
              <button
                @click="showDeleteModal = true"
                class="flex-shrink-0 py-md px-lg bg-error rounded-md text-white text-sm font-semibold cursor-pointer transition-all duration-200 hover:opacity-90"
              >
                Delete Account
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete Account Modal -->
    <DeleteAccountModal
      :isOpen="showDeleteModal"
      @close="showDeleteModal = false"
      @deleted="handleAccountDeleted"
    />
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { trackAuth } from '../services/analyticsApi';
import DeleteAccountModal from '../components/DeleteAccountModal.vue';

const router = useRouter();
const authStore = useAuthStore();

const isLoggingOut = ref(false);
const showDeleteModal = ref(false);

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

async function handleAccountDeleted() {
  // Clear auth state (user is already signed out on the server)
  authStore.user = null;
  // Track the event
  trackAuth('account_deleted', true);
  // Redirect to home page
  router.push('/');
}
</script>

