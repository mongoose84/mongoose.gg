import { describe, it, expect } from 'vitest';
import { exampleAccount, formatSignedLp } from '@/components/landing/exampleAccount';

describe('landing exampleAccount', () => {
  describe('formatSignedLp', () => {
    it('adds a plus sign to gains', () => {
      expect(formatSignedLp(19)).toBe('+19 LP');
    });

    it('uses a real minus sign (U+2212) for losses', () => {
      expect(formatSignedLp(-17)).toBe('−17 LP');
      expect(formatSignedLp(-17)).not.toContain('-');
    });

    it('treats zero as no loss', () => {
      expect(formatSignedLp(0)).toBe('+0 LP');
    });
  });

  it('keeps the example scores in the 0–100 range with three scores', () => {
    expect(exampleAccount.scores).toHaveLength(3);
    for (const score of exampleAccount.scores) {
      expect(score.value).toBeGreaterThanOrEqual(0);
      expect(score.value).toBeLessThanOrEqual(100);
    }
  });

  it('shows at most three champion cards', () => {
    expect(exampleAccount.champions.length).toBeLessThanOrEqual(3);
  });
});
