import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { ref, computed, nextTick } from 'vue';
import AnalysisStatusCard from '@/components/overview/AnalysisStatusCard.vue';

// Create reactive refs for mock
const mockStatus = ref('idle');
const mockIsRunning = ref(false);
const mockIsRateLimited = ref(false);
const mockHasFailed = ref(false);
const mockIsUpToDate = ref(false);
const mockIsLoading = ref(false);
const mockProgress = ref({ current: 0, total: 0 });
const mockErrorMessage = ref(null);
const mockLastSyncAt = ref(null);
const mockLoadStatus = vi.fn();
const mockTriggerAnalysis = vi.fn();
const mockClearError = vi.fn();

vi.mock('@/composables/useAnalysisStatus', () => ({
  useAnalysisStatus: () => ({
    status: mockStatus,
    isRunning: mockIsRunning,
    isRateLimited: mockIsRateLimited,
    hasFailed: mockHasFailed,
    isUpToDate: mockIsUpToDate,
    isLoading: mockIsLoading,
    progress: mockProgress,
    errorMessage: mockErrorMessage,
    lastSyncAt: mockLastSyncAt,
    loadStatus: mockLoadStatus,
    triggerAnalysis: mockTriggerAnalysis,
    clearError: mockClearError
  })
}));

// Mock apiClient
vi.mock('@/services/apiClient', () => ({
  setSessionExpiredCallback: vi.fn()
}));

