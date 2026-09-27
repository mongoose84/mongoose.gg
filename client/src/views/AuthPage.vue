<template>
  <div class="min-h-screen bg-background pt-14 desk:pt-20">
    <NavBar />

    <main class="min-h-[calc(100vh-3.5rem)] desk:min-h-[calc(100vh-5rem)] flex items-center justify-center px-4 py-10 desk:py-14">
      <div class="w-full max-w-[28rem] p-5 desk:p-7 bg-background-surface border border-border rounded-xl flex flex-col gap-7" data-testid="auth-card">

        <!-- ── Forgot Password State ── -->
        <template v-if="isForgotPassword">
          <div class="flex flex-col items-center text-center gap-3">
            <img src="/mongoose.png" alt="" class="h-10 w-[6.1875rem] object-contain" data-testid="auth-logo" />
            <h1 class="font-display font-semibold text-[1.75rem] leading-[1.15] text-text">Reset your password</h1>
            <p class="text-body text-text-soft">Enter your email and we'll send you a reset code.</p>
          </div>

          <div v-if="forgotErrorMessage" class="auth-error" role="alert">
            <BaseIcon name="triangle-alert" :size="20" />
            <span>{{ forgotErrorMessage }}</span>
          </div>

          <form @submit.prevent="handleForgotSubmit" class="flex flex-col gap-5" data-testid="forgot-form">
            <BaseInput
              id="forgot-email"
              v-model="forgotEmail"
              type="email"
              label="Email"
              placeholder="you@example.com"
              autocomplete="email"
              required
              :disabled="isSubmitting"
            />

            <BaseButton
              type="submit"
              variant="primary"
              size="lg"
              :loading="isSubmitting"
              :disabled="isSubmitting"
              block
            >
              {{ isSubmitting ? 'Sending…' : 'Send reset code' }}
            </BaseButton>
          </form>

          <div class="pt-5 border-t border-border flex justify-center">
            <BaseButton variant="ghost" size="md" :disabled="isSubmitting" data-testid="auth-back" @click="showLogin">
              Back to log in
            </BaseButton>
          </div>
        </template>

        <!-- ── Login / Register State ── -->
        <template v-else>
          <div class="flex flex-col items-center text-center gap-3">
            <img src="/mongoose.png" alt="" class="h-10 w-[6.1875rem] object-contain" data-testid="auth-logo" />
            <h1 class="font-display font-semibold text-[1.75rem] leading-[1.15] text-text" data-testid="auth-title">
              {{ isLogin ? 'Log in to Mongoose.gg' : 'Create your free account' }}
            </h1>
            <p class="text-body text-text-soft">
              {{ isLogin ? 'Welcome back. Your Overview is waiting.' : 'Next, link your Riot ID and we sync your recent matches.' }}
            </p>
          </div>

          <!-- Error message -->
          <div v-if="errorMessage" class="auth-error" role="alert">
            <BaseIcon name="triangle-alert" :size="20" />
            <span>{{ errorMessage }}</span>
          </div>

          <!-- Cookie consent rejection info -->
          <div v-if="consentRejected" class="auth-notice" role="alert">
            <BaseIcon name="info" :size="20" />
            <div class="flex flex-col items-start gap-1">
              <p>You've rejected cookies. Logging in needs an authentication cookie.</p>
              <button type="button" class="auth-notice__action mp-focusable" @click="updateCookiePreferences">
                Update cookie preferences
              </button>
            </div>
          </div>

          <form @submit.prevent="handleSubmit" class="flex flex-col gap-5" data-testid="auth-form">
            <!-- Username field for both login and signup -->
            <BaseInput
              id="username"
              v-model="formData.username"
              label="Username"
              placeholder="Your username"
              autocomplete="username"
              :error="usernameError"
              required
              minlength="3"
              maxlength="50"
              data-testid="form-group"
              @input="validateUsername"
            />

            <!-- Email field only for signup -->
            <BaseInput
              v-if="!isLogin"
              id="email"
              v-model="formData.email"
              type="email"
              label="Email"
              placeholder="you@example.com"
              autocomplete="email"
              required
            />

            <div class="flex flex-col gap-2">
              <BaseInput
                id="password"
                v-model="formData.password"
                type="password"
                label="Password"
                :placeholder="isLogin ? 'Your password' : 'At least 8 characters'"
                :autocomplete="isLogin ? 'current-password' : 'new-password'"
                required
                minlength="8"
              />

              <!-- Forgot password (login only) -->
              <div v-if="isLogin" class="flex justify-end">
                <button
                  type="button"
                  class="auth-link mp-focusable"
                  data-testid="auth-forgot"
                  @click="showForgotPassword"
                >
                  Forgot password?
                </button>
              </div>
            </div>

            <BaseButton
              type="submit"
              variant="primary"
              size="lg"
              :loading="isSubmitting"
              :disabled="isSubmitting || consentRejected"
              block
              data-testid="auth-submit"
            >
              {{ submitLabel }}
            </BaseButton>
          </form>

          <p class="pt-5 border-t border-border flex flex-wrap items-center justify-center gap-x-1 text-body-sm text-text-secondary">
            {{ isLogin ? 'New here?' : 'Already have an account?' }}
            <button
              type="button"
              class="auth-link mp-focusable"
              :disabled="isSubmitting"
              data-testid="auth-toggle"
              @click="toggleMode"
            >
              {{ isLogin ? 'Create free account' : 'Log in' }}
            </button>
          </p>
        </template>

      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import NavBar from '../components/NavBar.vue';
