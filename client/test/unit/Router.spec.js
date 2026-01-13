import { describe, it, expect } from 'vitest';
import { createRouter, createMemoryHistory } from 'vue-router';
import router from '@/router';

describe('Router Configuration', () => {
  it('should have all required routes defined', () => {
    const routes = router.getRoutes();
    const routeNames = routes.map(r => r.name);
    
    expect(routeNames).toContain('home');
    expect(routeNames).toContain('auth');
    expect(routeNames).toContain('privacy');
    expect(routeNames).toContain('terms');
  });

  it('should have correct paths for each route', () => {
    const routes = router.getRoutes();
    const routeMap = {};
    
    routes.forEach(r => {
      routeMap[r.name] = r.path;
    });
    
    expect(routeMap.home).toBe('/');
    expect(routeMap.auth).toBe('/auth');
    expect(routeMap.privacy).toBe('/privacy');
    expect(routeMap.terms).toBe('/terms');
  });

  it('should navigate to home route', async () => {
    const testRouter = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', name: 'home', component: { template: '<div>Home</div>' } }
      ]
    });
    
    await testRouter.push('/');
    expect(testRouter.currentRoute.value.path).toBe('/');
  });

  it('should navigate to auth route', async () => {
    const testRouter = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Home</div>' } },
        { path: '/auth', name: 'auth', component: { template: '<div>Auth</div>' } }
      ]
    });
    
    await testRouter.push('/auth');
    expect(testRouter.currentRoute.value.path).toBe('/auth');
  });

  it('should navigate to privacy route', async () => {
    const testRouter = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Home</div>' } },
        { path: '/privacy', name: 'privacy', component: { template: '<div>Privacy</div>' } }
      ]
    });
    
    await testRouter.push('/privacy');
    expect(testRouter.currentRoute.value.path).toBe('/privacy');
  });

  it('should navigate to terms route', async () => {
    const testRouter = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Home</div>' } },
        { path: '/terms', name: 'terms', component: { template: '<div>Terms</div>' } }
      ]
    });
    
    await testRouter.push('/terms');
    expect(testRouter.currentRoute.value.path).toBe('/terms');
  });

  it('should have scroll behavior defined', () => {
    expect(router.options.scrollBehavior).toBeDefined();
    expect(typeof router.options.scrollBehavior).toBe('function');
  });

  it('should scroll to top by default', () => {
    const scrollResult = router.options.scrollBehavior(
      { hash: '' },
      { hash: '' },
      null
    );
    expect(scrollResult).toEqual({ top: 0 });
  });

  it('should scroll to hash when provided', () => {
    const scrollResult = router.options.scrollBehavior(
      { hash: '#features' },
      {},
      null
    );
    expect(scrollResult.el).toBe('#features');
    expect(scrollResult.behavior).toBe('smooth');
  });
});
