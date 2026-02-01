<template>
  <BaseModal
    :is-open="authStore.sessionExpired"
    title="Session Expired"
    size="sm"
    :show-close-button="false"
    :prevent-close="isLoggingIn"
    @close="handleClose"
  >
    <div class="session-expired-content">
      <div class="icon-container">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="icon">
          <path fill-rule="evenodd" d="M12 1.5a5.25 5.25 0 00-5.25 5.25v3a3 3 0 00-3 3v6.75a3 3 0 003 3h10.5a3 3 0 003-3v-6.75a3 3 0 00-3-3v-3c0-2.9-2.35-5.25-5.25-5.25zm3.75 8.25v-3a3.75 3.75 0 10-7.5 0v3h7.5z" clip-rule="evenodd" />
        </svg>
      </div>
      
      <p class="message">{{ authStore.sessionExpiredMessage || 'Your session has expired due to inactivity. Please log in again to continue.' }}</p>
      
      <div v-if="showLoginForm" class="login-form">
        <BaseInput
          v-model="username"
          label="Username or Email"
          placeholder="Enter your username or email"
          :disabled="isLoggingIn"
          @keyup.enter="handleLogin"
        />
        <BaseInput
          v-model="password"
          type="password"
          label="Password"
          placeholder="Enter your password"
          :disabled="isLoggingIn"
          @keyup.enter="handleLogin"
        />
        <p v-if="loginError" class="error-message">{{ loginError }}</p>
      </div>
    </div>

    <template #footer>
      <div class="footer-buttons">
        <BaseButton
          v-if="!showLoginForm"
          variant="secondary"
          @click="goToLogin"
        >
          Go to Login
        </BaseButton>
        <BaseButton
          v-if="showLoginForm"
          variant="secondary"
          :disabled="isLoggingIn"
          @click="showLoginForm = false"
        >
          Cancel
        </BaseButton>
        <BaseButton
          v-if="showLoginForm"
          variant="primary"
          :loading="isLoggingIn"
          :disabled="!canLogin"
          @click="handleLogin"
        >
          Log In
        </BaseButton>
        <BaseButton
          v-if="!showLoginForm"
          variant="primary"
          @click="showLoginForm = true"
        >
          Log In Here
        </BaseButton>
      </div>
    </template>
  </BaseModal>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import BaseModal from './base/BaseModal.vue'
import BaseButton from './base/BaseButton.vue'
import BaseInput from './base/BaseInput.vue'

const router = useRouter()
const authStore = useAuthStore()

const showLoginForm = ref(false)
const username = ref('')
const password = ref('')
const isLoggingIn = ref(false)
const loginError = ref('')

const canLogin = computed(() => username.value.trim() && password.value)

function handleClose() {
  if (!isLoggingIn.value) {
    authStore.clearSessionExpired()
    router.push('/')
  }
}

function goToLogin() {
  authStore.clearSessionExpired()
  router.push('/auth?mode=login')
}

async function handleLogin() {
  if (!canLogin.value || isLoggingIn.value) return
  
  isLoggingIn.value = true
  loginError.value = ''
  
  try {
    await authStore.login({ username: username.value, password: password.value })
    // Success - clear the session expired state
    authStore.clearSessionExpired()
    showLoginForm.value = false
    username.value = ''
    password.value = ''
  } catch (e) {
    loginError.value = e.message || 'Login failed. Please try again.'
  } finally {
    isLoggingIn.value = false
  }
}
</script>

<style scoped>
.session-expired-content {
  text-align: center;
}

.icon-container {
  display: flex;
  justify-content: center;
  margin-bottom: var(--spacing-md);
}

.icon {
  width: 48px;
  height: 48px;
  color: var(--color-warning);
}

.message {
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-lg);
  line-height: 1.5;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
  text-align: left;
}

.error-message {
  color: var(--color-error);
  font-size: var(--font-size-sm);
  margin: 0;
}

.footer-buttons {
  display: flex;
  gap: var(--spacing-sm);
  width: 100%;
  justify-content: flex-end;
}
</style>

