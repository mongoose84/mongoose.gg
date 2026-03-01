<template>
  <li class="bg-background-surface border border-border rounded-lg p-xl" data-testid="linked-account-row">
    <div class="flex items-start justify-between gap-md">
      <div class="min-w-0">
        <div class="flex items-center gap-sm flex-wrap">
          <span class="text-sm font-semibold text-text" data-testid="account-name">{{ accountLabel }}</span>
          <span class="text-xs text-text-secondary">{{ regionLabel }}</span>
          <span
            v-if="account.isPrimary"
            class="text-xs px-2 py-0.5 rounded-sm bg-primary-soft text-primary font-semibold"
            data-testid="primary-badge"
          >
            Primary
          </span>
        </div>
        <div class="text-xs text-text-secondary mt-xs">
          Last synced: {{ lastSyncedLabel }}
        </div>
      </div>

      <div class="flex items-center gap-xs flex-wrap justify-end">

         <BaseButton
          v-if="!account.isPrimary"
          variant="ghost"
          size="sm"
          :disabled="isBusy"
          :aria-label="`Set ${accountLabel} as primary account`"
          data-testid="set-primary-button"
          @click="$emit('set-primary', account)"
        >
          Set as Primary
        </BaseButton>

        <BaseButton
          variant="ghost"
          size="sm"
          :loading="isSyncing"
          :disabled="isBusy"
          :aria-label="`Sync ${accountLabel}`"
          data-testid="sync-button"
          @click="$emit('sync', account)"
        >
          Sync
        </BaseButton>

        <BaseButton
          variant="ghost"
          size="sm"
          class="text-error"
          :disabled="isBusy"
          :aria-label="`Remove ${accountLabel}`"
          data-testid="remove-button"
          @click="$emit('remove', account)"
        >
          Remove
        </BaseButton>
      </div>
    </div>
  </li>
</template>

<script setup>
import { computed } from 'vue'
import { BaseButton } from '@/components/base'
import { formatRelativeTime } from '@/utils/formatters'

const props = defineProps({
  account: {
    type: Object,
    required: true
  },
  isSyncing: {
    type: Boolean,
    default: false
  },
  isBusy: {
    type: Boolean,
    default: false
  }
})

defineEmits(['sync', 'set-primary', 'remove'])

const accountLabel = computed(() => `${props.account.gameName}#${props.account.tagLine}`)

const regionLabel = computed(() => (props.account.region || '').toUpperCase())

const lastSyncedLabel = computed(() => {
  if (!props.account.lastSyncAt) return 'Never'
  const timestamp = new Date(props.account.lastSyncAt).getTime()
  if (Number.isNaN(timestamp)) return 'Unknown'
  return formatRelativeTime(timestamp)
})
</script>
