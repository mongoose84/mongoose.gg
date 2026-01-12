<template>
  <nav class="nav-bar">
    <div class="nav-container">
      <!-- Logo - navigates to /app/user if logged in, / if not -->
      <router-link :to="logoDestination" class="nav-logo-link">
        <img src="/pulse-icon.svg" alt="Pulse.gg" class="nav-logo-icon" />
        <span class="nav-logo-text">Pulse<span class="nav-logo-tld">.gg</span></span>
      </router-link>

      <!-- Desktop Navigation -->
      <div class="nav-links">
        <a href="/#features" class="nav-link">Features</a>
        <a href="/#pricing" class="nav-link">Pricing</a>
        <a href="/#how-it-works" class="nav-link">How It Works</a>
        <router-link to="/auth?mode=login" class="nav-link">Login</router-link>
      </div>

      <!-- CTA Button -->
      <div class="nav-cta">
        <router-link to="/auth?mode=signup" class="nav-btn-primary">
          Get Started
          <svg class="nav-arrow" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638L10.23 5.29a.75.75 0 111.04-1.08l5.5 5.25a.75.75 0 010 1.08l-5.5 5.25a.75.75 0 11-1.04-1.08l4.158-3.96H3.75A.75.75 0 013 10z" clip-rule="evenodd" />
          </svg>
        </router-link>
      </div>

      <!-- Mobile Menu Button -->
      <button @click="toggleMobile" class="nav-mobile-toggle" aria-label="Toggle menu">
        <svg v-if="!mobileOpen" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" class="nav-icon">
          <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
        </svg>
        <svg v-else xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" class="nav-icon">
          <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>

    <!-- Mobile Menu -->
    <Transition name="mobile-menu">
      <div v-if="mobileOpen" class="nav-mobile-menu">
        <a href="/#features" class="nav-mobile-link" @click="closeMobile">Features</a>
        <a href="/#pricing" class="nav-mobile-link" @click="closeMobile">Pricing</a>
        <a href="/#how-it-works" class="nav-mobile-link" @click="closeMobile">How It Works</a>
        <router-link to="/auth?mode=login" class="nav-mobile-link" @click="closeMobile">Login</router-link>
        <router-link to="/auth?mode=signup" class="nav-mobile-cta" @click="closeMobile">
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
.nav-bar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 50;
  background: rgba(0, 0, 0, 0.8);
  backdrop-filter: blur(12px);
  border-bottom: 1px solid var(--color-border);
}

.nav-container {
  max-width: 1280px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-md) var(--spacing-xl);
  height: 64px;
}

/* Logo */
.nav-logo-link {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  text-decoration: none;
  transition: opacity 0.2s;
}

.nav-logo-link:hover {
  opacity: 0.8;
}

.nav-logo-icon {
  width: 32px;
  height: 32px;
}

.nav-logo-text {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  color: var(--color-text);
}

.nav-logo-tld {
  color: var(--color-primary);
}

/* Desktop Navigation */
.nav-links {
  display: none;
  gap: var(--spacing-lg);
  align-items: center;
}

@media (min-width: 768px) {
  .nav-links {
    display: flex;
  }
}

.nav-link {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  text-decoration: none;
  transition: color 0.2s;
  letter-spacing: var(--letter-spacing);
}

.nav-link:hover {
  color: var(--color-text);
}

/* CTA Button */
.nav-cta {
  display: none;
}

@media (min-width: 768px) {
  .nav-cta {
    display: block;
  }
}

.nav-btn-primary {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-sm) var(--spacing-lg);
  background: var(--color-primary);
  color: white;
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-sm);
  letter-spacing: var(--letter-spacing);
  text-decoration: none;
  border-radius: var(--radius-md);
  transition: all 0.2s;
  box-shadow: var(--shadow-sm);
}

.nav-btn-primary:hover {
  box-shadow: var(--shadow-lg);
  transform: translateY(-1px);
}

.nav-arrow {
  width: 16px;
  height: 16px;
  transition: transform 0.2s;
}

.nav-btn-primary:hover .nav-arrow {
  transform: translateX(2px);
}

/* Mobile Toggle */
.nav-mobile-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xs);
  background: transparent;
  border: none;
  color: var(--color-text);
  cursor: pointer;
  border-radius: var(--radius-sm);
  transition: background 0.2s;
}

.nav-mobile-toggle:hover {
  background: var(--color-surface);
}

@media (min-width: 768px) {
  .nav-mobile-toggle {
    display: none;
  }
}

.nav-icon {
  width: 24px;
  height: 24px;
}

/* Mobile Menu */
.nav-mobile-menu {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: rgba(0, 0, 0, 0.95);
  backdrop-filter: blur(12px);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  padding: var(--spacing-lg);
  gap: var(--spacing-sm);
}

.nav-mobile-link {
  padding: var(--spacing-md);
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  text-decoration: none;
  border-radius: var(--radius-md);
  transition: all 0.2s;
  letter-spacing: var(--letter-spacing);
}

.nav-mobile-link:hover {
  color: var(--color-text);
  background: var(--color-surface);
}

.nav-mobile-cta {
  margin-top: var(--spacing-sm);
  padding: var(--spacing-md);
  background: var(--color-primary);
  color: white;
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-md);
  text-align: center;
  text-decoration: none;
  border-radius: var(--radius-md);
  transition: all 0.2s;
  letter-spacing: var(--letter-spacing);
}

.nav-mobile-cta:hover {
  box-shadow: var(--shadow-md);
}

/* Mobile Menu Transitions */
.mobile-menu-enter-active,
.mobile-menu-leave-active {
  transition: all 0.3s ease;
}

.mobile-menu-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

.mobile-menu-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
