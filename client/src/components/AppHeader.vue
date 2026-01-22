<template>
  <header class="app-header">
    <div class="header-content">
      <!-- Logo -->
      <router-link to="/app/user" class="header-logo">
        <img src="/mongoose.png" alt="Mongoose" class="logo-icon" />
        <span class="logo-text">Mongoose.gg <span class="beta-tag">Beta</span></span>
      </router-link>

      <!-- User section -->
      <div class="header-user" ref="userMenuRef">
        <button @click="toggleDropdown" class="user-button">
          <div class="user-avatar">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="avatar-icon">
              <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="user-info">
            <span class="user-name">{{ username }}</span>
            <span class="user-tier">{{ tierLabel }}</span>
          </div>
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="dropdown-arrow" :class="{ 'dropdown-arrow-open': isDropdownOpen }">
            <path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd" />
          </svg>
        </button>

        <!-- Dropdown menu -->
        <Transition name="dropdown">
          <div v-if="isDropdownOpen" class="user-dropdown">
            <button class="dropdown-item dropdown-item-disabled" disabled>
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="dropdown-icon">
                <path fill-rule="evenodd" d="M7.84 1.804A1 1 0 018.82 1h2.36a1 1 0 01.98.804l.331 1.652a6.993 6.993 0 011.929 1.115l1.598-.54a1 1 0 011.186.447l1.18 2.044a1 1 0 01-.205 1.251l-1.267 1.113a7.047 7.047 0 010 2.228l1.267 1.113a1 1 0 01.206 1.25l-1.18 2.045a1 1 0 01-1.187.447l-1.598-.54a6.993 6.993 0 01-1.929 1.115l-.33 1.652a1 1 0 01-.98.804H8.82a1 1 0 01-.98-.804l-.331-1.652a6.993 6.993 0 01-1.929-1.115l-1.598.54a1 1 0 01-1.186-.447l-1.18-2.044a1 1 0 01.205-1.251l1.267-1.114a7.05 7.05 0 010-2.227L1.821 7.773a1 1 0 01-.206-1.25l1.18-2.045a1 1 0 011.187-.447l1.598.54A6.993 6.993 0 017.51 3.456l.33-1.652zM10 13a3 3 0 100-6 3 3 0 000 6z" clip-rule="evenodd" />
              </svg>
              Settings
              <span class="coming-soon-badge">Coming Soon</span>
            </button>
            <div class="dropdown-divider"></div>
            <button @click="handleLogout" class="dropdown-item dropdown-item-danger">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="dropdown-icon">
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
.app-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 64px;
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
  backdrop-filter: blur(10px);
  z-index: 100;
}

.header-content {
  max-width: 1400px;
  margin: 0 auto;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--spacing-xl);
}

.header-logo {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  text-decoration: none;
  color: var(--color-text);
}

.logo-icon {
  width: 64px;
  height: 32px;
}

.logo-text {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
}

.beta-tag {
  font-size: 0.5em;
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-normal);
  vertical-align: top;
}

/* User section */
.header-user {
  position: relative;
}

.user-button {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-md);
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.2s;
}

.user-button:hover {
  background: var(--color-surface-hover);
  border-color: var(--color-primary);
}

.user-avatar {
  width: 64px;
  height: 32px;
  background: var(--color-primary-soft);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.avatar-icon {
  width: 20px;
  height: 20px;
  color: var(--color-primary);
}

.user-info {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
}

.user-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.user-tier {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.dropdown-arrow {
  width: 16px;
  height: 16px;
  color: var(--color-text-secondary);
  transition: transform 0.2s;
}

.dropdown-arrow-open {
  transform: rotate(180deg);
}

/* Dropdown menu */
.user-dropdown {
  position: absolute;
  top: calc(100% + var(--spacing-sm));
  right: 0;
  min-width: 200px;
	  /* Solid background so the account dropdown isn't see-through */
	  background-color: #020617;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  overflow: hidden;
  z-index: 200;
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  width: 100%;
  padding: var(--spacing-md);
  background: transparent;
  border: none;
  font-size: var(--font-size-sm);
  color: var(--color-text);
  cursor: pointer;
  transition: background 0.2s;
  text-align: left;
}

.dropdown-item:hover:not(:disabled) {
  background: var(--color-surface-hover);
}

.dropdown-item-disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.dropdown-item-danger {
  color: #ef4444;
}

.dropdown-item-danger:hover {
  background: rgba(239, 68, 68, 0.1);
}

.dropdown-icon {
  width: 16px;
  height: 16px;
}

.dropdown-divider {
  height: 1px;
  background: var(--color-border);
  margin: var(--spacing-xs) 0;
}

.coming-soon-badge {
  margin-left: auto;
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  background: var(--color-surface-hover);
  padding: 2px 6px;
  border-radius: var(--radius-sm);
}

/* Dropdown animation */
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

