<template>
  <section class="flex flex-col gap-md" data-testid="linked-accounts-section">
    <div class="flex items-baseline justify-between gap-sm flex-wrap">
      <h2 class="text-lg font-semibold text-text tracking-tight">
        Linked Riot Accounts ({{ linkedAccounts.length }} linked — {{ tierLabel }} tier)
      </h2>
      <span class="text-xs text-text-secondary">Manage your linked accounts</span>
    </div>

    <div class="bg-background-surface border border-border rounded-lg p-xl">
      <ul v-if="linkedAccounts.length > 0" class="flex flex-col gap-md" data-testid="linked-accounts-list">
        <LinkedAccountRow
          v-for="account in linkedAccounts"
          :key="account.puuid"
          :account="account"
          :is-syncing="syncingPuuids.has(account.puuid)"
          :is-busy="busyPuuid === account.puuid"
          @sync="handleSync"
          @set-primary="handleSetPrimary"
          @remove="openRemoveConfirm"
        />
      </ul>

      <div v-else class="text-sm text-text-secondary" data-testid="linked-accounts-empty">
        No linked Riot accounts yet.
      </div>

      <div class="mt-lg">
        <BaseButton
          v-if="showLinkButton"
          variant="primary"
          size="md"
          data-testid="link-another-account-button"
          @click="showLinkModal = true"
        >
          Link Another Account
        </BaseButton>

        <div
          v-else
          class="bg-background-surface border border-border rounded-lg p-xl flex flex-col items-start gap-sm"
          data-testid="upgrade-prompt"
        >
          <div class="flex items-center gap-sm">
            <LockClosedIcon class="w-5 h-5 text-text-secondary" aria-hidden="true" />
            <h3 class="text-sm font-semibold text-text">Link Unlimited Accounts</h3>
          </div>
          <p class="text-xs text-text-secondary mt-xs">
            Free tier supports 1 linked account. Upgrade to Pro to link all your accounts and view combined stats across them.
          </p>
          <BaseButton
            to="/#pricing"
            variant="primary"
            size="sm"
            class="mt-md"
            data-testid="upgrade-to-pro-button"
          >
            Upgrade to Pro
          </BaseButton>
        </div>
      </div>
    </div>

    <div class="sr-only" aria-live="polite" data-testid="linked-accounts-live-region">{{ liveMessage }}</div>

    <BaseModal
      :is-open="!!pendingRemoveAccount"
      title="Remove Riot Account"
      size="sm"
      :prevent-close="isRemoving"
      @close="closeRemoveConfirm"
    >
      <div class="flex flex-col gap-md">
        <p class="text-sm text-text">
          Remove {{ pendingRemoveLabel }}?
        </p>
        <div class="flex justify-end gap-sm">
          <BaseButton
            variant="secondary"
            size="sm"
            :disabled="isRemoving"
            data-testid="cancel-remove-account"
            @click="closeRemoveConfirm"
          >
            Cancel
          </BaseButton>
          <BaseButton
            variant="destructive"
            size="sm"
            :loading="isRemoving"
            :disabled="isRemoving"
            data-testid="confirm-remove-account"
            @click="confirmRemove"
          >
            Remove
          </BaseButton>
        </div>
      </div>
    </BaseModal>

    <LinkRiotAccountModal
      :is-open="showLinkModal"
      @close="showLinkModal = false"
      @success="handleLinkSuccess"
    />
  </section>
</template>

<script setup>
import { computed, ref } from 'vue'
import { LockClosedIcon } from '@heroicons/vue/24/outline'
import { useAuthStore } from '@/stores/authStore'
import { BaseButton, BaseModal } from '@/components/base'
import LinkRiotAccountModal from '@/components/LinkRiotAccountModal.vue'
import LinkedAccountRow from './LinkedAccountRow.vue'

const authStore = useAuthStore()

const showLinkModal = ref(false)
const busyPuuid = ref(null)
const syncingPuuids = ref(new Set())
const pendingRemoveAccount = ref(null)
const isRemoving = ref(false)
const liveMessage = ref('')

const linkedAccounts = computed(() => authStore.riotAccounts)
const normalizedTier = computed(() => authStore.normalizedTier)

const tierLabel = computed(() => {
  const value = normalizedTier.value
  if (value === 'pro') return 'Pro'
  if (value === 'premium') return 'Premium'
  return 'Free'
})

const showLinkButton = computed(() => {
  return !authStore.hasReachedRiotAccountLimit
})

const pendingRemoveLabel = computed(() => {
  if (!pendingRemoveAccount.value) return ''
  return `${pendingRemoveAccount.value.gameName}#${pendingRemoveAccount.value.tagLine}`
})

async function handleSetPrimary(account) {
  busyPuuid.value = account.puuid
  try {
    await authStore.setPrimary(account.puuid)
    liveMessage.value = `${account.gameName} set as primary account.`
  } catch (error) {
    liveMessage.value = error?.message || 'Failed to set primary account.'
  } finally {
    busyPuuid.value = null
  }
}

async function handleSync(account) {
  const next = new Set(syncingPuuids.value)
  next.add(account.puuid)
  syncingPuuids.value = next

  try {
    await authStore.triggerSync(account.puuid)
    liveMessage.value = `Sync started for ${account.gameName}.`
  } catch (error) {
    liveMessage.value = error?.message || 'Failed to start sync.'
  } finally {
    const updated = new Set(syncingPuuids.value)
    updated.delete(account.puuid)
    syncingPuuids.value = updated
  }
}

function openRemoveConfirm(account) {
  pendingRemoveAccount.value = account
}

function closeRemoveConfirm() {
  if (isRemoving.value) return
  pendingRemoveAccount.value = null
}

async function confirmRemove() {
  if (!pendingRemoveAccount.value) return

  const account = pendingRemoveAccount.value
  isRemoving.value = true
  busyPuuid.value = account.puuid

  try {
    await authStore.unlinkRiotAccount(account.puuid)
    pendingRemoveAccount.value = null
    liveMessage.value = `${account.gameName} removed.`
  } catch (error) {
    liveMessage.value = error?.message || 'Failed to remove account.'
  } finally {
    isRemoving.value = false
    busyPuuid.value = null
  }
}

async function handleLinkSuccess() {
  await authStore.refreshUser()
  liveMessage.value = 'Riot account linked.'
}
</script>
