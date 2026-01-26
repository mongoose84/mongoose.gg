import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import OverviewPlayerHeader from '@/components/overview/OverviewPlayerHeader.vue';

describe('OverviewPlayerHeader.vue', () => {
  const defaultProps = {
    summonerName: 'TestPlayer',
    level: 150,
    region: 'EUW',
    profileIconUrl: 'https://ddragon.leagueoflegends.com/cdn/16.1.1/img/profileicon/29.png',
    activeContexts: ['Solo', 'Duo']
  };

  const createWrapper = (props = {}) => {
    return mount(OverviewPlayerHeader, {
      props: { ...defaultProps, ...props }
    });
  };

  describe('Rendering', () => {
    it('renders the component', () => {
      const wrapper = createWrapper();
      expect(wrapper.exists()).toBe(true);
    });

    it('renders as a section element', () => {
      const wrapper = createWrapper();
      expect(wrapper.find('section.overview-player-header').exists()).toBe(true);
    });

    it('displays the summoner name', () => {
      const wrapper = createWrapper({ summonerName: 'Faker' });
      expect(wrapper.find('.summoner-name').text()).toBe('Faker');
    });

    it('displays the region', () => {
      const wrapper = createWrapper({ region: 'kr' });
      expect(wrapper.find('.region-tag').text()).toBe('KR');
    });

    it('converts eun1 to EUNE', () => {
      const wrapper = createWrapper({ region: 'eun1' });
      expect(wrapper.find('.region-tag').text()).toBe('EUNE');
    });

    it('converts euw1 to EUW', () => {
      const wrapper = createWrapper({ region: 'euw1' });
      expect(wrapper.find('.region-tag').text()).toBe('EUW');
    });

    it('converts na1 to NA', () => {
      const wrapper = createWrapper({ region: 'na1' });
      expect(wrapper.find('.region-tag').text()).toBe('NA');
    });

    it('handles unknown regions by uppercasing', () => {
      const wrapper = createWrapper({ region: 'unknown' });
      expect(wrapper.find('.region-tag').text()).toBe('UNKNOWN');
    });
  });

  describe('Profile Icon', () => {
    it('displays profile icon when profileIconUrl is provided', () => {
      const wrapper = createWrapper();
      const img = wrapper.find('.profile-icon');
      expect(img.exists()).toBe(true);
      expect(img.attributes('src')).toBe(defaultProps.profileIconUrl);
    });

    it('displays fallback SVG when profileIconUrl is null', () => {
      const wrapper = createWrapper({ profileIconUrl: null });
      expect(wrapper.find('.profile-icon').exists()).toBe(false);
      expect(wrapper.find('.profile-icon-fallback').exists()).toBe(true);
    });

    it('has correct alt text for profile icon', () => {
      const wrapper = createWrapper({ summonerName: 'Faker' });
      const img = wrapper.find('.profile-icon');
      expect(img.attributes('alt')).toBe('Faker profile icon');
    });
  });

  describe('Level Badge', () => {
    it('displays level badge when level is provided', () => {
      const wrapper = createWrapper({ level: 500 });
      const badge = wrapper.find('.level-badge');
      expect(badge.exists()).toBe(true);
      expect(badge.text()).toBe('500');
    });

    it('does not display level badge when level is null', () => {
      const wrapper = createWrapper({ level: null });
      expect(wrapper.find('.level-badge').exists()).toBe(false);
    });

    it('does not display level badge when level is 0', () => {
      const wrapper = createWrapper({ level: 0 });
      // 0 is falsy, so badge should not show
      expect(wrapper.find('.level-badge').exists()).toBe(false);
    });
  });

  describe('Context Badges', () => {
    it('displays context badges for each active context', () => {
      const wrapper = createWrapper({ activeContexts: ['Solo', 'Duo', 'Team'] });
      const badges = wrapper.findAll('.context-badge');
      expect(badges.length).toBe(3);
    });

    it('displays correct text for Solo context', () => {
      const wrapper = createWrapper({ activeContexts: ['Solo'] });
      expect(wrapper.find('.context-badge').text()).toBe('Solo');
    });

    it('displays correct text for Duo context', () => {
      const wrapper = createWrapper({ activeContexts: ['Duo'] });
      expect(wrapper.find('.context-badge').text()).toBe('Duo');
    });

    it('displays correct text for Team context', () => {
      const wrapper = createWrapper({ activeContexts: ['Team'] });
      expect(wrapper.find('.context-badge').text()).toBe('Team');
    });

    it('applies correct CSS class for Solo context', () => {
      const wrapper = createWrapper({ activeContexts: ['Solo'] });
      expect(wrapper.find('.context-badge.context-solo').exists()).toBe(true);
    });

    it('applies correct CSS class for Duo context', () => {
      const wrapper = createWrapper({ activeContexts: ['Duo'] });
      expect(wrapper.find('.context-badge.context-duo').exists()).toBe(true);
    });

    it('applies correct CSS class for Team context', () => {
      const wrapper = createWrapper({ activeContexts: ['Team'] });
      expect(wrapper.find('.context-badge.context-team').exists()).toBe(true);
    });

    it('does not display context badges when activeContexts is empty', () => {
      const wrapper = createWrapper({ activeContexts: [] });
      expect(wrapper.find('.context-badges').exists()).toBe(false);
    });

    it('does not display context badges when activeContexts is undefined', () => {
      const wrapper = mount(OverviewPlayerHeader, {
        props: {
          summonerName: 'Test',
          region: 'EUW'
        }
      });
      expect(wrapper.find('.context-badges').exists()).toBe(false);
    });

    it('handles unknown context values gracefully', () => {
      const wrapper = createWrapper({ activeContexts: ['Unknown'] });
      const badge = wrapper.find('.context-badge');
      expect(badge.text()).toBe('Unknown');
      expect(badge.classes()).toContain('context-unknown');
    });
  });

  describe('Props Validation', () => {
    it('requires summonerName prop', () => {
      const wrapper = createWrapper({ summonerName: 'RequiredName' });
      expect(wrapper.find('.summoner-name').text()).toBe('RequiredName');
    });

    it('requires region prop', () => {
      const wrapper = createWrapper({ region: 'NA' });
      expect(wrapper.find('.region-tag').text()).toBe('NA');
    });
  });
});

