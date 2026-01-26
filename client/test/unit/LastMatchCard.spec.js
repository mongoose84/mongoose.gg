import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createRouter, createWebHistory } from 'vue-router';
import LastMatchCard from '@/components/overview/LastMatchCard.vue';

// Create a mock router
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/app/matches/:matchId', name: 'match-details', component: { template: '<div />' } }
  ]
});

describe('LastMatchCard.vue', () => {
  const defaultProps = {
    matchId: 'EUW1_12345',
    championIconUrl: 'https://ddragon.leagueoflegends.com/cdn/16.1.1/img/champion/Ahri.png',
    championName: 'Ahri',
    result: 'Victory',
    kda: '10/2/8',
    timestamp: Date.now() - 3600000, // 1 hour ago
    queueType: 'Ranked Solo/Duo'
  };

  const createWrapper = (props = {}) => {
    return mount(LastMatchCard, {
      props: { ...defaultProps, ...props },
      global: {
        plugins: [router]
      }
    });
  };

  describe('Rendering', () => {
    it('renders the component', () => {
      const wrapper = createWrapper();
      expect(wrapper.exists()).toBe(true);
    });

    it('renders as a router-link when matchId is provided', () => {
      const wrapper = createWrapper();
      expect(wrapper.find('a.last-match-card').exists()).toBe(true);
    });

    it('renders empty state when matchId is null', () => {
      const wrapper = createWrapper({ matchId: null });
      expect(wrapper.find('.last-match-card.empty').exists()).toBe(true);
      expect(wrapper.find('.empty-state').exists()).toBe(true);
    });

    it('displays "No recent matches" in empty state', () => {
      const wrapper = createWrapper({ matchId: null });
      expect(wrapper.find('.empty-text').text()).toBe('No recent matches');
    });
  });

  describe('Champion Icon', () => {
    it('displays champion icon when championIconUrl is provided', () => {
      const wrapper = createWrapper();
      const img = wrapper.find('.champion-icon');
      expect(img.exists()).toBe(true);
      expect(img.attributes('src')).toBe(defaultProps.championIconUrl);
    });

    it('displays placeholder when championIconUrl is null', () => {
      const wrapper = createWrapper({ championIconUrl: null });
      expect(wrapper.find('.champion-icon').exists()).toBe(false);
      expect(wrapper.find('.champion-icon-placeholder').exists()).toBe(true);
    });

    it('has correct alt text for champion icon', () => {
      const wrapper = createWrapper({ championName: 'Lux' });
      const img = wrapper.find('.champion-icon');
      expect(img.attributes('alt')).toBe('Lux icon');
    });
  });

  describe('Match Info', () => {
    it('displays champion name', () => {
      const wrapper = createWrapper({ championName: 'Zed' });
      expect(wrapper.find('.champion-name').text()).toBe('Zed');
    });

    it('displays result badge', () => {
      const wrapper = createWrapper({ result: 'Victory' });
      expect(wrapper.find('.result-badge').text()).toBe('Victory');
    });

    it('displays KDA', () => {
      const wrapper = createWrapper({ kda: '5/3/12' });
      expect(wrapper.find('.kda').text()).toBe('5/3/12');
    });

    it('displays queue type', () => {
      const wrapper = createWrapper({ queueType: 'Ranked Flex' });
      expect(wrapper.find('.queue-type').text()).toBe('Ranked Flex');
    });

    it('does not display queue type when null', () => {
      const wrapper = createWrapper({ queueType: null });
      expect(wrapper.find('.queue-type').exists()).toBe(false);
    });
  });

  describe('Result Styling', () => {
    it('applies win class for Victory result', () => {
      const wrapper = createWrapper({ result: 'Victory' });
      expect(wrapper.find('.last-match-card.win').exists()).toBe(true);
      expect(wrapper.find('.result-badge.win').exists()).toBe(true);
    });

    it('applies win class for Win result', () => {
      const wrapper = createWrapper({ result: 'Win' });
      expect(wrapper.find('.last-match-card.win').exists()).toBe(true);
    });

    it('applies loss class for Defeat result', () => {
      const wrapper = createWrapper({ result: 'Defeat' });
      expect(wrapper.find('.last-match-card.loss').exists()).toBe(true);
      expect(wrapper.find('.result-badge.loss').exists()).toBe(true);
    });

    it('applies loss class for Loss result', () => {
      const wrapper = createWrapper({ result: 'Loss' });
      expect(wrapper.find('.last-match-card.loss').exists()).toBe(true);
    });

    it('handles case insensitive result values', () => {
      const wrapper = createWrapper({ result: 'VICTORY' });
      expect(wrapper.find('.last-match-card.win').exists()).toBe(true);
    });
  });

  describe('Router Link', () => {
    it('navigates to correct match details URL', () => {
      const wrapper = createWrapper({ matchId: 'EUW1_67890' });
      const link = wrapper.find('a.last-match-card');
      expect(link.attributes('href')).toBe('/app/matches/EUW1_67890');
    });
  });

  describe('Arrow Indicator', () => {
    it('displays arrow indicator for clickable card', () => {
      const wrapper = createWrapper();
      expect(wrapper.find('.arrow-indicator').exists()).toBe(true);
      expect(wrapper.find('.arrow-icon').exists()).toBe(true);
    });

    it('does not display arrow in empty state', () => {
      const wrapper = createWrapper({ matchId: null });
      expect(wrapper.find('.arrow-indicator').exists()).toBe(false);
    });
  });

  describe('Relative Time', () => {
    it('displays "Just now" for very recent matches', () => {
      const wrapper = createWrapper({ timestamp: Date.now() - 30000 }); // 30 seconds ago
      expect(wrapper.find('.timestamp').text()).toBe('Just now');
    });

    it('displays minutes for matches within an hour', () => {
      const wrapper = createWrapper({ timestamp: Date.now() - 1800000 }); // 30 minutes ago
      expect(wrapper.find('.timestamp').text()).toBe('30 min ago');
    });

    it('displays hours for matches within a day', () => {
      const wrapper = createWrapper({ timestamp: Date.now() - 7200000 }); // 2 hours ago
      expect(wrapper.find('.timestamp').text()).toBe('2 hours ago');
    });

    it('displays singular hour correctly', () => {
      const wrapper = createWrapper({ timestamp: Date.now() - 3600000 }); // 1 hour ago
      expect(wrapper.find('.timestamp').text()).toBe('1 hour ago');
    });

    it('displays days for matches within a week', () => {
      const wrapper = createWrapper({ timestamp: Date.now() - 259200000 }); // 3 days ago
      expect(wrapper.find('.timestamp').text()).toBe('3 days ago');
    });

    it('displays singular day correctly', () => {
      const wrapper = createWrapper({ timestamp: Date.now() - 86400000 }); // 1 day ago
      expect(wrapper.find('.timestamp').text()).toBe('1 day ago');
    });

    it('displays weeks for matches within a month', () => {
      const wrapper = createWrapper({ timestamp: Date.now() - 1209600000 }); // 2 weeks ago
      expect(wrapper.find('.timestamp').text()).toBe('2 weeks ago');
    });

    it('displays months for older matches', () => {
      const wrapper = createWrapper({ timestamp: Date.now() - 5184000000 }); // ~60 days / 2 months ago
      expect(wrapper.find('.timestamp').text()).toBe('2 months ago');
    });

    it('displays empty string when timestamp is null', () => {
      const wrapper = createWrapper({ timestamp: null });
      expect(wrapper.find('.timestamp').text()).toBe('');
    });
  });

  describe('Props Validation', () => {
    it('uses default championName when not provided', () => {
      const wrapper = mount(LastMatchCard, {
        props: { matchId: 'TEST_123' },
        global: { plugins: [router] }
      });
      expect(wrapper.find('.champion-name').text()).toBe('Unknown');
    });

    it('handles all props being provided', () => {
      const wrapper = createWrapper();
      expect(wrapper.exists()).toBe(true);
      expect(wrapper.find('.champion-name').text()).toBe('Ahri');
      expect(wrapper.find('.result-badge').text()).toBe('Victory');
      expect(wrapper.find('.kda').text()).toBe('10/2/8');
    });
  });
});

