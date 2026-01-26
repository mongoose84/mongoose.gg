<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="isOpen"
        class="fixed inset-0 bg-[rgba(0,0,0,0.8)] flex items-center justify-center z-[1000] p-xl"
        @click.self="handleClose"
        data-testid="modal-overlay"
      >
        <div class="bg-background-surface border border-border rounded-lg w-full max-w-[400px] backdrop-blur-[10px]" data-testid="modal-content">
          <div class="flex items-center justify-between p-lg border-b border-border">
            <h2 class="text-lg font-semibold text-text">Link Riot Account</h2>
            <button
              class="bg-transparent border-none p-xs cursor-pointer text-text-secondary transition-colors duration-200 hover:text-text disabled:opacity-60 disabled:cursor-not-allowed"
              @click="handleClose"
              :disabled="isSubmitting"
              data-testid="close-btn"
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-5 h-5">
                <path fill-rule="evenodd" d="M5.47 5.47a.75.75 0 011.06 0L12 10.94l5.47-5.47a.75.75 0 111.06 1.06L13.06 12l5.47 5.47a.75.75 0 11-1.06 1.06L12 13.06l-5.47 5.47a.75.75 0 01-1.06-1.06L10.94 12 5.47 6.53a.75.75 0 010-1.06z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>

          <form @submit.prevent="handleSubmit" class="p-lg flex flex-col gap-md">
            <div
              v-if="errorMessage"
              class="p-md bg-[rgba(239,68,68,0.1)] border border-[rgba(239,68,68,0.3)] rounded-md text-[#ef4444] text-sm"
            >
              {{ errorMessage }}
            </div>

            <div class="flex flex-col gap-xs">
              <label class="text-sm font-medium text-text" for="gameName">Game Name</label>
              <input
                id="gameName"
                v-model="formData.gameName"
                type="text"
                class="p-md bg-background border border-border rounded-md text-base text-text transition-all duration-200 focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-primary-soft placeholder:text-text-secondary disabled:opacity-60 disabled:cursor-not-allowed"
                :class="{ 'border-[#ef4444]': errors.gameName }"
                placeholder="e.g. Faker"
                :disabled="isSubmitting"
                maxlength="100"
              />
              <span v-if="errors.gameName" class="text-xs text-[#ef4444]">{{ errors.gameName }}</span>
            </div>

            <div class="flex flex-col gap-xs">
              <label class="text-sm font-medium text-text" for="tagLine">Tag Line</label>
              <input
                id="tagLine"
                v-model="formData.tagLine"
                type="text"
                class="p-md bg-background border border-border rounded-md text-base text-text transition-all duration-200 focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-primary-soft placeholder:text-text-secondary disabled:opacity-60 disabled:cursor-not-allowed"
                :class="{ 'border-[#ef4444]': errors.tagLine }"
                placeholder="e.g. NA1"
                :disabled="isSubmitting"
                minlength="3"
                maxlength="5"
              />
              <span v-if="errors.tagLine" class="text-xs text-[#ef4444]">{{ errors.tagLine }}</span>
            </div>

            <div class="flex flex-col gap-xs">
              <label class="text-sm font-medium text-text" for="region">Region</label>
              <select
                id="region"
                v-model="formData.region"
                class="p-md bg-background border border-border rounded-md text-base text-text transition-all duration-200 cursor-pointer focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-primary-soft disabled:opacity-60 disabled:cursor-not-allowed"
                :class="{ 'border-[#ef4444]': errors.region }"
                :disabled="isSubmitting"
              >
                <option value="" disabled>Select a region</option>
                <option v-for="r in regions" :key="r.value" :value="r.value" class="bg-background text-text">
                  {{ r.label }}
                </option>
              </select>
              <span v-if="errors.region" class="text-xs text-[#ef4444]">{{ errors.region }}</span>
            </div>

            <div class="flex justify-end gap-sm mt-md">
              <button
                type="button"
                class="bg-transparent text-text-secondary py-sm px-lg border border-border rounded-md font-medium text-sm cursor-pointer transition-all duration-200 hover:border-primary hover:text-primary disabled:opacity-60 disabled:cursor-not-allowed"
                @click="handleClose"
                :disabled="isSubmitting"
                data-testid="cancel-btn"
              >
                Cancel
              </button>
              <button
                type="submit"
                class="bg-primary text-white py-sm px-lg border-none rounded-md font-semibold text-sm cursor-pointer transition-all duration-200 flex items-center gap-xs hover:shadow-md hover:-translate-y-px disabled:opacity-60 disabled:cursor-not-allowed disabled:transform-none"
                :disabled="isSubmitting || !isFormValid"
              >
                <span v-if="isSubmitting" class="inline-block w-3.5 h-3.5 border-2 border-[rgba(255,255,255,0.3)] border-t-white rounded-full animate-spin" data-testid="loading-spinner"></span>
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
/* Vue Transition classes for modal animation */
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

