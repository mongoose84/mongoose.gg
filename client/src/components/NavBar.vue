<template>
  <nav class="fixed top-0 left-0 right-0 z-50 bg-[rgba(0,0,0,0.8)] backdrop-blur-[12px] border-b border-border">
    <div class="max-w-[1280px] mx-auto flex items-center justify-between px-xl py-md h-16">
      <!-- Logo - navigates to /app/user if logged in, / if not -->
      <router-link :to="logoDestination" class="flex items-center gap-sm no-underline transition-opacity duration-200 hover:opacity-80">
        <img src="/mongoose.png" alt="Mongoose" class="w-16 h-8" />
        <span class="text-xl font-bold tracking-tight text-text">Mongoose.gg <span class="text-[0.5em] text-text-secondary font-normal align-top">Beta</span></span>
      </router-link>

      <!-- Desktop Navigation -->
      <div class="hidden md:flex gap-lg items-center">
        <a href="/#features" class="text-sm font-medium text-text-secondary no-underline transition-colors duration-200 tracking-tight hover:text-text">Features</a>
        <a href="/#pricing" class="text-sm font-medium text-text-secondary no-underline transition-colors duration-200 tracking-tight hover:text-text">Pricing</a>
        <a href="/#how-it-works" class="text-sm font-medium text-text-secondary no-underline transition-colors duration-200 tracking-tight hover:text-text">How It Works</a>
        <router-link to="/auth?mode=login" class="text-sm font-medium text-text-secondary no-underline transition-colors duration-200 tracking-tight hover:text-text">Login</router-link>
      </div>

      <!-- CTA Button -->
      <div class="hidden md:block">
        <router-link to="/auth?mode=signup" class="inline-flex items-center gap-xs px-lg py-sm bg-primary text-white font-semibold text-sm tracking-tight no-underline rounded-md transition-all duration-200 shadow-sm hover:shadow-lg hover:-translate-y-px group">
          Get Started
          <svg class="w-4 h-4 transition-transform duration-200 group-hover:translate-x-0.5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638L10.23 5.29a.75.75 0 111.04-1.08l5.5 5.25a.75.75 0 010 1.08l-5.5 5.25a.75.75 0 11-1.04-1.08l4.158-3.96H3.75A.75.75 0 013 10z" clip-rule="evenodd" />
          </svg>
        </router-link>
      </div>

      <!-- Mobile Menu Button -->
      <button
        @click="toggleMobile"
        class="flex md:hidden items-center justify-center p-xs bg-transparent border-none text-text cursor-pointer rounded-sm transition-colors duration-200 hover:bg-background-surface"
        aria-label="Toggle menu"
      >
        <svg v-if="!mobileOpen" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" class="w-6 h-6">
          <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
        </svg>
        <svg v-else xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" class="w-6 h-6">
          <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>

    <!-- Mobile Menu -->
    <Transition name="mobile-menu">
      <div v-if="mobileOpen" class="absolute top-full left-0 right-0 bg-[rgba(0,0,0,0.95)] backdrop-blur-[12px] border-b border-border flex flex-col p-lg gap-sm">
        <a href="/#features" class="p-md text-base font-medium text-text-secondary no-underline rounded-md transition-all duration-200 tracking-tight hover:text-text hover:bg-background-surface" @click="closeMobile">Features</a>
        <a href="/#pricing" class="p-md text-base font-medium text-text-secondary no-underline rounded-md transition-all duration-200 tracking-tight hover:text-text hover:bg-background-surface" @click="closeMobile">Pricing</a>
        <a href="/#how-it-works" class="p-md text-base font-medium text-text-secondary no-underline rounded-md transition-all duration-200 tracking-tight hover:text-text hover:bg-background-surface" @click="closeMobile">How It Works</a>
        <router-link to="/auth?mode=login" class="p-md text-base font-medium text-text-secondary no-underline rounded-md transition-all duration-200 tracking-tight hover:text-text hover:bg-background-surface" @click="closeMobile">Login</router-link>
        <router-link to="/auth?mode=signup" class="mt-sm p-md bg-primary text-white font-semibold text-base text-center no-underline rounded-md transition-all duration-200 tracking-tight hover:shadow-md" @click="closeMobile">
          Get Started
        </router-link>
      </div>
    </Transition>
  </nav>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useAuthStore } from '../stores/authStore';

const authStore = useAuthStore();
const mobileOpen = ref(false);

// Logo destination based on auth state
const logoDestination = computed(() => {
  if (authStore.isAuthenticated && authStore.isVerified) {
    return '/app/user';
  }
  return '/';
});

const toggleMobile = () => {
  mobileOpen.value = !mobileOpen.value;
};

const closeMobile = () => {
  mobileOpen.value = false;
};
</script>

<style scoped>
/* Vue Transition classes for mobile menu animation */
.mobile-menu-enter-active,
.mobile-menu-leave-active {
  transition: all 0.3s ease;
}

.mobile-menu-enter-from,
.mobile-menu-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
