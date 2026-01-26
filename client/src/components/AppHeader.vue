<template>
  <header class="fixed top-0 left-0 right-0 h-16 bg-background-surface border-b border-border backdrop-blur-[10px] z-[100]">
    <div class="max-w-[1400px] mx-auto h-full flex items-center justify-between px-xl">
      <!-- Logo -->
      <router-link to="/app/user" class="flex items-center gap-sm no-underline text-text">
        <img src="/mongoose.png" alt="Mongoose" class="w-16 h-8" />
        <span class="text-lg font-bold tracking-tight">Mongoose.gg <span class="text-[0.5em] text-text-secondary font-normal align-top">Beta</span></span>
      </router-link>

      <!-- User section -->
      <div class="relative" ref="userMenuRef">
        <button
          @click="toggleDropdown"
          class="flex items-center gap-sm px-md py-sm bg-transparent border border-border rounded-md cursor-pointer transition-all duration-200 hover:bg-background-surface-hover hover:border-primary"
        >
          <div class="w-16 h-8 bg-primary-soft rounded-full flex items-center justify-center">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-5 h-5 text-primary">
              <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="flex flex-col items-start gap-0.5">
            <span class="text-sm font-medium text-text">{{ username }}</span>
            <span class="text-xs text-text-secondary uppercase tracking-wider">{{ tierLabel }}</span>
          </div>
          <svg
            xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor"
            class="w-4 h-4 text-text-secondary transition-transform duration-200"
            :class="{ 'rotate-180': isDropdownOpen }"
          >
            <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
          </svg>
        </button>

        <!-- Dropdown menu -->
        <Transition name="dropdown">
          <div v-if="isDropdownOpen" class="absolute top-[calc(100%+8px)] right-0 min-w-[200px] bg-[#020617] border border-border rounded-md shadow-lg overflow-hidden z-[200]">
            <button class="flex items-center gap-sm w-full p-md bg-transparent border-none text-sm text-text cursor-not-allowed transition-colors duration-200 text-left opacity-60" disabled>
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
                <path fill-rule="evenodd" d="M7.84 1.804A1 1 0 018.82 1h2.36a1 1 0 01.98.804l.331 1.652a6.993 6.993 0 011.929 1.115l1.598-.54a1 1 0 011.186.447l1.18 2.044a1 1 0 01-.205 1.251l-1.267 1.113a7.047 7.047 0 010 2.228l1.267 1.113a1 1 0 01.206 1.25l-1.18 2.045a1 1 0 01-1.187.447l-1.598-.54a6.993 6.993 0 01-1.929 1.115l-.33 1.652a1 1 0 01-.98.804H8.82a1 1 0 01-.98-.804l-.331-1.652a6.993 6.993 0 01-1.929-1.115l-1.598.54a1 1 0 01-1.186-.447l-1.18-2.044a1 1 0 01.205-1.251l1.267-1.114a7.05 7.05 0 010-2.227L1.821 7.773a1 1 0 01-.206-1.25l1.18-2.045a1 1 0 011.187-.447l1.598.54A6.993 6.993 0 017.51 3.456l.33-1.652zM10 13a3 3 0 100-6 3 3 0 000 6z" clip-rule="evenodd" />
              </svg>
              Settings
              <span class="ml-auto text-xs text-text-secondary bg-background-surface-hover px-1.5 py-0.5 rounded-sm">Coming Soon</span>
            </button>
            <div class="h-px bg-border my-xs"></div>
            <button @click="handleLogout" class="flex items-center gap-sm w-full p-md bg-transparent border-none text-sm text-[#ef4444] cursor-pointer transition-colors duration-200 text-left hover:bg-[rgba(239,68,68,0.1)]">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
                <path fill-rule="evenodd" d="M3 4.25A2.25 2.25 0 015.25 2h5.5A2.25 2.25 0 0113 4.25v2a.75.75 0 01-1.5 0v-2a.75.75 0 00-.75-.75h-5.5a.75.75 0 00-.75.75v11.5c0 .414.336.75.75.75h5.5a.75.75 0 00.75-.75v-2a.75.75 0 011.5 0v2A2.25 2.25 0 0110.75 18h-5.5A2.25 2.25 0 013 15.75V4.25z" clip-rule="evenodd" />
                <path fill-rule="evenodd" d="M19 10a.75.75 0 00-.75-.75H8.704l1.048-.943a.75.75 0 10-1.004-1.114l-2.5 2.25a.75.75 0 000 1.114l2.5 2.25a.75.75 0 101.004-1.114l-1.048-.943h9.546A.75.75 0 0019 10z" clip-rule="evenodd" />
              </svg>
              Logout
            </button>
          </div>
        </Transition>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { trackAuth } from '../services/analyticsApi';

const router = useRouter();
const authStore = useAuthStore();

const isDropdownOpen = ref(false);
const userMenuRef = ref(null);

const username = computed(() => authStore.username || 'User');
const tierLabel = computed(() => {
  const tier = authStore.tier;
  if (tier === 'pro') return 'Pro';
  if (tier === 'premium') return 'Premium';
  return 'Free';
});

const toggleDropdown = () => {
  isDropdownOpen.value = !isDropdownOpen.value;
};

const handleLogout = async () => {
  isDropdownOpen.value = false;
  try {
    await authStore.logout();
    trackAuth('logout', true);
    router.push('/');
  } catch (e) {
    console.error('Logout failed:', e);
    trackAuth('logout', false);
    // Still redirect even if logout fails
    router.push('/');
  }
};

// Close dropdown when clicking outside
const handleClickOutside = (event) => {
  if (userMenuRef.value && !userMenuRef.value.contains(event.target)) {
    isDropdownOpen.value = false;
  }
};

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
});
</script>

<style scoped>
/* Vue Transition classes for dropdown animation */
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>

