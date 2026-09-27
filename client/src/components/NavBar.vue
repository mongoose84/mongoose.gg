<template>
  <header class="fixed top-0 left-0 right-0 z-50 bg-background" data-testid="navbar">
    <div class="max-w-[1440px] mx-auto h-14 desk:h-20 px-4 desk:px-14 flex items-center gap-2 desk:gap-10">
      <!-- Logo - navigates to /app/user if logged in, / if not -->
      <router-link
        :to="logoDestination"
        class="mp-focusable flex items-center gap-2.5 rounded-pill no-underline font-display font-bold text-[1.0625rem] desk:text-title text-text"
        aria-label="Mongoose.gg home"
        data-testid="navbar-logo"
      >
        <img src="/mongoose.png" alt="" class="h-[1.875rem] w-[4.625rem] desk:h-10 desk:w-[6.1875rem] object-contain" />
        Mongoose.gg
      </router-link>

      <!-- Desktop section links -->
      <nav aria-label="Main" class="hidden desk:flex gap-1 p-1 rounded-pill bg-background-elevated">
        <a
          v-for="link in sectionLinks"
          :key="link.href"
          :href="link.href"
          class="nav-pill mp-focusable"
          :data-testid="`navbar-link-${link.id}`"
        >{{ link.label }}</a>
      </nav>

      <div class="flex-grow" />

      <router-link
        to="/auth?mode=login"
        class="mp-focusable flex items-center h-11 px-2.5 desk:px-3 rounded-pill text-body-sm font-medium text-text-soft no-underline hover:text-text"
        data-testid="navbar-login"
      >Log in</router-link>

      <router-link
        to="/auth?mode=signup"
        class="mp-btn mp-btn--primary mp-btn--sm hidden desk:inline-flex"
        data-testid="navbar-signup"
      >Create free account</router-link>

      <!-- Phone menu button -->
      <button
        type="button"
        class="mp-focusable desk:hidden flex items-center justify-center w-11 h-11 rounded-pill border-0 bg-background-elevated text-text cursor-pointer"
        :aria-label="mobileOpen ? 'Close menu' : 'Open menu'"
        :aria-expanded="mobileOpen"
        aria-controls="navbar-mobile-menu"
        data-testid="navbar-menu-toggle"
        @click="toggleMobile"
      >
        <BaseIcon :name="mobileOpen ? 'x' : 'menu'" :size="20" />
      </button>
    </div>

    <!-- Phone menu -->
    <Transition name="mobile-menu">
      <nav
        v-if="mobileOpen"
        id="navbar-mobile-menu"
        aria-label="Main"
        class="desk:hidden absolute top-full left-0 right-0 bg-background border-b border-border flex flex-col gap-1 px-4 pt-2 pb-5"
        data-testid="navbar-mobile-menu"
      >
        <a
          v-for="link in sectionLinks"
          :key="link.href"
          :href="link.href"
          class="mp-focusable flex items-center h-12 px-4 rounded-pill text-body font-medium text-text-soft no-underline hover:text-text hover:bg-background-elevated"
          @click="closeMobile"
        >{{ link.label }}</a>
        <router-link
          to="/auth?mode=signup"
          class="mp-btn mp-btn--primary mt-2 w-full"
          @click="closeMobile"
        >Create free account</router-link>
      </nav>
    </Transition>
  </header>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useAuthStore } from '../stores/authStore';
import { BaseIcon } from '@/components/base';

const authStore = useAuthStore();
const mobileOpen = ref(false);

const sectionLinks = [
  { id: 'features', href: '/#features', label: 'Features' },
  { id: 'how-it-works', href: '/#how-it-works', label: 'How it works' }
];

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
.nav-pill {
  display: flex;
  align-items: center;
  height: 2.25rem;
  padding: 0 1rem;
  border-radius: 999px;
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  font-weight: 500;
  text-decoration: none;
  transition: color 150ms ease-out;
}

.nav-pill:hover {
  color: var(--color-text);
}

/* Slide-down for the phone menu */
.mobile-menu-enter-active,
.mobile-menu-leave-active {
  transition: opacity 150ms ease-out, transform 150ms ease-out;
}

.mobile-menu-enter-from,
.mobile-menu-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}

@media (prefers-reduced-motion: reduce) {
  .nav-pill,
  .mobile-menu-enter-active,
  .mobile-menu-leave-active {
    transition: none;
  }
}
</style>
