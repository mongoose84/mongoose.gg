<template>
  <div class="user-page">
    <div class="user-container">
      <div class="welcome-section">
        <h1 class="welcome-title">Welcome, {{ username }}!</h1>
        <p class="welcome-subtitle">Your Pulse.gg dashboard</p>
      </div>

      <div class="user-cards">
        <!-- Solo Dashboard Card (Clickable) -->
        <div 
          v-if="hasLinkedAccount"
          class="user-card solo-card clickable" 
          @click="navigateToSoloDashboard"
          role="button"
          tabindex="0"
          @keydown.enter="navigateToSoloDashboard"
          @keydown.space.prevent="navigateToSoloDashboard"
        >
          <div class="card-header">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="card-icon">
              <path d="M11.25 5.337c0-.355-.186-.676-.401-.959a1.647 1.647 0 01-.349-1.003c0-1.036 1.007-1.875 2.25-1.875S15 2.34 15 3.375c0 .369-.128.713-.349 1.003-.215.283-.401.604-.401.959 0 .332.278.598.61.578 1.91-.114 3.79-.342 5.632-.676a.75.75 0 01.878.645 49.17 49.17 0 01.376 5.452.657.657 0 01-.66.664c-.354 0-.675-.186-.958-.401a1.647 1.647 0 00-1.003-.349c-1.035 0-1.875 1.007-1.875 2.25s.84 2.25 1.875 2.25c.369 0 .713-.128 1.003-.349.283-.215.604-.401.959-.401.31 0 .557.262.534.571a48.774 48.774 0 01-.595 4.845.75.75 0 01-.61.61c-1.82.317-3.673.533-5.555.642a.58.58 0 01-.611-.581c0-.355.186-.676.401-.959.221-.29.349-.634.349-1.003 0-1.035-1.007-1.875-2.25-1.875s-2.25.84-2.25 1.875c0 .369.128.713.349 1.003.215.283.401.604.401.959a.641.641 0 01-.658.643 49.118 49.118 0 01-4.708-.36.75.75 0 01-.645-.878c.293-1.614.504-3.257.629-4.924A.53.53 0 005.337 15c-.355 0-.676.186-.959.401-.29.221-.634.349-1.003.349-1.036 0-1.875-1.007-1.875-2.25s.84-2.25 1.875-2.25c.369 0 .713.128 1.003.349.283.215.604.401.959.401a.656.656 0 00.659-.663 47.703 47.703 0 00-.31-4.82.75.75 0 01.83-.832c1.343.155 2.703.254 4.077.294a.64.64 0 00.657-.642z" />
            </svg>
            <h2 class="card-title">Solo Dashboard</h2>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-arrow">
              <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="card-content">
            <div class="solo-summary" v-if="firstRiotAccount">
              <div class="solo-identity">
                <span class="solo-name">{{ firstRiotAccount.gameName }}#{{ firstRiotAccount.tagLine }}</span>
              </div>
              <p class="solo-hint">Click to view your solo performance, champions, and goals</p>
            </div>
          </div>
        </div>

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

        <!-- Riot Accounts Card -->
        <div class="user-card riot-accounts-card">
          <div class="card-header">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="card-icon">
              <path d="M11.25 5.337c0-.355-.186-.676-.401-.959a1.647 1.647 0 01-.349-1.003c0-1.036 1.007-1.875 2.25-1.875S15 2.34 15 3.375c0 .369-.128.713-.349 1.003-.215.283-.401.604-.401.959 0 .332.278.598.61.578 1.91-.114 3.79-.342 5.632-.676a.75.75 0 01.878.645 49.17 49.17 0 01.376 5.452.657.657 0 01-.66.664c-.354 0-.675-.186-.958-.401a1.647 1.647 0 00-1.003-.349c-1.035 0-1.875 1.007-1.875 2.25s.84 2.25 1.875 2.25c.369 0 .713-.128 1.003-.349.283-.215.604-.401.959-.401.31 0 .557.262.534.571a48.774 48.774 0 01-.595 4.845.75.75 0 01-.61.61c-1.82.317-3.673.533-5.555.642a.58.58 0 01-.611-.581c0-.355.186-.676.401-.959.221-.29.349-.634.349-1.003 0-1.035-1.007-1.875-2.25-1.875s-2.25.84-2.25 1.875c0 .369.128.713.349 1.003.215.283.401.604.401.959a.641.641 0 01-.658.643 49.118 49.118 0 01-4.708-.36.75.75 0 01-.645-.878c.293-1.614.504-3.257.629-4.924A.53.53 0 005.337 15c-.355 0-.676.186-.959.401-.29.221-.634.349-1.003.349-1.036 0-1.875-1.007-1.875-2.25s.84-2.25 1.875-2.25c.369 0 .713.128 1.003.349.283.215.604.401.959.401a.656.656 0 00.659-.663 47.703 47.703 0 00-.31-4.82.75.75 0 01.83-.832c1.343.155 2.703.254 4.077.294a.64.64 0 00.657-.642z" />
            </svg>
            <h2 class="card-title">Riot Accounts</h2>
          </div>
          <div class="card-content">
            <!-- Linked accounts list -->
            <template v-if="hasLinkedAccount">
              <div v-for="account in riotAccounts" :key="account.puuid" class="riot-account-item">
                <div class="riot-account-info">
                  <span class="riot-account-name">{{ account.gameName }}#{{ account.tagLine }}</span>
                  <span class="riot-account-region">{{ getRegionLabel(account.region) }}</span>
                </div>
                <div class="riot-account-status-container">
                  <!-- Show progress bar when syncing -->
                  <template v-if="getAccountSyncStatus(account) === 'syncing'">
                    <div class="sync-progress-container">
                      <div class="sync-progress-bar">
                        <div
                          class="sync-progress-fill"
                          :style="{ width: getProgressPercent(account) + '%' }"
                        ></div>
                      </div>
                      <span class="sync-progress-text">
                        {{ getProgressText(account) }}
                      </span>
                    </div>
                  </template>

                  <!-- Show error with retry button -->
                  <template v-else-if="getAccountSyncStatus(account) === 'failed'">
                    <div class="sync-error-container">
                      <span class="sync-error-text">{{ getErrorMessage(account) }}</span>
                      <button
                        class="btn-retry"
                        @click="handleRetrySync(account.puuid)"
                        :disabled="isRetrying"
                      >
                        Retry
                      </button>
                    </div>
                  </template>

                  <!-- Show normal status badge -->
                  <template v-else>
                    <span class="sync-badge" :class="getSyncStatusClass(getAccountSyncStatus(account))">
                      {{ getSyncStatusLabel(getAccountSyncStatus(account)) }}
                    </span>
                  </template>
                </div>
              </div>
              <button class="btn-link-account" @click="showLinkModal = true">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
                  <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z" />
                </svg>
                Link Another Account
              </button>
            </template>

            <!-- No accounts linked - CTA -->
            <template v-else-if="!linkPromptDismissed">
              <div class="link-cta">
                <p class="link-cta-text">Link your Riot account to start tracking your performance and stats.</p>
                <div class="link-cta-actions">
                  <button class="btn-primary" @click="showLinkModal = true">
                    Link Riot Account
                  </button>
                  <button class="btn-ghost-sm" @click="dismissLinkPrompt">
                    Maybe Later
                  </button>
                </div>
              </div>
            </template>

            <!-- Dismissed state -->
            <template v-else>
              <div class="link-dismissed">
                <p class="link-dismissed-text">No Riot accounts linked.</p>
                <button class="btn-link-account" @click="showLinkModal = true">
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
                    <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z" />
                  </svg>
                  Link Account
                </button>
              </div>
            </template>
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

    <!-- Link Riot Account Modal -->
    <LinkRiotAccountModal
      :isOpen="showLinkModal"
      @close="showLinkModal = false"
      @success="handleLinkSuccess"
    />
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { useSyncWebSocket } from '../composables/useSyncWebSocket';
import LinkRiotAccountModal from '../components/LinkRiotAccountModal.vue';

