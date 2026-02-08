/**
 * BaseButton Component Tests
 *
 * Tests for the reusable button component supporting multiple variants,
 * sizes, loading states, and rendering as button, router-link, or anchor.
 */

import { describe, it, expect } from 'vitest';
import { mount, RouterLinkStub } from '@vue/test-utils';
import BaseButton from '@/components/base/BaseButton.vue';

describe('BaseButton', () => {
  describe('Rendering', () => {
    it('renders as a button by default', () => {
      const wrapper = mount(BaseButton, {
        slots: { default: 'Click me' }
      });
      expect(wrapper.element.tagName).toBe('BUTTON');
      expect(wrapper.text()).toBe('Click me');
    });

    it('renders as router-link when "to" prop is provided', () => {
      const wrapper = mount(BaseButton, {
        props: { to: '/dashboard' },
        slots: { default: 'Go to Dashboard' },
        global: { stubs: { RouterLink: RouterLinkStub } }
      });
      expect(wrapper.findComponent(RouterLinkStub).exists()).toBe(true);
    });

    it('renders as anchor when "href" prop is provided', () => {
      const wrapper = mount(BaseButton, {
        props: { href: 'https://example.com' },
        slots: { default: 'External Link' }
      });
      expect(wrapper.element.tagName).toBe('A');
      expect(wrapper.attributes('href')).toBe('https://example.com');
    });
  });

  describe('Variants', () => {
    it.each(['primary', 'secondary', 'ghost', 'destructive'])('applies %s variant class', (variant) => {
      const wrapper = mount(BaseButton, {
        props: { variant },
        slots: { default: 'Button' }
      });
      expect(wrapper.classes()).toContain(`btn--${variant}`);
    });

    it('defaults to primary variant', () => {
      const wrapper = mount(BaseButton, {
        slots: { default: 'Button' }
      });
      expect(wrapper.classes()).toContain('btn--primary');
    });
  });

  describe('Sizes', () => {
    it.each(['sm', 'md', 'lg'])('applies %s size class', (size) => {
      const wrapper = mount(BaseButton, {
        props: { size },
        slots: { default: 'Button' }
      });
      expect(wrapper.classes()).toContain(`btn--${size}`);
    });

    it('defaults to md size', () => {
      const wrapper = mount(BaseButton, {
        slots: { default: 'Button' }
      });
      expect(wrapper.classes()).toContain('btn--md');
    });
  });

  describe('Loading state', () => {
    it('shows spinner when loading', () => {
      const wrapper = mount(BaseButton, {
        props: { loading: true },
        slots: { default: 'Loading...' }
      });
      expect(wrapper.find('.btn-spinner').exists()).toBe(true);
    });

    it('applies loading class when loading', () => {
      const wrapper = mount(BaseButton, {
        props: { loading: true },
        slots: { default: 'Loading...' }
      });
      expect(wrapper.classes()).toContain('btn--loading');
    });

    it('disables button when loading', () => {
      const wrapper = mount(BaseButton, {
        props: { loading: true },
        slots: { default: 'Loading...' }
      });
      expect(wrapper.attributes('disabled')).toBeDefined();
    });

    it('does not show spinner when not loading', () => {
      const wrapper = mount(BaseButton, {
        props: { loading: false },
        slots: { default: 'Click me' }
      });
      expect(wrapper.find('.btn-spinner').exists()).toBe(false);
    });
  });

  describe('Disabled state', () => {
    it('applies disabled class when disabled', () => {
      const wrapper = mount(BaseButton, {
        props: { disabled: true },
        slots: { default: 'Disabled' }
      });
      expect(wrapper.classes()).toContain('btn--disabled');
    });

    it('sets disabled attribute on button', () => {
      const wrapper = mount(BaseButton, {
        props: { disabled: true },
        slots: { default: 'Disabled' }
      });
      expect(wrapper.attributes('disabled')).toBeDefined();
    });

    it('does not set disabled attribute on anchor', () => {
      const wrapper = mount(BaseButton, {
        props: { href: 'https://example.com', disabled: true },
        slots: { default: 'Link' }
      });
      expect(wrapper.attributes('disabled')).toBeUndefined();
    });
  });

  describe('Block mode', () => {
    it('applies block class when block prop is true', () => {
      const wrapper = mount(BaseButton, {
        props: { block: true },
        slots: { default: 'Full Width' }
      });
      expect(wrapper.classes()).toContain('btn--block');
    });
  });

  describe('Button type', () => {
    it('defaults to type="button"', () => {
      const wrapper = mount(BaseButton, {
        slots: { default: 'Button' }
      });
      expect(wrapper.attributes('type')).toBe('button');
    });

    it('accepts custom type', () => {
      const wrapper = mount(BaseButton, {
        props: { type: 'submit' },
        slots: { default: 'Submit' }
      });
      expect(wrapper.attributes('type')).toBe('submit');
    });

    it('does not set type on anchor', () => {
      const wrapper = mount(BaseButton, {
        props: { href: 'https://example.com' },
        slots: { default: 'Link' }
      });
      expect(wrapper.attributes('type')).toBeUndefined();
    });
  });

  describe('Slots', () => {
    it('renders default slot content', () => {
      const wrapper = mount(BaseButton, {
        slots: { default: 'Button Text' }
      });
      expect(wrapper.text()).toContain('Button Text');
    });

    it('renders icon-left slot', () => {
      const wrapper = mount(BaseButton, {
        slots: {
          default: 'Button',
          'icon-left': '<span class="test-icon-left">←</span>'
        }
      });
      expect(wrapper.find('.test-icon-left').exists()).toBe(true);
    });

    it('renders icon-right slot', () => {
      const wrapper = mount(BaseButton, {
        slots: {
          default: 'Button',
          'icon-right': '<span class="test-icon-right">→</span>'
        }
      });
      expect(wrapper.find('.test-icon-right').exists()).toBe(true);
    });

    it('renders both icon slots together', () => {
      const wrapper = mount(BaseButton, {
        slots: {
          default: 'Button',
          'icon-left': '<span class="left">←</span>',
          'icon-right': '<span class="right">→</span>'
        }
      });
      expect(wrapper.find('.left').exists()).toBe(true);
      expect(wrapper.find('.right').exists()).toBe(true);
    });
  });

  describe('CSS classes', () => {
    it('always has btn base class', () => {
      const wrapper = mount(BaseButton, {
        slots: { default: 'Button' }
      });
      expect(wrapper.classes()).toContain('btn');
    });

    it('combines multiple classes correctly', () => {
      const wrapper = mount(BaseButton, {
        props: { variant: 'destructive', size: 'lg', block: true },
        slots: { default: 'Delete' }
      });
      expect(wrapper.classes()).toContain('btn');
      expect(wrapper.classes()).toContain('btn--destructive');
      expect(wrapper.classes()).toContain('btn--lg');
      expect(wrapper.classes()).toContain('btn--block');
    });
  });
});

