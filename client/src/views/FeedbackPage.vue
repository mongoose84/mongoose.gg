<template>
  <div class="min-h-screen p-2xl">
    <div class="max-w-[600px] mx-auto">
      <!-- Header -->
      <div class="mb-2xl">
        <h1 class="text-2xl font-bold text-text tracking-tight mb-sm">Send feedback</h1>
        <p class="text-sm text-text-secondary">
          Help us improve Mongoose.gg by reporting bugs or suggesting new features.
        </p>
      </div>

      <!-- Success State -->
      <div v-if="isSuccess" class="bg-success-soft border border-success rounded-lg p-xl text-center">
        <CheckCircleIcon class="w-12 h-12 text-success mx-auto mb-md" />
        <h2 class="text-lg font-semibold text-text mb-sm">Thank you for your feedback!</h2>
        <p class="text-sm text-text-secondary mb-lg">
          We appreciate you taking the time to help us improve.
        </p>
        <BaseButton variant="secondary" @click="resetForm">
          Send more feedback
        </BaseButton>
      </div>

      <!-- Feedback Form -->
      <form v-else @submit.prevent="handleSubmit" class="flex flex-col gap-xl">
        <!-- Type Selector -->
        <div class="flex flex-col gap-sm">
          <label class="text-sm font-medium text-text">What type of feedback?</label>
          <div class="flex gap-sm">
            <button
              type="button"
              @click="feedbackType = 'bug'"
              class="type-button flex-1"
              :class="{ 'type-button--active': feedbackType === 'bug' }"
            >
              <BugAntIcon class="w-5 h-5" />
              <span>Bug report</span>
            </button>
            <button
              type="button"
              @click="feedbackType = 'feature'"
              class="type-button flex-1"
              :class="{ 'type-button--active': feedbackType === 'feature' }"
            >
              <LightBulbIcon class="w-5 h-5" />
              <span>Feature request</span>
            </button>
          </div>
        </div>

        <!-- Summary -->
        <BaseInput
          v-model="summary"
          label="Summary"
          :placeholder="summaryPlaceholder"
          :error="errors.summary"
          :maxlength="200"
          required
          @blur="validateSummary"
        />

        <!-- Details (Bug: What happened?) -->
        <div v-if="feedbackType === 'bug'" class="flex flex-col gap-xs">
          <label for="details" class="text-sm font-medium text-text">
            What happened? <span class="text-error">*</span>
          </label>
          <textarea
            id="details"
            v-model="details"
            placeholder="Describe what went wrong..."
            class="textarea"
            :class="{ 'textarea--error': errors.details }"
            rows="4"
            maxlength="5000"
            @blur="validateDetails"
          ></textarea>
          <span v-if="errors.details" class="text-xs text-error">{{ errors.details }}</span>
        </div>

        <!-- Details (Feature: What problem?) - Optional -->
        <div v-else class="flex flex-col gap-xs">
          <label for="details" class="text-sm font-medium text-text">
            What problem are you trying to solve? <span class="text-text-muted">(optional)</span>
          </label>
          <textarea
            id="details"
            v-model="details"
            placeholder="Describe the problem or use case..."
            class="textarea"
            :class="{ 'textarea--error': errors.details }"
            rows="4"
            maxlength="5000"
            @blur="validateDetails"
          ></textarea>
          <span v-if="errors.details" class="text-xs text-error">{{ errors.details }}</span>
        </div>

        <!-- Optional field (Bug: Expected) -->
        <div v-if="feedbackType === 'bug'" class="flex flex-col gap-xs">
          <label for="expected" class="text-sm font-medium text-text">
            What did you expect? <span class="text-text-secondary">(optional)</span>
          </label>
          <textarea
            id="expected"
            v-model="expected"
            placeholder="Describe what you expected to happen..."
            class="textarea"
            rows="3"
            maxlength="2000"
          ></textarea>
        </div>

        <!-- Optional field (Feature: How would this help?) -->
        <div v-else class="flex flex-col gap-xs">
          <label for="expected" class="text-sm font-medium text-text">
            How would this help your climbing? <span class="text-text-secondary">(optional)</span>
          </label>
          <textarea
            id="expected"
            v-model="expected"
            placeholder="Describe how this feature would help you improve..."
            class="textarea"
            rows="3"
            maxlength="2000"
          ></textarea>
        </div>

        <!-- Context info -->
        <div class="text-xs text-text-secondary bg-background-elevated rounded-md px-md py-sm">
          From: {{ currentRoute }}
        </div>

        <!-- Error message -->
        <div v-if="submitError" class="bg-error-soft border border-error rounded-md p-md text-sm text-error">
          {{ submitError }}
        </div>

        <!-- Submit button -->
        <BaseButton
          type="submit"
          variant="primary"
          size="lg"
          :loading="isSubmitting"
          :disabled="!isFormValid"
          block
        >
          {{ isSubmitting ? 'Sending...' : 'Send feedback' }}
        </BaseButton>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { BugAntIcon, LightBulbIcon, CheckCircleIcon } from '@heroicons/vue/24/outline'