const router = useRouter();
const authStore = useAuthStore();
const { syncProgress, subscribe, resetProgress } = useSyncWebSocket();

const username = computed(() => authStore.username || 'User');
const email = computed(() => authStore.email || '');
const tier = computed(() => authStore.tier || 'free');
const riotAccounts = computed(() => authStore.riotAccounts);
const hasLinkedAccount = computed(() => authStore.hasLinkedAccount);

const showLinkModal = ref(false);
const linkPromptDismissed = ref(localStorage.getItem('linkPromptDismissed') === 'true');
const isRetrying = ref(false);

const tierLabel = computed(() => {
  if (tier.value === 'pro') return 'Pro';
  if (tier.value === 'premium') return 'Premium';
  return 'Free';
});

const tierClass = computed(() => {
  return `tier-${tier.value}`;
});

const firstRiotAccount = computed(() => {
  return riotAccounts.value?.[0] || null;
});

function navigateToSoloDashboard() {
  router.push('/app/solo');
}

// Region labels mapping
const regionLabels = {
  euw1: 'EUW',
  eun1: 'EUNE',
  na1: 'NA',
  kr: 'KR',
  jp1: 'JP',
  br1: 'BR',
  la1: 'LAN',
  la2: 'LAS',
  oc1: 'OCE',
  tr1: 'TR',
  ru: 'RU',
  ph2: 'PH',
  sg2: 'SG',
  th2: 'TH',
  tw2: 'TW',
  vn2: 'VN'
};

