<template>
  <div class="verify-page">
    <NavBar />
    
    <div class="verify-container">
      <div class="verify-card">
        <div class="verify-header">
          <img src="/pulse-icon.svg" alt="Pulse.gg" class="verify-logo" />
          <h1 class="verify-title">Verify Your Email</h1>
          <p class="verify-subtitle">
            We've sent a 6-digit code to <strong>{{ email }}</strong>
          </p>
        </div>

        <!-- Error message -->
        <div v-if="errorMessage" class="verify-error">
          {{ errorMessage }}
        </div>
        
        <form @submit.prevent="handleSubmit" class="verify-form">
          <div class="form-group">
            <label for="code" class="form-label">Verification Code</label>
            <input 
              id="code"
              v-model="code"
              type="text"
              class="form-input code-input"
              placeholder="000000"
              maxlength="6"
              pattern="[0-9]{6}"
              inputmode="numeric"
              autocomplete="one-time-code"
              required
              @input="handleCodeInput"
            />
            <span class="form-hint">Enter the 6-digit code from your email</span>
          </div>
          
          <button type="submit" class="verify-btn-submit" :disabled="isSubmitting || code.length !== 6">
            <span v-if="isSubmitting" class="verify-spinner"></span>
            {{ isSubmitting ? 'Verifying...' : 'Verify Email' }}
          </button>
        </form>
        
        <div class="verify-footer">
          <p class="verify-resend-text">Didn't receive the code?</p>
          <button @click="handleResend" class="verify-resend" :disabled="isSubmitting">
            Resend Code
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import NavBar from '../components/NavBar.vue';
import { useAuthStore } from '../stores/authStore';

const router = useRouter();
const authStore = useAuthStore();

const code = ref('');
const isSubmitting = ref(false);
const errorMessage = ref('');

const email = computed(() => authStore.email || 'your email');

onMounted(async () => {
  await authStore.initialize();
  
  // Redirect if not authenticated
  if (!authStore.isAuthenticated) {
    router.push('/auth?mode=login');
    return;
  }
  
  // Redirect if already verified
  if (authStore.isVerified) {
    router.push('/app/user');
  }
});

const handleCodeInput = (e) => {
  // Only allow digits
  code.value = e.target.value.replace(/\D/g, '').slice(0, 6);
};

const handleSubmit = async () => {
  if (isSubmitting.value || code.value.length !== 6) return;
  
  isSubmitting.value = true;
  errorMessage.value = '';
  
  try {
    await authStore.verify(code.value);
    router.push('/app/user');
  } catch (e) {
    errorMessage.value = e.message || 'Invalid verification code. Please try again.';
  } finally {
    isSubmitting.value = false;
  }
};

const handleResend = () => {
  // TODO: Implement resend functionality
  alert('Resend functionality coming soon!');
};
</script>

<style scoped>
.verify-page {
  min-height: 100vh;
  background: transparent;
  padding-top: 64px;
}

.verify-container {
  min-height: calc(100vh - 64px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xl);
}

.verify-card {
  width: 100%;
  max-width: 440px;
  padding: var(--spacing-2xl);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
}

.verify-header {
  text-align: center;
  margin-bottom: var(--spacing-xl);
}

.verify-logo {
  width: 64px;
  height: 64px;
  margin-bottom: var(--spacing-md);
}

.verify-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-xs);
  color: var(--color-text);
}

.verify-subtitle {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
}

.verify-subtitle strong {
  color: var(--color-text);
}

.verify-error {
  padding: var(--spacing-md);
  background: rgba(239, 68, 68, 0.1);
  border: 1px solid rgba(239, 68, 68, 0.3);
  border-radius: var(--radius-md);
  color: #ef4444;
  font-size: var(--font-size-sm);
  text-align: center;
  margin-bottom: var(--spacing-md);
}

.verify-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
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
  letter-spacing: var(--letter-spacing);
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

.code-input {
  text-align: center;
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: 0.5em;
  padding: var(--spacing-lg);
}

.form-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  text-align: center;
}

.verify-btn-submit {
  padding: var(--spacing-md);
  background: var(--color-primary);
  color: white;
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-md);
  letter-spacing: var(--letter-spacing);
  border: none;
  border-radius: var(--radius-md);
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: var(--shadow-sm);
  margin-top: var(--spacing-md);
  display: flex;
  align-items: center;
  justify-content: center;
}

.verify-btn-submit:hover:not(:disabled) {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.verify-btn-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.verify-footer {
  margin-top: var(--spacing-xl);
  padding-top: var(--spacing-xl);
  border-top: 1px solid var(--color-border);
  text-align: center;
}

.verify-resend-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-sm);
}

.verify-resend {
  background: transparent;
  border: none;
  color: var(--color-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: opacity 0.2s;
}

.verify-resend:hover {
  opacity: 0.8;
}

.verify-resend:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.verify-spinner {
  display: inline-block;
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: white;
  animation: spin 0.8s linear infinite;
  margin-right: var(--spacing-sm);
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>

