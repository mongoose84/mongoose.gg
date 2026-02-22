# Feature: Solo Analytics CTA on Overview Page

## Problem Statement
The Overview page currently has no direct path to the Solo page. Users who land on the Overview and want to see their detailed performance analytics must navigate through the sidebar. The Overview is designed as an orientation hub (5–15 seconds) that should route users to deeper analysis — but the only CTA-style link is to Champion Select. This creates a missed connection between the "Recent matches" section (which shows match activity and sync status) and the Solo Dashboard where those matches are analyzed.

## Proposed Solution
Add a `SoloAnalyticsCTA` card to the Overview page's "Recent matches" section, placed below the `AnalysisStatusCard` in the right column (`#recent-right` slot). The card follows the same interactive card pattern established by `ChampionSelectCTA` — icon, title, subtitle, and arrow indicator wrapped in a `router-link` to `/app/solo`. This creates a natural flow: the user sees their recent match activity (left column) and sync status (right column, top), then is prompted to dive into their analysis (right column, bottom).

## User Stories
### Primary User Story
As a solo player, I want a quick link from the Overview page to my Solo Dashboard so that I can jump straight into my performance analysis after checking my overview.

### Additional User Stories
- As a new user who just synced their matches, I want a clear call-to-action so that I know where to go to see my analysis results.
- As a returning user, I want to quickly navigate to my performance trends without hunting through the sidebar.

## Requirements

### Functional Requirements
1. A CTA card links to `/app/solo` from the Overview page's "Recent matches" section.
2. The card is placed below the `AnalysisStatusCard` in the `#recent-right` slot.
3. The card is only visible when `overviewData` is loaded (i.e., the user has a linked Riot account and data exists).
4. The card follows the same visual pattern as `ChampionSelectCTA`: icon + title + subtitle + arrow indicator.
5. Clicking the card navigates to `/app/solo`.

### Non-Functional Requirements
- **Performance**: No additional API calls — the CTA is static content with a router-link.
- **Accessibility**: Keyboard-focusable, visible focus ring, descriptive link text for screen readers.
- **Compatibility**: Responsive — works on desktop and stacks gracefully on mobile.

## Technical Approach

### Frontend Changes
**Framework**: Vue

**Components**:
- [ ] New component: `client/src/components/overview/SoloAnalyticsCTA.vue`
- [ ] Modified view: `client/src/views/OverviewPage.vue` (add CTA to `#recent-right` slot)

### Database Changes
None.

### API Contracts
None — this is a static navigation component.

## UI/UX Requirements

All views must follow the existing design system defined in [UI/UX Spec](../ui-ux.spec.md). Use design tokens — never hardcode colors, spacing, or shadows.

### `SoloAnalyticsCTA` Component

**Layout**: Placed inside the `#recent-right` slot of `OverviewLayout`, directly below `AnalysisStatusCard`. The slot content becomes a flex column with a gap between the two cards.

**Structure**:
```
┌─────────────────────────────────────────┐
│  Recent matches                         │  ← section title (existing)
│                                         │
│  ┌────────────────────┐ ┌─────────────┐ │
│  │                    │ │ Analysis    │ │  ← AnalysisStatusCard (existing)
│  │  Match Activity    │ │ Status Card │ │
│  │  Heatmap           │ └─────────────┘ │
│  │                    │ ┌─────────────┐ │
│  │                    │ │ 📊  Solo    │ │  ← SoloAnalyticsCTA (new)
│  │                    │ │  Analytics  │ │
│  │                    │ │  subtitle → │ │
│  └────────────────────┘ └─────────────┘ │
└─────────────────────────────────────────┘
```

**Card internal layout** (mirrors `ChampionSelectCTA`):
```
┌───────────────────────────────────────────────┐
│  ┌──────┐                                     │
│  │ icon │  Solo Analytics              →      │
│  │      │  Track your trends and improve      │
│  └──────┘                                     │
└───────────────────────────────────────────────┘
```

**Components**:
- Root element: `<router-link to="/app/solo">` — wraps the entire card, same as `ChampionSelectCTA`
- Icon wrapper: 72×72px circle with `bg: var(--color-primary-soft)`, containing a `ChartBarIcon` (Heroicons solid) at 36×36px in `var(--color-primary)`
- Title: `<h3>` — "Solo Analytics" — `font-size-lg`, `font-weight-semibold`, `color-text`
- Subtitle: `<p>` — "Track your trends and improve" — `font-size-sm`, `color-text-secondary`
- Arrow indicator: chevron-right SVG, same as `ChampionSelectCTA` — shifts right 2px on hover

