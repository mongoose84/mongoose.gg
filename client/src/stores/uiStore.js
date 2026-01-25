import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

const SIDEBAR_COLLAPSED_KEY = 'sidebarCollapsed'
const MOBILE_BREAKPOINT = 1024

export const useUiStore = defineStore('ui', () => {
  // State
  const sidebarCollapsed = ref(false)
  const windowWidth = ref(typeof window !== 'undefined' ? window.innerWidth : 1920)

  // Getters
  const isSidebarCollapsed = computed(() => sidebarCollapsed.value)
  const isMobile = computed(() => windowWidth.value < MOBILE_BREAKPOINT)
  const sidebarWidth = computed(() => sidebarCollapsed.value ? 64 : 256)

  // Actions
  function initializeSidebar() {
    // Load saved state from localStorage
    const savedState = localStorage.getItem(SIDEBAR_COLLAPSED_KEY)
    if (savedState !== null) {
      sidebarCollapsed.value = savedState === 'true'
    }

    // Auto-collapse on smaller screens
    if (window.innerWidth < MOBILE_BREAKPOINT) {
      sidebarCollapsed.value = true
    }

    // Update window width
    windowWidth.value = window.innerWidth
  }

  function toggleSidebar() {
    sidebarCollapsed.value = !sidebarCollapsed.value
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, sidebarCollapsed.value.toString())
  }

  function setSidebarCollapsed(collapsed) {
    sidebarCollapsed.value = collapsed
    localStorage.setItem(SIDEBAR_COLLAPSED_KEY, collapsed.toString())
  }

  function handleResize() {
    windowWidth.value = window.innerWidth
    // Auto-collapse on smaller screens if not already collapsed
    if (window.innerWidth < MOBILE_BREAKPOINT && !sidebarCollapsed.value) {
      sidebarCollapsed.value = true
    }
  }

  return {
    // State
    sidebarCollapsed,
    windowWidth,
    // Getters
    isSidebarCollapsed,
    isMobile,
    sidebarWidth,
    // Actions
    initializeSidebar,
    toggleSidebar,
    setSidebarCollapsed,
    handleResize
  }
})

