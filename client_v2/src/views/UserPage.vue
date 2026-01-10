<template>
  <div class="user-page">
    <div class="user-container">
      <div class="welcome-section">
        <h1 class="welcome-title">Welcome, {{ username }}!</h1>
        <p class="welcome-subtitle">Your Pulse.gg dashboard</p>
      </div>

      <div class="user-cards">
        <!-- Account Info Card -->
        <div class="user-card">
          <div class="card-header">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="card-icon">
              <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
            </svg>
            <h2 class="card-title">Account</h2>
          </div>
          <div class="card-content">
            <div class="info-row">
              <span class="info-label">Username</span>
              <span class="info-value">{{ username }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Email</span>
              <span class="info-value">{{ email }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">Tier</span>
              <span class="info-value tier-badge" :class="tierClass">{{ tierLabel }}</span>
            </div>
          </div>
        </div>

        <!-- Coming Soon Card -->
        <div class="user-card coming-soon-card">
          <div class="card-header">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="card-icon">
              <path fill-rule="evenodd" d="M14.615 1.595a.75.75 0 01.359.852L12.982 9.75h7.268a.75.75 0 01.548 1.262l-10.5 11.25a.75.75 0 01-1.272-.71l1.992-7.302H3.75a.75.75 0 01-.548-1.262l10.5-11.25a.75.75 0 01.913-.143z" clip-rule="evenodd" />
            </svg>
            <h2 class="card-title">Features</h2>
          </div>
          <div class="card-content">
            <p class="coming-soon-text">More features coming soon!</p>
            <ul class="feature-list">
              <li>Game statistics tracking</li>
              <li>Performance analytics</li>
              <li>Team management</li>
              <li>Match history</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useAuthStore } from '../stores/authStore';

const authStore = useAuthStore();

const username = computed(() => authStore.username || 'User');
const email = computed(() => authStore.email || '');
const tier = computed(() => authStore.tier || 'free');

const tierLabel = computed(() => {
  if (tier.value === 'pro') return 'Pro';
  if (tier.value === 'premium') return 'Premium';
  return 'Free';
});

const tierClass = computed(() => {
  return `tier-${tier.value}`;
});
</script>

<style scoped>
.user-page {
  min-height: calc(100vh - 64px);
  padding: var(--spacing-2xl);
}

.user-container {
  max-width: 1000px;
  margin: 0 auto;
}

.welcome-section {
  margin-bottom: var(--spacing-2xl);
}

.welcome-title {
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  margin-bottom: var(--spacing-xs);
}

.welcome-subtitle {
  font-size: var(--font-size-lg);
  color: var(--color-text-secondary);
}

.user-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: var(--spacing-xl);
}

.user-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-xl);
}

.card-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  margin-bottom: var(--spacing-lg);
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.card-icon {
  width: 24px;
  height: 24px;
  color: var(--color-primary);
}

.card-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.card-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.info-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.info-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.info-value {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.tier-badge {
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  text-transform: uppercase;
  font-size: var(--font-size-xs);
  letter-spacing: 0.05em;
}

.tier-free {
  background: var(--color-surface-hover);
  color: var(--color-text-secondary);
}

.tier-pro {
  background: rgba(59, 130, 246, 0.2);
  color: #3b82f6;
}

.tier-premium {
  background: rgba(168, 85, 247, 0.2);
  color: #a855f7;
}

.coming-soon-text {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-md);
}

.feature-list {
  list-style: none;
  padding: 0;
  margin: 0;
}

.feature-list li {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  padding: var(--spacing-sm) 0;
  border-bottom: 1px solid var(--color-border);
}

.feature-list li:last-child {
  border-bottom: none;
}

.feature-list li::before {
  content: '→ ';
  color: var(--color-primary);
}
</style>