import { BaseInput, BaseButton, BaseIcon } from '@/components/base';
import { useAuthStore } from '../stores/authStore';
import { useCookieConsent } from '../composables/useCookieConsent';
import { trackAuth } from '../services/analyticsApi';
import { forgotPassword } from '../services/authApi';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const cookieConsent = useCookieConsent();

// ── Cookie consent state ──
const consentRejected = cookieConsent.isRejected;

// ── Login / Register state ──
const isLogin = ref(true);
const isSubmitting = ref(false);
const errorMessage = ref('');
const usernameError = ref('');

const formData = ref({
  username: '',
  email: '',
  password: ''
});

const submitLabel = computed(() => {
  if (isSubmitting.value) {
    return isLogin.value ? 'Logging in…' : 'Creating account…';
  }
  return isLogin.value ? 'Log in' : 'Create free account';
});

// ── Forgot password state ──
const isForgotPassword = ref(false);
const forgotEmail = ref('');
const forgotErrorMessage = ref('');

// Get redirect destination from query params (for session expiry flow)
const redirectTo = computed(() => {
  const redirect = route.query.redirect;
  // Only allow internal redirects (starting with /)
  if (redirect && typeof redirect === 'string' && redirect.startsWith('/')) {
    return redirect;
  }
  return '/app/overview';
});

onMounted(async () => {
  // Initialize auth store to check current session
  await authStore.initialize();

  // Redirect if already authenticated
  if (authStore.isAuthenticated) {
    if (!authStore.isVerified) {
      router.push('/auth/verify');
    } else {
      router.push(redirectTo.value);
    }
    return;
  }

  // Check query params for mode
  if (route.query.mode === 'signup') {
    isLogin.value = false;
  } else if (route.query.mode === 'login') {
    isLogin.value = true;
  }
});

// Watch for route changes to update mode
watch(() => route.query.mode, (newMode) => {
  if (newMode === 'signup') {
    isLogin.value = false;
  } else if (newMode === 'login') {
    isLogin.value = true;
  }
});

const validateUsername = () => {
  const username = formData.value.username;
  usernameError.value = '';

  if (username.length > 0 && username.length < 3) {
    usernameError.value = 'Username must be at least 3 characters';
  } else if (username.length > 50) {
    usernameError.value = 'Username must be 50 characters or less';
  } else if (username && !/^[a-zA-Z0-9_-]+$/.test(username)) {
    usernameError.value = 'Username can only contain letters, numbers, underscores, and hyphens';
  }
};

const showForgotPassword = () => {
  isForgotPassword.value = true;
  forgotEmail.value = '';
  forgotErrorMessage.value = '';
  errorMessage.value = '';
};

