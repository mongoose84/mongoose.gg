<template>
  <BaseModal
    :is-open="isOpen"
    title="Link Riot Account"
    :prevent-close="isSubmitting"
    size="md"
    @close="handleClose"
    data-testid="modal-overlay"
  >
    <div v-if="isAtAccountLimit" class="bg-background-surface border border-border rounded-lg p-xl flex flex-col items-start gap-sm" data-testid="account-limit-upgrade-prompt">
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
        data-testid="account-limit-upgrade-button"
      >
        Upgrade to Pro
      </BaseButton>
    </div>

    <form v-else @submit.prevent="handleSubmit" class="flex flex-col gap-md">
      <!-- Error alert -->
      <div
        v-if="errorMessage"
        class="p-md bg-error-soft border border-error-border rounded-md text-error text-sm"
        role="alert"
      >
        {{ errorMessage }}
      </div>

      <BaseInput
        id="gameName"
        v-model="formData.gameName"
        label="Game Name"
        placeholder="e.g. Faker"
        :error="errors.gameName"
        :disabled="isSubmitting"
        maxlength="100"
      />

      <BaseInput
        id="tagLine"
        v-model="formData.tagLine"
        label="Tag Line"
        placeholder="e.g. NA1"
        :error="errors.tagLine"
        :disabled="isSubmitting"
        minlength="3"
        maxlength="5"
      />

      <!-- Region select (keeping native select for now) -->
      <div class="flex flex-col gap-xs">
        <label class="text-sm font-medium text-text" for="region">Region</label>
        <select
          id="region"
          v-model="formData.region"
          class="select-field"
          :class="{ 'select-field--error': errors.region }"
          :disabled="isSubmitting"
        >
          <option value="" disabled>Select a region</option>
          <option v-for="r in regions" :key="r.value" :value="r.value">
            {{ r.label }}
          </option>
        </select>
        <span v-if="errors.region" class="text-xs text-error">{{ errors.region }}</span>
      </div>

      <div class="flex justify-end gap-sm mt-md">
        <BaseButton
          type="button"
          variant="secondary"
          :disabled="isSubmitting"
          data-testid="cancel-btn"
          @click="handleClose"
        >
          Cancel
        </BaseButton>
        <BaseButton
          type="submit"
          variant="primary"
          :loading="isSubmitting"
          :disabled="!isFormValid"
        >
          {{ isSubmitting ? 'Linking...' : 'Link Account' }}
        </BaseButton>
      </div>
    </form>
  </BaseModal>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { LockClosedIcon } from '@heroicons/vue/24/outline'
import { useAuthStore } from '../stores/authStore'
import { BaseModal, BaseInput, BaseButton } from '@/components/base'

