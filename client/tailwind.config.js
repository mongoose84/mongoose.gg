/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      screens: {
        // Design-system layout breakpoint: below 900px everything stacks
        desk: '900px',
      },
      colors: {
        // Mongoose.gg design system (values in src/style.css)
        primary: {
          DEFAULT: 'var(--color-primary)',
          soft: 'var(--color-primary-soft)',
          dark: 'var(--color-primary-dark)',
          light: 'var(--color-primary-light)',
          accent: 'var(--color-primary-accent)',
          on: 'var(--color-on-primary)',
        },
        positive: {
          DEFAULT: 'var(--color-positive-text)',
          strong: 'var(--color-positive-text-strong)',
        },
        warn: {
          DEFAULT: 'var(--color-warn)',
          text: 'var(--color-warn-text)',
          soft: 'var(--color-warn-soft)',
        },
        track: {
          DEFAULT: 'var(--color-track)',
          strong: 'var(--color-track-strong)',
        },
        glass: 'var(--color-glass)',
        background: {
          DEFAULT: 'var(--color-bg)',
          surface: 'var(--color-surface)',
          elevated: 'var(--color-elevated)',
          selected: 'var(--color-surface-selected)',
          'selected-hover': 'var(--color-surface-selected-hover)',
          highlight: 'var(--color-surface-highlight)',
        },
        text: {
          DEFAULT: 'var(--color-text)',
          secondary: 'var(--color-text-secondary)',
          soft: 'var(--color-ink-soft)',
          faint: 'var(--color-ink-faint)',
        },
        border: {
          DEFAULT: 'var(--color-border)',
          highlight: 'var(--color-border-highlight)',
        },
        // Semantic colors
        success: {
          DEFAULT: 'var(--color-success)',
          soft: 'var(--color-success-soft)',
          border: 'var(--color-success-border)',
        },
        error: {
          DEFAULT: 'var(--color-error)',
          soft: 'var(--color-error-soft)',
          border: 'var(--color-error-border)',
        },
        warning: {
          DEFAULT: 'var(--color-warning)',
          soft: 'var(--color-warning-soft)',
          border: 'var(--color-warning-border)',
        },
        info: {
          DEFAULT: 'var(--color-info)',
          soft: 'var(--color-info-soft)',
          border: 'var(--color-info-border)',
        },
        muted: {
          DEFAULT: 'var(--color-muted)',
          soft: 'var(--color-muted-soft)',
        },
        // Win rate gradient colors
        winrate: {
          terrible: 'var(--color-winrate-terrible)',
          bad: 'var(--color-winrate-bad)',
          poor: 'var(--color-winrate-poor)',
          average: 'var(--color-winrate-average)',
          good: 'var(--color-winrate-good)',
          great: 'var(--color-winrate-great)',
        },
      },
      fontFamily: {
        sans: ['Satoshi', 'system-ui', '-apple-system', 'Segoe UI', 'sans-serif'],
        display: 'var(--font-display)',
        body: 'var(--font-body)',
        mono: ['ui-monospace', 'SFMono-Regular', 'Consolas', 'monospace'],
      },
      fontSize: {
        '4xs': ['0.5rem', { lineHeight: '1.4' }],      // 8px
        '3xs': ['0.5625rem', { lineHeight: '1.4' }],   // 9px
        '2xs': ['0.625rem', { lineHeight: '1.4' }],    // 10px
        'xs': ['var(--font-size-xs)', { lineHeight: '1.5' }],
        'sm': ['var(--font-size-sm)', { lineHeight: '1.5' }],
        'base': ['var(--font-size-md)', { lineHeight: '1.6' }],
        'lg': ['var(--font-size-lg)', { lineHeight: '1.6' }],
        'xl': ['var(--font-size-xl)', { lineHeight: '1.4' }],
        '2xl': ['var(--font-size-2xl)', { lineHeight: '1.2' }],
        // Design-system type styles (tokens.json → type.groups)
        // Landing hero only (public marketing pages)
        'display': ['3.75rem', { lineHeight: '1.06', letterSpacing: '-0.015em', fontWeight: '600' }],
        'score-xl': ['4rem', { lineHeight: '1', fontWeight: '700' }],
        'headline': ['2.75rem', { lineHeight: '1.12', letterSpacing: '-0.01em', fontWeight: '600' }],
        'score-lg': ['2.5rem', { lineHeight: '1', fontWeight: '700' }],
        'title-lg': ['1.625rem', { lineHeight: '1.2', fontWeight: '700' }],
        'stat': ['1.375rem', { lineHeight: '1.2', fontWeight: '700' }],
        'title': ['1.25rem', { lineHeight: '1.3', fontWeight: '600' }],
        'insight': ['1rem', { lineHeight: '1.4', fontWeight: '600' }],
        'body-lg': ['1rem', { lineHeight: '1.55', fontWeight: '400' }],
        'body': ['0.9375rem', { lineHeight: '1.55', fontWeight: '400' }],
        'body-sm': ['0.875rem', { lineHeight: '1.5', fontWeight: '400' }],
        'eyebrow': ['0.8125rem', { lineHeight: '1.3', letterSpacing: '0.06em', fontWeight: '700' }],
        'caption': ['0.8125rem', { lineHeight: '1.4', fontWeight: '400' }],
        'chip': ['0.75rem', { lineHeight: '1.3', fontWeight: '700' }],
      },
      spacing: {
        'xs': 'var(--spacing-xs)',
        'sm': 'var(--spacing-sm)',
        'md': 'var(--spacing-md)',
        'lg': 'var(--spacing-lg)',
        'xl': 'var(--spacing-xl)',
        '2xl': 'var(--spacing-2xl)',
      },
      borderRadius: {
        'sm': 'var(--radius-sm)',
        'md': 'var(--radius-md)',
        'lg': 'var(--radius-lg)',
        'card-sm': '1.25rem',  // radius-lg (insight cards)
        'xl': '1.5rem',        // radius-xl (cards)
        '2xl': '1.75rem',      // radius-2xl (champion hero)
        'pill': '999px',
      },
      boxShadow: {
        'sm': 'var(--shadow-sm)',
        'md': 'var(--shadow-md)',
        'lg': 'var(--shadow-lg)',
        'focus': 'var(--shadow-focus)',
      },
      letterSpacing: {
        'tight': 'var(--letter-spacing)',
      },
    },
  },
  plugins: [],
}
