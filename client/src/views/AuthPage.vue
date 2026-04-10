<template>
  <div class="min-h-screen bg-transparent pt-16">
    <NavBar />

    <div class="min-h-[calc(100vh-64px)] flex items-center justify-center p-xl">
      <div class="w-full max-w-[440px] p-2xl bg-background-surface border border-border rounded-lg backdrop-blur-[10px]" data-testid="auth-card">

        <!-- ── Forgot Password State ── -->
        <template v-if="isForgotPassword">
          <div class="flex flex-col items-center justify-center text-center mb-xl">
            <img src="/mongoose.png" alt="Mongoose" class="w-32 h-16 mb-md" data-testid="auth-logo" />
            <h1 class="text-2xl font-bold tracking-tight mb-xs text-text">Forgot Your Password?</h1>
            <p class="text-base text-text-secondary">Enter your email to receive a reset code</p>
          </div>

          <div v-if="forgotErrorMessage" class="p-md bg-error-soft border border-error-border rounded-md text-error text-sm mb-md" role="alert">
            {{ forgotErrorMessage }}
          </div>

          <form @submit.prevent="handleForgotSubmit" class="flex flex-col gap-lg" data-testid="forgot-form">
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
              {{ isSubmitting ? 'Sending...' : 'Send Reset Code' }}
            </BaseButton>
          </form>

          <div class="mt-xl pt-xl border-t border-border text-center">
            <BaseButton variant="ghost" size="sm" :disabled="isSubmitting" @click="showLogin">
              ← Back to Sign In
            </BaseButton>
          </div>
        </template>

        <!-- ── Login / Register State ── -->
        <template v-else>
          <div class="flex flex-col items-center justify-center text-center mb-xl min-h-[200px]">
            <img src="/mongoose.png" alt="Mongoose" class="w-32 h-16 mb-md" data-testid="auth-logo" />
            <h1 class="text-2xl font-bold tracking-tight mb-xs text-text">Welcome to Mongoose.gg <span class="text-[0.5em] text-text-secondary font-normal align-top">Beta</span></h1>
            <p class="text-base text-text-secondary">{{ isLogin ? 'Sign in to your account' : 'Create your account' }}</p>
          </div>

          <!-- Error message -->
          <div v-if="errorMessage" class="p-md bg-error-soft border border-error-border rounded-md text-error text-sm text-center mb-md" role="alert">
            {{ errorMessage }}
          </div>

          <!-- Cookie consent rejection info banner -->
          <div v-if="consentRejected" class="p-md bg-warning-soft border border-warning-border rounded-md text-warning text-sm mb-md" role="alert">
            <p class="m-0 mb-xs">You've rejected cookies. Login requires an authentication cookie.</p>
            <BaseButton
              variant="ghost"
              size="sm"
              class="text-warning hover:opacity-80 p-0 h-auto mt-xs"
              @click="updateCookiePreferences"
            >
              Update cookie preferences
            </BaseButton>
          </div>

          <form @submit.prevent="handleSubmit" class="flex flex-col gap-lg" data-testid="auth-form">
            <!-- Username field for both login and signup -->
            <BaseInput
              id="username"
              v-model="formData.username"
              label="Username"
              placeholder="Your username"
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
              required
            />

            <BaseInput
              id="password"
              v-model="formData.password"
              type="password"
              label="Password"
              placeholder="••••••••"
              required
              minlength="8"
            />

            <!-- Remember me + Forgot password row (login only) -->
            <div v-if="isLogin" class="flex items-center justify-between gap-sm">
              <div class="flex items-center gap-sm">
                <input
                  id="rememberMe"
                  v-model="formData.rememberMe"
                  type="checkbox"
                  class="w-[18px] h-[18px] accent-primary cursor-pointer"
                />
                <label for="rememberMe" class="text-sm text-text-secondary cursor-pointer">Keep me logged in for 30 days</label>
              </div>
              <button
                type="button"
                class="text-sm text-primary hover:opacity-80 transition-opacity bg-transparent border-none cursor-pointer"
                @click="showForgotPassword"
              >
                Forgot password?
              </button>
            </div>

            <BaseButton
              type="submit"
              variant="primary"
              size="lg"
              :loading="isSubmitting"
              :disabled="isSubmitting || consentRejected"
              class="mt-md"
            >
              {{ isSubmitting ? 'Please wait...' : (isLogin ? 'Sign In' : 'Create Account') }}
            </BaseButton>
          </form>

          <div class="mt-xl pt-xl border-t border-border text-center">
            <BaseButton
              variant="ghost"
              size="sm"
              :disabled="isSubmitting"
              data-testid="auth-toggle"
              @click="toggleMode"
            >
              {{ isLogin ? 'Need an account? Sign up' : 'Already have an account? Sign in' }}
            </BaseButton>
          </div>
        </template>

      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import NavBar from '../components/NavBar.vue';
import { BaseInput, BaseButton } from '@/components/base';
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
  password: '',
  rememberMe: false
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
  formData.value = { username: '', email: '', password: '', rememberMe: false };
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
        password: formData.value.password,
        rememberMe: formData.value.rememberMe
      });

      trackAuth('login', true, { rememberMe: formData.value.rememberMe });

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
