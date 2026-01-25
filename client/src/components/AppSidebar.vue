<template>
  <aside :class="['app-sidebar', { collapsed: isCollapsed }]">
    <!-- Logo / Toggle Section -->
    <div class="sidebar-header">
      <router-link to="/app/overview" class="sidebar-logo">
        <img src="/mongoose.png" alt="Mongoose" class="logo-icon" />
        <Transition name="fade">
          <span v-if="!isCollapsed" class="logo-text">Mongoose.gg <span class="beta-tag">Beta</span></span>
        </Transition>
      </router-link>
      <button @click="toggleSidebar" class="toggle-btn" :title="isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="toggle-icon">
          <path fill-rule="evenodd" d="M2 4.75A.75.75 0 012.75 4h14.5a.75.75 0 010 1.5H2.75A.75.75 0 012 4.75zM2 10a.75.75 0 01.75-.75h14.5a.75.75 0 010 1.5H2.75A.75.75 0 012 10zm0 5.25a.75.75 0 01.75-.75h14.5a.75.75 0 010 1.5H2.75a.75.75 0 01-.75-.75z" clip-rule="evenodd" />
        </svg>
      </button>
    </div>

    <!-- Navigation Items -->
    <nav class="sidebar-nav">
      <router-link to="/app/overview" class="nav-item" :title="isCollapsed ? 'Overview' : ''">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon">
          <path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z" />
        </svg>
        <Transition name="fade">
          <span v-if="!isCollapsed" class="nav-label">Overview</span>
        </Transition>
      </router-link>

      <router-link to="/app/champion-select" class="nav-item" :title="isCollapsed ? 'Champion Select' : ''">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
        </svg>
        <Transition name="fade">
          <span v-if="!isCollapsed" class="nav-label">Champion Select</span>
        </Transition>
      </router-link>

      <router-link to="/app/matches" class="nav-item" :title="isCollapsed ? 'Matches' : ''">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon">
          <path fill-rule="evenodd" d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V4a2 2 0 00-2-2H6zm1 2a1 1 0 000 2h6a1 1 0 100-2H7zm6 7a1 1 0 011 1v3a1 1 0 11-2 0v-3a1 1 0 011-1zm-3 3a1 1 0 100 2h.01a1 1 0 100-2H10zm-4 1a1 1 0 011-1h.01a1 1 0 110 2H7a1 1 0 01-1-1zm1-4a1 1 0 100 2h.01a1 1 0 100-2H7zm2 1a1 1 0 011-1h.01a1 1 0 110 2H10a1 1 0 01-1-1zm4-4a1 1 0 100 2h.01a1 1 0 100-2H13zM9 9a1 1 0 011-1h.01a1 1 0 110 2H10a1 1 0 01-1-1zM7 8a1 1 0 000 2h.01a1 1 0 000-2H7z" clip-rule="evenodd" />
        </svg>
        <Transition name="fade">
          <span v-if="!isCollapsed" class="nav-label">Matches</span>
        </Transition>
      </router-link>

      <!-- Analysis Section with Submenu -->
      <div class="nav-section" :class="{ 'has-popout': isCollapsed }">
        <div class="nav-item-header">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon">
            <path d="M2 11a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 01-1 1H3a1 1 0 01-1-1v-5zM8 7a1 1 0 011-1h2a1 1 0 011 1v9a1 1 0 01-1 1H9a1 1 0 01-1-1V7zM14 4a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z" />
          </svg>
          <Transition name="fade">
            <span v-if="!isCollapsed" class="nav-label">Analysis</span>
          </Transition>
        </div>
        <!-- Expanded: show inline submenu -->
        <div v-if="!isCollapsed" class="nav-submenu">
          <router-link to="/app/solo" class="nav-subitem">
            <span class="nav-sublabel">Solo</span>
          </router-link>
          <router-link to="/app/duo" class="nav-subitem">
            <span class="nav-sublabel">Duo</span>
          </router-link>
          <router-link to="/app/team" class="nav-subitem">
            <span class="nav-sublabel">Team</span>
          </router-link>
        </div>
        <!-- Collapsed: show popout menu on hover -->
        <div v-if="isCollapsed" class="nav-popout">
          <div class="popout-header">Analysis</div>
          <router-link to="/app/solo" class="popout-item">Solo</router-link>
          <router-link to="/app/duo" class="popout-item">Duo</router-link>
          <router-link to="/app/team" class="popout-item">Team</router-link>
        </div>
      </div>

      <router-link to="/app/goals" class="nav-item" :title="isCollapsed ? 'Goals' : ''">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
        </svg>
        <Transition name="fade">
          <span v-if="!isCollapsed" class="nav-label">Goals</span>
        </Transition>
      </router-link>
    </nav>

    <!-- User Section at Bottom -->
    <div class="sidebar-footer">
      <router-link to="/app/user" class="user-item" :title="isCollapsed ? username : ''">
        <div class="user-avatar">
          <img
            v-if="profileIconUrl"
            :src="profileIconUrl"
            :alt="`${username} profile icon`"
            class="avatar-img"
            @error="handleIconError"
          />
          <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="avatar-icon">
            <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
          </svg>
        </div>
        <Transition name="fade">
          <div v-if="!isCollapsed" class="user-info">
            <span class="user-name">{{ username }}</span>
            <span class="user-tier">{{ tierLabel }}</span>
          </div>
        </Transition>
      </router-link>

      <!-- Version Badge -->
      <Transition name="fade">
        <div v-if="!isCollapsed" class="sidebar-version">
          v{{ version }}
        </div>
      </Transition>
    </div>
  </aside>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useAuthStore } from '../stores/authStore';
