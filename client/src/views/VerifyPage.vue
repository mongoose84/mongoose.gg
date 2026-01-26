<template>
  <div class="min-h-screen bg-transparent pt-16">
    <NavBar />

    <div class="min-h-[calc(100vh-64px)] flex items-center justify-center p-xl">
      <div class="w-full max-w-[440px] p-2xl bg-background-surface border border-border rounded-lg backdrop-blur-[10px]" data-testid="verify-card">
        <div class="text-center mb-xl">
          <img src="/mongoose.png" alt="Mongoose" class="w-32 h-16 mb-md mx-auto" />
          <h1 class="text-2xl font-bold tracking-tight mb-xs text-text">Verify Your Email</h1>
          <p class="text-base text-text-secondary">
            We've sent a 6-digit code to <strong class="text-text">{{ email }}</strong>
          </p>
        </div>

        <!-- Success message -->
        <div v-if="successMessage" class="p-md bg-[rgba(34,197,94,0.1)] border border-[rgba(34,197,94,0.3)] rounded-md text-[#22c55e] text-sm text-center mb-md" data-testid="verify-success">
          {{ successMessage }}
        </div>

        <!-- Error message -->
        <div v-if="errorMessage" class="p-md bg-[rgba(239,68,68,0.1)] border border-[rgba(239,68,68,0.3)] rounded-md text-[#ef4444] text-sm text-center mb-md" data-testid="verify-error">
          {{ errorMessage }}
        </div>

        <form @submit.prevent="handleSubmit" class="flex flex-col gap-lg" data-testid="verify-form">
          <div class="flex flex-col gap-xs" data-testid="form-group">
            <label for="code" class="text-sm font-medium text-text tracking-tight">Verification Code</label>
            <input
              id="code"
              v-model="code"
              type="text"
              class="p-lg bg-background border border-border rounded-md text-2xl font-bold text-text text-center tracking-[0.5em] transition-all duration-200 focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-primary-soft"
              placeholder="000000"
              maxlength="6"
              pattern="[0-9]{6}"
              inputmode="numeric"
              autocomplete="one-time-code"
              required
              @input="handleCodeInput"
            />
            <span class="text-xs text-text-secondary text-center">Enter the 6-digit code from your email</span>
          </div>

          <button
            type="submit"
            class="p-md bg-primary text-white font-semibold text-base tracking-tight border-none rounded-md cursor-pointer transition-all duration-200 shadow-sm mt-md flex items-center justify-center hover:shadow-md hover:-translate-y-0.5 disabled:opacity-60 disabled:cursor-not-allowed disabled:transform-none"
            :disabled="isSubmitting || code.length !== 6"
            data-testid="verify-btn-submit"
          >
            <span v-if="isSubmitting" class="inline-block w-4 h-4 border-2 border-[rgba(255,255,255,0.3)] rounded-full border-t-white animate-spin mr-sm"></span>
            {{ isSubmitting ? 'Verifying...' : 'Verify Email' }}
          </button>
        </form>

        <div class="mt-xl pt-xl border-t border-border text-center">
          <p class="text-sm text-text-secondary mb-sm">Didn't receive the code?</p>
          <button
            @click="handleResend"
            class="bg-transparent border-none text-primary text-sm font-medium cursor-pointer transition-opacity duration-200 hover:opacity-80 disabled:opacity-60 disabled:cursor-not-allowed"
            :disabled="isResending || resendCooldown > 0"
            data-testid="verify-resend"
          >
            <span v-if="isResending">Sending...</span>
            <span v-else-if="resendCooldown > 0">Resend in {{ resendCooldown }}s</span>
            <span v-else>Resend Code</span>
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
import { resendVerification } from '../services/authApi';

const router = useRouter();
const authStore = useAuthStore();

const code = ref('');
const isSubmitting = ref(false);
const isResending = ref(false);
const errorMessage = ref('');
const successMessage = ref('');
const resendCooldown = ref(0);

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
  successMessage.value = '';

  try {
    await authStore.verify(code.value);
    router.push('/app/user');
  } catch (e) {
    errorMessage.value = e.message || 'Invalid verification code. Please try again.';
  } finally {
    isSubmitting.value = false;
  }
};

const startCooldown = (seconds) => {
  resendCooldown.value = seconds;
  const interval = setInterval(() => {
    resendCooldown.value--;
    if (resendCooldown.value <= 0) {
      clearInterval(interval);
    }
  }, 1000);
};

const handleResend = async () => {
  if (isResending.value || resendCooldown.value > 0) return;

  isResending.value = true;
  errorMessage.value = '';
  successMessage.value = '';

  try {
    await resendVerification();
    successMessage.value = 'A new verification code has been sent to your email.';
    startCooldown(60);
  } catch (e) {
    if (e.code === 'RATE_LIMITED' && e.waitSeconds) {
      startCooldown(e.waitSeconds);
      errorMessage.value = e.message;
    } else {
      errorMessage.value = e.message || 'Failed to resend verification code. Please try again.';
    }
  } finally {
    isResending.value = false;
  }
};
</script>
