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
    expect(text).toContain('Mongoose');
    expect(text).toContain('v');
  });

  it('has correct aria-label for accessibility', () => {
    const wrapper = mount(VersionBadge);
    expect(wrapper.find('[aria-label="App Version"]').exists()).toBe(true);
  });

  it('displays all version parts', () => {
    const wrapper = mount(VersionBadge);
    expect(wrapper.find('[data-testid="version-label"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="version-sep"]').exists()).toBe(true);
    expect(wrapper.find('[data-testid="version-value"]').exists()).toBe(true);
  });
});
