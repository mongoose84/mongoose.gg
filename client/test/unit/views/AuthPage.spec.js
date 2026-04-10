import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { ref } from 'vue';
import AuthPage from '@/views/AuthPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

// Mock the authStore
const mockLogin = vi.fn().mockResolvedValue({ emailVerified: true });
const mockRegister = vi.fn().mockResolvedValue(undefined);
const mockInitialize = vi.fn().mockResolvedValue(undefined);
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    isAuthenticated: false,
    isVerified: false,
    initialize: mockInitialize,
    login: mockLogin,
    register: mockRegister,
    error: null
  })
}));

// Mock analytics
vi.mock('@/services/analyticsApi', () => ({
  trackAuth: vi.fn()
}));

// Mock authApi — forgotPassword
const mockForgotPassword = vi.fn().mockResolvedValue({ message: 'ok' });
vi.mock('@/services/authApi', () => ({
  forgotPassword: (...args) => mockForgotPassword(...args)
}));

// Mock useCookieConsent
const mockIsRejected = ref(false);
const mockResetConsent = vi.fn();
vi.mock('@/composables/useCookieConsent', () => ({
  useCookieConsent: () => ({
    isRejected: mockIsRejected,
    getConsent: vi.fn().mockReturnValue('accepted'),
    resetConsent: mockResetConsent,
    shouldShowBanner: vi.fn().mockReturnValue(false),
    setupCrossTabSync: vi.fn().mockReturnValue(() => {})
  })
}));

