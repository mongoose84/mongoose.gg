<template>
  <div class="min-h-screen bg-transparent pt-16">
    <NavBar />

    <div class="min-h-[calc(100vh-64px)] flex items-center justify-center p-xl">
      <div class="w-full max-w-[440px] p-2xl bg-background-surface border border-border rounded-lg backdrop-blur-[10px]" data-testid="auth-card">
        <div class="flex flex-col items-center justify-center text-center mb-xl min-h-[200px]">
          <img src="/mongoose.png" alt="Mongoose" class="w-32 h-16 mb-md" data-testid="auth-logo" />
          <h1 class="text-2xl font-bold tracking-tight mb-xs text-text">Welcome to Mongoose.gg <span class="text-[0.5em] text-text-secondary font-normal align-top">Beta</span></h1>
          <p class="text-base text-text-secondary">{{ isLogin ? 'Sign in to your account' : 'Create your account' }}</p>
        </div>

        <!-- Error message -->
        <div v-if="errorMessage" class="p-md bg-[rgba(239,68,68,0.1)] border border-[rgba(239,68,68,0.3)] rounded-md text-[#ef4444] text-sm text-center mb-md">
          {{ errorMessage }}
        </div>

        <form @submit.prevent="handleSubmit" class="flex flex-col gap-lg" data-testid="auth-form">
          <!-- Username field for both login and signup -->
          <div class="flex flex-col gap-xs" data-testid="form-group">
            <label for="username" class="text-sm font-medium text-text tracking-tight">Username</label>
            <input
              id="username"
              v-model="formData.username"
              type="text"
              class="p-md bg-background border border-border rounded-md text-base text-text transition-all duration-200 focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-primary-soft placeholder:text-text-secondary"
              :class="{ 'border-[#ef4444] focus:border-[#ef4444] focus:ring-[rgba(239,68,68,0.2)]': usernameError }"
              placeholder="Your username"
              required
              minlength="3"
              maxlength="50"
              @input="validateUsername"
            />
            <span v-if="usernameError" class="text-xs text-[#ef4444] mt-xs">{{ usernameError }}</span>
          </div>

          <!-- Email field only for signup -->
          <div v-if="!isLogin" class="flex flex-col gap-xs">
            <label for="email" class="text-sm font-medium text-text tracking-tight">Email</label>
            <input
              id="email"
              v-model="formData.email"
              type="email"
              class="p-md bg-background border border-border rounded-md text-base text-text transition-all duration-200 focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-primary-soft placeholder:text-text-secondary"
              placeholder="you@example.com"
              required
            />
          </div>

          <div class="flex flex-col gap-xs">
            <label for="password" class="text-sm font-medium text-text tracking-tight">Password</label>
            <input
              id="password"
              v-model="formData.password"
              type="password"
              class="p-md bg-background border border-border rounded-md text-base text-text transition-all duration-200 focus:outline-none focus:border-primary focus:ring-[3px] focus:ring-primary-soft placeholder:text-text-secondary"
              placeholder="••••••••"
              required
              minlength="8"
            />
          </div>

          <!-- Remember me checkbox for login -->
          <div v-if="isLogin" class="flex items-center gap-sm">
            <input
              id="rememberMe"
              v-model="formData.rememberMe"
              type="checkbox"
              class="w-[18px] h-[18px] accent-primary cursor-pointer"
            />
            <label for="rememberMe" class="text-sm text-text-secondary cursor-pointer">Keep me logged in for 30 days</label>
          </div>

          <button
            type="submit"
            class="p-md bg-primary text-white font-semibold text-base tracking-tight border-none rounded-md cursor-pointer transition-all duration-200 shadow-sm mt-md hover:shadow-md hover:-translate-y-0.5 disabled:opacity-60 disabled:cursor-not-allowed disabled:transform-none"
            :disabled="isSubmitting"
          >
            <span v-if="isSubmitting" class="inline-block w-4 h-4 border-2 border-[rgba(255,255,255,0.3)] rounded-full border-t-white animate-spin mr-sm"></span>
            {{ isSubmitting ? 'Please wait...' : (isLogin ? 'Sign In' : 'Create Account') }}
          </button>
        </form>

        <div class="mt-xl pt-xl border-t border-border text-center">
          <button
            @click="toggleMode"
            class="bg-transparent border-none text-primary text-sm font-medium cursor-pointer transition-opacity duration-200 hover:opacity-80 disabled:opacity-60 disabled:cursor-not-allowed"
            :disabled="isSubmitting"
            data-testid="auth-toggle"
          >
            {{ isLogin ? 'Need an account? Sign up' : 'Already have an account? Sign in' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import NavBar from '../components/NavBar.vue';
import { useAuthStore } from '../stores/authStore';
import { trackAuth } from '../services/analyticsApi';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

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

onMounted(async () => {
  // Initialize auth store to check current session
  await authStore.initialize();

  // Redirect if already authenticated
  if (authStore.isAuthenticated) {
    if (!authStore.isVerified) {
      router.push('/auth/verify');
    } else {
      router.push('/app/overview');
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

const handleSubmit = async () => {
  if (isSubmitting.value) return;
  if (usernameError.value) return;

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
        router.push('/app/overview');
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
