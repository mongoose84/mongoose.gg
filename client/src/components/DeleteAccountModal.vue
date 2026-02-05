<template>
  <BaseModal
    :isOpen="isOpen"
    title="Delete Account"
    size="md"
    :showCloseButton="!isDeleting"
    :preventClose="isDeleting"
    @close="handleClose"
  >
    <div class="delete-account-content">
      <!-- Warning -->
      <div class="warning-box">
        <svg xmlns="http://www.w3.org/2000/svg" class="warning-icon" viewBox="0 0 24 24" fill="currentColor">
          <path fill-rule="evenodd" d="M9.401 3.003c1.155-2 4.043-2 5.197 0l7.355 12.748c1.154 2-.29 4.5-2.599 4.5H4.645c-2.309 0-3.752-2.5-2.598-4.5L9.4 3.003zM12 8.25a.75.75 0 01.75.75v3.75a.75.75 0 01-1.5 0V9a.75.75 0 01.75-.75zm0 8.25a.75.75 0 100-1.5.75.75 0 000 1.5z" clip-rule="evenodd" />
        </svg>
        <p class="warning-text">
          This action is <strong>permanent</strong> and cannot be undone. 
          All your data, linked Riot accounts, and LP history will be deleted.
        </p>
      </div>

      <!-- Confirmation input -->
      <div class="confirmation-section">
        <label for="confirm-delete" class="confirmation-label">
          Type <strong>DELETE</strong> to confirm:
        </label>
        <BaseInput
          id="confirm-delete"
          v-model="confirmText"
          placeholder="DELETE"
          :disabled="isDeleting"
          autocomplete="off"
        />
      </div>

      <!-- Password input -->
      <div class="password-section">
        <BaseInput
          id="password"
          v-model="password"
          type="password"
          label="Enter your password"
          placeholder="Your password"
          :error="error"
          :disabled="isDeleting"
          autocomplete="current-password"
          required
        />
      </div>
    </div>

    <template #footer>
      <BaseButton
        variant="secondary"
        :disabled="isDeleting"
        @click="handleClose"
      >
        Cancel
      </BaseButton>
      <BaseButton
        variant="destructive"
        :disabled="!canDelete"
        :loading="isDeleting"
        @click="handleDelete"
      >
        {{ isDeleting ? 'Deleting...' : 'Delete My Account' }}
      </BaseButton>
    </template>
  </BaseModal>
</template>

<script setup>
import { ref, computed } from 'vue'
import BaseModal from '@/components/base/BaseModal.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseInput from '@/components/base/BaseInput.vue'
import { deleteAccount } from '@/services/authApi'

const props = defineProps({
  isOpen: {
    type: Boolean,
    required: true
  }
})

const emit = defineEmits(['close', 'deleted'])

const confirmText = ref('')
const password = ref('')
const isDeleting = ref(false)
const error = ref(null)

const canDelete = computed(() => {
  return confirmText.value === 'DELETE' && password.value.length > 0 && !isDeleting.value
})

function handleClose() {
  if (!isDeleting.value) {
    resetState()
    emit('close')
  }
}

function resetState() {
  confirmText.value = ''
  password.value = ''
  error.value = null
  isDeleting.value = false
}

async function handleDelete() {
  if (!canDelete.value) return

  isDeleting.value = true
  error.value = null

  try {
    await deleteAccount(password.value)
    emit('deleted')
    handleClose()
  } catch (e) {
    if (e.code === 'INVALID_PASSWORD') {
      error.value = 'Invalid password. Please try again.'
    } else {
      error.value = e.message || 'Failed to delete account'
    }
  } finally {
    isDeleting.value = false
  }
}
</script>

<style scoped>
.delete-account-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.warning-box {
  display: flex;
  gap: var(--spacing-md);
  padding: var(--spacing-md);
  background: var(--color-error-soft);
  border: 1px solid var(--color-error);
  border-radius: var(--radius-md);
}

.warning-icon {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  color: var(--color-error);
}

.warning-text {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text);
  line-height: 1.5;
}

.confirmation-section,
.password-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.confirmation-label {
  font-size: var(--font-size-sm);
  color: var(--color-text);
}
</style>

