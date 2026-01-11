# Pulse.gg UI Design Guidelines

This document defines the visual design system for the Pulse.gg client_v2 application. Include this file when developing any new UI components to ensure consistency.

## Design Philosophy

**Theme:** Vercel Developer aesthetic adapted for gaming — dark, technical, cutting-edge with a premium feel.

**Key Principles:**
- Dark-first design with gaming-themed background image and dark overlay
- Purple accent color for brand identity
- Minimal, clean interfaces with generous whitespace
- Subtle transparency and blur effects for depth (backdrop-filter)
- Tight typography with the Inter font family

**Background Treatment:**
- Background image: `/gaming-bg.jpg` (fixed, cover)
- Dark overlay: `linear-gradient(135deg, rgba(0, 0, 0, 0.85), rgba(0, 0, 0, 0.80))`
- Applied via `body::before` pseudo-element for consistent layering

---

## Color Palette

### Core Colors

| Token | Value | Usage |
|-------|-------|-------|
| `--color-primary` | `#6d28d9` | Primary actions, links, focus states, accents |
| `--color-primary-soft` | `rgba(109, 40, 217, 0.1)` | Hover backgrounds, subtle highlights |
| `--color-bg` | `#000000` | Page background |
| `--color-surface` | `rgba(255, 255, 255, 0.03)` | Cards, panels, elevated containers |
| `--color-elevated` | `rgba(255, 255, 255, 0.05)` | Nested elevated elements |
| `--color-text` | `#ffffff` | Primary text |
| `--color-text-secondary` | `#888888` | Secondary/muted text |
| `--color-border` | `rgba(109, 40, 217, 0.15)` | Borders, dividers |

### Semantic Colors

| Purpose | Color | Usage |
|---------|-------|-------|
| Success | `#22c55e` | Wins, positive indicators, confirmations |
| Error | `#ef4444` | Losses, errors, destructive actions |
| Warning | `#f59e0b` | Cautions, pending states |
| Info | `#3b82f6` | Informational messages |

---

## Typography

### Font Family

```css
font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
```

Import: `@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap');`

### Font Sizes

| Token | Size | Line Height | Usage |
|-------|------|-------------|-------|
| `--font-size-xs` | `0.75rem` (12px) | 1.5 | Labels, badges, captions |
| `--font-size-sm` | `0.875rem` (14px) | 1.5 | Secondary text, form labels |
| `--font-size-md` | `1rem` (16px) | 1.6 | Body text, inputs |
| `--font-size-lg` | `1.125rem` (18px) | 1.6 | Subheadings |
| `--font-size-xl` | `1.5rem` (24px) | 1.4 | Section titles |
| `--font-size-2xl` | `2.5rem` (40px) | 1.2 | Page titles, hero text |

### Font Weights

| Token | Value | Usage |
|-------|-------|-------|
| `--font-weight-normal` | `400` | Body text |
| `--font-weight-medium` | `500` | Default weight, labels |
| `--font-weight-semibold` | `600` | Buttons, emphasis |
| `--font-weight-bold` | `700` | Headings, strong emphasis |

### Letter Spacing

```css
--letter-spacing: -0.015em;  /* Tight tracking for modern look */
```

---

## Spacing Scale

| Token | Size | Usage |
|-------|------|-------|
| `--spacing-xs` | `0.5rem` (8px) | Tight gaps, inline spacing |
| `--spacing-sm` | `0.75rem` (12px) | Form element gaps |
| `--spacing-md` | `1rem` (16px) | Standard padding, gaps |
| `--spacing-lg` | `1.5rem` (24px) | Section spacing |
| `--spacing-xl` | `2rem` (32px) | Large section padding |
| `--spacing-2xl` | `3rem` (48px) | Major section breaks |

---

## Border Radius

| Token | Size | Usage |
|-------|------|-------|
| `--radius-sm` | `0.375rem` (6px) | Badges, small elements |
| `--radius-md` | `0.5rem` (8px) | Buttons, inputs, cards |
| `--radius-lg` | `0.75rem` (12px) | Modals, large cards |

---

## Shadows

| Token | Value | Usage |
|-------|-------|-------|
| `--shadow-sm` | `0 2px 8px rgba(0, 0, 0, 0.5)` | Subtle elevation |
| `--shadow-md` | `0 8px 30px rgba(109, 40, 217, 0.15)` | Cards, buttons on hover |
| `--shadow-lg` | `0 20px 60px rgba(109, 40, 217, 0.25)` | Modals, dropdowns |