describe('AnalysisStatusCard', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();

    // Reset mock values
    mockStatus.value = 'idle';
    mockIsRunning.value = false;
    mockIsRateLimited.value = false;
    mockHasFailed.value = false;
    mockIsUpToDate.value = false;
    mockIsLoading.value = false;
    mockProgress.value = { current: 0, total: 0 };
    mockErrorMessage.value = null;
    mockLastSyncAt.value = null;
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  function mountCard() {
    return mount(AnalysisStatusCard, {
      global: {
        stubs: {
          BaseCard: {
            template: '<div class="base-card"><slot /></div>'
          },
          BaseButton: {
            template: '<button :disabled="disabled || loading" @click="$emit(\'click\')"><slot /></button>',
            props: ['loading', 'disabled'],
            emits: ['click']
          },
          CheckCircleIcon: { template: '<svg data-testid="check-icon" />' },
          ClockIcon: { template: '<svg data-testid="clock-icon" />' },
          ExclamationCircleIcon: { template: '<svg data-testid="error-icon" />' }
        }
      }
    });
  }

  describe('mounting', () => {
    it('calls loadStatus on mount', async () => {
      mountCard();
      await flushPromises();
      expect(mockLoadStatus).toHaveBeenCalled();
    });
  });

  describe('idle state', () => {
    it('displays "Ready to analyze" when idle without lastSyncAt', () => {
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Ready to analyze');
    });

    it('shows idle status dot', () => {
      const wrapper = mountCard();
      expect(wrapper.find('.status-dot--idle').exists()).toBe(true);
    });

    it('shows Analyze button', () => {
      const wrapper = mountCard();
      expect(wrapper.find('button').text()).toBe('Analyze');
    });
  });

  describe('running state', () => {
    beforeEach(() => {
      mockIsRunning.value = true;
      mockStatus.value = 'syncing';
    });

    it('displays "Analyzing games..." when running', () => {
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Analyzing games...');
    });

    it('shows spinner when running', () => {
      const wrapper = mountCard();
      expect(wrapper.find('.status-spinner').exists()).toBe(true);
    });

    it('shows progress bar when total > 0', () => {
      mockProgress.value = { current: 3, total: 10 };
      const wrapper = mountCard();
      expect(wrapper.find('.progress-bar').exists()).toBe(true);
      expect(wrapper.text()).toContain('3 / 10');
    });

    it('shows progress subtitle', () => {
      mockProgress.value = { current: 5, total: 20 };
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Processing match 5 of 20');
    });

    it('does not show action button when running', () => {
      const wrapper = mountCard();
      // showActionButton = hasFailed || !isActiveOrPending, so hidden when running
      expect(wrapper.find('button').exists()).toBe(false);
    });
  });

  describe('rate limited state', () => {
    beforeEach(() => {
      mockIsRunning.value = true;
      mockIsRateLimited.value = true;
    });

    it('displays "Waiting on Riot API..." when rate limited', () => {
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Waiting on Riot API...');
    });

    it('shows clock icon when rate limited', () => {
      const wrapper = mountCard();
      // Since icons are stubbed, check for the svg element
      expect(wrapper.find('svg').exists()).toBe(true);
    });

    it('shows rate limit subtitle', () => {
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Rate limit reached, resuming shortly');
    });
  });

  describe('error state', () => {
    beforeEach(() => {
      mockHasFailed.value = true;
      mockStatus.value = 'failed';
      mockErrorMessage.value = 'Connection timeout';
    });

    it('displays "Analysis failed" when failed', () => {
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Analysis failed');
    });

    it('shows error icon when failed', () => {
      const wrapper = mountCard();
      // Since icons are stubbed, check for the svg element
      expect(wrapper.find('svg').exists()).toBe(true);
    });

    it('shows error message as subtitle', () => {
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Connection timeout');
    });

    it('shows Retry button when failed', () => {
      const wrapper = mountCard();
      expect(wrapper.find('button').text()).toBe('Retry');
    });

    it('calls clearError and triggerAnalysis on retry click', async () => {
      const wrapper = mountCard();
      await wrapper.find('button').trigger('click');

      expect(mockClearError).toHaveBeenCalled();
      expect(mockTriggerAnalysis).toHaveBeenCalled();
    });
  });

  describe('up-to-date state', () => {
    beforeEach(() => {
      mockIsUpToDate.value = true;
      mockStatus.value = 'completed';
      mockLastSyncAt.value = '2026-02-01T12:00:00Z';
    });

    it('displays "Analysis up to date" when up to date', () => {
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Analysis up to date');
    });

    it('shows check icon when up to date', () => {
      const wrapper = mountCard();
      // Since icons are stubbed, check for the svg element
      expect(wrapper.find('svg').exists()).toBe(true);
    });

    it('shows last updated time in subtitle', () => {
      const wrapper = mountCard();
      expect(wrapper.text()).toContain('Last updated');
    });

    it('shows Analyze button to re-analyze', () => {
      const wrapper = mountCard();
      expect(wrapper.find('button').text()).toBe('Analyze');
    });
  });

  describe('optimistic pending state', () => {
    it('shows spinner and "Starting analysis..." immediately on click before WebSocket update', async () => {
      mockTriggerAnalysis.mockResolvedValue(true);
      const wrapper = mountCard();

      await wrapper.find('button').trigger('click');
      // Don't flush promises — check the optimistic state synchronously
      expect(wrapper.text()).toContain('Starting analysis...');
      expect(wrapper.find('.status-spinner').exists()).toBe(true);
    });

    it('hides the action button while optimistically pending', async () => {
      mockTriggerAnalysis.mockResolvedValue(true);
      const wrapper = mountCard();

      await wrapper.find('button').trigger('click');
      expect(wrapper.find('button').exists()).toBe(false);
    });

    it('restores action button if triggerAnalysis fails', async () => {
      mockTriggerAnalysis.mockResolvedValue(false);
      const wrapper = mountCard();

      await wrapper.find('button').trigger('click');
      await flushPromises();
      expect(wrapper.find('button').exists()).toBe(true);
    });

    it('clears "Last updated" subtitle while HTTP request is in-flight after clicking from up-to-date state', async () => {
      mockIsUpToDate.value = true;
      mockLastSyncAt.value = '2026-04-04T12:00:00Z';

      // Use a deferred promise so the HTTP request stays in-flight and we can
      // inspect the optimistic (isPending = true) UI state before it resolves.
      let resolve;
      mockTriggerAnalysis.mockReturnValue(new Promise(r => { resolve = r; }));
      const wrapper = mountCard();

      expect(wrapper.text()).toContain('Last updated');

      // Don't await — let handleAction run up to its first `await` only
      wrapper.find('button').trigger('click');
      await nextTick();

      // While in-flight: isPending = true, subtitle should be hidden
      expect(wrapper.text()).not.toContain('Last updated');

      resolve(true);
    });

    it('clears isPending after triggerAnalysis resolves when isUpToDate was already true (WS delivers no change)', async () => {
      // Simulate a user who has synced before: isUpToDate starts true.
      // The WS only sends sync_complete (no progress events), so isUpToDate
      // never changes and the watcher never fires. The immediate post-await
      // check must clear isPending instead.
      mockIsUpToDate.value = true;
      mockLastSyncAt.value = '2026-04-04T12:00:00Z';
      mockTriggerAnalysis.mockResolvedValue(true);
      const wrapper = mountCard();

      await wrapper.find('button').trigger('click');
      await flushPromises();

      // isPending should be false — Analyze button visible again, no spinner
      expect(wrapper.find('button').exists()).toBe(true);
      expect(wrapper.find('.status-spinner').exists()).toBe(false);
    });

    it('clears isPending via 30-second safety timeout when WS never delivers a status update', async () => {
      vi.useFakeTimers();
      // Status is not settled: no isRunning, isUpToDate, or hasFailed
      mockTriggerAnalysis.mockResolvedValue(true);
      const wrapper = mountCard();

      await wrapper.find('button').trigger('click');
      await flushPromises();

      // Still pending — no WS update has arrived
      expect(wrapper.find('.status-spinner').exists()).toBe(true);

      // Advance past the 30-second safety timeout
      vi.advanceTimersByTime(30_000);
      await flushPromises();

      expect(wrapper.find('.status-spinner').exists()).toBe(false);
      expect(wrapper.find('button').exists()).toBe(true);

      vi.useRealTimers();
    });
  });

  describe('action button', () => {
    it('calls triggerAnalysis on Analyze click', async () => {
      const wrapper = mountCard();
      await wrapper.find('button').trigger('click');

      expect(mockTriggerAnalysis).toHaveBeenCalled();
    });

    it('is disabled when loading', () => {
      mockIsLoading.value = true;
      const wrapper = mountCard();

      // Button should have loading state
      expect(wrapper.find('button').attributes('disabled')).toBeDefined();
    });
  });

  describe('progress bar', () => {
    it('calculates correct progress percentage', () => {
      mockIsRunning.value = true;
      mockProgress.value = { current: 25, total: 100 };

      const wrapper = mountCard();
      const progressFill = wrapper.find('.progress-bar__fill');

      expect(progressFill.attributes('style')).toContain('width: 25%');
    });

    it('applies rate-limited class when rate limited', () => {
      mockIsRunning.value = true;
      mockIsRateLimited.value = true;
      mockProgress.value = { current: 5, total: 10 };

      const wrapper = mountCard();
      expect(wrapper.find('.progress-bar__fill--rate-limited').exists()).toBe(true);
    });

    it('does not show progress bar when total is 0', () => {
      mockIsRunning.value = true;
      mockProgress.value = { current: 0, total: 0 };

      const wrapper = mountCard();
      expect(wrapper.find('.progress-bar').exists()).toBe(false);
    });
  });
});