**Behavior**:
- Hover: border transitions to `var(--color-primary)`, background to `var(--color-elevated)`, lift effect (`translateY(-2px)`, `shadow-md`), arrow shifts right
- Focus: `box-shadow: 0 0 0 3px var(--color-primary-soft)`, no outline
- Click: navigates to `/app/solo`
- No loading, error, or empty states — the card is static and always renders when the overview has data

**Accessibility**:
- `router-link` renders as `<a>`, inherently focusable and keyboard-activatable
- Focus ring via `box-shadow: 0 0 0 3px var(--color-primary-soft)`
- Arrow icon has no semantic meaning (decorative) — no aria-label needed on it
- Title and subtitle text provide sufficient link context for screen readers

**`OverviewPage.vue` changes**:

The `#recent-right` slot currently renders only `<AnalysisStatusCard />`. It needs to render both cards stacked vertically:

```vue
<template #recent-right>
  <div class="recent-right-stack">
    <AnalysisStatusCard />
    <SoloAnalyticsCTA />
  </div>
</template>
```

The wrapper `div.recent-right-stack` uses:
```css
display: flex;
flex-direction: column;
gap: var(--spacing-md);
```

### Mobile Responsive (≤480px)
- Card switches to `flex-direction: column`, `align-items: flex-start`
- Icon wrapper shrinks to 56×56px, icon to 28×28px
- Title drops to `font-size-md`
- Arrow indicator hidden

## Testing Strategy

### Unit Tests
**Frameworks**: Vitest

- [ ] `SoloAnalyticsCTA` renders with correct title and subtitle text
- [ ] `SoloAnalyticsCTA` contains a router-link pointing to `/app/solo`
- [ ] `SoloAnalyticsCTA` renders the icon, title, subtitle, and arrow elements
- [ ] `OverviewPage` renders `SoloAnalyticsCTA` when overview data is present

### Manual Testing Scenarios
1. Navigate to `/app/overview` with a linked account — verify the Solo Analytics CTA card appears below the Analysis Status Card in the "Recent matches" section.
2. Click the CTA — verify navigation to `/app/solo`.
3. Keyboard-navigate (Tab) to the CTA — verify visible focus ring. Press Enter — verify navigation.
4. Resize browser to mobile width — verify card stacks properly and arrow hides.
5. View overview with no linked account (empty state) — verify CTA is not shown.

## Validation Criteria
Feature is considered complete when:
- [ ] `SoloAnalyticsCTA.vue` component created following `ChampionSelectCTA` pattern
- [ ] Component is rendered in `OverviewPage.vue` `#recent-right` slot below `AnalysisStatusCard`
- [ ] Clicking the card navigates to `/app/solo`
- [ ] Visual style matches `ChampionSelectCTA` (hover effects, transitions, spacing)
- [ ] Responsive layout works on desktop and mobile
- [ ] Focus state is visible for keyboard navigation
- [ ] Unit tests pass
- [ ] No new lint or type errors introduced

## Dependencies
### Internal Dependencies
- [ ] `ChampionSelectCTA.vue` — pattern reference
- [ ] `OverviewLayout.vue` — slot structure (no changes needed)
- [ ] `OverviewPage.vue` — slot content update
- [ ] Heroicons (`@heroicons/vue`) — `ChartBarIcon`

### External Dependencies
None.

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Right column becomes too tall relative to left (heatmap) on small screens | Low | Low | Both columns stack on mobile (< 768px) via existing `section-row` flex behavior |

## Timeline and Milestones
- [ ] **Phase 1**: Create `SoloAnalyticsCTA.vue` component (~15 min)
- [ ] **Phase 2**: Integrate into `OverviewPage.vue` (~5 min)
- [ ] **Phase 3**: Add unit tests (~15 min)
- [ ] **Phase 4**: Manual QA and responsive check (~10 min)

## Open Questions
- [ ] Should the subtitle text be dynamic (e.g., "You have X new games to review") or static? **Recommendation**: Start static — dynamic adds API dependency and complexity for marginal value on a navigation CTA.
- [ ] Should the icon use `ChartBarIcon` or `ArrowTrendingUpIcon`? Both are available in Heroicons. `ChartBarIcon` signals analytics; `ArrowTrendingUpIcon` signals trends/improvement.

## References
- [ChampionSelectCTA.vue](../../client/src/components/overview/ChampionSelectCTA.vue) — pattern reference
- [UI/UX Spec](ui-ux.spec.md) — design system tokens, CTA patterns, overview page responsibilities
