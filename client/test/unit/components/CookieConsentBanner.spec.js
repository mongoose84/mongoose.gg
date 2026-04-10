import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { ref } from 'vue'
import CookieConsentBanner from '@/components/CookieConsentBanner.vue'

// ── useCookieConsent mock ────────────────────────────────────────────────────
const mockShouldShowBanner = ref(true)
const mockSetConsent = vi.fn()
const mockSetupCrossTabSync = vi.fn().mockReturnValue(() => {})

vi.mock('@/composables/useCookieConsent', () => ({
  useCookieConsent: () => ({
    shouldShowBanner: () => mockShouldShowBanner.value,
    setConsent: mockSetConsent,
    setupCrossTabSync: mockSetupCrossTabSync
  })
}))

// ── router-link stub ─────────────────────────────────────────────────────────
vi.mock('vue-router', () => ({
  RouterLink: {
    template: '<a :href="to"><slot /></a>',
    props: ['to']
  }
}))

// ── helpers ──────────────────────────────────────────────────────────────────
function mountBanner() {
  return mount(CookieConsentBanner, {
    global: {
      stubs: {
        Transition: false, // keep v-if reactive
        BaseButton: {
          template: '<button v-bind="$attrs" @click="$emit(\'click\')"><slot /></button>',
          emits: ['click']
        },
        RouterLink: {
          template: '<a><slot /></a>'
        }
      }
    }
  })
}

describe('CookieConsentBanner.vue', () => {
  beforeEach(() => {
    mockShouldShowBanner.value = true
    mockSetConsent.mockReset()
    mockSetupCrossTabSync.mockReset().mockReturnValue(() => {})
  })

  // ── visibility ─────────────────────────────────────────────────────────────

  describe('visibility', () => {
    it('renders the banner when shouldShowBanner returns true', () => {
      mockShouldShowBanner.value = true
      const wrapper = mountBanner()

      expect(wrapper.find('.cookie-consent-banner').exists()).toBe(true)
    })

    it('does not render the banner when shouldShowBanner returns false', () => {
      mockShouldShowBanner.value = false
      const wrapper = mountBanner()

      expect(wrapper.find('.cookie-consent-banner').exists()).toBe(false)
    })
  })

  // ── accessibility ───────────────────────────────────────────────────────────

  describe('accessibility', () => {
    it('has role="dialog" on the banner element', () => {
      const wrapper = mountBanner()

      expect(wrapper.find('.cookie-consent-banner').attributes('role')).toBe('dialog')
    })

    it('has aria-label="Cookie consent"', () => {
      const wrapper = mountBanner()

      expect(wrapper.find('.cookie-consent-banner').attributes('aria-label')).toBe('Cookie consent')
    })

    it('has aria-describedby pointing to the description paragraph', () => {
      const wrapper = mountBanner()

      const banner = wrapper.find('.cookie-consent-banner')
      const describedById = banner.attributes('aria-describedby')
      expect(describedById).toBeTruthy()

      const descriptionEl = wrapper.find(`#${describedById}`)
      expect(descriptionEl.exists()).toBe(true)
      expect(descriptionEl.text()).toContain('authentication cookie')
    })
  })

  // ── content ─────────────────────────────────────────────────────────────────

  describe('content', () => {
    it('displays the title "We use cookies"', () => {
      const wrapper = mountBanner()

      expect(wrapper.text()).toContain('We use cookies')
    })

    it('displays the description text', () => {
      const wrapper = mountBanner()

      expect(wrapper.text()).toContain('authentication cookie')
    })

    it('shows policy links for Cookie Policy and Privacy Policy', () => {
      const wrapper = mountBanner()

      expect(wrapper.text()).toContain('Cookie Policy')
      expect(wrapper.text()).toContain('Privacy Policy')
    })

    it('shows both Accept and Reject buttons', () => {
      const wrapper = mountBanner()

      expect(wrapper.find('[data-testid="accept-cookies"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="reject-cookies"]').exists()).toBe(true)
    })
  })

  // ── consent actions ─────────────────────────────────────────────────────────

  describe('consent actions', () => {
    it('calls setConsent("accepted") when Accept is clicked', async () => {
      const wrapper = mountBanner()

      await wrapper.find('[data-testid="accept-cookies"]').trigger('click')

      expect(mockSetConsent).toHaveBeenCalledOnce()
      expect(mockSetConsent).toHaveBeenCalledWith('accepted')
    })

    it('calls setConsent("rejected") when Reject is clicked', async () => {
      const wrapper = mountBanner()

      await wrapper.find('[data-testid="reject-cookies"]').trigger('click')

      expect(mockSetConsent).toHaveBeenCalledOnce()
      expect(mockSetConsent).toHaveBeenCalledWith('rejected')
    })
  })

  // ── backdrop ─────────────────────────────────────────────────────────────────

  describe('backdrop', () => {
    it('renders the backdrop element', () => {
      const wrapper = mountBanner()

      expect(wrapper.find('.cookie-consent-backdrop').exists()).toBe(true)
    })

    it('clicking the backdrop does NOT call setConsent', async () => {
      const wrapper = mountBanner()

      await wrapper.find('.cookie-consent-backdrop').trigger('click')

      expect(mockSetConsent).not.toHaveBeenCalled()
    })
  })

  // ── cross-tab sync lifecycle ──────────────────────────────────────────────

  describe('cross-tab sync', () => {
    it('calls setupCrossTabSync on mount', () => {
      mountBanner()

      expect(mockSetupCrossTabSync).toHaveBeenCalledOnce()
    })

    it('calls the cleanup function returned by setupCrossTabSync on unmount', () => {
      const mockCleanup = vi.fn()
      mockSetupCrossTabSync.mockReturnValue(mockCleanup)

      const wrapper = mountBanner()
      wrapper.unmount()

      expect(mockCleanup).toHaveBeenCalledOnce()
    })
  })
})
