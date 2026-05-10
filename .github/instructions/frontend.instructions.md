---
applyTo: "client/src/**/*.{vue,js,ts,css,scss,sass,less}"
description: "Frontend implementation rules for Vue, JavaScript, TypeScript, and styling files under client/src. Use when writing or editing app UI code."
---
# Frontend Rules

Load [ui-ux.spec.md](../specs/ui-ux.spec.md) only when changing user-facing behavior, layout, design tokens, or accessibility expectations.
Load [architecture.spec.md](../specs/architecture.spec.md) only when changing API usage, route behavior, or DTO assumptions.
Load [component.spec.md](../specs/component.spec.md) only when you need the full component template.
Use [client/AGENTS.md](../../client/AGENTS.md) for client build and run context.

## Component And State Rules

- Use Vue 3 Composition API with `<script setup>`.
- Components, views, and stateful UI slices should handle loading, error, content, and empty states when applicable.
- Declare props and emits explicitly.
- Keep naming consistent with nearby files: PascalCase components and pages, `use*` composables, `*Store.js` stores, and `*Api.js` services.
- Prefer existing component folders and patterns before creating new structure.

## Data And Store Rules

- Route API calls through service modules in `services/`.
- Use the shared API client helpers already present in the repo.
- Handle async state with `isLoading` and `error`; use `console.error` for caught failures.
- Keep Pinia stores aligned with nearby options-style patterns unless the local area already differs.

## Styling And Accessibility

- Use Tailwind for layout and sizing and CSS custom properties for themed values.
- Reuse design tokens from the UI/UX spec instead of introducing ad hoc colors or spacing.
- Prefer semantic HTML, label form controls correctly, and add `aria-label` for icon-only buttons.
- Add `data-testid` to interactive or assertion-critical elements.
- Keep keyboard access and contrast expectations intact.

## Feature-Specific Rules

- Register only the Chart.js pieces you need and avoid rendering charts when there is no data.
- Use responsive chart configuration and keep chart containers explicitly sized.
- Add or update unit tests in [frontend-unit-test.instructions.md](frontend-unit-test.instructions.md) when frontend logic changes.
