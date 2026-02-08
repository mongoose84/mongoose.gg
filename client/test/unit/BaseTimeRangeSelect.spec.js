/**
 * BaseTimeRangeSelect Component Tests
 *
 * Tests for the reusable time range select component used for filtering
 * matches and stats by time period.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import BaseTimeRangeSelect from '@/components/base/BaseTimeRangeSelect.vue';

describe('BaseTimeRangeSelect', () => {
  const defaultOptions = [
    { value: 'current_season', label: 'Current Season' },
    { value: '1w', label: 'Last Week' },
    { value: '1m', label: 'Last Month' },
    { value: '3m', label: 'Last 3 Months' },
    { value: '6m', label: 'Last 6 Months' },
    { value: 'all', label: 'All Time' }
  ];

  describe('Rendering', () => {
    it('renders a select element', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m' }
      });
      expect(wrapper.find('select').exists()).toBe(true);
    });

    it('renders all default options', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m' }
      });
      const options = wrapper.findAll('option');
      expect(options).toHaveLength(defaultOptions.length);
      defaultOptions.forEach((opt, index) => {
        expect(options[index].text()).toBe(opt.label);
        expect(options[index].element.value).toBe(opt.value);
      });
    });

    it('renders custom options when provided', () => {
      const customOptions = [
        { value: '1w', label: 'Last Week' },
        { value: '1m', label: 'Last Month' }
      ];
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1w', options: customOptions }
      });
      const options = wrapper.findAll('option');
      expect(options).toHaveLength(2);
      expect(options[0].text()).toBe('Last Week');
      expect(options[1].text()).toBe('Last Month');
    });

    it('does not show label by default', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m' }
      });
      expect(wrapper.find('label').exists()).toBe(false);
    });

    it('shows label when showLabel is true', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m', showLabel: true }
      });
      expect(wrapper.find('label').exists()).toBe(true);
      expect(wrapper.find('label').text()).toBe('Time Range');
    });

    it('shows custom label text', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m', showLabel: true, label: 'Time Period' }
      });
      expect(wrapper.find('label').text()).toBe('Time Period');
    });
  });

  describe('v-model binding', () => {
    it('sets the correct initial value', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '3m' }
      });
      expect(wrapper.find('select').element.value).toBe('3m');
    });

    it('emits update:modelValue on change', async () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m' }
      });
      await wrapper.find('select').setValue('6m');
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')[0]).toEqual(['6m']);
    });

    it('updates selected value when modelValue prop changes', async () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m' }
      });
      expect(wrapper.find('select').element.value).toBe('1m');
      await wrapper.setProps({ modelValue: 'all' });
      expect(wrapper.find('select').element.value).toBe('all');
    });
  });

  describe('Accessibility', () => {
    it('has default aria-label', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m' }
      });
      expect(wrapper.find('select').attributes('aria-label')).toBe('Filter matches by time range');
    });

    it('accepts custom aria-label', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m', ariaLabel: 'Select time period' }
      });
      expect(wrapper.find('select').attributes('aria-label')).toBe('Select time period');
    });

    it('associates label with select via for/id when custom id provided', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m', showLabel: true, id: 'my-select' }
      });
      expect(wrapper.find('label').attributes('for')).toBe('my-select');
      expect(wrapper.find('select').attributes('id')).toBe('my-select');
    });

    it('auto-generates unique id when not provided', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m', showLabel: true }
      });
      const selectId = wrapper.find('select').attributes('id');
      const labelFor = wrapper.find('label').attributes('for');

      // Should start with 'time-range-' prefix and have a generated suffix
      expect(selectId).toMatch(/^time-range-/);
      expect(labelFor).toBe(selectId);
    });

    it('uses Vue useId for auto-generated ids', () => {
      // Verify the component uses Vue's useId() pattern for unique IDs
      // The ID should follow Vue's useId format (e.g., 'time-range-v-0')
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m', showLabel: true }
      });

      const selectId = wrapper.find('select').attributes('id');

      // Should have the time-range prefix with Vue's generated suffix
      expect(selectId).toMatch(/^time-range-v-\d+$/);
    });
  });

  describe('CSS classes', () => {
    it('has wrapper class', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m' }
      });
      expect(wrapper.find('.time-range-select-wrapper').exists()).toBe(true);
    });

    it('has select class', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m' }
      });
      expect(wrapper.find('.time-range-select').exists()).toBe(true);
    });

    it('has label class when label is shown', () => {
      const wrapper = mount(BaseTimeRangeSelect, {
        props: { modelValue: '1m', showLabel: true }
      });
      expect(wrapper.find('.time-range-label').exists()).toBe(true);
    });
  });
});

