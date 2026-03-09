<template>
  <section class="overview-account-cards" data-testid="overview-account-cards">
    <div class="header-row">
      <h2 class="section-title">Your Accounts</h2>
    </div>
    <div class="account-cards-container">
      <div
        v-for="account in normalizedAccounts"
        :key="account.accountId"
        :data-testid="`account-card-${account.accountId}`"
        :class="['account-card', { 'account-card--active': account.isActive }]"
      >
        <div class="card-top-meta">
          <span v-if="account.isActive" class="primary-chip">Primary</span>
        </div>

        <div class="account-main-row">
          <div class="account-avatar">
            <img
              v-if="account.profileIconUrl && !hasIconError(account.accountId)"
              :src="account.profileIconUrl"
              :alt="`${account.gameName} profile icon`"
              class="account-avatar-image"
              @error="handleIconError(account.accountId)"
            />
            <svg
              v-else
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              fill="currentColor"
              class="account-avatar-fallback"
              aria-hidden="true"
            >
              <path
                fill-rule="evenodd"
                d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z"
                clip-rule="evenodd"
              />
            </svg>
            <span
              v-if="account.summonerLevel"
              class="level-badge"
            >{{ account.summonerLevel }}</span>
          </div>

          <div class="account-meta">
            <!-- Game Name + Tag -->
            <div class="account-name">
              <span class="game-name">{{ account.gameName }}</span>
              <span v-if="account.tagLine" class="tag-line">#{{ account.tagLine }}</span>
            </div>

            <!-- Rank -->
            <div class="account-region-rank">
              <span v-if="account.rank" class="rank">{{ account.rank }}</span>
              <span v-if="account.rank && account.lp !== null && account.lp !== undefined" class="separator">·</span>
              <span v-if="account.lp !== null && account.lp !== undefined" class="lp-value">{{ account.lp }} LP</span>
            </div>
          </div>
        </div>

      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, ref } from 'vue'
import { getProfileIconUrl } from '@/utils/leagueAssets'

const props = defineProps({
  accounts: {
    type: Array,
    required: true,
    default: () => []
  },
  linkedAccounts: {
    type: Array,
    default: () => []
  },
  activeAccountPuuid: {
    type: String,
    default: null
  }
})

const emit = defineEmits(['select'])

const iconErrorsByAccount = ref({})

function hasIconError(accountId) {
  return Boolean(iconErrorsByAccount.value[accountId])
}

function handleIconError(accountId) {
  iconErrorsByAccount.value = {
    ...iconErrorsByAccount.value,
    [accountId]: true
  }
}

function normalizeText(value) {
  return typeof value === 'string' ? value.trim().toLowerCase() : ''
}

function getLinkedAccountMatch(account) {
  const accountId = account?.accountId
  const puuid = account?.puuid
  const gameName = normalizeText(account?.gameName || account?.summonerName || account?.name)
  const tagLine = normalizeText(account?.tagLine || account?.tag)

  return (props.linkedAccounts || []).find((linked) => {
    if (accountId && linked?.accountId && linked.accountId === accountId) {
      return true
    }

    if (puuid && linked?.puuid && linked.puuid === puuid) {
      return true
    }

    const linkedGameName = normalizeText(linked?.gameName || linked?.summonerName || linked?.name)
    const linkedTagLine = normalizeText(linked?.tagLine || linked?.tag)
    return gameName && tagLine && linkedGameName === gameName && linkedTagLine === tagLine
  }) || null
}

const normalizedAccounts = computed(() => {
  return (props.accounts || []).map((account, index) => {
    const accountId = account.accountId || account.puuid || account.id || `account-${index}`
    const gameName = account.gameName || account.summonerName || account.name || 'Unknown Account'
    const tagLine = account.tagLine || account.tag || ''
    const isActive = Boolean(props.activeAccountPuuid && account.puuid === props.activeAccountPuuid)
    const linkedAccount = getLinkedAccountMatch(account)
    const resolvedProfileIconId = account.profileIconId ?? linkedAccount?.profileIconId ?? null
    const profileIconUrl = account.profileIconUrl || (resolvedProfileIconId ? getProfileIconUrl(resolvedProfileIconId) : null)
    const summonerLevel = account.summonerLevel ?? linkedAccount?.summonerLevel ?? null

    return {
      ...account,
      accountId,
      gameName,
      tagLine,
      isActive,
      profileIconUrl,
      summonerLevel
    }
  })
})
</script>

<style scoped>
.overview-account-cards {
  width: 100%;
}

.header-row {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
}

.section-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.account-cards-container {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: var(--spacing-md);
  width: 100%;
}

.account-card {
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  min-height: 132px;
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  text-align: left;
  box-shadow: var(--shadow-sm);
  transition: transform 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease;
}

.account-card::before {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  opacity: 0.8;
  transition: opacity 0.2s ease;
}

.account-card:hover {
  transform: translateY(-2px);
}

.account-card {
  background: linear-gradient(160deg,
    rgba(255, 255, 255, 0.045) 0%,
    var(--color-surface) 65%);
}

.account-card::before {
  background: linear-gradient(180deg,
    rgba(255, 255, 255, 0.08) 0%,
    rgba(255, 255, 255, 0) 45%);
}

.account-card:hover {
  border-color: rgba(109, 40, 217, 0.28);
}

.account-card.account-card--active {
  border-color: rgba(109, 40, 217, 0.45);
  box-shadow: var(--shadow-md);
}

.account-card.account-card--active::after {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: var(--color-primary-light);
}

.card-top-meta {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: var(--spacing-xs);
}

.primary-chip {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-sm);
  padding: 2px 8px;
  font-size: 11px;
  line-height: 1.2;
  letter-spacing: 0.02em;
}

.primary-chip {
  color: #d8b4fe;
  border: 1px solid rgba(168, 85, 247, 0.4);
  background: rgba(168, 85, 247, 0.14);
}

.account-main-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  min-width: 0;
}

.account-avatar {
  position: relative;
  width: 52px;
  height: 52px;
  border-radius: 50%;
  overflow: visible;
  background: var(--color-surface);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border: 2px solid var(--color-primary);
}

.account-avatar-image {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  object-fit: cover;
}

.account-avatar-fallback {
  width: 24px;
  height: 24px;
  color: var(--color-text-secondary);
  z-index: 1;
}.level-badge {
  position: absolute;
  bottom: -2px;
  right: -2px;
  background: var(--color-primary);
  color: white;
  font-weight: var(--font-weight-bold);
  font-size: 11px;
  line-height: 1;
  padding: 3px 6px;
  min-width: 24px;
  text-align: center;
  border-radius: 10px;
}

.account-meta {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.account-name {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: var(--spacing-xs);
  min-width: 0;
}

.game-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tag-line {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.account-region-rank {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.separator {
  color: var(--color-text-secondary);
}

.rank {
  font-weight: var(--font-weight-medium);
}

.rank-muted {
  color: var(--color-text-secondary);
}

/* Mobile responsive */
@media (max-width: 640px) {
  .header-row {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-xs);
  }

  .account-card {
    padding: var(--spacing-md);
  }

  .account-card:hover {
    transform: none;
  }

  .account-avatar {
    width: 48px;
    height: 48px;
  }

  .game-name {
    font-size: var(--font-size-xs);
  }
}
</style>
