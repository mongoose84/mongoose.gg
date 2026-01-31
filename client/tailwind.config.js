/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Vercel Developer Theme - Technical cutting-edge
        primary: {
          DEFAULT: 'var(--color-primary)',
          soft: 'var(--color-primary-soft)',
          dark: 'var(--color-primary-dark)',
          light: 'var(--color-primary-light)',
          accent: 'var(--color-primary-accent)',
        },
        background: {
          DEFAULT: 'var(--color-bg)',
          surface: 'var(--color-surface)',
          elevated: 'var(--color-elevated)',
        },
        text: {
          DEFAULT: 'var(--color-text)',
          secondary: 'var(--color-text-secondary)',
        },
        border: {
          DEFAULT: 'var(--color-border)',
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
        sans: ['Inter', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
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
      },
      boxShadow: {
        'sm': 'var(--shadow-sm)',
        'md': 'var(--shadow-md)',
        'lg': 'var(--shadow-lg)',
      },
      letterSpacing: {
        'tight': 'var(--letter-spacing)',
      },
    },
  },
  plugins: [],
}
