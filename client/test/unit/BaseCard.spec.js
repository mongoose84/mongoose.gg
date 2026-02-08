/**
 * BaseCard Component Tests
 *
 * Tests for the reusable card component supporting multiple variants,
 * slots (header, body, footer), and clickable states.
 */

import { describe, it, expect } from 'vitest';
import { mount, RouterLinkStub } from '@vue/test-utils';
import BaseCard from '@/components/base/BaseCard.vue';

describe('BaseCard', () => {
  describe('Rendering', () => {
    it('renders as a div by default', () => {
      const wrapper = mount(BaseCard, {
        slots: { default: 'Card content' }
      });
      expect(wrapper.element.tagName).toBe('DIV');
    });

    it('renders default slot content in card-body', () => {
      const wrapper = mount(BaseCard, {
        slots: { default: 'Card content' }
      });
      expect(wrapper.find('.card-body').text()).toBe('Card content');
    });

    it('renders as router-link when "to" prop is provided', () => {
      const wrapper = mount(BaseCard, {
        props: { to: '/details' },
        slots: { default: 'Clickable card' },
        global: { stubs: { RouterLink: RouterLinkStub } }
      });
      expect(wrapper.findComponent(RouterLinkStub).exists()).toBe(true);
    });

    it('renders as anchor when "href" prop is provided', () => {
      const wrapper = mount(BaseCard, {
        props: { href: 'https://example.com' },
        slots: { default: 'External link card' }
      });
      expect(wrapper.element.tagName).toBe('A');
      expect(wrapper.attributes('href')).toBe('https://example.com');
    });
  });

  describe('Variants', () => {
    it.each(['default', 'interactive', 'highlighted', 'elevated'])('applies %s variant class', (variant) => {
      const wrapper = mount(BaseCard, {
        props: { variant },
        slots: { default: 'Content' }
      });
      expect(wrapper.classes()).toContain(`card--${variant}`);
    });

    it('defaults to default variant', () => {
      const wrapper = mount(BaseCard, {
        slots: { default: 'Content' }
      });
      expect(wrapper.classes()).toContain('card--default');
    });
  });

  describe('Header', () => {
    it('renders header when title prop is provided', () => {
      const wrapper = mount(BaseCard, {
        props: { title: 'Card Title' },
        slots: { default: 'Content' }
      });
      expect(wrapper.find('.card-header').exists()).toBe(true);
      expect(wrapper.find('.card-title').text()).toBe('Card Title');
    });

    it('renders subtitle when provided', () => {
      const wrapper = mount(BaseCard, {
        props: { title: 'Title', subtitle: 'Subtitle text' },
        slots: { default: 'Content' }
      });
      expect(wrapper.find('.card-subtitle').text()).toBe('Subtitle text');
    });

    it('does not render header when no title or header slot', () => {
      const wrapper = mount(BaseCard, {
        slots: { default: 'Content only' }
      });
      expect(wrapper.find('.card-header').exists()).toBe(false);
    });

    it('renders header slot content', () => {
      const wrapper = mount(BaseCard, {
        slots: {
          default: 'Content',
          header: '<div class="custom-header">Custom Header</div>'
        }
      });
      expect(wrapper.find('.card-header').exists()).toBe(true);
      expect(wrapper.find('.custom-header').text()).toBe('Custom Header');
    });

    it('header slot overrides title prop', () => {
      const wrapper = mount(BaseCard, {
        props: { title: 'Prop Title' },
        slots: {
          default: 'Content',
          header: '<span>Slot Header</span>'
        }
      });
      expect(wrapper.find('.card-title').exists()).toBe(false);
      expect(wrapper.find('.card-header').text()).toBe('Slot Header');
    });
  });

  describe('Footer', () => {
    it('renders footer slot when provided', () => {
      const wrapper = mount(BaseCard, {
        slots: {
          default: 'Content',
          footer: '<button>Action</button>'
        }
      });
      expect(wrapper.find('.card-footer').exists()).toBe(true);
      expect(wrapper.find('.card-footer button').exists()).toBe(true);
    });

    it('does not render footer when no footer slot', () => {
      const wrapper = mount(BaseCard, {
        slots: { default: 'Content' }
      });
      expect(wrapper.find('.card-footer').exists()).toBe(false);
    });
  });

  describe('Body padding', () => {
    it('has padding by default', () => {
      const wrapper = mount(BaseCard, {
        slots: { default: 'Content' }
      });
      expect(wrapper.find('.card-body').classes()).not.toContain('card-body--no-padding');
    });

    it('removes padding when noPadding is true', () => {
      const wrapper = mount(BaseCard, {
        props: { noPadding: true },
        slots: { default: 'Content' }
      });
      expect(wrapper.find('.card-body').classes()).toContain('card-body--no-padding');
    });
  });

  describe('Clickable state', () => {
    it('adds clickable class when "to" is provided', () => {
      const wrapper = mount(BaseCard, {
        props: { to: '/page' },
        slots: { default: 'Content' },
        global: { stubs: { RouterLink: RouterLinkStub } }
      });
      expect(wrapper.classes()).toContain('card--clickable');
    });

    it('adds clickable class when "href" is provided', () => {
      const wrapper = mount(BaseCard, {
        props: { href: 'https://example.com' },
        slots: { default: 'Content' }
      });
      expect(wrapper.classes()).toContain('card--clickable');
    });

    it('does not add clickable class when neither to nor href', () => {
      const wrapper = mount(BaseCard, {
        slots: { default: 'Content' }
      });
      expect(wrapper.classes()).not.toContain('card--clickable');
    });
  });
});

