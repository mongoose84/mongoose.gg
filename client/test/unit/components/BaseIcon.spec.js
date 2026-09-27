/**
 * BaseIcon Component Tests
 *
 * Lucide icons from src/assets/icons rendered inline at the design-system sizes.
 */

import { describe, it, expect, vi, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import BaseIcon from '@/components/base/BaseIcon.vue';

describe('BaseIcon', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the named icon as inline svg', () => {
    const wrapper = mount(BaseIcon, { props: { name: 'check' } });
    const svg = wrapper.find('svg');
    expect(svg.exists()).toBe(true);
    expect(svg.find('path').attributes('d')).toBe('M20 6L9 17l-5-5');
    expect(wrapper.attributes('data-icon')).toBe('check');
  });

  it('uses 20px by default and hides the icon from assistive tech', () => {
    const wrapper = mount(BaseIcon, { props: { name: 'swords' } });
    const svg = wrapper.find('svg');
    expect(svg.attributes('width')).toBe('20');
    expect(svg.attributes('height')).toBe('20');
    expect(svg.attributes('aria-hidden')).toBe('true');
    expect(svg.attributes('focusable')).toBe('false');
    expect(wrapper.attributes('aria-hidden')).toBe('true');
  });

  it('applies the requested size', () => {
    const wrapper = mount(BaseIcon, { props: { name: 'house', size: 16 } });
    expect(wrapper.find('svg').attributes('width')).toBe('16');
    expect(wrapper.attributes('style')).toContain('width: 16px');
  });

  it('warns and renders nothing for an unknown icon', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const wrapper = mount(BaseIcon, { props: { name: 'not-an-icon' } });
    expect(wrapper.find('svg').exists()).toBe(false);
    expect(warn).toHaveBeenCalledWith(expect.stringContaining('not-an-icon'));
  });
});
