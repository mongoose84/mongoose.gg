# Feature: Overview ChampionSelectCTA Most-Played Champion Mural

## Problem Statement
The `ChampionSelectCTA` on the Overview page currently looks visually generic compared to neighboring cards with richer gameplay context. Users cannot immediately tell that this CTA is personalized to their profile, which reduces scan-time relevance and click motivation during short between-game sessions.

## Proposed Solution
Add an optional background mural/fill to `ChampionSelectCTA` using the user’s most-played champion (within a defined recent range), while preserving readability and current CTA behavior. The card remains fully clickable and falls back to the current neutral style when champion data or artwork is unavailable.

## User Stories
### Primary User Story
As a solo player checking my Overview, I want the Champion Select CTA to reflect my most-played champion so that the card feels personalized and immediately relevant.

### Additional User Stories
- As a player with little/no match history, I want the CTA to remain clear and usable without a mural so that the page still works reliably.
- As a user in a high-stress pre-game moment, I want CTA text to stay readable over the mural so I can parse and click quickly.

## Requirements

### Functional Requirements
1. The Overview page computes/selects one champion as “most played” for mural use.
2. `ChampionSelectCTA` accepts mural inputs (image URL + champion name) and renders a background mural when present.
3. A gradient/overlay is always applied above mural imagery to preserve foreground text/icon contrast.
4. On image load failure or missing data, CTA renders current default background with no layout shift.
5. CTA route and interaction remain unchanged (`/app/champion-select`).

### Non-Functional Requirements
- **Performance**: No meaningful delay to Overview rendering; mural image must not block first contentful render of CTA text.
- **Security**: Only trusted CDN URLs are used for champion artwork generation.
- **Scalability**: Works for all champions and future patch updates without per-champion custom logic.
- **Accessibility**: Maintain WCAG AA contrast for text and focus states regardless of background image.
- **Compatibility**: Works in supported desktop browsers (Chromium + Firefox baseline used in project E2E).

## Technical Approach

### Backend Changes
**Language**: C#
**Components**:
- [ ] Data models: `server/Application/DTOs/Overview/OverviewDto.cs` (add `mostPlayedChampion` contract)
- [ ] Business logic: `server/Infrastructure/Database/Repositories/OverviewStatsRepository.cs` (query/derive most-played champion)
- [ ] API endpoints: `server/Application/Endpoints/Overview/OverviewEndpoint.cs` (map and return field)
- [ ] Database migrations: none required

### Frontend Changes
**Framework**: Vue

**Components**:
- [ ] UI components: `client/src/components/overview/ChampionSelectCTA.vue`
- [ ] State management: none (consume overview payload only)
- [ ] API integration: `client/src/views/OverviewPage.vue` (read `overviewData.mostPlayedChampion`)
- [ ] Styles: CTA overlay + background image treatment in component scoped CSS

### Database Changes
**Database**: MySQL

**Schema Changes**:
- [ ] New tables: none
- [ ] Modified tables: none
- [ ] New indexes: none
- [ ] Data migrations needed: none

### API Contracts
Extend overview endpoint:
```
GET /api/v2/overview/{userId}
```
**Response addition**:
```json
{
  "mostPlayedChampion": {
    "championName": "Ahri",
    "gamesPlayed": 28,
    "source": "current_season"
  }
}
```

## UI/UX Requirements

All views must follow the existing design system defined in [UI/UX Spec](../../.github/specs/ui-ux.spec.md). Use design tokens — never hardcode colors, spacing, or shadows.

### Overview `ChampionSelectCTA`

**Layout**: Existing card in Overview “Today at a glance” right column. No structure changes to layout sections.

**Structure**:
```
+------------------------------------------------------+
| [low-opacity champion mural background]              |
| [dark gradient overlay for readability]              |
|                                                      |
|  (icon)  Champion Select Helper                 (>)  |
|          Get personal matchup tips before lock-in    |
+------------------------------------------------------+
```

**Components**:
- CTA container: existing router-link card pattern
- Background layer: absolute-position mural image layer (non-interactive)
- Overlay layer: gradient using theme tokens to protect text contrast
- Foreground layer: existing icon/title/subtitle/arrow unchanged

**Behavior**:
- Loading: show CTA foreground immediately; mural can load progressively.
- Failure fallback: if mural fails to load, keep current neutral background only.
- No changes to hover/focus interaction except ensuring overlays do not obscure focus ring.
- Error mapping:
  - Missing champion data → neutral CTA background
  - Mural fetch/image failure → neutral CTA background

**Accessibility**:
- Preserve existing `:focus-visible` ring and keyboard navigation.
- Ensure overlay strength keeps title/subtitle contrast at WCAG AA.
- Decorative background image should not introduce extra screen reader noise.

## Testing Strategy

### Unit Tests
**Frameworks**: Vitest (frontend), xUnit (backend)

- [ ] CTA renders mural layer when `muralUrl` prop is present
- [ ] CTA falls back to neutral style when `muralUrl` is missing or errored
- [ ] CTA text and link remain visible/functional in both states
- [ ] Overview DTO mapping includes `mostPlayedChampion`

### Integration Tests
- [ ] Overview page loads without mural data and still renders CTA
- [ ] Overview page with backend-provided champion data applies mural URL
- [ ] Overview endpoint returns new field with authenticated access

### Manual Testing Scenarios
1. User with rich history sees personalized mural in Overview CTA.
2. New user/no history sees existing neutral CTA with unchanged layout.
3. Simulated broken image URL confirms fallback and text readability.

## Validation Criteria
Feature is considered complete when:
- [ ] CTA supports optional most-played mural without breaking current behavior
- [ ] Fallback state is robust and visually consistent
- [ ] Contrast and keyboard focus remain compliant
- [ ] Unit tests for CTA states pass
- [ ] API docs/spec updated with new overview field

## Dependencies
### Internal Dependencies
- [ ] Overview endpoint response contract (`mostPlayedChampion`)
- [ ] Overview CTA component (`ChampionSelectCTA.vue`)

### External Dependencies
- [ ] Riot Data Dragon splash artwork URL availability

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Splash URL mapping fails for edge champion names | Medium | Medium | Reuse existing champion-name normalization utility and fallback on error |
| Text contrast degrades on bright murals | High | Medium | Enforce fixed dark overlay and verify visually on bright champions |
| Overview query adds backend cost | Medium | Medium | Keep query bounded (e.g., existing recent window) and index-friendly |

## Timeline and Milestones
- [ ] **Phase 1**: Backend contract + repository implementation
- [ ] **Phase 2**: Frontend CTA mural integration
- [ ] **Phase 3**: Tests and accessibility verification
- [ ] **Phase 4**: Documentation and rollout

## Open Questions
- [ ] Should “most played” use `current_season` only, or respect Overview queue/time context?
- [ ] Should mural always reflect top champion overall, or top champion in primary queue only?

## Handoff Checklist
Before implementation begins:
- [ ] Backend-driven approach approved
- [ ] Mural URL strategy confirmed (Data Dragon splash endpoint)
- [ ] Contrast overlay standards approved
- [ ] Test expectations approved

## References
- [Feature Template](../../.github/specs/feature-template.spec.md)
- [UI/UX Spec](../../.github/specs/ui-ux.spec.md)
- [Overview View](../../client/src/views/OverviewPage.vue)
- [CTA Component](../../client/src/components/overview/ChampionSelectCTA.vue)
- [Overview DTO](../../server/Application/DTOs/Overview/OverviewDto.cs)
- [Overview Endpoint](../../server/Application/Endpoints/Overview/OverviewEndpoint.cs)
