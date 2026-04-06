/**
 * BaseModal Component Tests
 *
 * Tests for the reusable modal component built on HeadlessUI Dialog.
 * Supports multiple sizes, slots, close prevention, and accessibility.
 */

import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import BaseModal from '@/components/base/BaseModal.vue';
import { headlessUIStubs } from '@test/helpers';

describe('BaseModal', () => {
  const mountModal = (props = {}, slots = {}) => {
    return mount(BaseModal, {
      props: { isOpen: true, ...props },
      slots: { default: 'Modal content', ...slots },
      global: { stubs: headlessUIStubs }
    });
  };

  describe('Visibility', () => {
    it('renders when isOpen is true', () => {
      const wrapper = mountModal({ isOpen: true });
      expect(wrapper.find('[data-testid="modal-content"]').exists()).toBe(true);
    });

    it('does not render when isOpen is false', () => {
      const wrapper = mount(BaseModal, {
        props: { isOpen: false },
        slots: { default: 'Content' },
        global: { stubs: headlessUIStubs }
      });
      expect(wrapper.find('[data-testid="modal-content"]').exists()).toBe(false);
    });
  });

  describe('Title', () => {
    it('renders title when provided', () => {
      const wrapper = mountModal({ title: 'Modal Title' });
      expect(wrapper.find('.modal-header').exists()).toBe(true);
      expect(wrapper.text()).toContain('Modal Title');
    });

    it('does not render header when no title, header slot, or close button', () => {
      const wrapper = mountModal({ showCloseButton: false });
      expect(wrapper.find('.modal-header').exists()).toBe(false);
    });
  });

  describe('Close button', () => {
    it('shows close button by default', () => {
      const wrapper = mountModal({ title: 'Title' });
      expect(wrapper.find('.modal-close-btn').exists()).toBe(true);
    });

    it('hides close button when showCloseButton is false', () => {
      const wrapper = mountModal({ title: 'Title', showCloseButton: false });
      expect(wrapper.find('.modal-close-btn').exists()).toBe(false);
    });

    it('emits close when close button is clicked', async () => {
      const wrapper = mountModal({ title: 'Title' });
      await wrapper.find('.modal-close-btn').trigger('click');
      expect(wrapper.emitted('close')).toBeTruthy();
    });

    it('does not emit close when preventClose is true', async () => {
      const wrapper = mountModal({ title: 'Title', preventClose: true });
      await wrapper.find('.modal-close-btn').trigger('click');
      expect(wrapper.emitted('close')).toBeFalsy();
    });

    it('disables close button when preventClose is true', () => {
      const wrapper = mountModal({ title: 'Title', preventClose: true });
      expect(wrapper.find('.modal-close-btn').attributes('disabled')).toBeDefined();
    });

    it('close button has aria-label', () => {
      const wrapper = mountModal({ title: 'Title' });
      expect(wrapper.find('.modal-close-btn').attributes('aria-label')).toBe('Close modal');
    });
  });

  describe('Sizes', () => {
    // Note: Size classes are applied via :class binding on DialogPanel.
    // Since we use stubs, we verify the component receives the correct size prop
    // and computes the correct class. We test the computed sizeClass indirectly.
    it.each(['sm', 'md', 'lg', 'xl', 'full'])('passes %s size to DialogPanel', (size) => {
      const wrapper = mount(BaseModal, {
        props: { isOpen: true, size },
        slots: { default: 'Content' },
        global: {
          stubs: {
            ...headlessUIStubs,
            DialogPanel: {
              template: '<div :class="computedClass" data-testid="modal-content"><slot /></div>',
              props: ['class'],
              computed: {
                computedClass() {
                  return ['modal-panel', this.class];
                }
              }
            }
          }
        }
      });
      expect(wrapper.find('[data-testid="modal-content"]').classes()).toContain(`modal-panel--${size}`);
    });

    it('defaults to md size', () => {
      const wrapper = mount(BaseModal, {
        props: { isOpen: true },
        slots: { default: 'Content' },
        global: {
          stubs: {
            ...headlessUIStubs,
            DialogPanel: {
              template: '<div :class="computedClass" data-testid="modal-content"><slot /></div>',
              props: ['class'],
              computed: {
                computedClass() {
                  return ['modal-panel', this.class];
                }
              }
            }
          }
        }
      });
      expect(wrapper.find('[data-testid="modal-content"]').classes()).toContain('modal-panel--md');
    });
  });

  describe('Slots', () => {
    it('renders default slot in modal-body', () => {
      const wrapper = mountModal({}, { default: 'Body content' });
      expect(wrapper.find('.modal-body').text()).toBe('Body content');
    });

    it('renders header slot', () => {
      const wrapper = mountModal({}, {
        default: 'Body',
        header: '<div class="custom-header">Custom Header</div>'
      });
      expect(wrapper.find('.custom-header').exists()).toBe(true);
    });

    it('renders footer slot', () => {
      const wrapper = mountModal({}, {
        default: 'Body',
        footer: '<button class="action-btn">Save</button>'
      });
      expect(wrapper.find('.modal-footer').exists()).toBe(true);
      expect(wrapper.find('.action-btn').exists()).toBe(true);
    });

    it('does not render footer when no footer slot', () => {
      const wrapper = mountModal();
      expect(wrapper.find('.modal-footer').exists()).toBe(false);
    });
  });

  describe('Header visibility', () => {
    it('shows header when title is provided', () => {
      const wrapper = mountModal({ title: 'Title', showCloseButton: false });
      expect(wrapper.find('.modal-header').exists()).toBe(true);
    });

    it('shows header when header slot is provided', () => {
      const wrapper = mountModal(
        { showCloseButton: false },
        { header: '<span>Header</span>' }
      );
      expect(wrapper.find('.modal-header').exists()).toBe(true);
    });

    it('shows header when showCloseButton is true', () => {
      const wrapper = mountModal({ showCloseButton: true });
      expect(wrapper.find('.modal-header').exists()).toBe(true);
    });
  });
});

