<template>
  <Transition name="slide-up">
    <div
      v-if="shouldShowBanner"
      class="cookie-consent-banner"
      role="dialog"
      aria-label="Cookie consent"
      :aria-describedby="`${uniqueId}-description`"
    >
      <!-- Dim scrim (inert — accidental clicks must not record a consent decision) -->
      <div class="cookie-consent-backdrop"></div>

      <div class="cookie-consent-container">
        <div class="banner-content">
          <div class="banner-text">
            <h2 class="banner-title">We use cookies</h2>
            <p :id="`${uniqueId}-description`" class="banner-description">
              Mongoose.gg uses an authentication cookie to keep you logged in. Without cookies,
              login and analytics features won't be available.
            </p>
            <p class="banner-links">
              Learn more in our
              <router-link to="/cookies" class="policy-link mp-focusable">Cookie Policy</router-link>
              and
              <router-link to="/privacy" class="policy-link mp-focusable">Privacy Policy</router-link>.
            </p>
          </div>

          <div class="button-row">
            <button
              type="button"
              class="mp-btn mp-btn--secondary"
              data-testid="reject-cookies"
              @click="handleReject"
            >
              Reject cookies
            </button>
            <button
              type="button"
              class="mp-btn mp-btn--primary"
              data-testid="accept-cookies"
              @click="handleAccept"
            >
              Accept cookies
            </button>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useCookieConsent } from '../composables/useCookieConsent'

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
/* Mongoose.gg design system: flat surface card on a dim scrim, pill buttons, no shadow or emoji */
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
  inset: 0;
  background-color: rgba(10, 8, 16, 0.6);
  z-index: -1;
}

.cookie-consent-container {
  display: flex;
  justify-content: center;
  padding: 0 1rem 1rem;
}

.banner-content {
  width: 100%;
  max-width: 50rem;
  box-sizing: border-box;
  padding: 1.25rem;
  border-radius: 1.5rem;
  background-color: var(--color-surface);
  border: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.banner-text {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.banner-title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.25rem;
  font-weight: 600;
  line-height: 1.3;
  color: var(--color-text);
}

.banner-description,
.banner-links {
  margin: 0;
  font-size: 0.875rem;
  font-weight: 400;
  line-height: 1.5;
  color: var(--color-ink-soft);
}

.banner-links {
  color: var(--color-text-secondary);
}

.policy-link {
  border-radius: 0.25rem;
  color: var(--color-positive-text);
  font-weight: 500;
  text-decoration: underline;
  text-underline-offset: 2px;
  transition: color 150ms ease-out;
}

.policy-link:hover {
  color: var(--color-positive-text-strong);
}

/* Phones: full-width buttons, the main action on top */
.button-row {
  display: flex;
  flex-direction: column-reverse;
  gap: 0.5rem;
}

.button-row .mp-btn {
  width: 100%;
}

@media (min-width: 900px) {
  .cookie-consent-container {
    padding: 0 3.5rem 1.75rem;
  }

  .banner-content {
    padding: 1.75rem;
    flex-direction: row;
    align-items: flex-end;
    gap: 1.75rem;
  }

  .banner-text {
    flex: 1;
  }

  .button-row {
    flex-direction: row;
    flex-shrink: 0;
    gap: 0.75rem;
  }

  .button-row .mp-btn {
    width: auto;
  }
}

/* Entrance: slides up once over 300ms; instant with reduced motion */
.slide-up-enter-active,
.slide-up-leave-active {
  transition: transform 300ms ease-out, opacity 300ms ease-out;
}

.slide-up-enter-from,
.slide-up-leave-to {
  transform: translateY(8px);
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .slide-up-enter-active,
  .slide-up-leave-active,
  .policy-link {
    transition: none;
  }
}
</style>