const showLogin = () => {
  isForgotPassword.value = false;
  isLogin.value = true;
  forgotErrorMessage.value = '';
  router.replace({ path: '/auth', query: { mode: 'login' } });
};

const updateCookiePreferences = () => {
  cookieConsent.resetConsent();
};

const toggleMode = () => {
  isLogin.value = !isLogin.value;
  formData.value = { username: '', email: '', password: '' };
  errorMessage.value = '';
  usernameError.value = '';

  // Update URL without navigating
  router.replace({
    path: '/auth',
    query: { mode: isLogin.value ? 'login' : 'signup' }
  });
};

const handleForgotSubmit = async () => {
  if (isSubmitting.value) return;

  isSubmitting.value = true;
  forgotErrorMessage.value = '';

  try {
    await forgotPassword(forgotEmail.value);
    // Always redirect — the API never leaks whether the email exists
    router.push({ path: '/auth/reset-password', query: { email: forgotEmail.value } });
  } catch (e) {
    forgotErrorMessage.value = e.message || 'Something went wrong. Please try again.';
  } finally {
    isSubmitting.value = false;
  }
};

const handleSubmit = async () => {
  if (isSubmitting.value) return;
  if (usernameError.value) return;
  if (consentRejected.value) return;

  isSubmitting.value = true;
  errorMessage.value = '';

  try {
    if (isLogin.value) {
      // Login flow
      const result = await authStore.login({
        username: formData.value.username,
        password: formData.value.password
      });

      trackAuth('login', true);

      if (!result.emailVerified) {
        router.push('/auth/verify');
      } else {
        router.push(redirectTo.value);
      }
    } else {
      // Signup flow
      await authStore.register({
        username: formData.value.username,
        email: formData.value.email,
        password: formData.value.password
      });

      trackAuth('register', true);

      // After signup, redirect to verification
      router.push('/auth/verify');
    }
  } catch (e) {
    // Track failed auth attempts
    trackAuth(isLogin.value ? 'login' : 'register', false, { errorCode: e.code });

    // Handle specific error codes
    if (e.code === 'USERNAME_TAKEN') {
      usernameError.value = 'This username is already taken';
    } else if (e.code === 'USERNAME_TOO_LONG') {
      usernameError.value = 'Username must be 50 characters or less';
    } else if (e.code === 'USERNAME_INVALID') {
      usernameError.value = 'Username contains invalid characters';
    } else {
      errorMessage.value = e.message || 'An error occurred. Please try again.';
    }
  } finally {
    isSubmitting.value = false;
  }
};
</script>

<style scoped>
/* MessageBox (design system), error: a failed request is a system state, so it uses the error tokens */
.auth-error {
  display: flex;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  border-radius: 0.75rem;
  background: var(--color-error-soft);
  border: 1px solid var(--color-error-border);
  color: var(--color-text);
  font-size: 0.875rem;
  line-height: 1.5;
}

.auth-error :deep(.base-icon) {
  color: var(--color-error);
}

/* MessageBox (design system), notice: neutral information, not an error */
.auth-notice {
  display: flex;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  border-radius: 0.75rem;
  background: var(--color-elevated);
  border: 1px solid var(--color-border);
  color: var(--color-ink-soft);
  font-size: 0.875rem;
  line-height: 1.5;
}

.auth-notice :deep(.base-icon) {
  color: var(--color-text-secondary);
}

.auth-notice__action,
.auth-link {
  padding: 0;
  border: 0;
  border-radius: 0.25rem;
  background: transparent;
  color: var(--color-positive-text);
  font-family: var(--font-body);
  font-size: 0.875rem;
  font-weight: 700;
  cursor: pointer;
  transition: color 150ms ease-out;
}

.auth-link {
  min-height: 2.75rem;
}

.auth-notice__action:hover,
.auth-link:hover:not(:disabled) {
  color: var(--color-positive-text-strong);
}

.auth-link:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

@media (prefers-reduced-motion: reduce) {
  .auth-notice__action,
  .auth-link {
    transition: none;
  }
}
</style>
