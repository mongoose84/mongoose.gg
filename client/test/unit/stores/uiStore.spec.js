import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useUiStore } from '@/stores/uiStore'

const mockGetItem = vi.fn()
const mockSetItem = vi.fn()
const mockRemoveItem = vi.fn()

Object.defineProperty(global, 'localStorage', {
  value: {
    getItem: mockGetItem,
    setItem: mockSetItem,
    removeItem: mockRemoveItem,
    clear: vi.fn()
  },
  writable: true
})

describe('uiStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    mockGetItem.mockReturnValue(null)
    Object.defineProperty(window, 'innerWidth', { value: 1920, writable: true, configurable: true })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('initial state', () => {
    it('starts with sidebar expanded by default', () => {
      const store = useUiStore()
      expect(store.sidebarCollapsed).toBe(false)
    })

    it('isSidebarCollapsed reflects sidebarCollapsed', () => {
      const store = useUiStore()
      expect(store.isSidebarCollapsed).toBe(false)
    })

    it('isMobile is false at 1920px width', () => {
      const store = useUiStore()
      expect(store.isMobile).toBe(false)
    })

    it('sidebarWidth is 256 when expanded', () => {
      const store = useUiStore()
      expect(store.sidebarWidth).toBe(256)
    })

    it('sidebarWidth is 64 when collapsed', () => {
      const store = useUiStore()
      store.sidebarCollapsed = true
      expect(store.sidebarWidth).toBe(64)
    })
  })

  describe('initializeSidebar', () => {
    it('loads saved collapsed=true from localStorage', () => {
      mockGetItem.mockReturnValue('true')
      const store = useUiStore()
      store.initializeSidebar()
      expect(store.sidebarCollapsed).toBe(true)
    })

    it('loads saved collapsed=false from localStorage', () => {
      mockGetItem.mockReturnValue('false')
      const store = useUiStore()
      store.initializeSidebar()
      expect(store.sidebarCollapsed).toBe(false)
    })

    it('auto-collapses if window width < 1024', () => {
      Object.defineProperty(window, 'innerWidth', { value: 800, writable: true, configurable: true })
      mockGetItem.mockReturnValue(null)
      const store = useUiStore()
      store.initializeSidebar()
      expect(store.sidebarCollapsed).toBe(true)
    })

    it('does not collapse if window width >= 1024', () => {
      Object.defineProperty(window, 'innerWidth', { value: 1200, writable: true, configurable: true })
      mockGetItem.mockReturnValue(null)
      const store = useUiStore()
      store.initializeSidebar()
      expect(store.sidebarCollapsed).toBe(false)
    })

    it('updates windowWidth from window.innerWidth', () => {
      Object.defineProperty(window, 'innerWidth', { value: 1440, writable: true, configurable: true })
      const store = useUiStore()
      store.initializeSidebar()
      expect(store.windowWidth).toBe(1440)
    })
  })

  describe('toggleSidebar', () => {
    it('collapses sidebar when expanded', () => {
      const store = useUiStore()
      store.sidebarCollapsed = false
      store.toggleSidebar()
      expect(store.sidebarCollapsed).toBe(true)
    })

    it('expands sidebar when collapsed', () => {
      const store = useUiStore()
      store.sidebarCollapsed = true
      store.toggleSidebar()
      expect(store.sidebarCollapsed).toBe(false)
    })

    it('saves new state to localStorage', () => {
      const store = useUiStore()
      store.sidebarCollapsed = false
      store.toggleSidebar()
      expect(mockSetItem).toHaveBeenCalledWith('sidebarCollapsed', 'true')
    })
  })

  describe('setSidebarCollapsed', () => {
    it('sets collapsed to true', () => {
      const store = useUiStore()
      store.setSidebarCollapsed(true)
      expect(store.sidebarCollapsed).toBe(true)
    })

    it('sets collapsed to false', () => {
      const store = useUiStore()
      store.sidebarCollapsed = true
      store.setSidebarCollapsed(false)
      expect(store.sidebarCollapsed).toBe(false)
    })

    it('persists value to localStorage', () => {
      const store = useUiStore()
      store.setSidebarCollapsed(true)
      expect(mockSetItem).toHaveBeenCalledWith('sidebarCollapsed', 'true')
    })
  })

  describe('handleResize', () => {
    it('updates windowWidth from window.innerWidth', () => {
      Object.defineProperty(window, 'innerWidth', { value: 768, writable: true, configurable: true })
      const store = useUiStore()
      store.handleResize()
      expect(store.windowWidth).toBe(768)
    })

    it('auto-collapses sidebar when below mobile breakpoint', () => {
      Object.defineProperty(window, 'innerWidth', { value: 800, writable: true, configurable: true })
      const store = useUiStore()
      store.sidebarCollapsed = false
      store.handleResize()
      expect(store.sidebarCollapsed).toBe(true)
    })

    it('does not change collapsed state when already collapsed on mobile', () => {
      Object.defineProperty(window, 'innerWidth', { value: 800, writable: true, configurable: true })
      const store = useUiStore()
      store.sidebarCollapsed = true
      store.handleResize()
      expect(store.sidebarCollapsed).toBe(true)
    })

    it('does not collapse sidebar on desktop resize', () => {
      Object.defineProperty(window, 'innerWidth', { value: 1440, writable: true, configurable: true })
      const store = useUiStore()
      store.sidebarCollapsed = false
      store.handleResize()
      expect(store.sidebarCollapsed).toBe(false)
    })

    it('isMobile computed updates after resize', () => {
      Object.defineProperty(window, 'innerWidth', { value: 800, writable: true, configurable: true })
      const store = useUiStore()
      store.handleResize()
      expect(store.isMobile).toBe(true)
    })
  })
})
