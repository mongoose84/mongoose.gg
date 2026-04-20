# Feature: Landing Page Content Cleanup & Feature Flags

## Problem Statement

The landing page currently advertises functionality that does not exist yet. Specific elements are:

- Hardcoded static counters presented as live data (user rating `0/5`, `493 spots left`)
- Feature cards and How It Works steps describing Goals, Team Dashboards, and team-based climbing — all of which are stub pages with "coming soon" copy
- A full Pricing section with tier breakdowns, money-back guarantees, and CTAs — despite no payment processor, no tier enforcement on API endpoints, and no billing logic

This creates a trust problem: a first-time visitor who signs up after reading about goal tracking or team dashboards will immediately encounter placeholder pages. It also sets up future launch moments poorly — pricing, teams, and goals all deserve their own reveal rather than being pre-announced with no delivery date.

## Proposed Solution

Two parallel changes:

1. **Delete now** — remove the two genuinely fake content elements (hardcoded static counters with no system behind them)
2. **Feature-flag** — introduce a single `VITE_ENABLE_UPCOMING_FEATURES` env var that hides future-facing sections. When set to `false` (default), the landing page shows only what is shipped today. When set to `true`, all upcoming sections are visible for internal review or controlled rollout.

Additionally, the **Post-Game Takeaways** feature card copy needs a rewrite to match what the existing Match Narrative endpoint actually delivers, regardless of the feature flag.

## User Stories

### Primary User Story
As a new visitor, I want the landing page to accurately describe what I can use today so that I sign up with realistic expectations and am not disappointed on first use.

### Additional User Stories
- As a developer, I want a single env var to toggle all upcoming-feature sections so that I can preview them locally without shipping them to production
- As a product owner, I want upcoming features gated behind a flag so that we can do a controlled reveal (pricing launch, goals launch, teams launch) with a single config change

---

## Requirements

### Functional Requirements

1. Remove the `0/5 User Rating` stat counter from the hero section entirely
2. Remove the `freeUsersLeft` hardcoded ref and the promo banner pill ("First 500 users get free Pro tier — 493 spots left") from the hero section entirely
3. Rewrite the H1 to drop the "Built for Teams" claim (teams are not shipped)
4. Rewrite the hero subtitle to reflect shipped features only
5. Rewrite the **Post-Game Takeaways** feature card description to match what the Match Narrative endpoint delivers (lane matchup breakdown, not AI coaching bullets)
6. Introduce a `VITE_ENABLE_UPCOMING_FEATURES` env var (default `false`)
7. Gate the following behind the flag:
   - **Goal Setting & Progress** feature card
   - **Team Dashboards** feature card
   - **How It Works — Step 4** ("Climb Together / Pro team")
   - **Entire Pricing section** (`#pricing`)
   - **Footer "Pricing" link**
   - **"30-day money-back guarantee"** footer copy
8. When the flag is `false`, the How It Works steps must renumber correctly (no gap at step 4)

### Non-Functional Requirements
- **No backend changes** — this is a pure frontend content change
- **Accessibility**: Section numbering in How It Works must remain visually and semantically sequential when steps are hidden
- **Env var**: Must follow Vite convention (`VITE_` prefix), read via `import.meta.env.VITE_ENABLE_UPCOMING_FEATURES`
- **No runtime API calls removed** — `getPublicStats()` fetch stays; active players and games analyzed counters remain

---

## Technical Approach

### Backend Changes
None.

### Frontend Changes

**File**: `client/src/views/LandingPage.vue`

Changes by section:

#### 1. Hero Section

- **Delete** the promo banner pill (`<div class="inline-flex items-center gap-sm ...">`) — the entire element
- **Delete** `freeUsersLeft` ref and all references to it in `<script setup>`
- **Delete** the `0/5` stat counter block (the third `<div class="text-center">` inside the stats row)
- **Rewrite** H1:
  ```
  Before: "The Solo Queue Improvement Tracker / Built for Teams"
  After:  "The Solo Queue Improvement Tracker / Built to Help You Climb"
  ```
- **Rewrite** hero subtitle paragraph to remove "climb better as a team" — keep solo-focused copy

#### 2. Features Array (in `<script setup>`)

- **Rewrite** Post-Game Takeaways card description:
  ```
  Before: "After every game, get 2-3 specific things to focus on next time. No walls of stats—just what matters."
  After:  "After every game, see a lane-by-lane breakdown of how each matchup played out—gold leads, early advantages, and where the game shifted."
  ```
- **Gate** Goal Setting & Progress card behind flag: add a `flag: 'upcoming'` property (or equivalent) and filter the `features` array in the template
- **Gate** Team Dashboards card behind flag (same mechanism)

#### 3. How It Works Steps Array

- **Gate** Step 4 ("Climb Together / Pro") behind flag
- Renumbering is automatic since the `v-for` uses `index` — no additional change needed

#### 4. Pricing Section

- Wrap entire `<section id="pricing">` in `v-if="enableUpcomingFeatures"`

#### 5. Footer

- Wrap the `<a href="#pricing">Pricing</a>` link in `v-if="enableUpcomingFeatures"`
- Wrap the money-back guarantee `<p>` in `v-if="enableUpcomingFeatures"`

