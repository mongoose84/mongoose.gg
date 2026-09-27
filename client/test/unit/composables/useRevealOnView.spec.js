import { describe, it, expect, vi, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { vRevealOnView } from '@/composables/useRevealOnView';

const TestSection = {
  directives: { revealOnView: vRevealOnView },
  template: '<section v-reveal-on-view data-testid="section">Content</section>'
};

function mockMatchMedia(reduce) {
  vi.stubGlobal('matchMedia', vi.fn(() => ({ matches: reduce })));
  window.matchMedia = globalThis.matchMedia;
}

describe('vRevealOnView', () => {
  let observerCallback;
  const observe = vi.fn();
  const disconnect = vi.fn();

  function mockObserver() {
    vi.stubGlobal('IntersectionObserver', vi.fn(function (callback) {
      observerCallback = callback;
      this.observe = observe;
      this.disconnect = disconnect;
    }));
  }

  afterEach(() => {
    vi.unstubAllGlobals();
    observe.mockClear();
    disconnect.mockClear();
  });

  it('hides the section until it scrolls into view, then reveals it once', () => {
    mockMatchMedia(false);
    mockObserver();
    const wrapper = mount(TestSection);
    const el = wrapper.get('[data-testid="section"]').element;

    expect(el.classList.contains('mp-reveal')).toBe(true);
    expect(el.classList.contains('mp-reveal--visible')).toBe(false);

    observerCallback([{ isIntersecting: true }]);
    expect(el.classList.contains('mp-reveal--visible')).toBe(true);
    expect(disconnect).toHaveBeenCalled();
  });

  it('does nothing with prefers-reduced-motion', () => {
    mockMatchMedia(true);
    mockObserver();
    const wrapper = mount(TestSection);
    const el = wrapper.get('[data-testid="section"]').element;

    expect(el.classList.contains('mp-reveal')).toBe(false);
    expect(observe).not.toHaveBeenCalled();
  });

  it('leaves the section visible when IntersectionObserver is missing', () => {
    mockMatchMedia(false);
    vi.stubGlobal('IntersectionObserver', undefined);
    const wrapper = mount(TestSection);

    expect(wrapper.get('[data-testid="section"]').classes()).not.toContain('mp-reveal');
  });
});
