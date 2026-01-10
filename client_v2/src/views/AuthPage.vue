<template>
  <div class="auth-page">
    <NavBar />
    
    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-header">
          <img src="/pulse-icon.svg" alt="Pulse.gg" class="auth-logo" />
          <h1 class="auth-title">Welcome to Pulse.gg</h1>
          <p class="auth-subtitle">{{ isLogin ? 'Sign in to your account' : 'Create your account' }}</p>
        </div>
        
        <form @submit.prevent="handleSubmit" class="auth-form">
          <div v-if="!isLogin" class="form-group">
            <label for="username" class="form-label">Username</label>
            <input 
              id="username"
              v-model="formData.username"
              type="text"
              class="form-input"
              placeholder="Your username"
              required
            />
          </div>
          
          <div class="form-group">
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
            />
          </div>
          
          <button type="submit" class="auth-btn-submit">
            {{ isLogin ? 'Sign In' : 'Create Account' }}
          </button>
        </form>
        
        <div class="auth-footer">
          <button @click="toggleMode" class="auth-toggle">
            {{ isLogin ? 'Need an account? Sign up' : 'Already have an account? Sign in' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import NavBar from '../components/NavBar.vue';

const route = useRoute();
const isLogin = ref(true);
const formData = ref({
  username: '',
  email: '',
  password: ''
});

onMounted(() => {
  // Check query params for mode
  if (route.query.mode === 'signup') {
    isLogin.value = false;
  } else if (route.query.mode === 'login') {
    isLogin.value = true;
  }
  // Default is login if no mode specified
});

const toggleMode = () => {
  isLogin.value = !isLogin.value;
  formData.value = { username: '', email: '', password: '' };
};

const handleSubmit = () => {
  console.log('Form submitted:', formData.value);
  // TODO: Implement authentication logic
  alert('Authentication coming soon!');
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
  text-align: center;
  margin-bottom: var(--spacing-xl);
}

.auth-logo {
  width: 64px;
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
</style>
