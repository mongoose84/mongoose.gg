# Client Source — Local Context

> Vue 3 application code under `client/src/`.
> For repo-wide invariants see [CLAUDE.md](../../CLAUDE.md); for build/run/test commands see [client/CLAUDE.md](../CLAUDE.md).

Load [ui-ux.spec.md](../../.github/specs/ui-ux.spec.md) only when changing user-facing behavior, layout, design tokens, or accessibility expectations.
Load [architecture.spec.md](../../.github/specs/architecture.spec.md) only when changing API usage, route behavior, or DTO assumptions.
Load [component.spec.md](../../.github/specs/component.spec.md) only when you need the full component template.

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
- All visual decisions follow the Mongoose.gg design system (`.claude/skills/mongoose-design/reference/`, summarized in ui-ux.spec.md §2). Use design-system tokens only — no ad hoc colors, spacing, radii or shadows. Old-theme styling in existing files is legacy, never a pattern to copy.
- Prefer semantic HTML, label form controls correctly, and add `aria-label` for icon-only buttons.
- Add `data-testid` to interactive or assertion-critical elements.
- Keep keyboard access and contrast expectations intact.

## Feature-Specific Rules

- Build charts from design-system components (`BaseColumnChart`, `BaseTrendTile`, `BaseFormStrip`, …) or inline SVG; there is no chart library. Don't render a chart without data, and keep chart containers explicitly sized.
- Add or update unit tests — see [client/test/unit/CLAUDE.md](../test/unit/CLAUDE.md) — when frontend logic changes.