---

## Component Patterns

### Cards

```css
.card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-xl);
  backdrop-filter: blur(10px);
}
```

### Buttons

**Primary Button:**
```css
.btn-primary {
  background: var(--color-primary);
  color: white;
  padding: var(--spacing-md);
  border: none;
  border-radius: var(--radius-md);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-md);
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: var(--shadow-sm);
}
.btn-primary:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}
.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}
```

**Ghost/Secondary Button:**
```css
.btn-ghost {
  background: transparent;
  color: var(--color-primary);
  border: 1px solid var(--color-border);
  /* ...rest similar to primary */
}
.btn-ghost:hover {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
}
```

### Form Inputs

```css
.form-input {
  padding: var(--spacing-md);
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  font-size: var(--font-size-md);
  color: var(--color-text);
  transition: all 0.2s;
}
.form-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}
.form-input::placeholder {
  color: var(--color-text-secondary);
}
```

### Form Labels

```css
.form-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-xs);
}
```

### Error States

```css
.form-input-error {
  border-color: #ef4444;
}
.form-input-error:focus {
  border-color: #ef4444;
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.2);
}
.form-error-text {
  font-size: var(--font-size-xs);
  color: #ef4444;
}
.alert-error {
  padding: var(--spacing-md);
  background: rgba(239, 68, 68, 0.1);
  border: 1px solid rgba(239, 68, 68, 0.3);
  border-radius: var(--radius-md);
  color: #ef4444;
}
```

### Dropdowns

```css
.dropdown {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-lg);
  overflow: hidden;
}
.dropdown-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md);
  font-size: var(--font-size-sm);
  color: var(--color-text);
  cursor: pointer;
  transition: background 0.2s;
}
.dropdown-item:hover {
  background: var(--color-elevated);
}
.dropdown-divider {
  height: 1px;
  background: var(--color-border);
  margin: var(--spacing-xs) 0;
}
```

---

## Animations & Transitions

### Standard Transition

```css
transition: all 0.2s ease;
```

### Hover Lift Effect

```css
:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}
```

### Dropdown Animation

```css
.dropdown-enter-active,
.dropdown-leave-active {
  transition: all 0.2s ease;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
```

### Loading Spinner

```css
.spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: white;
  animation: spin 0.8s linear infinite;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}
```

---

## Layout Guidelines

### Page Structure

- **Header height:** `64px` fixed
- **Max content width:** `1400px` centered
- **Page padding:** `var(--spacing-xl)` (32px)
- **Card max-width for forms:** `440px`

### Z-Index Scale

| Layer | Z-Index | Usage |
|-------|---------|-------|
| Background | 0 | Background image/overlay |
| Content | 1 | Main app content |
| Header | 100 | Fixed navigation |
| Dropdowns | 200 | Menus, popovers |
| Modals | 300 | Modal dialogs |
| Toasts | 400 | Notifications |

---

## Tailwind Integration

The design tokens are available as Tailwind utilities via `tailwind.config.js`:

```js
// Use Tailwind classes that reference CSS variables
<div class="bg-background-surface text-text border-border">
<button class="bg-primary text-white rounded-md shadow-md">
```

Prefer CSS variables for consistency. Use Tailwind for layout utilities (flex, grid, spacing).

---

## Accessibility

- **Focus states:** Always visible with `box-shadow: 0 0 0 3px var(--color-primary-soft)`
- **Color contrast:** Ensure 4.5:1 ratio for text on backgrounds
- **Disabled states:** Use `opacity: 0.6` and `cursor: not-allowed`
- **Interactive elements:** Minimum touch target of 44x44px on mobile

---

## Icons

Use [Heroicons](https://heroicons.com/) (v2) via `@heroicons/vue`:

```vue
<script setup>
import { UserIcon, CogIcon, ArrowRightIcon } from '@heroicons/vue/24/solid';
// or /24/outline for outlined variants
</script>
```

Standard icon sizes:
- Small: `16x16px`
- Medium: `20x20px`
- Large: `24x24px`

---

## File Reference

- **CSS Variables:** `client_v2/src/style.css`
- **Tailwind Config:** `client_v2/tailwind.config.js`
- **Example Components:**
  - Cards & Forms: `client_v2/src/views/AuthPage.vue`
  - Header & Dropdown: `client_v2/src/components/AppHeader.vue`
  - Landing Page: `client_v2/src/views/LandingPage.vue`