describe('AuthPage.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockForgotPassword.mockReset().mockResolvedValue({ message: 'ok' });
    mockLogin.mockReset().mockResolvedValue({ emailVerified: true });
    mockRegister.mockReset().mockResolvedValue(undefined);
    mockInitialize.mockReset().mockResolvedValue(undefined);
    mockIsRejected.value = false;
    mockResetConsent.mockReset();
  });

  const createWrapper = (query = {}) => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Home</div>' } },
        { path: '/auth', component: { template: '<div>Auth</div>' } },
        { path: '/auth/verify', component: { template: '<div>Verify</div>' } },
        { path: '/auth/reset-password', component: { template: '<div>Reset</div>' } },
        { path: '/app/overview', component: { template: '<div>Overview</div>' } },
      ]
    });

    return mount(AuthPage, {
      global: {
        plugins: [router],
        stubs: {
          NavBar: true,
        }
      },
      props: {}
    });
  };

  it('renders the auth page', () => {
    const wrapper = createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the welcome message', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Welcome to Mongoose.gg');
  });

  it('has email and password inputs', () => {
    const wrapper = createWrapper();
    const inputs = wrapper.findAll('input');
    expect(inputs.length).toBeGreaterThan(0);
  });

  it('has a toggle between login and signup', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Sign in');
  });

  it('has a submit button', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Sign In');
  });

  it('displays login mode by default', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Sign in to your account');
  });

  it('toggles between login and signup modes', async () => {
    const wrapper = createWrapper();
    
    // Initially in login mode
    expect(wrapper.text()).toContain('Sign in to your account');
    
    // Click toggle button
    const toggleBtn = wrapper.find('[data-testid="auth-toggle"]');
    await toggleBtn.trigger('click');

    // Should now show signup text
    expect(wrapper.text()).toContain('Create your account');
  });

  it('shows username field in signup mode', async () => {
    const wrapper = createWrapper();

    // Toggle to signup
    const toggleBtn = wrapper.find('[data-testid="auth-toggle"]');
    await toggleBtn.trigger('click');

    // Should show username input
    expect(wrapper.text()).toContain('Username');
  });

  it('shows username field in login mode (used for login)', () => {
    const wrapper = createWrapper();

    // Username field is shown in both login and signup modes
    const hasUsernameLabel = wrapper.text().includes('Username');
    expect(hasUsernameLabel).toBe(true);
  });

  it('displays auth card with proper styling', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="auth-card"]').exists()).toBe(true);
  });

  it('displays auth logo', () => {
    const wrapper = createWrapper();
    const logo = wrapper.find('[data-testid="auth-logo"]');
    expect(logo.exists()).toBe(true);
  });

  it('has proper form structure', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="auth-form"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="form-group"]').exists()).toBe(true);
  });

  it('submit button text changes with mode', async () => {
    const wrapper = createWrapper();

    // Login mode
    expect(wrapper.text()).toContain('Sign In');
    expect(wrapper.text()).not.toContain('Create Account');

    // Toggle to signup
    const toggleBtn = wrapper.find('[data-testid="auth-toggle"]');
    await toggleBtn.trigger('click');
    
    // Should show Create Account
    expect(wrapper.text()).toContain('Create Account');
    expect(wrapper.text()).not.toContain('Sign In');
  });

  it('form inputs have proper attributes', () => {
    const wrapper = createWrapper();

    // Check that username and password inputs exist and have required attribute
    const usernameInput = wrapper.find('#username');
    const passwordInput = wrapper.find('#password');

    expect(usernameInput.exists()).toBe(true);
    expect(passwordInput.exists()).toBe(true);
    expect(usernameInput.attributes('required')).toBeDefined();
    expect(passwordInput.attributes('required')).toBeDefined();
  });

  it('displays password input with masked type', () => {
    const wrapper = createWrapper();
    const passwordInput = wrapper.find('input[type="password"]');
    expect(passwordInput.exists()).toBe(true);
  });

  // ── Forgot Password Flow ──

  describe('Forgot Password', () => {
    const switchToForgotPassword = async (wrapper) => {
      const forgotBtn = wrapper.find('button[type="button"]');
      // The "Forgot password?" button is a plain <button type="button">
      const btn = wrapper.findAll('button').find(b => b.text().includes('Forgot password'));
      expect(btn).toBeTruthy();
      await btn.trigger('click');
    };

    it('shows forgot password UI when "Forgot password?" is clicked', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      expect(wrapper.text()).toContain('Forgot Your Password?');
      expect(wrapper.text()).toContain('Enter your email to receive a reset code');
      expect(wrapper.find('[data-testid="forgot-form"]').exists()).toBe(true);
    });

    it('hides login/register form when in forgot password state', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      expect(wrapper.find('[data-testid="auth-form"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="auth-toggle"]').exists()).toBe(false);
    });

    it('shows email input in forgot password form', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      const emailInput = wrapper.find('#forgot-email');
      expect(emailInput.exists()).toBe(true);
      expect(emailInput.attributes('type')).toBe('email');
    });

    it('shows "Send Reset Code" submit button', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      expect(wrapper.text()).toContain('Send Reset Code');
    });

    it('shows "Back to Sign In" button', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      const backBtn = wrapper.findAll('button').find(b => b.text().includes('Back to Sign In'));
      expect(backBtn).toBeTruthy();
    });

    it('returns to login mode when "Back to Sign In" is clicked', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);
      expect(wrapper.find('[data-testid="forgot-form"]').exists()).toBe(true);

      const backBtn = wrapper.findAll('button').find(b => b.text().includes('Back to Sign In'));
      await backBtn.trigger('click');

      expect(wrapper.find('[data-testid="forgot-form"]').exists()).toBe(false);
      expect(wrapper.find('[data-testid="auth-form"]').exists()).toBe(true);
      expect(wrapper.text()).toContain('Sign in to your account');
    });

    it('calls forgotPassword API on form submit', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      const emailInput = wrapper.find('#forgot-email');
      await emailInput.setValue('test@example.com');

      const form = wrapper.find('[data-testid="forgot-form"]');
      await form.trigger('submit');

      expect(mockForgotPassword).toHaveBeenCalledWith('test@example.com');
    });

    it('redirects to reset-password page on successful submit', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      const emailInput = wrapper.find('#forgot-email');
      await emailInput.setValue('test@example.com');

      const form = wrapper.find('[data-testid="forgot-form"]');
      await form.trigger('submit');

      // Wait for async handler to complete
      await vi.dynamicImportSettled();
      await wrapper.vm.$nextTick();

      // Router should have navigated to reset-password with email query param
      const { currentRoute } = wrapper.vm.$.appContext.config.globalProperties.$router;
      expect(currentRoute.value.path).toBe('/auth/reset-password');
      expect(currentRoute.value.query.email).toBe('test@example.com');
    });

    it('displays error message when forgotPassword API fails', async () => {
      mockForgotPassword.mockRejectedValueOnce(new Error('Rate limit exceeded'));

      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      const emailInput = wrapper.find('#forgot-email');
      await emailInput.setValue('test@example.com');

      const form = wrapper.find('[data-testid="forgot-form"]');
      await form.trigger('submit');

      // Wait for async handler
      await vi.dynamicImportSettled();
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain('Rate limit exceeded');
    });

    it('displays generic error when API error has no message', async () => {
      mockForgotPassword.mockRejectedValueOnce(new Error());

      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      const emailInput = wrapper.find('#forgot-email');
      await emailInput.setValue('test@example.com');

      const form = wrapper.find('[data-testid="forgot-form"]');
      await form.trigger('submit');

      await vi.dynamicImportSettled();
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain('Something went wrong. Please try again.');
    });

    it('clears forgot error message when switching back to login', async () => {
      mockForgotPassword.mockRejectedValueOnce(new Error('Some error'));

      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      // Trigger error
      const emailInput = wrapper.find('#forgot-email');
      await emailInput.setValue('fail@example.com');
      await wrapper.find('[data-testid="forgot-form"]').trigger('submit');
      await vi.dynamicImportSettled();
      await wrapper.vm.$nextTick();
      expect(wrapper.text()).toContain('Some error');

      // Go back to login
      const backBtn = wrapper.findAll('button').find(b => b.text().includes('Back to Sign In'));
      await backBtn.trigger('click');

      // Switch back to forgot — error should be cleared
      await switchToForgotPassword(wrapper);
      expect(wrapper.text()).not.toContain('Some error');
    });

    it('displays auth logo in forgot password state', async () => {
      const wrapper = createWrapper();

      await switchToForgotPassword(wrapper);

      const logo = wrapper.find('[data-testid="auth-logo"]');
      expect(logo.exists()).toBe(true);
    });
  });

  // ── Cookie Consent ──

  describe('Cookie consent rejection', () => {
    it('shows the warning banner when consent is rejected', async () => {
      mockIsRejected.value = true;
      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      expect(wrapper.text()).toContain("You've rejected cookies");
    });

    it('submit button is disabled when consent is rejected', async () => {
      mockIsRejected.value = true;
      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      const submitBtn = wrapper.find('[data-testid="auth-form"] button[type="submit"]');
      expect(submitBtn.element.disabled).toBe(true);
    });

    it('does not call login when form is submitted with rejected consent', async () => {
      mockIsRejected.value = true;
      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      // Trigger form submit directly — bypasses button disabled state
      const form = wrapper.find('[data-testid="auth-form"]');
      await form.trigger('submit');
      await wrapper.vm.$nextTick();

      expect(mockLogin).not.toHaveBeenCalled();
    });

    it('calls resetConsent when "Update cookie preferences" is clicked', async () => {
      mockIsRejected.value = true;
      const wrapper = createWrapper();
      await wrapper.vm.$nextTick();

      const updateBtn = wrapper.findAll('button').find(b => b.text().includes('Update cookie preferences'));
      expect(updateBtn).toBeTruthy();
      await updateBtn.trigger('click');

      expect(mockResetConsent).toHaveBeenCalledOnce();
    });
  });
});
