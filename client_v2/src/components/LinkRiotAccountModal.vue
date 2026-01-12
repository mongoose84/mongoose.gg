<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="isOpen" class="modal-overlay" @click.self="handleClose">
        <div class="modal-content">
          <div class="modal-header">
            <h2 class="modal-title">Link Riot Account</h2>
            <button class="close-btn" @click="handleClose" :disabled="isSubmitting">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 011.06 0L12 10.94l5.47-5.47a.75.75 0 111.06 1.06L13.06 12l5.47 5.47a.75.75 0 11-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 01-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 010-1.06z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>

          <form @submit.prevent="handleSubmit" class="modal-form">
            <div v-if="errorMessage" class="alert-error">
              {{ errorMessage }}
            </div>

            <div class="form-group">
              <label class="form-label" for="gameName">Game Name</label>
              <input
                id="gameName"
                v-model="formData.gameName"
                type="text"
                class="form-input"
                :class="{ 'form-input-error': errors.gameName }"
                placeholder="e.g. Faker"
                :disabled="isSubmitting"
                maxlength="100"
              />
              <span v-if="errors.gameName" class="form-error-text">{{ errors.gameName }}</span>
            </div>

            <div class="form-group">
              <label class="form-label" for="tagLine">Tag Line</label>
              <input
                id="tagLine"
                v-model="formData.tagLine"
                type="text"
                class="form-input"
                :class="{ 'form-input-error': errors.tagLine }"
                placeholder="e.g. NA1"
                :disabled="isSubmitting"
                minlength="3"
                maxlength="5"
              />
              <span v-if="errors.tagLine" class="form-error-text">{{ errors.tagLine }}</span>
            </div>

            <div class="form-group">
              <label class="form-label" for="region">Region</label>
              <select
                id="region"
                v-model="formData.region"
                class="form-input form-select"
                :class="{ 'form-input-error': errors.region }"
                :disabled="isSubmitting"
              >
                <option value="" disabled>Select a region</option>
                <option v-for="r in regions" :key="r.value" :value="r.value">
                  {{ r.label }}
                </option>
              </select>
              <span v-if="errors.region" class="form-error-text">{{ errors.region }}</span>
            </div>

            <div class="modal-actions">
              <button type="button" class="btn-ghost" @click="handleClose" :disabled="isSubmitting">
                Cancel
              </button>
              <button type="submit" class="btn-primary" :disabled="isSubmitting || !isFormValid">
                <span v-if="isSubmitting" class="loading-spinner"></span>
                {{ isSubmitting ? 'Linking...' : 'Link Account' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'

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
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: var(--spacing-xl);
}

.modal-content {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  width: 100%;
  max-width: 400px;
  backdrop-filter: blur(10px);
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.modal-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}

.close-btn {
  background: none;
  border: none;
  padding: var(--spacing-xs);
  cursor: pointer;
  color: var(--color-text-secondary);
  transition: color 0.2s;
}

.close-btn:hover {
  color: var(--color-text);
}

.close-btn svg {
  width: 20px;
  height: 20px;
}

.modal-form {
  padding: var(--spacing-lg);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.form-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
}

.form-input {
  padding: var(--spacing-md);
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: var(--font-size-md);
  color: var(--color-text);
  transition: all 0.2s;
}

.form-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.form-input::placeholder {
  color: var(--color-text-secondary);
}

.form-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.form-input-error {
  border-color: #ef4444;
}

.form-select {
  cursor: pointer;
}

.form-select option {
  background: var(--color-bg);
  color: var(--color-text);
}

.form-error-text {
  font-size: var(--font-size-xs);
  color: #ef4444;
}

.alert-error {
  padding: var(--spacing-md);
  background: rgba(239, 68, 68, 0.1);
  border: 1px solid rgba(239, 68, 68, 0.3);
  border-radius: var(--radius-md);
  color: #ef4444;
  font-size: var(--font-size-sm);
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-sm);
  margin-top: var(--spacing-md);
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
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.btn-primary:hover:not(:disabled) {
  box-shadow: var(--shadow-md);
  transform: translateY(-1px);
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-ghost {
  background: transparent;
  color: var(--color-text-secondary);
  padding: var(--spacing-sm) var(--spacing-lg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-weight: var(--font-weight-medium);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-ghost:hover:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.btn-ghost:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.loading-spinner {
  width: 14px;
  height: 14px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* Modal transitions */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-active .modal-content,
.modal-leave-active .modal-content {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-from .modal-content,
.modal-leave-to .modal-content {
  transform: scale(0.95);
  opacity: 0;
}
</style>

