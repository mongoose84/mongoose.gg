import { describe, it, expect } from 'vitest';
import { featureFlags } from '@/utils/featureFlags';

describe('featureFlags', () => {
  it('is an object', () => {
    expect(typeof featureFlags).toBe('object');
    expect(featureFlags).not.toBeNull();
  });

  it('includes the required keys teamAnalytics and goals', () => {
    expect(featureFlags).toHaveProperty('teamAnalytics');
    expect(featureFlags).toHaveProperty('goals');
  });

  it('teamAnalytics is a boolean', () => {
    expect(typeof featureFlags.teamAnalytics).toBe('boolean');
  });

  it('goals is a boolean', () => {
    expect(typeof featureFlags.goals).toBe('boolean');
  });

  it('teamAnalytics is false in test environment (VITE_ env var not set)', () => {
    // In test env, VITE_FEATURE_TEAM_ANALYTICS is undefined, so !== 'true'
    expect(featureFlags.teamAnalytics).toBe(false);
  });

  it('goals is false in test environment (VITE_ env var not set)', () => {
    // In test env, VITE_FEATURE_GOALS is undefined, so !== 'true'
    expect(featureFlags.goals).toBe(false);
  });
});
