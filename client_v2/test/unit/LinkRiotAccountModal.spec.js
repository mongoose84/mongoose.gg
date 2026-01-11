import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import LinkRiotAccountModal from '@/components/LinkRiotAccountModal.vue';

// Mock the authStore
const mockLinkRiotAccount = vi.fn();

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    linkRiotAccount: mockLinkRiotAccount
  })
}));

describe('LinkRiotAccountModal.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockLinkRiotAccount.mockReset();
  });

  const createWrapper = (props = {}) => {
    return mount(LinkRiotAccountModal, {
      props: {
        isOpen: true,
        ...props
      },
      global: {
        stubs: {
          Teleport: true
        }
      }
    });
  };

  describe('Rendering', () => {
    it('renders when isOpen is true', () => {
      const wrapper = createWrapper({ isOpen: true });
      expect(wrapper.find('.modal-overlay').exists()).toBe(true);
    });

    it('does not render when isOpen is false', () => {
      const wrapper = createWrapper({ isOpen: false });
      expect(wrapper.find('.modal-overlay').exists()).toBe(false);
    });

    it('displays the modal title', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Link Riot Account');
    });

    it('has game name, tag line, and region inputs', () => {
      const wrapper = createWrapper();
      expect(wrapper.find('#gameName').exists()).toBe(true);
      expect(wrapper.find('#tagLine').exists()).toBe(true);
      expect(wrapper.find('#region').exists()).toBe(true);
    });

    it('has Cancel and Link Account buttons', () => {
      const wrapper = createWrapper();
      expect(wrapper.text()).toContain('Cancel');
      expect(wrapper.text()).toContain('Link Account');
    });

    it('displays all region options', () => {
      const wrapper = createWrapper();
      const options = wrapper.findAll('option');
      // 16 regions + 1 disabled placeholder
      expect(options.length).toBe(17);
      expect(wrapper.text()).toContain('Europe West (EUW)');
      expect(wrapper.text()).toContain('North America (NA)');
      expect(wrapper.text()).toContain('Korea (KR)');
    });
  });

  describe('Form Validation', () => {
    it('submit button is disabled when form is empty', () => {
      const wrapper = createWrapper();
      const submitBtn = wrapper.find('button[type="submit"]');
      expect(submitBtn.attributes('disabled')).toBeDefined();
    });

    it('submit button is enabled when form is valid', async () => {
      const wrapper = createWrapper();
      
      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      
      const submitBtn = wrapper.find('button[type="submit"]');
      expect(submitBtn.attributes('disabled')).toBeUndefined();
    });

    it('shows error for empty game name on submit', async () => {
      const wrapper = createWrapper();
      
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');
      
      expect(wrapper.text()).toContain('Game name is required');
    });

    it('shows error for empty tag line on submit', async () => {
      const wrapper = createWrapper();
      
      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');
      
      expect(wrapper.text()).toContain('Tag line is required');
    });

    it('shows error for invalid tag line characters', async () => {
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR#1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');

      expect(wrapper.text()).toContain('Tag line must contain only letters and numbers');
    });

    it('shows error for tag line that is too short', async () => {
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');

      expect(wrapper.text()).toContain('Tag line must be 3-5 characters');
    });

    it('shows error for tag line that is too long', async () => {
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('TOOLONG');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');

      expect(wrapper.text()).toContain('Tag line must be 3-5 characters');
    });

    it('accepts valid 3-5 character tag lines', async () => {
      mockLinkRiotAccount.mockResolvedValue({ success: true });
      const wrapper = createWrapper();

      // Test 3 character tag
      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('NA1');
      await wrapper.find('#region').setValue('na1');
      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(mockLinkRiotAccount).toHaveBeenCalledWith({
        gameName: 'Faker',
        tagLine: 'NA1',
        region: 'na1'
      });
    });

    it('accepts numeric-only tag lines', async () => {
      mockLinkRiotAccount.mockResolvedValue({ success: true });
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Player');
      await wrapper.find('#tagLine').setValue('12345');
      await wrapper.find('#region').setValue('euw1');
      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(mockLinkRiotAccount).toHaveBeenCalledWith({
        gameName: 'Player',
        tagLine: '12345',
        region: 'euw1'
      });
    });

    it('shows error when region is not selected', async () => {
      const wrapper = createWrapper();
      
      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('form').trigger('submit');
      
      expect(wrapper.text()).toContain('Please select a region');
    });
  });

  describe('Submit Handling', () => {
    it('calls linkRiotAccount with correct data on submit', async () => {
      mockLinkRiotAccount.mockResolvedValue({ success: true });
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(mockLinkRiotAccount).toHaveBeenCalledWith({
        gameName: 'Faker',
        tagLine: 'KR1',
        region: 'kr'
      });
    });

    it('trims whitespace from game name input', async () => {
      mockLinkRiotAccount.mockResolvedValue({ success: true });
      const wrapper = createWrapper();

      // Game name can have leading/trailing whitespace which gets trimmed
      // Tag line cannot have whitespace due to regex validation
      await wrapper.find('#gameName').setValue('  Faker  ');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(mockLinkRiotAccount).toHaveBeenCalledWith({
        gameName: 'Faker',
        tagLine: 'KR1',
        region: 'kr'
      });
    });

    it('shows loading state during submission', async () => {
      mockLinkRiotAccount.mockImplementation(() => new Promise(() => {})); // Never resolves
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');

      expect(wrapper.text()).toContain('Linking...');
      expect(wrapper.find('.loading-spinner').exists()).toBe(true);
    });

    it('emits success and close events on successful submit', async () => {
      mockLinkRiotAccount.mockResolvedValue({ success: true });
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(wrapper.emitted('success')).toBeTruthy();
      expect(wrapper.emitted('close')).toBeTruthy();
    });
  });

  describe('Error Handling', () => {
    it('displays error for RIOT_ACCOUNT_NOT_FOUND', async () => {
      mockLinkRiotAccount.mockRejectedValue({ code: 'RIOT_ACCOUNT_NOT_FOUND' });
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('FakePlayer');
      await wrapper.find('#tagLine').setValue('XXX');
      await wrapper.find('#region').setValue('euw1');
      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(wrapper.text()).toContain('Riot account not found');
    });

    it('displays error for ACCOUNT_ALREADY_LINKED', async () => {
      mockLinkRiotAccount.mockRejectedValue({ code: 'ACCOUNT_ALREADY_LINKED' });
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(wrapper.text()).toContain('already linked to another user');
    });

    it('displays generic error message for unknown errors', async () => {
      mockLinkRiotAccount.mockRejectedValue({ message: 'Network error' });
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');
      await flushPromises();

      expect(wrapper.text()).toContain('Network error');
    });
  });

  describe('Modal Lifecycle', () => {
    it('emits close event when Cancel button is clicked', async () => {
      const wrapper = createWrapper();

      await wrapper.find('.btn-ghost').trigger('click');

      expect(wrapper.emitted('close')).toBeTruthy();
    });

    it('emits close event when clicking overlay', async () => {
      const wrapper = createWrapper();

      await wrapper.find('.modal-overlay').trigger('click');

      expect(wrapper.emitted('close')).toBeTruthy();
    });

    it('emits close event when clicking close button', async () => {
      const wrapper = createWrapper();

      await wrapper.find('.close-btn').trigger('click');

      expect(wrapper.emitted('close')).toBeTruthy();
    });

    it('does not close when clicking modal content', async () => {
      const wrapper = createWrapper();

      await wrapper.find('.modal-content').trigger('click');

      expect(wrapper.emitted('close')).toBeFalsy();
    });

    it('resets form when modal reopens', async () => {
      const wrapper = createWrapper({ isOpen: true });

      // Fill form
      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');

      // Close and reopen
      await wrapper.setProps({ isOpen: false });
      await wrapper.setProps({ isOpen: true });

      // Form should be reset
      expect(wrapper.find('#gameName').element.value).toBe('');
      expect(wrapper.find('#tagLine').element.value).toBe('');
    });

    it('disables close button while submitting', async () => {
      mockLinkRiotAccount.mockImplementation(() => new Promise(() => {}));
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');

      expect(wrapper.find('.close-btn').attributes('disabled')).toBeDefined();
    });

    it('disables Cancel button while submitting', async () => {
      mockLinkRiotAccount.mockImplementation(() => new Promise(() => {}));
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');

      expect(wrapper.find('.btn-ghost').attributes('disabled')).toBeDefined();
    });

    it('disables form inputs while submitting', async () => {
      mockLinkRiotAccount.mockImplementation(() => new Promise(() => {}));
      const wrapper = createWrapper();

      await wrapper.find('#gameName').setValue('Faker');
      await wrapper.find('#tagLine').setValue('KR1');
      await wrapper.find('#region').setValue('kr');
      await wrapper.find('form').trigger('submit');

      expect(wrapper.find('#gameName').attributes('disabled')).toBeDefined();
      expect(wrapper.find('#tagLine').attributes('disabled')).toBeDefined();
      expect(wrapper.find('#region').attributes('disabled')).toBeDefined();
    });
  });
});

