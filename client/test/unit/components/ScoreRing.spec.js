/**
 * ScoreRing Component Tests
 *
 * 0–100 score ring: meter semantics, colour rule (primary at 70+, warn below) and clamping.
 */

import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import ScoreRing from '@/components/base/ScoreRing.vue';

describe('ScoreRing', () => {
  it('renders the score as a meter with a spoken value', () => {
    const wrapper = mount(ScoreRing, { props: { value: 82, label: 'Laning' } });
    const ring = wrapper.get('[data-testid="score-ring"]');
    expect(ring.attributes('role')).toBe('meter');
    expect(ring.attributes('aria-valuenow')).toBe('82');
    expect(ring.attributes('aria-valuetext')).toBe('82 out of 100, strong');
    expect(ring.attributes('aria-label')).toBe('Laning');
    expect(wrapper.get('[data-testid="score-ring-value"]').text()).toBe('82');
  });

  it('uses the warn colour below 70', () => {
    expect(mount(ScoreRing, { props: { value: 69, label: 'Discipline' } }).classes()).toContain('score-ring--warn');
    expect(mount(ScoreRing, { props: { value: 70, label: 'Discipline' } }).classes()).not.toContain('score-ring--warn');
  });

  it('clamps values outside 0–100', () => {
    const wrapper = mount(ScoreRing, { props: { value: 140, label: 'Laning' } });
    expect(wrapper.get('[data-testid="score-ring-value"]').text()).toBe('100');
  });

  it('fills the ring in proportion to the score', () => {
    const wrapper = mount(ScoreRing, { props: { value: 50, label: 'Laning', size: 140 } });
    const [filled, total] = wrapper.findAll('circle')[1].attributes('stroke-dasharray').split(' ').map(Number);
    expect(filled / total).toBeCloseTo(0.5, 2);
  });
});
