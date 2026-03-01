import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import DeleteAccountModal from '@/components/DeleteAccountModal.vue'

vi.mock('@/services/authApi', () => ({
  deleteAccount: vi.fn()
}))

import { deleteAccount } from '@/services/authApi'

describe('DeleteAccountModal.vue', () => {
  const mountComponent = (props = {}) => {
    return mount(DeleteAccountModal, {
      props: {
        isOpen: true,
        ...props
      },
      global: {
        stubs: {
          BaseModal: {
            props: ['isOpen'],
            emits: ['close'],
            template: '<div v-if="isOpen"><slot /><slot name="footer" /></div>'
          },
          BaseInput: {
            props: ['modelValue', 'id', 'type', 'label', 'placeholder', 'error', 'disabled'],
            emits: ['update:modelValue'],
            template: '<input :id="id" :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)" />'
          },
          BaseButton: {
            props: ['disabled', 'loading', 'variant'],
            emits: ['click'],
            template: '<button :disabled="disabled" @click="$emit(\'click\')"><slot /></button>'
          }
        }
      }
    })
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('emits close and deleted when delete succeeds', async () => {
    deleteAccount.mockResolvedValue({ success: true })
    const wrapper = mountComponent()

    await wrapper.find('#confirm-delete').setValue('DELETE')
    await wrapper.find('#password').setValue('correct-password')
    await nextTick()

    const buttons = wrapper.findAll('button')
    await buttons[1].trigger('click')

    expect(deleteAccount).toHaveBeenCalledWith('correct-password')
    expect(wrapper.emitted('deleted')).toBeTruthy()
    expect(wrapper.emitted('close')).toBeTruthy()
  })
})
