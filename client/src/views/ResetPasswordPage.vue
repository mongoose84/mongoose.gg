<template>
  <div class="min-h-screen bg-transparent pt-16">
    <NavBar />

    <div class="min-h-[calc(100vh-64px)] flex items-center justify-center p-xl">
      <div class="w-full max-w-[440px] p-2xl bg-background-surface border border-border rounded-lg backdrop-blur-[10px]">
        <!-- Header -->
        <div class="flex flex-col items-center justify-center text-center mb-xl">
          <img src="/mongoose.png" alt="Mongoose" class="w-32 h-16 mb-md mx-auto" />
          <h1 class="text-2xl font-bold tracking-tight mb-xs text-text">Reset Your Password</h1>
          <p class="text-base text-text-secondary">Enter the code sent to your email and set a new password</p>
        </div>

        <!-- Error alert -->
        <div v-if="errorMessage" class="p-md bg-error-soft border border-error-border rounded-md text-error text-sm mb-md" role="alert">
          {{ errorMessage }}
        </div>

        <form @submit.prevent="handleSubmit" class="flex flex-col gap-lg">
          <!-- Email -->
          <BaseInput
            id="reset-email"
            v-model="email"
            type="email"
            label="Email"
            placeholder="you@example.com"
            autocomplete="email"
            required
            :disabled="isSubmitting"
          />

          <!-- 6-digit code -->
          <div class="flex flex-col gap-xs">
            <label for="reset-code" class="text-sm font-medium text-text tracking-tight">Reset Code</label>
            <input
              id="reset-code"
              v-model="code"
              type="text"
              class="p-lg bg-background border border-border rounded-md text-2xl font-bold text-text text-center tracking-[0.5em] transition-all duration-200 focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-primary-soft disabled:opacity-60 disabled:cursor-not-allowed"
              placeholder="000000"
              maxlength="6"
              pattern="[0-9]{6}"
              inputmode="numeric"
              autocomplete="one-time-code"
              :disabled="isSubmitting"
              @input="handleCodeInput"
            />
            <span class="text-xs text-text-secondary text-center">Enter the 6-digit code from your email</span>
          </div>

          <!-- New password -->
          <BaseInput
            id="reset-new-password"
            v-model="newPassword"
            type="password"
            label="New Password"
            placeholder="••••••••"
            hint="Must be at least 8 characters"
            minlength="8"
            autocomplete="new-password"
            required
            :disabled="isSubmitting"
          />

          <BaseButton
            type="submit"
            variant="primary"
            size="lg"
            :loading="isSubmitting"
            :disabled="isSubmitting || !isFormValid"
            block
          >
            {{ isSubmitting ? 'Resetting...' : 'Reset Password' }}
          </BaseButton>
        </form>

        <div class="mt-xl pt-xl border-t border-border text-center">
          <BaseButton variant="ghost" size="sm" :to="{ path: '/auth', query: { mode: 'login' } }">
            Remember your password? Sign in
          </BaseButton>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import NavBar from '../components/NavBar.vue';
import { BaseInput, BaseButton } from '@/components/base';
import { resetPassword } from '../services/authApi';

const route = useRoute();
const router = useRouter();

const email = ref('');
const code = ref('');
const newPassword = ref('');
const isSubmitting = ref(false);
const errorMessage = ref('');

const isFormValid = computed(() => code.value.length === 6 && newPassword.value.length >= 8);

onMounted(() => {
  // Pre-fill email from query param (URL-decoded automatically by the router)
  if (route.query.email) {
    email.value = String(route.query.email);
  }
});

const handleCodeInput = (e) => {
  code.value = e.target.value.replace(/\D/g, '').slice(0, 6);
};

const ERROR_MESSAGES = {
  INVALID_CODE: 'Invalid or expired code. Please request a new one.',
  PASSWORD_TOO_SHORT: 'Password must be at least 8 characters.',
  INVALID_EMAIL: 'Please enter a valid email address.'
};

const handleSubmit = async () => {
  if (isSubmitting.value || !isFormValid.value) return;

  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    await resetPassword({ email: email.value, code: code.value, newPassword: newPassword.value });
    // User must log in with the new password — redirect to login
    router.push({ path: '/auth', query: { mode: 'login' } });
  } catch (e) {
    errorMessage.value = ERROR_MESSAGES[e.code] || e.message || 'Something went wrong. Please try again.';
  } finally {
    isSubmitting.value = false;
  }
};
</script>

