import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import MainChampionCard from '@/components/MainChampionCard.vue';

// Mock soloApi to prevent real HTTP calls
vi.mock('@/services/soloApi', () => ({
  getChampionMatchups: vi.fn().mockResolvedValue({ matchups: [] })
}));

// Mock HeadlessUI TabGroup components
vi.mock('@headlessui/vue', () => ({
  TabGroup: {
    name: 'TabGroup',
    props: ['selectedIndex', 'as'],
    emits: ['change'],
    template: '<div class="tab-group"><slot /></div>'
  },
  TabList: {
    name: 'TabList',
    template: '<div class="tab-list"><slot /></div>'
  },
  Tab: {
    name: 'Tab',
    template: '<div><slot :selected="false" /></div>'
  },
  TabPanels: {
    name: 'TabPanels',
    template: '<div class="tab-panels"><slot /></div>'
  },
  TabPanel: {
    name: 'TabPanel',
    template: '<div class="tab-panel"><slot /></div>'
  }
}));

const sampleMainChampions = [
  {
    role: 'TOP',
    champions: [
      {
        championId: 1,
        championName: 'Garen',
        gamesPlayed: 20,
        winRate: 60,
        avgKda: 3.5,
        mScore: 75
      },
      {
        championId: 2,
        championName: 'Darius',
        gamesPlayed: 15,
        winRate: 53,
        avgKda: 2.8,
        mScore: 62
      },
      {
        championId: 3,
        championName: 'Malphite',
        gamesPlayed: 8,
        winRate: 47,
        avgKda: 1.9,
        mScore: 45
      }
    ]
  },
  {
    role: 'JUNGLE',
    champions: [
      {
        championId: 4,
        championName: 'Vi',
        gamesPlayed: 12,
        winRate: 58,
        avgKda: 4.1,
        mScore: 70
      }
    ]
  }
];