function getRegionLabel(region) {
  return regionLabels[region] || region?.toUpperCase() || 'Unknown';
}

// Subscribe to WebSocket updates for all linked accounts
onMounted(() => {
  for (const account of riotAccounts.value) {
    subscribe(account.puuid);
  }
});

// Watch for new accounts and subscribe
watch(riotAccounts, (newAccounts) => {
  for (const account of newAccounts) {
    subscribe(account.puuid);
  }
}, { deep: true });

/**
 * Get the effective sync status for an account (WebSocket or API)
 */
function getAccountSyncStatus(account) {
  const wsProgress = syncProgress.get(account.puuid);
  if (wsProgress && wsProgress.status !== 'idle') {
    return wsProgress.status;
  }
  return account.syncStatus || 'pending';
}

/**
 * Get progress percentage for an account
 */
function getProgressPercent(account) {
  const wsProgress = syncProgress.get(account.puuid);
  if (wsProgress && wsProgress.total > 0) {
    return Math.round((wsProgress.progress / wsProgress.total) * 100);
  }
  return 0;
}

/**
 * Get progress text (e.g., "45 / 100 matches synced")
 */
function getProgressText(account) {
  const wsProgress = syncProgress.get(account.puuid);
  if (wsProgress) {
    return `${wsProgress.progress} / ${wsProgress.total} matches synced`;
  }
  return 'Syncing...';
}

/**
 * Get error message for failed sync
 */
function getErrorMessage(account) {
  const wsProgress = syncProgress.get(account.puuid);
  return wsProgress?.error || 'Sync failed';
}

/**
 * Handle retry sync button click
 */
async function handleRetrySync(puuid) {
  isRetrying.value = true;
  try {
    resetProgress(puuid);
    await authStore.triggerSync(puuid);
  } catch (e) {
    console.error('Failed to trigger sync:', e);
  } finally {
    isRetrying.value = false;
  }
}

function getSyncStatusClass(status) {
  switch (status) {
    case 'completed': return 'sync-completed';
    case 'syncing': return 'sync-syncing';
    case 'pending': return 'sync-pending';
    case 'failed': return 'sync-failed';
    default: return 'sync-pending';
  }
}

