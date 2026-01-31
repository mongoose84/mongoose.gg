/**
 * Test Utilities
 * 
 * Common utilities for setting up and running tests.
 * Provides consistent patterns for mounting components, setting up stores,
 * and handling async operations.
 */

import { mount, shallowMount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { vi } from 'vitest';

// ============ Store Setup ============

/**
 * Creates and activates a fresh Pinia instance for testing
 * Call in beforeEach to ensure test isolation
 */
export function setupPinia() {
  const pinia = createPinia();
  setActivePinia(pinia);
  return pinia;
}

// ============ Component Mounting Helpers ============

/**
 * Creates default HeadlessUI stubs for modal/dialog testing
 * Use in global.stubs when mounting components that use HeadlessUI
 */
export const headlessUIStubs = {
  TransitionRoot: {
    template: '<div v-if="show"><slot /></div>',
    props: ['show']
  },
  TransitionChild: {
    template: '<div><slot /></div>'
  },
  Dialog: {
    template: '<div class="modal-root" data-testid="modal-overlay"><slot /></div>',
    props: ['as']
  },
  DialogPanel: {
    template: '<div class="modal-panel" data-testid="modal-content"><slot /></div>',
    props: ['class']
  },
  DialogTitle: {
    template: '<h2><slot /></h2>',
    props: ['as']
  },
  Menu: {
    template: '<div class="menu"><slot /></div>',
    props: ['as']
  },
  MenuButton: {
    template: '<button class="menu-button"><slot /></button>'
  },
  MenuItems: {
    template: '<div class="menu-items"><slot /></div>',
    props: ['as']
  },
  MenuItem: {
    template: '<div class="menu-item"><slot /></div>',
    props: ['as']
  },
  Listbox: {
    template: '<div class="listbox"><slot /></div>',
    props: ['modelValue']
  },
  ListboxButton: {
    template: '<button class="listbox-button"><slot /></button>'
  },
  ListboxOptions: {
    template: '<ul class="listbox-options"><slot /></ul>'
  },
  ListboxOption: {
    template: '<li class="listbox-option"><slot /></li>',
    props: ['value']
  }
};

/**
 * Creates a wrapper with common test configuration.
 *
 * When using `attachToBody: true`, the returned object includes a `cleanup()`
 * function that should be called in afterEach to remove the container from the DOM.
 *
 * @param {Object} component - Vue component to mount
 * @param {Object} options - Mount options (props, stubs, etc.)
 * @param {boolean} options.attachToBody - If true, attaches to document.body (for modals/portals)
 * @param {boolean} options.shallow - If true, uses shallowMount instead of mount
 * @returns {Object} Vue Test Utils wrapper, with added `cleanup()` method when attachToBody is true
 *
 * @example
 * // For modals/portals that need body attachment:
 * let wrapper;
 * beforeEach(() => {
 *   wrapper = createWrapper(MyModal, { attachToBody: true, props: { isOpen: true } });
 * });
 * afterEach(() => {
 *   wrapper.cleanup(); // Removes container from DOM
 * });
 */
export function createWrapper(component, options = {}) {
  const {
    props = {},
    stubs = {},
    attachToBody = false,
    shallow = false,
    ...rest
  } = options;

  const mountFn = shallow ? shallowMount : mount;
  const mountOptions = {
    props,
    global: {
      stubs: {
        ...headlessUIStubs,
        ...stubs
      }
    },
    ...rest
  };

  let container = null;

  // Optionally attach to document body (needed for modals/portals)
  if (attachToBody) {
    container = document.createElement('div');
    document.body.appendChild(container);
    mountOptions.attachTo = container;
  }

  const wrapper = mountFn(component, mountOptions);

  // Add cleanup method to remove container from DOM
  wrapper.cleanup = () => {
    wrapper.unmount();
    if (container && container.parentNode) {
      container.parentNode.removeChild(container);
    }
  };

  return wrapper;
}

// ============ Async Helpers ============

/**
 * Wait for a specific condition to be true.
 *
 * WARNING: This function uses real timers (Date.now, setTimeout).
 * It will DEADLOCK if vi.useFakeTimers() is active because the clock
 * won't advance. Either:
 *   1. Call vi.useRealTimers() before using waitFor(), or
 *   2. Use vi.waitFor() from Vitest which handles fake timers, or
 *   3. Manually advance timers with vi.advanceTimersByTimeAsync()
 *
 * @param {Function} condition - Function that returns true when condition is met
 * @param {number} timeout - Maximum time to wait in ms (default: 1000)
 * @param {number} interval - Check interval in ms (default: 50)
 * @throws {Error} If condition is not met within timeout, or if fake timers detected
 */
export async function waitFor(condition, timeout = 1000, interval = 50) {
  // Guard against fake timers - check if setTimeout is mocked
  // This is a heuristic: if vi.isFakeTimers exists and returns true, warn
  if (typeof vi !== 'undefined' && vi.isFakeTimers && vi.isFakeTimers()) {
    throw new Error(
      'waitFor() cannot be used with fake timers - it will deadlock. ' +
      'Call vi.useRealTimers() first, or use vi.advanceTimersByTimeAsync() instead.'
    );
  }

  const startTime = Date.now();

  while (Date.now() - startTime < timeout) {
    if (condition()) return true;
    await new Promise(resolve => setTimeout(resolve, interval));
  }

  throw new Error(`waitFor condition not met within ${timeout}ms`);
}

/**
 * Creates a deferred promise for testing async flows
 * Returns { promise, resolve, reject } to control resolution timing
 */
export function createDeferredPromise() {
  let resolve, reject;
  const promise = new Promise((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

// ============ Mock Helpers ============

// Store original fetch to enable proper restoration
let originalFetch = null;

/**
 * Creates a mock fetch function with configurable responses.
 * IMPORTANT: Call restoreFetch() in afterEach to prevent test pollution.
 * @param {Object} defaultResponse - Default response for unmocked URLs
 * @returns {Function} The mock fetch function
 */
export function createMockFetch(defaultResponse = { ok: true }) {
  // Store original only on first call to avoid overwriting with a mock
  if (originalFetch === null) {
    originalFetch = global.fetch;
  }
  const mockFetch = vi.fn().mockResolvedValue(defaultResponse);
  global.fetch = mockFetch;
  return mockFetch;
}

/**
 * Restores the original fetch function.
 * Call this in afterEach when using createMockFetch.
 */
export function restoreFetch() {
  if (originalFetch !== null) {
    global.fetch = originalFetch;
    originalFetch = null;
  }
}

/**
 * Clears all mocks and restores original implementations.
 * Also restores fetch if it was mocked via createMockFetch.
 * Call in afterEach for clean test isolation.
 */
export function cleanupMocks() {
  vi.clearAllMocks();
  vi.restoreAllMocks();
  restoreFetch();
}

// ============ DOM Helpers ============

/**
 * Finds an element by data-testid attribute
 * @param {Wrapper} wrapper - Vue Test Utils wrapper
 * @param {string} testId - The data-testid value
 */
export function findByTestId(wrapper, testId) {
  return wrapper.find(`[data-testid="${testId}"]`);
}

/**
 * Finds all elements by data-testid attribute
 * @param {Wrapper} wrapper - Vue Test Utils wrapper
 * @param {string} testId - The data-testid value
 */
export function findAllByTestId(wrapper, testId) {
  return wrapper.findAll(`[data-testid="${testId}"]`);
}