describe('MainChampionCard.vue', () => {
  const mountComponent = (props = {}) => {
    return mount(MainChampionCard, {
      props: {
        mainChampions: sampleMainChampions,
        userId: 42,
        queueType: 'ranked_solo',
        timeRange: '1m',
        ...props
      }
    });
  };

  describe('Rendering with champion data', () => {
    it('renders the component', () => {
      const wrapper = mountComponent();
      expect(wrapper.exists()).toBe(true);
    });

    it('displays the "Your Champions" heading', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('Your Champions');
    });

    it('displays "Top picks based on your performance" subtitle', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('Top picks based on your performance');
    });

    it('renders champion names when data is present', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('Garen');
      expect(wrapper.text()).toContain('Darius');
      expect(wrapper.text()).toContain('Malphite');
    });

    it('renders role tabs for each role in the data', () => {
      const wrapper = mountComponent();
      // TabList should be rendered via the TabGroup stub
      expect(wrapper.find('.tab-list').exists()).toBe(true);
    });

    it('shows the #1 Pick badge for the first champion', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('#1 Pick');
    });

    it('shows rank badges (#2, #3) for other champions', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('#2');
      expect(wrapper.text()).toContain('#3');
    });

    it('displays stat labels: Win Rate, KDA, Games, M-Score', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('Win Rate');
      expect(wrapper.text()).toContain('KDA');
      expect(wrapper.text()).toContain('Games');
      expect(wrapper.text()).toContain('M-Score');
    });

    it('displays champion game counts', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('20');
      expect(wrapper.text()).toContain('15');
      expect(wrapper.text()).toContain('8');
    });

    it('renders matchup sections (Strong / Weak)', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('Strong');
      expect(wrapper.text()).toContain('Weak');
    });
  });

  describe('Empty / no-data state', () => {
    it('renders the empty state when mainChampions is empty', () => {
      const wrapper = mountComponent({ mainChampions: [] });
      expect(wrapper.text()).toContain('No champion data yet');
    });

    it('shows the "Your Champions" heading in empty state too', () => {
      const wrapper = mountComponent({ mainChampions: [] });
      expect(wrapper.text()).toContain('Your Champions');
    });

    it('does not render TabGroup when there is no data', () => {
      const wrapper = mountComponent({ mainChampions: [] });
      expect(wrapper.find('.tab-group').exists()).toBe(false);
    });

    it('does not render champion names in empty state', () => {
      const wrapper = mountComponent({ mainChampions: [] });
      expect(wrapper.text()).not.toContain('Garen');
    });
  });

  describe('Computed properties', () => {
    it('hasData is true when mainChampions has entries', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.hasData).toBe(true);
    });

    it('hasData is false when mainChampions is empty', () => {
      const wrapper = mountComponent({ mainChampions: [] });
      expect(wrapper.vm.hasData).toBe(false);
    });

    it('roles computed returns role strings from mainChampions', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.roles).toContain('TOP');
      expect(wrapper.vm.roles).toContain('JUNGLE');
    });

    it('championsForRole returns champions for the given role', () => {
      const wrapper = mountComponent();
      const topChamps = wrapper.vm.championsForRole('TOP');
      expect(topChamps).toHaveLength(3);
      expect(topChamps[0].championName).toBe('Garen');
    });

    it('championsForRole returns empty array for unknown role', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.championsForRole('MID')).toEqual([]);
    });
  });

  describe('Helper methods', () => {
    it('formatKda formats number to 2 decimal places', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.formatKda(3.5)).toBe('3.50');
      expect(wrapper.vm.formatKda(2.0)).toBe('2.00');
    });

    it('formatKda returns "—" for null/undefined', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.formatKda(null)).toBe('—');
      expect(wrapper.vm.formatKda(undefined)).toBe('—');
    });

    it('formatMScore rounds to nearest integer', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.formatMScore(75.6)).toBe(76);
      expect(wrapper.vm.formatMScore(74.4)).toBe(74);
    });

    it('formatMScore returns "—" for null', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.formatMScore(null)).toBe('—');
    });

    it('getWinRateBarClass returns bg-success for >= 55%', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.getWinRateBarClass(60)).toBe('bg-success');
      expect(wrapper.vm.getWinRateBarClass(55)).toBe('bg-success');
    });

    it('getWinRateBarClass returns bg-error for < 45%', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.getWinRateBarClass(44)).toBe('bg-error');
      expect(wrapper.vm.getWinRateBarClass(0)).toBe('bg-error');
    });

    it('getKdaColorClass returns text-success for kda >= 3.0', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.getKdaColorClass(3.0)).toBe('text-success');
      expect(wrapper.vm.getKdaColorClass(5.0)).toBe('text-success');
    });

    it('getKdaColorClass returns text-error for kda < 2.0', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.getKdaColorClass(1.9)).toBe('text-error');
      expect(wrapper.vm.getKdaColorClass(0)).toBe('text-error');
    });

    it('getKdaColorClass returns yellow class for 2.0 <= kda < 3.0', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.getKdaColorClass(2.0)).toBe('text-[#eab308]');
      expect(wrapper.vm.getKdaColorClass(2.9)).toBe('text-[#eab308]');
    });

    it('getMScoreTextClass returns text-info for valid score', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.getMScoreTextClass(70)).toBe('text-info');
    });

    it('getMScoreTextClass returns text-text-secondary for null', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.getMScoreTextClass(null)).toBe('text-text-secondary');
    });

    it('getMatchupsForChampion returns empty good/bad when no matchupsData', () => {
      const wrapper = mountComponent({ userId: null });
      const result = wrapper.vm.getMatchupsForChampion(1, 'TOP');
      expect(result).toEqual({ good: [], bad: [] });
    });
  });

  describe('Tab interaction', () => {
    it('selectedTabIndex defaults to 0', () => {
      const wrapper = mountComponent();
      expect(wrapper.vm.selectedTabIndex).toBe(0);
    });

    it('handleTabChange updates selectedTabIndex', async () => {
      const wrapper = mountComponent();
      wrapper.vm.handleTabChange(1);
      await wrapper.vm.$nextTick();
      expect(wrapper.vm.selectedTabIndex).toBe(1);
    });

    it('selectedTabIndex resets to 0 when mainChampions becomes empty', async () => {
      const wrapper = mountComponent();
      wrapper.vm.handleTabChange(1);
      await wrapper.vm.$nextTick();
      await wrapper.setProps({ mainChampions: [] });
      expect(wrapper.vm.selectedTabIndex).toBe(0);
    });
  });
});
