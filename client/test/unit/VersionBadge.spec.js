import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import VersionBadge from '@/components/VersionBadge.vue';

describe('VersionBadge.vue', () => {
  it('renders the version badge', () => {
    const wrapper = mount(VersionBadge);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays version text', () => {
    const wrapper = mount(VersionBadge);
    const text = wrapper.text();
    expect(text).toContain('Pulse.gg');
    expect(text).toContain('v');
  });

  it('has correct class name applied', () => {
    const wrapper = mount(VersionBadge);
    expect(wrapper.find('.version-badge').exists()).toBe(true);
  });

  it('displays all version parts', () => {
    const wrapper = mount(VersionBadge);
    expect(wrapper.find('.version-label').exists()).toBe(true);
    expect(wrapper.find('.version-sep').exists()).toBe(true);
    expect(wrapper.find('.version-value').exists()).toBe(true);
  });
});
