import { createRouter, createWebHistory } from 'vue-router'
import LandingPage from '../views/LandingPage.vue'
import { useAuthStore } from '../stores/authStore'
import { trackPageView } from '../services/analyticsApi'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: LandingPage
    },
    {
      path: '/auth',
      name: 'auth',
      component: () => import('../views/AuthPage.vue')
    },
    {
      path: '/v2/auth',
      redirect: '/auth'
    },
    {
      path: '/auth/verify',
      name: 'verify',
      component: () => import('../views/VerifyPage.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/privacy',
      name: 'privacy',
      component: () => import('../views/PrivacyPage.vue')
    },
    {
      path: '/terms',
      name: 'terms',
      component: () => import('../views/TermsPage.vue')
    },
    {
      path: '/app',
      component: () => import('../layouts/AppLayout.vue'),
      meta: { requiresAuth: true, requiresVerified: true },
      children: [
        {
          path: '',
          redirect: '/app/overview'
        },
        {
          path: 'overview',
          name: 'app-overview',
          component: () => import('../views/OverviewPage.vue')
        },
        {
          path: 'champion-select',
          name: 'app-champion-select',
          component: () => import('../views/ChampionSelectPage.vue')
        },
        {
          path: 'matches',
          name: 'app-matches',
          component: () => import('../views/MatchesPage.vue')
        },
        {
          path: 'solo',
          name: 'app-solo',
          component: () => import('../views/SoloPage.vue')
        },
        {
          path: 'duo',
          name: 'app-duo',
          component: () => import('../views/DuoPage.vue')
        },
        {
          path: 'team',
          name: 'app-team',
          component: () => import('../views/TeamPage.vue')
        },
        {
          path: 'goals',
          name: 'app-goals',
          component: () => import('../views/GoalsPage.vue')
        },
        {
          path: 'user',
          name: 'app-user',
          component: () => import('../views/UserSettingsPage.vue')
        }
      ]
    },
    {
      path: '/v2/app/solo',
      redirect: '/app/solo'
    }
  ],
  scrollBehavior(to, from, savedPosition) {
    if (to.hash) {
      return {
        el: to.hash,
        behavior: 'smooth',
      }
    }
    if (savedPosition) {
      return savedPosition
    }
    return { top: 0 }
  }
})

// Navigation guards
router.beforeEach(async (to, from, next) => {
  const authStore = useAuthStore()

  // Initialize auth store if not already done
  if (!authStore.isInitialized) {
    await authStore.initialize()
  }

  // Check if route requires authentication
  if (to.meta.requiresAuth) {
    if (!authStore.isAuthenticated) {
      return next({ path: '/auth', query: { mode: 'login', redirect: to.fullPath } })
    }

    // Check if route requires verified email
    if (to.meta.requiresVerified && !authStore.isVerified) {
      return next('/auth/verify')
    }
  }

  // Redirect verified users away from verify page
  if (to.name === 'verify' && authStore.isAuthenticated && authStore.isVerified) {
    return next('/app/overview')
  }

  next()
})

// Track page views after navigation
router.afterEach((to, from) => {
  trackPageView(to.fullPath, from.fullPath || null)
})

export default router