import BaseButton from '../components/base/BaseButton.vue'
import BaseInput from '../components/base/BaseInput.vue'
import { submitFeedback } from '../services/feedbackApi'

const route = useRoute()

// Form state
const feedbackType = ref('bug')
const summary = ref('')
const details = ref('')
const expected = ref('')
const errors = ref({})
const isSubmitting = ref(false)
const submitError = ref(null)
const isSuccess = ref(false)

// Capture the route the user came from
const previousRoute = ref('')
const currentRoute = computed(() => previousRoute.value || route.path)

onMounted(() => {
  // Store the referrer route (the page user was on before navigating to feedback)
  const referrer = document.referrer
  if (referrer && referrer.includes(window.location.origin)) {
    try {
      const url = new URL(referrer)
      previousRoute.value = url.pathname
    } catch {
      previousRoute.value = route.path
    }
  } else {
    previousRoute.value = route.path
  }
})

// Dynamic placeholder based on type
const summaryPlaceholder = computed(() => {
  return feedbackType.value === 'bug'
    ? 'e.g., Match history not loading'
    : 'e.g., Add champion win rate comparison'
})

// Validation
function validateSummary() {
  if (!summary.value.trim()) {
    errors.value.summary = 'Summary is required'
    return false
  }
  errors.value.summary = null
  return true
}

function validateDetails() {
  // Details are only required for bug reports
  if (feedbackType.value === 'bug' && !details.value.trim()) {
    errors.value.details = 'Please describe what happened'
    return false
  }
  errors.value.details = null
  return true
}

// Form validity - details only required for bugs
const isFormValid = computed(() => {
  const hasSummary = summary.value.trim().length > 0
  const hasDetailsIfRequired = feedbackType.value !== 'bug' || details.value.trim().length > 0
  return hasSummary && hasDetailsIfRequired
})

// Submit handler
async function handleSubmit() {
  // Validate all fields
  const summaryValid = validateSummary()
  const detailsValid = validateDetails()

  if (!summaryValid || !detailsValid) {
    return
  }

  isSubmitting.value = true
  submitError.value = null

  try {
    // Combine details with expected if provided
    let fullDetails = details.value.trim()
    if (expected.value.trim()) {
      const expectedLabel = feedbackType.value === 'bug'
        ? 'Expected behavior'
        : 'How this would help'
      fullDetails += `\n\n**${expectedLabel}:**\n${expected.value.trim()}`
    }

    await submitFeedback({
      type: feedbackType.value,
      summary: summary.value.trim(),
      details: fullDetails,
      route: currentRoute.value
    })

    isSuccess.value = true
  } catch (error) {
    submitError.value = 'We couldn\'t send your feedback right now. Please try again.'
    console.error('Feedback submission failed:', error)
  } finally {
    isSubmitting.value = false
  }
}

// Reset form for sending more feedback
function resetForm() {
  feedbackType.value = 'bug'
  summary.value = ''
  details.value = ''
  expected.value = ''
  errors.value = {}
  submitError.value = null
  isSuccess.value = false
}
</script>

<style scoped>
/* Type selector buttons */
.type-button {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md) var(--spacing-lg);
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.type-button:hover {
  border-color: var(--color-primary);
  color: var(--color-text);
}

.type-button--active {
  background: var(--color-primary-soft);
  border-color: var(--color-primary);
  color: var(--color-primary);
}

/* Textarea styles */
.textarea {
  width: 100%;
  padding: var(--spacing-md);
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: var(--font-size-md);
  font-family: inherit;
  color: var(--color-text);
  resize: vertical;
  min-height: 100px;
  transition: all 0.2s ease;
}

.textarea::placeholder {
  color: var(--color-text-secondary);
}

.textarea:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.textarea--error {
  border-color: var(--color-error);
}

.textarea--error:focus {
  box-shadow: 0 0 0 3px var(--color-error-soft);
}
</style>

