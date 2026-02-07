import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  formatRole,
  formatRoleWithAdc,
  formatDuration,
  formatRelativeTime,
  formatDate,
  formatNumber,
  formatWinRate,
  formatPercent,
  formatLpPerGame,
  formatGoldDiff,
  formatCsDiff,
  formatKda,
  formatKdaFromParticipant,
  calculateKdaRatio
} from '@/utils/formatters';

describe('formatters', () => {
  // ============================================================================
  // Role Formatting
  // ============================================================================

  describe('formatRole', () => {
    it('returns empty string for null/undefined', () => {
      expect(formatRole(null)).toBe('');
      expect(formatRole(undefined)).toBe('');
      expect(formatRole('')).toBe('');
    });

    it('formats standard roles correctly', () => {
      expect(formatRole('TOP')).toBe('Top');
      expect(formatRole('JUNGLE')).toBe('Jungle');
      expect(formatRole('MIDDLE')).toBe('Mid');
      expect(formatRole('BOTTOM')).toBe('Bot');
      expect(formatRole('UTILITY')).toBe('Support');
    });

    it('handles alternative role names', () => {
      expect(formatRole('MID')).toBe('Mid');
      expect(formatRole('ADC')).toBe('Bot');
      expect(formatRole('SUPPORT')).toBe('Support');
    });

    it('handles special roles', () => {
      // Note: formatRole uses roleMap lookup, but empty string values still
      // return the original value due to falsy check in || operator
      expect(formatRole('FILL')).toBe('Fill');
      expect(formatRole('ARAM')).toBe('ARAM');
    });

    it('returns original value for NONE/UNKNOWN (maps to empty but falsy check returns original)', () => {
      // roleMap has NONE: '' and UNKNOWN: '', but '' || role returns role
      expect(formatRole('NONE')).toBe('NONE');
      expect(formatRole('UNKNOWN')).toBe('UNKNOWN');
    });

    it('is case-insensitive', () => {
      expect(formatRole('top')).toBe('Top');
      expect(formatRole('Top')).toBe('Top');
      expect(formatRole('jungle')).toBe('Jungle');
    });

    it('returns original value for unknown roles', () => {
      expect(formatRole('CUSTOM_ROLE')).toBe('CUSTOM_ROLE');
    });
  });

  describe('formatRoleWithAdc', () => {
    it('returns empty string for null/undefined', () => {
      expect(formatRoleWithAdc(null)).toBe('');
      expect(formatRoleWithAdc(undefined)).toBe('');
      expect(formatRoleWithAdc('')).toBe('');
    });

    it('formats BOTTOM as ADC instead of Bot', () => {
      expect(formatRoleWithAdc('BOTTOM')).toBe('ADC');
      expect(formatRoleWithAdc('ADC')).toBe('ADC');
    });

    it('formats other roles the same as formatRole', () => {
      expect(formatRoleWithAdc('TOP')).toBe('Top');
      expect(formatRoleWithAdc('JUNGLE')).toBe('Jungle');
      expect(formatRoleWithAdc('MIDDLE')).toBe('Mid');
      expect(formatRoleWithAdc('UTILITY')).toBe('Support');
    });

    it('handles UNKNOWN as Fill (different from formatRole)', () => {
      expect(formatRoleWithAdc('UNKNOWN')).toBe('Fill');
    });
  });

  // ============================================================================
  // Time Formatting
  // ============================================================================

  describe('formatDuration', () => {
    it('returns -- for null/undefined', () => {
      expect(formatDuration(null)).toBe('--');
      expect(formatDuration(undefined)).toBe('--');
    });

    it('formats seconds correctly', () => {
      expect(formatDuration(0)).toBe('0:00');
      expect(formatDuration(30)).toBe('0:30');
      expect(formatDuration(60)).toBe('1:00');
      expect(formatDuration(90)).toBe('1:30');
      expect(formatDuration(125)).toBe('2:05');
    });

    it('formats longer durations', () => {
      expect(formatDuration(1800)).toBe('30:00');
      expect(formatDuration(1965)).toBe('32:45');
      expect(formatDuration(3600)).toBe('60:00');
    });

    it('pads seconds with leading zero', () => {
      expect(formatDuration(61)).toBe('1:01');
      expect(formatDuration(69)).toBe('1:09');
    });
  });

  describe('formatRelativeTime', () => {
    let mockNow;

    beforeEach(() => {
      // Mock Date.now to return a fixed timestamp
      mockNow = new Date('2026-02-07T12:00:00Z').getTime();
      vi.spyOn(Date, 'now').mockReturnValue(mockNow);
    });

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('returns empty string for falsy values', () => {
      expect(formatRelativeTime(null)).toBe('');
      expect(formatRelativeTime(undefined)).toBe('');
      expect(formatRelativeTime(0)).toBe('');
    });

    describe('long format (default)', () => {
      it('returns "Just now" for less than 1 minute', () => {
        expect(formatRelativeTime(mockNow - 30000)).toBe('Just now');
      });

      it('formats minutes correctly', () => {
        expect(formatRelativeTime(mockNow - 60000)).toBe('1 min ago');
        expect(formatRelativeTime(mockNow - 300000)).toBe('5 min ago');
        expect(formatRelativeTime(mockNow - 3540000)).toBe('59 min ago');
      });

      it('formats hours correctly with pluralization', () => {
        expect(formatRelativeTime(mockNow - 3600000)).toBe('1 hour ago');
        expect(formatRelativeTime(mockNow - 7200000)).toBe('2 hours ago');
        expect(formatRelativeTime(mockNow - 82800000)).toBe('23 hours ago');
      });

      it('formats days correctly with pluralization', () => {
        expect(formatRelativeTime(mockNow - 86400000)).toBe('1 day ago');
        expect(formatRelativeTime(mockNow - 172800000)).toBe('2 days ago');
        expect(formatRelativeTime(mockNow - 518400000)).toBe('6 days ago');
      });

      it('formats weeks correctly with pluralization', () => {
        expect(formatRelativeTime(mockNow - 604800000)).toBe('1 week ago');
        expect(formatRelativeTime(mockNow - 1209600000)).toBe('2 weeks ago');
        expect(formatRelativeTime(mockNow - 1814400000)).toBe('3 weeks ago'); // 21 days
      });

      it('formats months correctly with pluralization', () => {
        expect(formatRelativeTime(mockNow - 2592000000)).toBe('1 month ago');
        expect(formatRelativeTime(mockNow - 5184000000)).toBe('2 months ago');
      });
    });

    describe('short format', () => {
      it('returns "Just now" for less than 1 minute', () => {
        expect(formatRelativeTime(mockNow - 30000, { short: true })).toBe('Just now');
      });

      it('formats minutes with m suffix', () => {
        expect(formatRelativeTime(mockNow - 60000, { short: true })).toBe('1m ago');
        expect(formatRelativeTime(mockNow - 300000, { short: true })).toBe('5m ago');
      });

      it('formats hours with h suffix', () => {
        expect(formatRelativeTime(mockNow - 3600000, { short: true })).toBe('1h ago');
        expect(formatRelativeTime(mockNow - 7200000, { short: true })).toBe('2h ago');
      });

      it('formats days with d suffix', () => {
        expect(formatRelativeTime(mockNow - 86400000, { short: true })).toBe('1d ago');
        expect(formatRelativeTime(mockNow - 172800000, { short: true })).toBe('2d ago');
      });

      it('formats weeks with w suffix', () => {
        expect(formatRelativeTime(mockNow - 604800000, { short: true })).toBe('1w ago');
        expect(formatRelativeTime(mockNow - 1209600000, { short: true })).toBe('2w ago');
      });

      it('formats months with mo suffix', () => {
        expect(formatRelativeTime(mockNow - 2592000000, { short: true })).toBe('1mo ago');
        expect(formatRelativeTime(mockNow - 5184000000, { short: true })).toBe('2mo ago');
      });
    });
  });

  describe('formatDate', () => {
    it('formats Date objects correctly', () => {
      const date = new Date('2026-01-15T12:00:00Z');
      expect(formatDate(date)).toBe('Jan 15');
    });

    it('formats timestamps correctly', () => {
      const timestamp = new Date('2026-03-22T12:00:00Z').getTime();
      expect(formatDate(timestamp)).toBe('Mar 22');
    });

    it('handles different months', () => {
      expect(formatDate(new Date('2026-12-25'))).toBe('Dec 25');
      expect(formatDate(new Date('2026-06-01'))).toBe('Jun 1');
    });
  });

  // ============================================================================
  // Number Formatting
  // ============================================================================

  describe('formatNumber', () => {
    it('returns 0 for null/undefined', () => {
      expect(formatNumber(null)).toBe('0');
      expect(formatNumber(undefined)).toBe('0');
    });

    it('returns number as string for values under 1000', () => {
      expect(formatNumber(0)).toBe('0');
      expect(formatNumber(100)).toBe('100');
      expect(formatNumber(999)).toBe('999');
    });

    it('formats values >= 1000 with k suffix', () => {
      expect(formatNumber(1000)).toBe('1.0k');
      expect(formatNumber(1500)).toBe('1.5k');
      expect(formatNumber(10000)).toBe('10.0k');
      expect(formatNumber(123456)).toBe('123.5k');
    });
  });

  describe('formatWinRate', () => {
    it('returns -- for null/undefined/NaN', () => {
      expect(formatWinRate(null)).toBe('--');
      expect(formatWinRate(undefined)).toBe('--');
      expect(formatWinRate(NaN)).toBe('--');
    });

    it('formats valid percentages with 1 decimal', () => {
      expect(formatWinRate(50)).toBe('50.0%');
      expect(formatWinRate(50.55)).toBe('50.5%'); // JS toFixed uses banker's rounding
      expect(formatWinRate(50.56)).toBe('50.6%');
      expect(formatWinRate(0)).toBe('0.0%');
      expect(formatWinRate(100)).toBe('100.0%');
    });
  });

  describe('formatPercent', () => {
    it('returns -- for null/undefined/NaN', () => {
      expect(formatPercent(null)).toBe('--');
      expect(formatPercent(undefined)).toBe('--');
      expect(formatPercent(NaN)).toBe('--');
    });

    it('formats with 0 decimals by default', () => {
      expect(formatPercent(50)).toBe('50%');
      expect(formatPercent(50.6)).toBe('51%');
    });

    it('respects custom decimal places', () => {
      expect(formatPercent(50.56, 1)).toBe('50.6%');
      expect(formatPercent(50.556, 2)).toBe('50.56%'); // JS toFixed uses banker's rounding
      expect(formatPercent(50.557, 2)).toBe('50.56%');
      expect(formatPercent(50.558, 2)).toBe('50.56%');
      expect(formatPercent(50.559, 2)).toBe('50.56%');
    });
  });

  describe('formatLpPerGame', () => {
    it('returns -- for null/undefined/NaN', () => {
      expect(formatLpPerGame(null)).toBe('--');
      expect(formatLpPerGame(undefined)).toBe('--');
      expect(formatLpPerGame(NaN)).toBe('--');
    });

    it('formats positive values with + sign', () => {
      expect(formatLpPerGame(5)).toBe('+5.0');
      expect(formatLpPerGame(15.5)).toBe('+15.5');
    });

    it('formats negative values with - sign', () => {
      expect(formatLpPerGame(-5)).toBe('-5.0');
      expect(formatLpPerGame(-15.5)).toBe('-15.5');
    });

    it('formats zero without sign', () => {
      expect(formatLpPerGame(0)).toBe('0.0');
    });
  });

  describe('formatGoldDiff', () => {
    it('returns N/A for null/undefined', () => {
      expect(formatGoldDiff(null)).toBe('N/A');
      expect(formatGoldDiff(undefined)).toBe('N/A');
    });

    it('formats positive values with + sign', () => {
      expect(formatGoldDiff(500)).toBe('+500');
      expect(formatGoldDiff(0)).toBe('+0');
    });

    it('formats negative values with - sign', () => {
      expect(formatGoldDiff(-500)).toBe('-500');
    });

    it('formats values >= 1000 with k suffix', () => {
      expect(formatGoldDiff(1000)).toBe('+1.0k');
      expect(formatGoldDiff(1500)).toBe('+1.5k');
      expect(formatGoldDiff(-2500)).toBe('-2.5k');
    });

    it('uses locale formatting when useLocale is true', () => {
      expect(formatGoldDiff(1500, { useLocale: true })).toBe('+1,500');
      expect(formatGoldDiff(-2500, { useLocale: true })).toBe('-2,500');
    });
  });

  describe('formatCsDiff', () => {
    it('returns N/A for null/undefined', () => {
      expect(formatCsDiff(null)).toBe('N/A');
      expect(formatCsDiff(undefined)).toBe('N/A');
    });

    it('formats positive values with + sign and CS suffix', () => {
      expect(formatCsDiff(10)).toBe('+10 CS');
      expect(formatCsDiff(0)).toBe('+0 CS');
    });

    it('formats negative values with - sign and CS suffix', () => {
      expect(formatCsDiff(-15)).toBe('-15 CS');
    });
  });

  // ============================================================================
  // KDA Formatting
  // ============================================================================

  describe('formatKda', () => {
    it('formats kills/deaths/assists correctly', () => {
      expect(formatKda(5, 2, 10)).toBe('5/2/10');
      expect(formatKda(0, 0, 0)).toBe('0/0/0');
      expect(formatKda(15, 3, 7)).toBe('15/3/7');
    });
  });

  describe('formatKdaFromParticipant', () => {
    it('formats participant object correctly', () => {
      expect(formatKdaFromParticipant({ kills: 5, deaths: 2, assists: 10 })).toBe('5/2/10');
      expect(formatKdaFromParticipant({ kills: 0, deaths: 0, assists: 0 })).toBe('0/0/0');
    });
  });

  describe('calculateKdaRatio', () => {
    it('calculates KDA ratio correctly', () => {
      expect(calculateKdaRatio(5, 2, 10)).toBe(7.5);
      expect(calculateKdaRatio(10, 5, 5)).toBe(3);
    });

    it('returns kills + assists when deaths is 0 (perfect KDA)', () => {
      expect(calculateKdaRatio(5, 0, 10)).toBe(15);
      expect(calculateKdaRatio(0, 0, 0)).toBe(0);
    });

    it('handles edge cases', () => {
      expect(calculateKdaRatio(0, 1, 0)).toBe(0);
      expect(calculateKdaRatio(1, 1, 1)).toBe(2);
    });
  });
});

