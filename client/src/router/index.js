import { createRouter, createWebHistory } from 'vue-router'
import SoloDashboard from '../views/SoloDashboard.vue'
import DuoDashboard from '../views/DuoDashboard.vue'
import TeamDashboard from '../views/TeamDashboard.vue'
import Home from '../views/HomeView.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Home,
  },
  {
    path: '/solo',
    name: 'SoloDashboard',
    component: SoloDashboard,

    // pass the query string as a prop so the component can read it easily
    props: route => {
      const userName = route.query.userName || '';
      let userId;
      if (route.query.userId !== undefined) {
        const parsedId = Number(route.query.userId);
        userId = isNaN(parsedId) ? undefined : parsedId;
      }
      return { userName, userId };
    },
  },
  {
    path: '/duo',
    name: 'DuoDashboard',
    component: DuoDashboard,

    props: route => {
      const userName = route.query.userName || '';
      let userId;
      if (route.query.userId !== undefined) {
        const parsedId = Number(route.query.userId);
        userId = isNaN(parsedId) ? undefined : parsedId;
      }
      return { userName, userId };
    },
  },
  {
    path: '/team',
    name: 'TeamDashboard',
    component: TeamDashboard,

    props: route => {
      const userName = route.query.userName || '';
      let userId;
      if (route.query.userId !== undefined) {
        const parsedId = Number(route.query.userId);
        userId = isNaN(parsedId) ? undefined : parsedId;
      }
      return { userName, userId };
    },
  }
]

export default createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})