#### 6. Script Setup

Add to the top of `<script setup>`:
```js
const enableUpcomingFeatures = import.meta.env.VITE_ENABLE_UPCOMING_FEATURES === 'true'
```

### Env Files

- **`client/.env`** (production default): `VITE_ENABLE_UPCOMING_FEATURES=false`
- **`client/.env.development`** (or `.env.local`): `VITE_ENABLE_UPCOMING_FEATURES=true` — so developers see the full page locally

---

## UI/UX Requirements

### Hero Section (after changes)

```
┌─────────────────────────────────────────────────────┐
│                  [Mongoose logo]                     │
│              Mongoose.gg  [Beta]                     │
│                                                      │
│   The Solo Queue Improvement Tracker                 │
│       Built to Help You Climb                        │
│                                                      │
│   Not just another builds app.                       │
│   Better champ select picks, post-game               │
│   takeaways that stick, and track your               │
│   progress over time.                                │
│                                                      │
│  [Start Improving Now →]  [See How It Works]         │
│                                                      │
│   1,234 Active Players    45,000 Games Analyzed      │
│     (live from API)         (live from API)          │
└─────────────────────────────────────────────────────┘
```

Note: the three-column stats row becomes two columns once the `0/5 User Rating` counter is removed. The remaining two counters should stay centered, e.g. keep `justify-center` and let the gap create natural breathing room.

### Features Grid (flag = false)

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ ⚔️ Champ     │  │ 📝 Post-Game │  │ 📊 Match     │
│ Select       │  │ Takeaways    │  │ History      │
│ Matchup      │  │              │  │              │
│ Highlights   │  │ (rewritten   │  │              │
│              │  │  copy)       │  │              │
└──────────────┘  └──────────────┘  └──────────────┘
┌──────────────┐
│ 🎮 Queue     │
│ Filtering    │
└──────────────┘
```

### Features Grid (flag = true, adds)

```
┌──────────────┐  ┌──────────────┐
│ 📈 Goal      │  │ 👥 Team      │
│ Setting &    │  │ Dashboards   │
│ Progress     │  │              │
└──────────────┘  └──────────────┘
```

### How It Works (flag = false — 3 steps)

```
  [1]                  [2]                  [3]
Link Your Riot    Get Champ Select    Review Post-Game
  Account           Tips               Takeaways
```

### How It Works (flag = true — 4 steps)

```
  [1]          [2]           [3]              [4]
Link Riot   Champ Select  Post-Game       Climb Together
 Account       Tips        Takeaways         (Pro)
```

---

## Testing Strategy

### Unit Tests

Update `client/test/unit/views/LandingPage.spec.js`:

- [ ] Assert promo banner pill is not rendered
- [ ] Assert `0/5 User Rating` counter is not rendered
- [ ] Assert H1 no longer contains "Built for Teams"
- [ ] Assert Post-Game Takeaways card uses updated description copy
- [ ] Assert Pricing section is hidden when `VITE_ENABLE_UPCOMING_FEATURES=false`
- [ ] Assert Pricing section is visible when `VITE_ENABLE_UPCOMING_FEATURES=true`
- [ ] Assert Goal Setting and Team Dashboards feature cards are hidden when flag is false
- [ ] Assert How It Works renders 3 steps when flag is false, 4 when true
- [ ] Assert Active Players and Games Analyzed counters still render

### Manual Testing Scenarios
1. Load landing page with flag `false` — verify no pricing section, no goals card, no team card, no step 4, no "0/5", no promo banner
2. Load landing page with flag `true` — verify all sections appear, renumbering is correct
3. Verify "Active Players" and "Games Analyzed" still load from the API
4. Verify all CTA buttons ("Start Improving Now") still route to `/auth?mode=signup`
5. Verify footer "Pricing" link disappears with flag off, reappears with flag on

---

## Validation Criteria
- [ ] No hardcoded fake data visible to end users (promo counter, star rating)
- [ ] Hero H1 no longer references "Teams"
- [ ] Post-Game Takeaways card copy matches what the Narrative endpoint delivers
- [ ] All flagged sections hidden in production build (`VITE_ENABLE_UPCOMING_FEATURES=false`)
- [ ] All flagged sections visible with flag `true` — no visual regressions
- [ ] Unit tests updated and passing
- [ ] How It Works step numbering is sequential in both flag states

## Dependencies
### Internal Dependencies
- `client/src/views/LandingPage.vue`
- `client/test/unit/views/LandingPage.spec.js`
- `client/.env` / `client/.env.development`

### External Dependencies
None.

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Env var not set in CI/CD causes flag to be `true` in production | High | Low | Default in `.env` is `false`; CI reads from `.env` unless overridden |
| Existing unit tests assert on copy that is being changed | Low | High | Update tests as part of this PR — spec above lists all affected assertions |

## References
- [Architecture spec — implemented endpoints](../specs/architecture.spec.md)
- [UI/UX spec — design tokens and component patterns](../specs/ui-ux.spec.md)
- [Content audit conversation — April 20, 2026](../../docs/planning/product_backlog.md)