import { useUiStore } from '../stores/uiStore';
import pkg from '../../package.json';

const authStore = useAuthStore();
const uiStore = useUiStore();
const version = pkg.version || '0.0.0';

// Local state
const iconError = ref(false);

// Sidebar state from store
const isCollapsed = computed(() => uiStore.isSidebarCollapsed);

// Data Dragon version for profile icons
const ddVersion = '16.1.1';

// Initialize sidebar state
onMounted(() => {
  uiStore.initializeSidebar();
  window.addEventListener('resize', uiStore.handleResize);
});

onUnmounted(() => {
  window.removeEventListener('resize', uiStore.handleResize);
});

// Toggle sidebar
function toggleSidebar() {
  uiStore.toggleSidebar();
}

// User data
const username = computed(() => authStore.username || 'User');
const tierLabel = computed(() => {
  const tier = authStore.tier;
  if (tier === 'pro') return 'Pro';
  if (tier === 'premium') return 'Premium';
  return 'Free';
});

// Profile icon from first Riot account
const primaryRiotAccount = computed(() => authStore.primaryRiotAccount);

const profileIconUrl = computed(() => {
  const profileIconId = primaryRiotAccount.value?.profileIconId;
  if (!profileIconId || iconError.value) return null;
  return `https://ddragon.leagueoflegends.com/cdn/${ddVersion}/img/profileicon/${profileIconId}.png`;
});

function handleIconError() {
  iconError.value = true;
}
</script>


<style scoped>
.app-sidebar {
  position: fixed;
  top: 0;
  left: 0;
  height: 100vh;
  width: 256px;
  background: var(--color-surface);
  border-right: 1px solid var(--color-border);
  backdrop-filter: blur(10px);
  display: flex;
  flex-direction: column;
  transition: width 0.3s ease;
  z-index: 100;
}

.app-sidebar.collapsed {
  width: 64px;
  overflow: visible;
}

/* Header Section */
.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-lg) var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
  min-height: 64px;
}

.sidebar-logo {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  text-decoration: none;
  color: var(--color-text);
  flex: 1;
  min-width: 0;
}

.logo-icon {
  width: 32px;
  height: 16px;
  flex-shrink: 0;
}

.logo-text {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  white-space: nowrap;
}

.beta-tag {
  font-size: 0.5em;
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-normal);
  vertical-align: top;
}

.toggle-btn {
  background: transparent;
  border: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  padding: var(--spacing-xs);
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  flex-shrink: 0;
}

.toggle-btn:hover {
  background: var(--color-elevated);
  color: var(--color-text);
}

.toggle-icon {
  width: 20px;
  height: 20px;
}

/* Navigation Section */
.sidebar-nav {
  flex: 1;
  padding: var(--spacing-md) 0;
  overflow-y: auto;
  overflow-x: hidden;
}

.app-sidebar.collapsed .sidebar-nav {
  overflow: visible;
}

