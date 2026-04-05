import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import SessionExpiredBanner from '@/components/SessionExpiredBanner.vue';
import { useAuthStore } from '@/stores/authStore';

// Mock vue-router
const mockPush = vi.fn();
const mockCurrentRoute = { value: { fullPath: '/dashboard' } };

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: mockPush,
    currentRoute: mockCurrentRoute
  })
}));

// Mock apiClient to prevent real callback registration
vi.mock('@/services/apiClient', () => ({
  setSessionExpiredCallback: vi.fn()
}));

// Mock authApi
vi.mock('@/services/authApi', () => ({
  getCurrentUser: vi.fn(),
  login: vi.fn(),
  logout: vi.fn()
}));

describe('SessionExpiredBanner', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    mockCurrentRoute.value = { fullPath: '/dashboard' };
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  function mountBanner(options = {}) {
    return mount(SessionExpiredBanner, {
      global: {
        stubs: {
          Transition: false, // Use real transition for v-if testing
          BaseButton: {
            template: '<button @click="$emit(\'click\')"><slot /></button>',
            emits: ['click']
          },
          LockClosedIcon: { template: '<svg data-testid="lock-icon" />' }
        }
      },
      ...options
    });
  }

  describe('rendering', () => {
    it('does NOT render when sessionExpired is false', () => {
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      
      expect(authStore.sessionExpired).toBe(false);
      expect(wrapper.find('.session-expired-banner').exists()).toBe(false);
    });

    it('renders when sessionExpired is true', async () => {
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      
      // Set sessionExpired to true (directly on the reactive ref)
      authStore.$patch({ sessionExpired: true });
      await flushPromises();

      expect(wrapper.find('.session-expired-banner').exists()).toBe(true);
    });

    it('displays lock icon', async () => {
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      authStore.$patch({ sessionExpired: true });
      await flushPromises();

      // Check for the stubbed LockClosedIcon component
      expect(wrapper.findComponent({ name: 'LockClosedIcon' }).exists() ||
             wrapper.find('svg').exists()).toBe(true);
    });

    it('displays session expired message', async () => {
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      authStore.$patch({ sessionExpired: true });
      await flushPromises();

      expect(wrapper.text()).toContain('Your session has expired');
    });

    it('displays Log In button', async () => {
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      authStore.$patch({ sessionExpired: true });
      await flushPromises();

      expect(wrapper.find('button').exists()).toBe(true);
      expect(wrapper.find('button').text()).toBe('Log In');
    });
  });

  describe('goToLogin button click', () => {
    it('calls authStore.clearSessionExpired on click', async () => {
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      authStore.$patch({ sessionExpired: true });
      await flushPromises();

      const clearSpy = vi.spyOn(authStore, 'clearSessionExpired');
      
      await wrapper.find('button').trigger('click');

      expect(clearSpy).toHaveBeenCalled();
    });

    it('navigates to login with redirect parameter', async () => {
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      authStore.$patch({ sessionExpired: true });
      await flushPromises();

      await wrapper.find('button').trigger('click');

      expect(mockPush).toHaveBeenCalledWith('/auth?mode=login&redirect=%2Fdashboard');
    });

    it('preserves complex route path with query params', async () => {
      mockCurrentRoute.value = { fullPath: '/matches/123?tab=timeline&view=detailed' };
      
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      authStore.$patch({ sessionExpired: true });
      await flushPromises();

      await wrapper.find('button').trigger('click');

      expect(mockPush).toHaveBeenCalledWith(
        '/auth?mode=login&redirect=%2Fmatches%2F123%3Ftab%3Dtimeline%26view%3Ddetailed'
      );
    });

    it('properly URL encodes special characters in path', async () => {
      mockCurrentRoute.value = { fullPath: '/search?q=Player#1' };
      
      const wrapper = mountBanner();
      const authStore = useAuthStore();
      authStore.$patch({ sessionExpired: true });
      await flushPromises();

      await wrapper.find('button').trigger('click');

      const redirectParam = mockPush.mock.calls[0][0].split('redirect=')[1];
      expect(decodeURIComponent(redirectParam)).toBe('/search?q=Player#1');
    });
  });
});