const props = defineProps({
  isOpen: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['close', 'success'])

const authStore = useAuthStore()

const regions = [
  { value: 'euw1', label: 'Europe West (EUW)' },
  { value: 'eun1', label: 'Europe Nordic & East (EUNE)' },
  { value: 'na1', label: 'North America (NA)' },
  { value: 'kr', label: 'Korea (KR)' },
  { value: 'jp1', label: 'Japan (JP)' },
  { value: 'br1', label: 'Brazil (BR)' },
  { value: 'la1', label: 'Latin America North (LAN)' },
  { value: 'la2', label: 'Latin America South (LAS)' },
  { value: 'oc1', label: 'Oceania (OCE)' },
  { value: 'tr1', label: 'Turkey (TR)' },
  { value: 'ru', label: 'Russia (RU)' },
  { value: 'ph2', label: 'Philippines (PH)' },
  { value: 'sg2', label: 'Singapore (SG)' },
  { value: 'th2', label: 'Thailand (TH)' },
  { value: 'tw2', label: 'Taiwan (TW)' },
  { value: 'vn2', label: 'Vietnam (VN)' }
]

const formData = reactive({
  gameName: '',
  tagLine: '',
  region: ''
})

const errors = reactive({
  gameName: '',
  tagLine: '',
  region: ''
})

const isSubmitting = ref(false)
const errorMessage = ref('')

const normalizedTier = computed(() => {
  if (typeof authStore.normalizedTier === 'string') {
    return authStore.normalizedTier
  }

  const rawTier = authStore.tier
  if (typeof rawTier !== 'string') return 'free'
  return rawTier.trim().toLowerCase() || 'free'
})

const linkedAccountCount = computed(() => {
  if (Array.isArray(authStore.riotAccounts)) {
    return authStore.riotAccounts.length
  }
  return 0
})

const isAtAccountLimit = computed(() => {
  if (typeof authStore.hasReachedRiotAccountLimit === 'boolean') {
    return authStore.hasReachedRiotAccountLimit
  }

  return normalizedTier.value === 'free' && linkedAccountCount.value >= 1
})

// Validation
const isFormValid = computed(() => {
  return formData.gameName.trim() && formData.tagLine.trim() && formData.region
})

// Reset form when modal opens
watch(() => props.isOpen, (newVal) => {
  if (newVal) {
    formData.gameName = ''
    formData.tagLine = ''
    formData.region = ''
    errors.gameName = ''
    errors.tagLine = ''
    errors.region = ''
    errorMessage.value = ''
  }
})

function validateForm() {
  let valid = true
  errors.gameName = ''
  errors.tagLine = ''
  errors.region = ''

  if (!formData.gameName.trim()) {
    errors.gameName = 'Game name is required'
    valid = false
  } else if (formData.gameName.length > 100) {
    errors.gameName = 'Game name must be 100 characters or less'
    valid = false
  }

  if (!formData.tagLine.trim()) {
    errors.tagLine = 'Tag line is required'
    valid = false
  } else if (!/^[a-zA-Z0-9]+$/.test(formData.tagLine)) {
    errors.tagLine = 'Tag line must contain only letters and numbers'
    valid = false
  } else if (formData.tagLine.length < 3 || formData.tagLine.length > 5) {
    errors.tagLine = 'Tag line must be 3-5 characters'
    valid = false
  }

  if (!formData.region) {
    errors.region = 'Please select a region'
    valid = false
  }

  return valid
}

async function handleSubmit() {
  if (isAtAccountLimit.value) {
    errorMessage.value = 'Free tier is limited to 1 linked account. Upgrade to Pro for unlimited accounts.'
    return
  }

  if (!validateForm()) return

  isSubmitting.value = true
  errorMessage.value = ''

  try {
    await authStore.linkRiotAccount({
      gameName: formData.gameName.trim(),
      tagLine: formData.tagLine.trim(),
      region: formData.region
    })
    emit('success')
    emit('close')
  } catch (e) {
    // Map error codes to user-friendly messages
    if (e.code === 'RIOT_ACCOUNT_NOT_FOUND') {
      errorMessage.value = 'Riot account not found. Please check your Game Name and Tag Line.'
    } else if (e.code === 'ACCOUNT_ALREADY_LINKED') {
      errorMessage.value = 'This Riot account is already linked to another user.'
    } else if (e.code === 'ACCOUNT_LIMIT_REACHED') {
      errorMessage.value = 'Free tier is limited to 1 linked account. Upgrade to Pro for unlimited accounts.'
    } else {
      errorMessage.value = e.message || 'Failed to link account. Please try again.'
    }
  } finally {
    isSubmitting.value = false
  }
}

function handleClose() {
  if (!isSubmitting.value) {
    emit('close')
  }
}
</script>

<style scoped>
/* Select field styles (until we have a BaseSelect component) */
.select-field {
  width: 100%;
  padding: var(--spacing-md);
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: var(--font-size-md);
  color: var(--color-text);
  cursor: pointer;
  transition: all 0.2s ease;
}

.select-field:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.select-field:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.select-field--error {
  border-color: var(--color-error);
}

.select-field--error:focus {
  box-shadow: 0 0 0 3px var(--color-error-soft);
}

.select-field option {
  background: var(--color-bg);
  color: var(--color-text);
}
</style>

