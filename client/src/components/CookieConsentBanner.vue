<template>
  <Transition name="slide-up">
    <div
      v-if="shouldShowBanner"
      class="cookie-consent-banner"
      role="dialog"
      aria-label="Cookie consent"
      :aria-describedby="`${uniqueId}-description`"
    >
      <!-- Semi-transparent backdrop (inert — accidental clicks must not record a consent decision) -->
      <div class="cookie-consent-backdrop"></div>

      <!-- Banner container -->
      <div class="cookie-consent-container">
        <div class="banner-content">
          <!-- Header with icon and title -->
          <div class="banner-header">
            <span class="cookie-icon">🍪</span>
            <h2 class="banner-title">We use cookies</h2>
          </div>

          <!-- Description -->
          <p :id="`${uniqueId}-description`" class="banner-description">
            Mongoose.gg uses an authentication cookie to keep you logged in. Without cookies,
            login and analytics features won't be available.
          </p>

          <!-- Policy links -->
          <p class="banner-links">
            Learn more in our
            <router-link to="/cookies" class="policy-link">Cookie Policy</router-link>
            and
            <router-link to="/privacy" class="policy-link">Privacy Policy</router-link>.
          </p>

          <!-- Button row -->
          <div class="button-row">
            <BaseButton
              variant="secondary"
              size="md"
              @click="handleReject"
              data-testid="reject-cookies"
            >
              Reject Cookies
            </BaseButton>
            <BaseButton
              variant="primary"
              size="md"
              @click="handleAccept"
              data-testid="accept-cookies"
            >
              Accept Cookies
            </BaseButton>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useCookieConsent } from '../composables/useCookieConsent'
import BaseButton from './base/BaseButton.vue'

const cookieConsent = useCookieConsent()

// Generate unique ID for this component instance for aria-describedby
const uniqueId = ref(`cookie-banner-${Math.random().toString(36).substring(2, 11)}`)

const shouldShowBanner = computed(() => cookieConsent.shouldShowBanner())
let cleanupCrossTabSync = null

function handleAccept() {
  cookieConsent.setConsent('accepted')
}

function handleReject() {
  cookieConsent.setConsent('rejected')
}

onMounted(() => {
  // Setup cross-tab synchronization
  cleanupCrossTabSync = cookieConsent.setupCrossTabSync()
})

onBeforeUnmount(() => {
  cleanupCrossTabSync?.()
  cleanupCrossTabSync = null
})
</script>

<style scoped>
.cookie-consent-banner {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 500;
  display: flex;
  flex-direction: column;
}

.cookie-consent-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.4);
  backdrop-filter: blur(4px);
  z-index: -1;
}

.cookie-consent-container {
  display: flex;
  justify-content: center;
  align-items: flex-end;
  padding: var(--spacing-lg);
  min-height: auto;
}

.banner-content {
  background-color: #111111;
  border: 1px solid rgba(109, 40, 217, 0.3);
  border-bottom: none;
  border-radius: var(--radius-lg);
  border-bottom-left-radius: 0;
  border-bottom-right-radius: 0;
  padding: var(--spacing-lg) var(--spacing-xl);
  max-width: 800px;
  width: 100%;
  box-shadow: 0 -8px 32px rgba(0, 0, 0, 0.6);
}

.banner-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-md);
  margin-bottom: var(--spacing-md);
}

.cookie-icon {
  font-size: 24px;
  line-height: 1;
}

.banner-title {
  font-size: var(--font-size-lg);
  font-weight: 600;
  color: var(--color-text);
  margin: 0;
  line-height: 1.4;
}

.banner-description {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin: 0 0 var(--spacing-md) 0;
  line-height: 1.6;
}

.banner-links {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin: var(--spacing-md) 0;
  line-height: 1.6;
}

.policy-link {
  color: var(--color-primary);
  text-decoration: none;
  transition: opacity 0.2s ease;
}

.policy-link:hover {
  opacity: 0.8;
  text-decoration: underline;
}

.button-row {
  display: flex;
  gap: var(--spacing-md);
  margin-top: var(--spacing-lg);
  justify-content: flex-end;
}

/* Responsive: stack buttons on mobile */
@media (max-width: 640px) {
  .banner-content {
    padding: var(--spacing-md) var(--spacing-lg);
  }

  .button-row {
    flex-direction: column-reverse;
    gap: var(--spacing-sm);
  }

  :deep(.btn) {
    width: 100%;
  }
}

/* Animations */
.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.3s ease-out;
}

.slide-up-enter-from {
  transform: translateY(100%);
  opacity: 0;
}

.slide-up-leave-to {
  transform: translateY(100%);
  opacity: 0;
}

.slide-up-enter-to,
.slide-up-leave-from {
  transform: translateY(0);
  opacity: 1;
}
</style>