function getSyncStatusLabel(status) {
  switch (status) {
    case 'completed': return 'Synced';
    case 'syncing': return 'Syncing...';
    case 'pending': return 'Pending';
    case 'failed': return 'Error';
    default: return 'Pending';
  }
}

function dismissLinkPrompt() {
  linkPromptDismissed.value = true;
  localStorage.setItem('linkPromptDismissed', 'true');
}

function handleLinkSuccess() {
  // Modal will close automatically, accounts will refresh via store
}
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

/* Clickable card (Solo Dashboard) */
.user-card.clickable {
  cursor: pointer;
  transition: all 0.2s ease;
}

.user-card.clickable:hover {
  border-color: var(--color-primary);
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(147, 51, 234, 0.2);
}

.user-card.clickable:active {
  transform: translateY(0);
}

.user-card.clickable .card-header {
  position: relative;
}

.nav-arrow {
  width: 20px;
  height: 20px;
  color: var(--color-text-secondary);
  margin-left: auto;
  transition: transform 0.2s ease;
}

.user-card.clickable:hover .nav-arrow {
  transform: translateX(4px);
  color: var(--color-primary);
}

.solo-summary {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.solo-identity {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.solo-name {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.solo-hint {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin: 0;
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

/* Riot Accounts Card Styles */
.riot-account-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-sm) 0;
  border-bottom: 1px solid var(--color-border);
}

.riot-account-item:last-of-type {
  border-bottom: none;
}

.riot-account-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.riot-account-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.riot-account-region {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.sync-badge {
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
}

.sync-completed {
  background: rgba(34, 197, 94, 0.2);
  color: #22c55e;
}

.sync-syncing {
  background: rgba(59, 130, 246, 0.2);
  color: #3b82f6;
}

.sync-pending {
  background: rgba(245, 158, 11, 0.2);
  color: #f59e0b;
}

.sync-failed {
  background: rgba(239, 68, 68, 0.2);
  color: #ef4444;
}

/* Sync Progress Styles */
.riot-account-status-container {
  display: flex;
  align-items: center;
  min-width: 140px;
  justify-content: flex-end;
}

.sync-progress-container {
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 140px;
}

.sync-progress-bar {
  height: 6px;
  background: var(--color-surface-hover);
  border-radius: 3px;
  overflow: hidden;
}

.sync-progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #3b82f6, #60a5fa);
  border-radius: 3px;
  transition: width 0.3s ease;
}

.sync-progress-text {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  text-align: right;
}

.sync-error-container {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.sync-error-text {
  font-size: var(--font-size-xs);
  color: #ef4444;
  max-width: 100px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.btn-retry {
  padding: 4px 8px;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: #ef4444;
  background: rgba(239, 68, 68, 0.1);
  border: 1px solid rgba(239, 68, 68, 0.3);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-retry:hover:not(:disabled) {
  background: rgba(239, 68, 68, 0.2);
}

.btn-retry:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-link-account {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  background: none;
  border: none;
  color: var(--color-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  padding: var(--spacing-sm) 0;
  transition: opacity 0.2s;
}

.btn-link-account:hover {
  opacity: 0.8;
}

.btn-icon {
  width: 16px;
  height: 16px;
}

.link-cta {
  text-align: center;
  padding: var(--spacing-md) 0;
}

.link-cta-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-lg);
}

.link-cta-actions {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  align-items: center;
}

.btn-primary {
  background: var(--color-primary);
  color: white;
  padding: var(--spacing-sm) var(--spacing-lg);
  border: none;
  border-radius: var(--radius-md);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-primary:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-1px);
}

.btn-ghost-sm {
  background: none;
  border: none;
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
  cursor: pointer;
  padding: var(--spacing-xs);
  transition: color 0.2s;
}

.btn-ghost-sm:hover {
  color: var(--color-text);
}

.link-dismissed {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md) 0;
}

.link-dismissed-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}
</style>

