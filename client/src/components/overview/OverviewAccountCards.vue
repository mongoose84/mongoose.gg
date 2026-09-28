<template>
  <section class="overview-account-cards" data-testid="overview-account-cards">
    <div class="header-row">
      <h1 class="section-title">Your accounts</h1>
    </div>
    <div class="account-cards-container">
      <div
        v-for="account in normalizedAccounts"
        :key="account.accountId"
        :data-testid="`account-card-${account.accountId}`"
        :class="['account-card', { 'account-card--active': account.isActive }]"
      >
        <div class="card-top-meta">
          <span v-if="account.isPrimary" class="primary-chip">Primary</span>
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
            <span v-else class="account-avatar-fallback" aria-hidden="true">
              <BaseIcon name="user" :size="24" />
            </span>
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

            <!-- Flex Rank -->
            <div class="rank-line">
              <span class="rank-label">Flex</span>
              <span class="rank-separator" aria-hidden="true">·</span>
              <span class="rank-value">{{ account.flexRankDisplay }}</span>
            </div>

            <!-- Solo Rank -->
            <div class="rank-line">
              <span class="rank-label">Solo</span>
              <span class="rank-separator" aria-hidden="true">·</span>
              <span class="rank-value">{{ account.soloRankDisplay }}</span>
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
import BaseIcon from '@/components/base/BaseIcon.vue'

const props = defineProps({
  accounts: {
    type: Array,
    required: true
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

function getLinkedAccountMatch(account) {
  const accountId = account?.accountId
  const puuid = account?.puuid

  return (props.linkedAccounts || []).find((linked) => {
    if (accountId && linked?.accountId && linked.accountId === accountId) {
      return true
    }

    if (puuid && linked?.puuid && linked.puuid === puuid) {
      return true
    }

    return false
  }) || null
}

function formatRankDisplay(tier, rank, lp) {
  if (!tier || !rank) {
    return 'Unranked'
  }
  
  const formattedTier = tier.charAt(0).toUpperCase() + tier.slice(1).toLowerCase()
  const lpDisplay = (lp !== null && lp !== undefined) ? ` · ${lp} LP` : ''
  return `${formattedTier} ${rank}${lpDisplay}`
}

const normalizedAccounts = computed(() => {
  return (props.accounts || []).map((account, index) => {
    const accountId = account.accountId || account.puuid || account.id || `account-${index}`
    const gameName = account.gameName || account.summonerName || account.name || 'Unknown Account'
    const tagLine = account.tagLine || account.tag || ''
    const isActive = Boolean(
      props.activeAccountPuuid &&
      (accountId === props.activeAccountPuuid || account.puuid === props.activeAccountPuuid)
    )
    const linkedAccount = getLinkedAccountMatch(account)
    const isPrimary = Boolean(account.isPrimary ?? linkedAccount?.isPrimary)
    const resolvedProfileIconId = account.profileIconId ?? linkedAccount?.profileIconId ?? null
    const profileIconUrl = account.profileIconUrl || (resolvedProfileIconId ? getProfileIconUrl(resolvedProfileIconId) : null)
    const summonerLevel = account.summonerLevel ?? linkedAccount?.summonerLevel ?? null
    
    // Resolve rank data from linked account
    const flexTier = account.flexTier ?? linkedAccount?.flexTier ?? null
    const flexRank = account.flexRank ?? linkedAccount?.flexRank ?? null
    const flexLp = account.flexLp ?? linkedAccount?.flexLp ?? null
    const soloTier = account.soloTier ?? linkedAccount?.soloTier ?? null
    const soloRank = account.soloRank ?? linkedAccount?.soloRank ?? null
    const soloLp = account.soloLp ?? linkedAccount?.soloLp ?? null
    
    const flexRankDisplay = formatRankDisplay(flexTier, flexRank, flexLp)
    const soloRankDisplay = formatRankDisplay(soloTier, soloRank, soloLp)

    return {
      ...account,
      accountId,
      gameName,
      tagLine,
      isActive,
      isPrimary,
      profileIconUrl,
      summonerLevel,
      flexRankDisplay,
      soloRankDisplay
    }
  })
})
</script>

<style scoped>
/* Overall mode: one card per linked account, in place of the champion hero */
.overview-account-cards {
  width: 100%;
}

.header-row {
  margin-bottom: 1.25rem;
}

.section-title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 2.75rem;
  font-weight: 600;
  line-height: 1.12;
  letter-spacing: -0.01em;
  color: var(--color-text);
}

.account-cards-container {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1.25rem;
  width: 100%;
}

.account-card {
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 0.75rem;
  min-height: 8.25rem;
  padding: 1.75rem;
  background: var(--color-surface);
  border: 2px solid transparent;
  border-radius: 1.5rem;
  text-align: left;
}

.account-card.account-card--active {
  border-color: var(--color-primary-accent);
}

.card-top-meta {
  position: absolute;
  top: 1rem;
  right: 1rem;
  display: flex;
  gap: 0.5rem;
}

.primary-chip {
  display: inline-flex;
  align-items: center;
  height: 1.5rem;
  padding: 0 0.625rem;
  border-radius: 999px;
  background: var(--color-primary-soft);
  color: var(--color-positive-text-strong);
  font-size: 0.75rem;
  font-weight: 700;
  line-height: 1;
}

.account-main-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  min-width: 0;
}

.account-avatar {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  width: 3.5rem;
  height: 3.5rem;
  border-radius: 999px;
  background: var(--color-surface-selected);
}

.account-avatar-image {
  width: 100%;
  height: 100%;
  border-radius: 999px;
  object-fit: cover;
}

.account-avatar-fallback {
  display: inline-flex;
  color: var(--color-text-secondary);
}

.level-badge {
  position: absolute;
  bottom: -0.25rem;
  right: -0.375rem;
  min-width: 1.75rem;
  padding: 0.25rem 0.375rem;
  border-radius: 999px;
  background: var(--color-surface-selected);
  color: var(--color-text);
  font-size: 0.75rem;
  font-weight: 700;
  line-height: 1;
  text-align: center;
  font-variant-numeric: tabular-nums;
}

.account-meta {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.account-name {
  display: flex;
  align-items: baseline;
  flex-wrap: wrap;
  gap: 0.25rem;
  min-width: 0;
}

.game-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-family: var(--font-display);
  font-size: 1.25rem;
  font-weight: 600;
  line-height: 1.3;
  color: var(--color-text);
}

.tag-line {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

.rank-line {
  display: flex;
  align-items: center;
  gap: 0.375rem;
  font-size: 0.875rem;
  line-height: 1.5;
}

.rank-label,
.rank-separator {
  color: var(--color-text-secondary);
}

.rank-value {
  color: var(--color-ink-soft);
  font-variant-numeric: tabular-nums;
}

@media (max-width: 899px) {
  .account-cards-container {
    grid-template-columns: minmax(0, 1fr);
  }

  .section-title {
    font-size: 2rem;
  }
}
</style>
