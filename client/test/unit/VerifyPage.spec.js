import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import VerifyPage from '@/views/VerifyPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

// Mock the resendVerification API
vi.mock('@/services/authApi', () => ({
  resendVerification: vi.fn().mockResolvedValue({ success: true })
}));

// Mock the authStore with configurable values
const mockAuthStore = {
  isAuthenticated: true,
  isVerified: false,
  email: 'test@example.com',
  initialize: vi.fn().mockResolvedValue(undefined),
  verify: vi.fn().mockResolvedValue({ verified: true })
};

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => mockAuthStore
}));

describe('VerifyPage.vue', () => {
  let router;

  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    
    // Reset mock values
    mockAuthStore.isAuthenticated = true;
    mockAuthStore.isVerified = false;
    mockAuthStore.email = 'test@example.com';
    mockAuthStore.verify.mockResolvedValue({ verified: true });
    
    router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Home</div>' } },
        { path: '/auth', component: { template: '<div>Auth</div>' } },
        { path: '/auth/verify', component: { template: '<div>Verify</div>' } },
        { path: '/app/overview', component: { template: '<div>Overview</div>' } },
      ]
    });
  });

  const createWrapper = async () => {
    const wrapper = mount(VerifyPage, {
      global: {
        plugins: [router],
        stubs: {
          NavBar: true,
        }
      }
    });
    await flushPromises();
    return wrapper;
  };

  it('renders the verify page', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the page title', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.text()).toContain('Verify Your Email');
  });

  it('displays the user email', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.text()).toContain('test@example.com');
  });

  it('has a code input field', async () => {
    const wrapper = await createWrapper();
    const input = wrapper.find('#code');
    expect(input.exists()).toBe(true);
    expect(input.attributes('maxlength')).toBe('6');
  });

  it('has a submit button', async () => {
    const wrapper = await createWrapper();
    const button = wrapper.find('[data-testid="verify-btn-submit"]');
    expect(button.exists()).toBe(true);
    expect(button.text()).toContain('Verify Email');
  });

  it('submit button is disabled when code length is not 6', async () => {
    const wrapper = await createWrapper();
    const input = wrapper.find('#code');
    const button = wrapper.find('[data-testid="verify-btn-submit"]');

    await input.setValue('123');
    expect(button.attributes('disabled')).toBeDefined();
  });

  it('submit button is enabled when code length is 6', async () => {
    const wrapper = await createWrapper();
    const input = wrapper.find('#code');
    const button = wrapper.find('[data-testid="verify-btn-submit"]');

    await input.setValue('123456');
    expect(button.attributes('disabled')).toBeUndefined();
  });

  it('filters non-numeric input', async () => {
    const wrapper = await createWrapper();
    const input = wrapper.find('#code');
    
    await input.setValue('abc123def456');
    await input.trigger('input');
    
    // The handleCodeInput should filter to only digits
    // Note: v-model may keep original, so check the actual input behavior
    expect(wrapper.find('#code').element.value).toMatch(/^[0-9]*$/);
  });

  it('displays resend button', async () => {
    const wrapper = await createWrapper();
    const resendBtn = wrapper.find('[data-testid="verify-resend"]');
    expect(resendBtn.exists()).toBe(true);
    expect(resendBtn.text()).toContain('Resend Code');
  });

  it('calls verify on form submit with correct code', async () => {
    const wrapper = await createWrapper();
    const input = wrapper.find('#code');
    
    await input.setValue('123456');
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    
    expect(mockAuthStore.verify).toHaveBeenCalledWith('123456');
  });

  it('displays error message on verification failure', async () => {
    mockAuthStore.verify.mockRejectedValueOnce(new Error('Invalid code'));
    
    const wrapper = await createWrapper();
    await wrapper.find('#code').setValue('123456');
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    
    expect(wrapper.find('[data-testid="verify-error"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('Invalid code');
  });

  it('displays success message after resending code', async () => {
    const { resendVerification } = await import('@/services/authApi');
    resendVerification.mockResolvedValueOnce({ success: true });

    const wrapper = await createWrapper();
    await wrapper.find('[data-testid="verify-resend"]').trigger('click');
    await flushPromises();

    expect(wrapper.find('[data-testid="verify-success"]').exists()).toBe(true);
    expect(wrapper.text()).toContain('new verification code');
  });

  it('has proper form structure', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.find('[data-testid="verify-card"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="verify-form"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="form-group"]').exists()).toBe(true);
  });
});

