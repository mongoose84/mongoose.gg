/**
 * Section entrance from the design system: a section fades in from 8px below
 * over 300ms the first time it scrolls into view, and never animates again.
 * Skipped entirely with prefers-reduced-motion or without IntersectionObserver.
 *
 * Usage: import { vRevealOnView } from '@/composables/useRevealOnView'
 *        <section v-reveal-on-view>…</section>
 */

const HIDDEN_CLASS = 'mp-reveal'
const VISIBLE_CLASS = 'mp-reveal--visible'

function prefersReducedMotion() {
  return typeof window !== 'undefined'
    && typeof window.matchMedia === 'function'
    && window.matchMedia('(prefers-reduced-motion: reduce)').matches
}

export const vRevealOnView = {
  mounted(el) {
    if (prefersReducedMotion() || typeof IntersectionObserver === 'undefined') {
      return
    }

    el.classList.add(HIDDEN_CLASS)
    const observer = new IntersectionObserver((entries) => {
      if (entries.some((entry) => entry.isIntersecting)) {
        el.classList.add(VISIBLE_CLASS)
        observer.disconnect()
      }
    }, { threshold: 0.15 })

    observer.observe(el)
    el._revealObserver = observer
  },

  unmounted(el) {
    el._revealObserver?.disconnect()
    delete el._revealObserver
  }
}
