<template>
  <div class="auth-page">
    <NavBar />

    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-header">
          <img src="/mongoose.png" alt="Mongoose" class="auth-logo" />
          <h1 class="auth-title">Welcome to Mongoose.gg <span class="beta-tag">Beta</span></h1>
          <p class="auth-subtitle">{{ isLogin ? 'Sign in to your account' : 'Create your account' }}</p>
        </div>

        <!-- Error message -->
        <div v-if="errorMessage" class="auth-error">
          {{ errorMessage }}
        </div>

        <form @submit.prevent="handleSubmit" class="auth-form">
          <!-- Username field for both login and signup -->
          <div class="form-group">
            <label for="username" class="form-label">Username</label>
            <input
              id="username"
              v-model="formData.username"
              type="text"
              class="form-input"
              :class="{ 'form-input-error': usernameError }"
              placeholder="Your username"
              required
              minlength="3"
              maxlength="50"
              @input="validateUsername"
            />
            <span v-if="usernameError" class="form-error">{{ usernameError }}</span>
          </div>

          <!-- Email field only for signup -->
          <div v-if="!isLogin" class="form-group">
            <label for="email" class="form-label">Email</label>
            <input
              id="email"
              v-model="formData.email"
              type="email"
              class="form-input"
              placeholder="you@example.com"
              required
            />
          </div>

          <div class="form-group">
            <label for="password" class="form-label">Password</label>
            <input
              id="password"
              v-model="formData.password"
              type="password"
              class="form-input"
              placeholder="••••••••"
              required
              minlength="8"
            />
          </div>

          <!-- Remember me checkbox for login -->
          <div v-if="isLogin" class="form-checkbox-group">
            <input
              id="rememberMe"
              v-model="formData.rememberMe"
              type="checkbox"
              class="form-checkbox"
            />
            <label for="rememberMe" class="form-checkbox-label">Keep me logged in for 30 days</label>
          </div>

          <button type="submit" class="auth-btn-submit" :disabled="isSubmitting">
            <span v-if="isSubmitting" class="auth-spinner"></span>
            {{ isSubmitting ? 'Please wait...' : (isLogin ? 'Sign In' : 'Create Account') }}
          </button>
        </form>

        <div class="auth-footer">
          <button @click="toggleMode" class="auth-toggle" :disabled="isSubmitting">
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
      router.push('/app/user');
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
        router.push('/app/user');
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
.auth-page {
  min-height: 100vh;
  background: transparent;
  padding-top: 64px;
}

.auth-container {
  min-height: calc(100vh - 64px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-xl);
}

.auth-card {
  width: 100%;
  max-width: 440px;
  padding: var(--spacing-2xl);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
}

.auth-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center; /* This centers content vertically */
  text-align: center;
  margin-bottom: var(--spacing-xl);
  min-height: 200px; /* Optional: set a min-height for better vertical centering */
}

.auth-logo {
  width: 128px;
  height: 64px;
  margin-bottom: var(--spacing-md);

}

.auth-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-xs);
  color: var(--color-text);
}

.beta-tag {
  font-size: 0.5em;
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-normal);
  vertical-align: top;
}

.auth-subtitle {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
}

.auth-form {
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

.form-input::placeholder {
  color: var(--color-text-secondary);
}

.auth-btn-submit {
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
}

.auth-btn-submit:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.auth-footer {
  margin-top: var(--spacing-xl);
  padding-top: var(--spacing-xl);
  border-top: 1px solid var(--color-border);
  text-align: center;
}

.auth-toggle {
  background: transparent;
  border: none;
  color: var(--color-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: opacity 0.2s;
}

.auth-toggle:hover {
  opacity: 0.8;
}

.auth-toggle:disabled,
.auth-btn-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.auth-error {
  padding: var(--spacing-md);
  background: rgba(239, 68, 68, 0.1);
  border: 1px solid rgba(239, 68, 68, 0.3);
  border-radius: var(--radius-md);
  color: #ef4444;
  font-size: var(--font-size-sm);
  text-align: center;
  margin-bottom: var(--spacing-md);
}

.form-input-error {
  border-color: #ef4444;
}

.form-input-error:focus {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.2);
}

.form-error {
  font-size: var(--font-size-xs);
  color: #ef4444;
  margin-top: var(--spacing-xs);
}

.form-checkbox-group {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.form-checkbox {
  width: 18px;
  height: 18px;
  accent-color: var(--color-primary);
  cursor: pointer;
}

.form-checkbox-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  cursor: pointer;
}

.auth-spinner {
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
