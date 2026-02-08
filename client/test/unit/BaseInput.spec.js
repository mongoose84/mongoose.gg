/**
 * BaseInput Component Tests
 *
 * Tests for the reusable input component supporting labels, validation,
 * error states, icons, and accessibility features.
 */

import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import BaseInput from '@/components/base/BaseInput.vue';

describe('BaseInput', () => {
  describe('Rendering', () => {
    it('renders an input element', () => {
      const wrapper = mount(BaseInput);
      expect(wrapper.find('input').exists()).toBe(true);
    });

    it('renders with text type by default', () => {
      const wrapper = mount(BaseInput);
      expect(wrapper.find('input').attributes('type')).toBe('text');
    });

    it('accepts custom input type', () => {
      const wrapper = mount(BaseInput, {
        props: { type: 'password' }
      });
      expect(wrapper.find('input').attributes('type')).toBe('password');
    });

    it('renders placeholder text', () => {
      const wrapper = mount(BaseInput, {
        props: { placeholder: 'Enter text...' }
      });
      expect(wrapper.find('input').attributes('placeholder')).toBe('Enter text...');
    });
  });

  describe('Label', () => {
    it('renders label when provided', () => {
      const wrapper = mount(BaseInput, {
        props: { label: 'Email Address' }
      });
      expect(wrapper.find('label').exists()).toBe(true);
      expect(wrapper.find('label').text()).toContain('Email Address');
    });

    it('does not render label when not provided', () => {
      const wrapper = mount(BaseInput);
      expect(wrapper.find('label').exists()).toBe(false);
    });

    it('shows required indicator when required', () => {
      const wrapper = mount(BaseInput, {
        props: { label: 'Email', required: true }
      });
      expect(wrapper.find('.input-required').exists()).toBe(true);
      expect(wrapper.find('.input-required').text()).toBe('*');
    });

    it('associates label with input via for/id', () => {
      const wrapper = mount(BaseInput, {
        props: { label: 'Email', id: 'email-input' }
      });
      expect(wrapper.find('label').attributes('for')).toBe('email-input');
      expect(wrapper.find('input').attributes('id')).toBe('email-input');
    });
  });

  describe('v-model binding', () => {
    it('sets initial value from modelValue', () => {
      const wrapper = mount(BaseInput, {
        props: { modelValue: 'initial value' }
      });
      expect(wrapper.find('input').element.value).toBe('initial value');
    });

    it('emits update:modelValue on input', async () => {
      const wrapper = mount(BaseInput, {
        props: { modelValue: '' }
      });
      await wrapper.find('input').setValue('new value');
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')[0]).toEqual(['new value']);
    });

    it('updates input value when modelValue prop changes', async () => {
      const wrapper = mount(BaseInput, {
        props: { modelValue: 'old' }
      });
      await wrapper.setProps({ modelValue: 'new' });
      expect(wrapper.find('input').element.value).toBe('new');
    });
  });

  describe('Error state', () => {
    it('displays error message when error prop is set', () => {
      const wrapper = mount(BaseInput, {
        props: { error: 'This field is required' }
      });
      expect(wrapper.find('.input-error').exists()).toBe(true);
      expect(wrapper.find('.input-error').text()).toBe('This field is required');
    });

    it('applies error class to container', () => {
      const wrapper = mount(BaseInput, {
        props: { error: 'Error message' }
      });
      expect(wrapper.find('.input-container--error').exists()).toBe(true);
    });

    it('sets aria-invalid when error is present', () => {
      const wrapper = mount(BaseInput, {
        props: { error: 'Error' }
      });
      expect(wrapper.find('input').attributes('aria-invalid')).toBe('true');
    });

    it('does not show error when not provided', () => {
      const wrapper = mount(BaseInput);
      expect(wrapper.find('.input-error').exists()).toBe(false);
    });
  });

  describe('Hint text', () => {
    it('displays hint when provided', () => {
      const wrapper = mount(BaseInput, {
        props: { hint: 'Enter your email address' }
      });
      expect(wrapper.find('.input-hint').exists()).toBe(true);
      expect(wrapper.find('.input-hint').text()).toBe('Enter your email address');
    });

    it('error takes precedence over hint', () => {
      const wrapper = mount(BaseInput, {
        props: { hint: 'Hint text', error: 'Error text' }
      });
      expect(wrapper.find('.input-error').exists()).toBe(true);
      expect(wrapper.find('.input-hint').exists()).toBe(false);
    });
  });

  describe('Disabled state', () => {
    it('sets disabled attribute when disabled', () => {
      const wrapper = mount(BaseInput, {
        props: { disabled: true }
      });
      expect(wrapper.find('input').attributes('disabled')).toBeDefined();
    });

    it('applies disabled class to container', () => {
      const wrapper = mount(BaseInput, {
        props: { disabled: true }
      });
      expect(wrapper.find('.input-container--disabled').exists()).toBe(true);
    });
  });

  describe('Input attributes', () => {
    it('sets required attribute', () => {
      const wrapper = mount(BaseInput, {
        props: { required: true }
      });
      expect(wrapper.find('input').attributes('required')).toBeDefined();
    });

    it('sets minlength attribute', () => {
      const wrapper = mount(BaseInput, {
        props: { minlength: 5 }
      });
      expect(wrapper.find('input').attributes('minlength')).toBe('5');
    });

    it('sets maxlength attribute', () => {
      const wrapper = mount(BaseInput, {
        props: { maxlength: 100 }
      });
      expect(wrapper.find('input').attributes('maxlength')).toBe('100');
    });

    it('sets autocomplete attribute', () => {
      const wrapper = mount(BaseInput, {
        props: { autocomplete: 'email' }
      });
      expect(wrapper.find('input').attributes('autocomplete')).toBe('email');
    });

    it('defaults autocomplete to off', () => {
      const wrapper = mount(BaseInput);
      expect(wrapper.find('input').attributes('autocomplete')).toBe('off');
    });
  });

  describe('Icon slots', () => {
    it('renders icon-left slot', () => {
      const wrapper = mount(BaseInput, {
        slots: { 'icon-left': '<span class="test-icon">🔍</span>' }
      });
      expect(wrapper.find('.input-icon--left').exists()).toBe(true);
      expect(wrapper.find('.test-icon').exists()).toBe(true);
    });

    it('renders icon-right slot', () => {
      const wrapper = mount(BaseInput, {
        slots: { 'icon-right': '<span class="test-icon">✓</span>' }
      });
      expect(wrapper.find('.input-icon--right').exists()).toBe(true);
      expect(wrapper.find('.test-icon').exists()).toBe(true);
    });

    it('applies padding class for left icon', () => {
      const wrapper = mount(BaseInput, {
        slots: { 'icon-left': '<span>Icon</span>' }
      });
      expect(wrapper.find('input').classes()).toContain('has-icon-left');
    });

    it('applies padding class for right icon', () => {
      const wrapper = mount(BaseInput, {
        slots: { 'icon-right': '<span>Icon</span>' }
      });
      expect(wrapper.find('input').classes()).toContain('has-icon-right');
    });
  });

  describe('Exposed methods', () => {
    it('exposes focus method', () => {
      const wrapper = mount(BaseInput);
      expect(typeof wrapper.vm.focus).toBe('function');
    });

    it('exposes blur method', () => {
      const wrapper = mount(BaseInput);
      expect(typeof wrapper.vm.blur).toBe('function');
    });

    it('exposes inputRef', () => {
      const wrapper = mount(BaseInput);
      expect(wrapper.vm.inputRef).toBeDefined();
    });
  });

  describe('Accessibility', () => {
    it('sets aria-describedby when error is present', () => {
      const wrapper = mount(BaseInput, {
        props: { error: 'Error', id: 'test-input' }
      });
      expect(wrapper.find('input').attributes('aria-describedby')).toBe('test-input-error');
      expect(wrapper.find('.input-error').attributes('id')).toBe('test-input-error');
    });

    it('error message has role="alert"', () => {
      const wrapper = mount(BaseInput, {
        props: { error: 'Error message' }
      });
      expect(wrapper.find('.input-error').attributes('role')).toBe('alert');
    });

    it('required indicator is aria-hidden', () => {
      const wrapper = mount(BaseInput, {
        props: { label: 'Field', required: true }
      });
      expect(wrapper.find('.input-required').attributes('aria-hidden')).toBe('true');
    });
  });
});

