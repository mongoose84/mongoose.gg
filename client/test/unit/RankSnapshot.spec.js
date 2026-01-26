import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import RankSnapshot from '@/components/overview/RankSnapshot.vue';

describe('RankSnapshot.vue', () => {
  const defaultProps = {
    primaryQueueLabel: 'RANKED SOLO/DUO',
    rank: 'GOLD IV',
    lp: 75,
    lpDeltaLast20: 45,
    last20Wins: 12,
    last20Losses: 8,
    wlLast20: [true, true, false, true, false, true, true, true, false, true, true, false, true, true, false, true, false, true, true, true]
  };

  const createWrapper = (props = {}) => {
    return mount(RankSnapshot, {
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
      expect(wrapper.find('section.rank-snapshot').exists()).toBe(true);
    });

    it('displays the primary queue label', () => {
      const wrapper = createWrapper({ primaryQueueLabel: 'RANKED FLEX' });
      expect(wrapper.find('.queue-label').text()).toBe('RANKED FLEX');
    });
  });

  describe('Rank Emblem', () => {
    it('displays rank emblem when rank is provided', () => {
      const wrapper = createWrapper({ rank: 'GOLD IV' });
      const img = wrapper.find('.rank-emblem');
      expect(img.exists()).toBe(true);
      expect(img.attributes('src')).toBe('/assets/ranked/emblem-gold.png');
    });

    it('displays placeholder when rank is null', () => {
      const wrapper = createWrapper({ rank: null });
      expect(wrapper.find('.rank-emblem').exists()).toBe(false);
      expect(wrapper.find('.rank-emblem-placeholder').exists()).toBe(true);
    });

    it('has correct alt text for rank emblem', () => {
      const wrapper = createWrapper({ rank: 'DIAMOND II' });
      const img = wrapper.find('.rank-emblem');
      expect(img.attributes('alt')).toBe('diamond emblem');
    });

    it('generates correct emblem URL for different tiers', () => {
      const tiers = ['IRON', 'BRONZE', 'SILVER', 'GOLD', 'PLATINUM', 'EMERALD', 'DIAMOND', 'MASTER', 'GRANDMASTER', 'CHALLENGER'];
      tiers.forEach(tier => {
        const wrapper = createWrapper({ rank: `${tier} IV` });
        const img = wrapper.find('.rank-emblem');
        expect(img.attributes('src')).toBe(`/assets/ranked/emblem-${tier.toLowerCase()}.png`);
      });
    });
  });

  describe('Rank Display', () => {
    it('displays formatted rank text', () => {
      const wrapper = createWrapper({ rank: 'SILVER IV' });
      expect(wrapper.find('.rank-text').text()).toBe('Silver IV');
    });

    it('displays "Unranked" when rank is null', () => {
      const wrapper = createWrapper({ rank: null });
      expect(wrapper.find('.rank-text').text()).toBe('Unranked');
    });

    it('formats tier with proper capitalization', () => {
      const wrapper = createWrapper({ rank: 'GRANDMASTER' });
      expect(wrapper.find('.rank-text').text()).toBe('Grandmaster');
    });

    it('displays LP when provided', () => {
      const wrapper = createWrapper({ lp: 99 });
      expect(wrapper.find('.lp-text').text()).toBe('99 LP');
    });

    it('does not display LP when null', () => {
      const wrapper = createWrapper({ lp: null });
      expect(wrapper.find('.lp-text').exists()).toBe(false);
    });

    it('displays 0 LP correctly', () => {
      const wrapper = createWrapper({ lp: 0 });
      expect(wrapper.find('.lp-text').text()).toBe('0 LP');
    });
  });

  describe('LP Delta', () => {
    it('displays positive LP delta with plus sign', () => {
      const wrapper = createWrapper({ lpDeltaLast20: 50 });
      expect(wrapper.find('.lp-delta').text()).toBe('+50 LP (Last 20)');
    });

    it('displays negative LP delta', () => {
      const wrapper = createWrapper({ lpDeltaLast20: -30 });
      expect(wrapper.find('.lp-delta').text()).toBe('-30 LP (Last 20)');
    });

    it('displays zero LP delta with plus-minus sign', () => {
      const wrapper = createWrapper({ lpDeltaLast20: 0 });
      expect(wrapper.find('.lp-delta').text()).toBe('±0 LP (Last 20)');
    });

    it('applies positive class for positive delta', () => {
      const wrapper = createWrapper({ lpDeltaLast20: 25 });
      expect(wrapper.find('.lp-delta.positive').exists()).toBe(true);
    });

    it('applies negative class for negative delta', () => {
      const wrapper = createWrapper({ lpDeltaLast20: -25 });
      expect(wrapper.find('.lp-delta.negative').exists()).toBe(true);
    });

    it('applies neutral class for zero delta', () => {
      const wrapper = createWrapper({ lpDeltaLast20: 0 });
      expect(wrapper.find('.lp-delta.neutral').exists()).toBe(true);
    });
  });

  describe('Winrate Display', () => {
    it('displays winrate with wins and losses', () => {
      const wrapper = createWrapper({ last20Wins: 12, last20Losses: 8 });
      expect(wrapper.find('.winrate-text').text()).toBe('60% (12W–8L)');
    });

    it('displays 100% winrate correctly', () => {
      const wrapper = createWrapper({ last20Wins: 20, last20Losses: 0 });
      expect(wrapper.find('.winrate-text').text()).toBe('100% (20W–0L)');
    });

    it('displays 0% winrate correctly', () => {
      const wrapper = createWrapper({ last20Wins: 0, last20Losses: 20 });
      expect(wrapper.find('.winrate-text').text()).toBe('0% (0W–20L)');
    });

    it('displays "No games" when no games played', () => {
      const wrapper = createWrapper({ last20Wins: 0, last20Losses: 0 });
      expect(wrapper.find('.winrate-text').text()).toBe('No games');
    });

    it('rounds winrate to nearest integer', () => {
      const wrapper = createWrapper({ last20Wins: 7, last20Losses: 13 });
      expect(wrapper.find('.winrate-text').text()).toBe('35% (7W–13L)');
    });
  });

  describe('W/L Strip', () => {
    it('displays W/L strip when wlLast20 has data', () => {
      const wrapper = createWrapper({ wlLast20: [true, false, true] });
      expect(wrapper.find('.wl-strip').exists()).toBe(true);
    });

    it('does not display W/L strip when wlLast20 is empty', () => {
      const wrapper = createWrapper({ wlLast20: [] });
      expect(wrapper.find('.wl-strip').exists()).toBe(false);
    });

    it('renders correct number of indicators', () => {
      const wlLast20 = [true, false, true, true, false];
      const wrapper = createWrapper({ wlLast20 });
      const indicators = wrapper.findAll('.wl-indicator');
      expect(indicators.length).toBe(5);
    });

    it('applies win class for wins', () => {
      const wrapper = createWrapper({ wlLast20: [true] });
      expect(wrapper.find('.wl-indicator.win').exists()).toBe(true);
    });

    it('applies loss class for losses', () => {
      const wrapper = createWrapper({ wlLast20: [false] });
      expect(wrapper.find('.wl-indicator.loss').exists()).toBe(true);
    });

    it('renders wins and losses in correct order', () => {
      const wlLast20 = [true, false, true];
      const wrapper = createWrapper({ wlLast20 });
      const indicators = wrapper.findAll('.wl-indicator');
      expect(indicators[0].classes()).toContain('win');
      expect(indicators[1].classes()).toContain('loss');
      expect(indicators[2].classes()).toContain('win');
    });

    it('handles full 20 game history', () => {
      const wlLast20 = Array(20).fill(true).map((_, i) => i % 2 === 0);
      const wrapper = createWrapper({ wlLast20 });
      const indicators = wrapper.findAll('.wl-indicator');
      expect(indicators.length).toBe(20);
    });
  });

  describe('Props Validation', () => {
    it('requires primaryQueueLabel prop', () => {
      const wrapper = createWrapper({ primaryQueueLabel: 'RANKED FLEX' });
      expect(wrapper.find('.queue-label').text()).toBe('RANKED FLEX');
    });

    it('handles missing optional props gracefully', () => {
      const wrapper = mount(RankSnapshot, {
        props: { primaryQueueLabel: 'RANKED SOLO/DUO' }
      });
      expect(wrapper.exists()).toBe(true);
      expect(wrapper.find('.rank-text').text()).toBe('Unranked');
      expect(wrapper.find('.lp-text').exists()).toBe(false);
      expect(wrapper.find('.wl-strip').exists()).toBe(false);
    });
  });
});

