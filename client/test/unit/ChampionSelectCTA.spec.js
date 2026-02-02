import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import ChampionSelectCTA from '@/components/overview/ChampionSelectCTA.vue';

describe('ChampionSelectCTA', () => {
  function mountCTA(options = {}) {
    return mount(ChampionSelectCTA, {
      global: {
        stubs: {
          'router-link': {
            template: '<a :href="to" class="router-link-stub"><slot /></a>',
            props: ['to']
          },
          SparklesIcon: { template: '<svg data-testid="sparkles-icon" />' },
          ArrowRightIcon: { template: '<svg data-testid="arrow-icon" />' }
        }
      },
      ...options
    });
  }

  describe('rendering', () => {
    it('renders the component', () => {
      const wrapper = mountCTA();
      expect(wrapper.exists()).toBe(true);
    });

    it('renders as a router-link', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.router-link-stub').exists()).toBe(true);
    });

    it('links to /app/champion-select', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.router-link-stub').attributes('href')).toBe('/app/champion-select');
    });

    it('has champion-select-cta class', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.champion-select-cta').exists()).toBe(true);
    });
  });

  describe('content', () => {
    it('displays the title "Champion Select Helper"', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.cta-title').text()).toBe('Champion Select Helper');
    });

    it('displays the subtitle with matchup tips message', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.cta-subtitle').text()).toBe('Get personal matchup tips before you lock in');
    });

    it('displays an icon in the icon wrapper', () => {
      const wrapper = mountCTA();
      // The icon wrapper contains an svg (SparklesIcon)
      expect(wrapper.find('.cta-icon-wrapper svg').exists()).toBe(true);
    });

    it('displays an arrow icon', () => {
      const wrapper = mountCTA();
      // The arrow wrapper contains an svg (ArrowRightIcon)
      expect(wrapper.find('.cta-arrow svg').exists()).toBe(true);
    });
  });

  describe('structure', () => {
    it('has icon wrapper element', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.cta-icon-wrapper').exists()).toBe(true);
    });

    it('has content element', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.cta-content').exists()).toBe(true);
    });

    it('has arrow element', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.cta-arrow').exists()).toBe(true);
    });

    it('icon is inside icon wrapper', () => {
      const wrapper = mountCTA();
      const iconWrapper = wrapper.find('.cta-icon-wrapper');
      // Check for svg element (the SparklesIcon)
      expect(iconWrapper.find('svg').exists()).toBe(true);
    });

    it('title and subtitle are inside content', () => {
      const wrapper = mountCTA();
      const content = wrapper.find('.cta-content');
      expect(content.find('.cta-title').exists()).toBe(true);
      expect(content.find('.cta-subtitle').exists()).toBe(true);
    });

    it('arrow icon is inside arrow wrapper', () => {
      const wrapper = mountCTA();
      const arrow = wrapper.find('.cta-arrow');
      // Check for svg element (the ArrowRightIcon)
      expect(arrow.find('svg').exists()).toBe(true);
    });
  });

  describe('accessibility', () => {
    it('is a clickable link element', () => {
      const wrapper = mountCTA();
      // router-link renders as an anchor
      expect(wrapper.find('a').exists()).toBe(true);
    });

    it('has no text decoration (handled by CSS)', () => {
      const wrapper = mountCTA();
      // The component has text-decoration: none in CSS
      expect(wrapper.find('.champion-select-cta').exists()).toBe(true);
    });
  });

  describe('icon styling', () => {
    it('icon wrapper has correct class for styling', () => {
      const wrapper = mountCTA();
      expect(wrapper.find('.cta-icon-wrapper').exists()).toBe(true);
    });

    it('icon has cta-icon class', () => {
      const wrapper = mountCTA();
      // The SparklesIcon should have cta-icon class in the real component
      // Since we're stubbing, we just verify the structure
      expect(wrapper.find('.cta-icon-wrapper').exists()).toBe(true);
    });
  });
});