.nav-item,
.nav-item-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  margin: 0 var(--spacing-sm);
  color: var(--color-text-secondary);
  text-decoration: none;
  border-radius: var(--radius-md);
  transition: all 0.2s;
  cursor: pointer;
  white-space: nowrap;
}

.nav-item:hover,
.nav-item-header:hover {
  background: var(--color-elevated);
  color: var(--color-text);
}

.nav-item.router-link-active {
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

.nav-icon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
}

.nav-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  letter-spacing: var(--letter-spacing);
}

/* Center icons when collapsed */
.app-sidebar.collapsed .nav-item,
.app-sidebar.collapsed .nav-item-header {
  justify-content: center;
  padding: var(--spacing-md);
}

.app-sidebar.collapsed .nav-subitem {
  justify-content: center;
  padding: var(--spacing-sm) var(--spacing-md);
}

/* Submenu */
.nav-section {
  margin-bottom: var(--spacing-xs);
}

.nav-item-header {
  cursor: default;
}

.nav-submenu {
  margin-left: var(--spacing-xl);
  margin-top: var(--spacing-xs);
}

.nav-subitem {
  display: flex;
  align-items: center;
  padding: var(--spacing-sm) var(--spacing-md);
  margin: 0 var(--spacing-sm);
  color: var(--color-text-secondary);
  text-decoration: none;
  border-radius: var(--radius-md);
  transition: all 0.2s;
  font-size: var(--font-size-sm);
}

.nav-subitem:hover {
  background: var(--color-elevated);
  color: var(--color-text);
}

.nav-subitem.router-link-active {
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

.nav-sublabel {
  font-weight: var(--font-weight-medium);
  letter-spacing: var(--letter-spacing);
}

/* Popout Menu (for collapsed sidebar) */
.nav-section.has-popout {
  position: relative;
}

.nav-popout {
  position: absolute;
  left: 100%;
  top: 0;
  min-width: 140px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-xs);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  opacity: 0;
  visibility: hidden;
  transform: translateX(-8px);
  transition: all 0.2s ease;
  z-index: 100;
  margin-left: var(--spacing-xs);
}

.nav-section.has-popout:hover .nav-popout {
  opacity: 1;
  visibility: visible;
  transform: translateX(0);
}

.popout-header {
  padding: var(--spacing-sm) var(--spacing-md);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border-bottom: 1px solid var(--color-border);
  margin-bottom: var(--spacing-xs);
}

.popout-item {
  display: block;
  padding: var(--spacing-sm) var(--spacing-md);
  color: var(--color-text-secondary);
  text-decoration: none;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  border-radius: var(--radius-sm);
  transition: all 0.15s ease;
}

.popout-item:hover {
  background: var(--color-elevated);
  color: var(--color-text);
}

.popout-item.router-link-active {
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

/* Footer Section */
.sidebar-footer {
  border-top: 1px solid var(--color-border);
  padding: var(--spacing-md) 0;
}

.user-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  margin: 0 var(--spacing-sm);
  color: var(--color-text);
  text-decoration: none;
  border-radius: var(--radius-md);
  transition: all 0.2s;
  white-space: nowrap;
}

.user-item:hover {
  background: var(--color-elevated);
}

.user-item.router-link-active {
  background: var(--color-primary-soft);
}

/* Center user avatar when collapsed */
.app-sidebar.collapsed .user-item {
  justify-content: center;
  padding: var(--spacing-md);
}

.user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  overflow: hidden;
  background: var(--color-elevated);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border: 2px solid var(--color-border);
}

.avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.avatar-icon {
  width: 20px;
  height: 20px;
  color: var(--color-text-secondary);
}

.user-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
  flex: 1;
}

.user-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-tier {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.sidebar-version {
  padding: var(--spacing-sm) var(--spacing-md);
  text-align: center;
  font-size: var(--font-size-xs);
  color: #6b7280;
  border-top: 1px solid var(--color-border);
  margin-top: var(--spacing-sm);
}

/* Transitions */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* Scrollbar styling */
.sidebar-nav::-webkit-scrollbar {
  width: 4px;
}

.sidebar-nav::-webkit-scrollbar-track {
  background: transparent;
}

.sidebar-nav::-webkit-scrollbar-thumb {
  background: var(--color-border);
  border-radius: 2px;
}

.sidebar-nav::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-secondary);
}

/* Responsive */
@media (max-width: 1024px) {
  .app-sidebar {
    width: 64px;
  }
}
</style>
