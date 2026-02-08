/**
 * BaseQueueToggle Component Tests
 *
 * Tests for the reusable toggle button group component used for filtering
 * by queue type (Ranked Solo, Flex, Normal, ARAM, etc.).
 */

import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import BaseQueueToggle from '@/components/base/BaseQueueToggle.vue';

describe('BaseQueueToggle', () => {
  const defaultOptions = [
    { value: 'all', label: 'All Queues' },
    { value: 'ranked_solo', label: 'Ranked Solo/Duo' },
    { value: 'ranked_flex', label: 'Ranked Flex' },
    { value: 'normal', label: 'Normal' },
    { value: 'aram', label: 'ARAM' }
  ];

  describe('Rendering', () => {
    it('renders a button group container', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all' }
      });
      expect(wrapper.find('.queue-toggle-group').exists()).toBe(true);
    });

    it('renders all default options as buttons', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all' }
      });
      const buttons = wrapper.findAll('button');
      expect(buttons).toHaveLength(defaultOptions.length);
      defaultOptions.forEach((opt, index) => {
        expect(buttons[index].text()).toBe(opt.label);
      });
    });

    it('renders custom options when provided', () => {
      const customOptions = [
        { value: 'all', label: 'All' },
        { value: 'ranked', label: 'Ranked' }
      ];
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all', options: customOptions }
      });
      const buttons = wrapper.findAll('button');
      expect(buttons).toHaveLength(2);
      expect(buttons[0].text()).toBe('All');
      expect(buttons[1].text()).toBe('Ranked');
    });
  });

  describe('Active state', () => {
    it('marks the selected option as active', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'ranked_solo' }
      });
      const buttons = wrapper.findAll('button');
      const activeButton = buttons.find(b => b.text() === 'Ranked Solo/Duo');
      expect(activeButton.classes()).toContain('queue-toggle-btn--active');
    });

    it('marks non-selected options as inactive', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'ranked_solo' }
      });
      const buttons = wrapper.findAll('button');
      const inactiveButtons = buttons.filter(b => b.text() !== 'Ranked Solo/Duo');
      inactiveButtons.forEach(btn => {
        expect(btn.classes()).toContain('queue-toggle-btn--inactive');
        expect(btn.classes()).not.toContain('queue-toggle-btn--active');
      });
    });

    it('updates active state when modelValue changes', async () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all' }
      });
      const buttons = wrapper.findAll('button');
      expect(buttons[0].classes()).toContain('queue-toggle-btn--active');

      await wrapper.setProps({ modelValue: 'aram' });
      expect(buttons[0].classes()).not.toContain('queue-toggle-btn--active');
      expect(buttons[4].classes()).toContain('queue-toggle-btn--active');
    });
  });

  describe('v-model binding', () => {
    it('emits update:modelValue when a button is clicked', async () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all' }
      });
      const buttons = wrapper.findAll('button');
      await buttons[2].trigger('click'); // Click 'Ranked Flex'
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')[0]).toEqual(['ranked_flex']);
    });

    it('emits correct value for each option', async () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all' }
      });
      const buttons = wrapper.findAll('button');

      for (let i = 0; i < defaultOptions.length; i++) {
        await buttons[i].trigger('click');
        const emitted = wrapper.emitted('update:modelValue');
        expect(emitted[i]).toEqual([defaultOptions[i].value]);
      }
    });
  });

  describe('Accessibility', () => {
    it('has role="group" on container', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all' }
      });
      expect(wrapper.find('.queue-toggle-group').attributes('role')).toBe('group');
    });

    it('has default aria-label on container', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all' }
      });
      expect(wrapper.find('.queue-toggle-group').attributes('aria-label')).toBe('Filter by queue type');
    });

    it('accepts custom aria-label', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all', ariaLabel: 'Select queue' }
      });
      expect(wrapper.find('.queue-toggle-group').attributes('aria-label')).toBe('Select queue');
    });

    it('sets aria-pressed on active button', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'ranked_solo' }
      });
      const buttons = wrapper.findAll('button');
      const activeButton = buttons.find(b => b.text() === 'Ranked Solo/Duo');
      expect(activeButton.attributes('aria-pressed')).toBe('true');
    });

    it('sets aria-pressed="false" on inactive buttons', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'ranked_solo' }
      });
      const buttons = wrapper.findAll('button');
      const inactiveButtons = buttons.filter(b => b.text() !== 'Ranked Solo/Duo');
      inactiveButtons.forEach(btn => {
        expect(btn.attributes('aria-pressed')).toBe('false');
      });
    });

    it('all buttons have type="button"', () => {
      const wrapper = mount(BaseQueueToggle, {
        props: { modelValue: 'all' }
      });
      const buttons = wrapper.findAll('button');
      buttons.forEach(btn => {
        expect(btn.attributes('type')).toBe('button');
      });
    });
  });
});

