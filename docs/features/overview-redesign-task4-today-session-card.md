# Task 4: `TodaySessionCard.vue` component

> Source: [overview-redesign-frontend.md](overview-redesign-frontend.md) — extracted from Task 4

**Scope**: Build `TodaySessionCard.vue` in `client/src/components/overview/`. Self-contained card that receives session data via props and implements the fallback chain.

**File to create**:
- `client/src/components/overview/TodaySessionCard.vue`

**Props**:
- `sessionStats` — the `sessionStats` object from the API (nullable)
- `combinedStats` — the `combinedStats` object for season fallback (nullable)
- `loading` — boolean for skeleton state

**Behavior**:
- **Has games today** (`sessionStats.gamesToday > 0`): Label "TODAY'S SESSION", show today's W/L, WR, KDA, best champion with icon and splash mural background
- **No games today, has games this week** (`sessionStats.gamesThisWeek > 0`): Label "THIS WEEK", show weekly stats — no champion, no mural
- **No games at all**: Label "THIS SEASON", use `combinedStats` (totalGames, winRate, avgKda) — plainest state, no W/L strip
- **Loading**: Skeleton placeholder matching card dimensions

**Visual design**:

The card uses a **visual degradation** strategy — most striking when you've played today (mural + accent + short strip), moderately interesting for the week (accent + longer strip), and calm for the season (just the hero number). This communicates recency and encourages play.

### Hero win rate number
Win rate is the dominant visual element. All other stats are supporting detail.
- WR number: `font-size-2xl` (40px), `font-weight-bold`, color-coded via `useWinRateColor()`
- Supporting stats (W/L count, KDA): `font-size-sm`, `text-text-secondary`, inline row beside the hero number

### Champion splash mural background (TODAY'S SESSION only)
Reuse the `ChampionSelectCTA` mural technique when a best champion exists:
- Champion splash art at ~50% opacity, `object-fit: cover`
- Gradient overlay from left using `color-mix(in srgb, var(--color-surface) 98%–78%, transparent)` — same layering as `ChampionSelectCTA` (`.cta-mural-layer` + `.cta-overlay-layer` + `.cta-foreground` z-index pattern)
- Best champion name + 24px icon displayed top-right of card
- Only shown in the "TODAY'S SESSION" state. "THIS WEEK" and "THIS SEASON" use the plain surface card.

### Performance-tinted left border accent
Replace default `var(--color-border)` on the left edge with a 3px sentiment stripe:

| Session WR | Border color | Token |
|---|---|---|
| ≥ 55% | Green | `var(--color-success-border)` |
| 45–55% | Default purple | `var(--color-border)` |
| < 45% | Red | `var(--color-error-border)` |
| No data | Default purple | `var(--color-border)` |

Remaining three borders stay `1px solid var(--color-border)`.

### W/L strip
Full-width below stats row, using the `wl-indicator` pattern from `RankSnapshot`:
- Indicators at `12px` (slightly larger than RankSnapshot's `10px` — fewer competing elements on this card)
- "TODAY'S SESSION": short/concentrated strip — naturally communicates small sample
- "THIS WEEK": denser, more visual texture
- "THIS SEASON": no strip (game count is high enough that individual dots aren't meaningful)

### Base card styling
- `background: var(--color-surface)`, `border-radius: var(--radius-lg)`, `backdrop-filter: blur(10px)`
- Section label: `text-xs uppercase tracking-wide text-text-secondary`
- Hover: `transform: translateY(-2px)` + `box-shadow: var(--shadow-md)` + `transition: all 0.2s ease`

### Layouts per state

**TODAY'S SESSION** (most visually striking):
```
┌─ [3px success/error/neutral left border] ─────────────────────┐
│ [champion splash mural, 50% opacity, gradient fade from left] │
│                                                                │
│  TODAY'S SESSION                    Best: [icon] Jinx          │
│                                                                │
│  67%            5W 2L  ·  2.8 KDA                              │
│  (40px, green)  (14px, secondary)                              │
│                                                                │
│  ●●●●●○○                                                      │
│  (W/L strip)                                                   │
└────────────────────────────────────────────────────────────────┘
```

**THIS WEEK** (no mural, no champion):
```
┌─ [3px neutral border] ────────────────────────────────────────┐
│                                                                │
│  THIS WEEK                                                     │
│                                                                │
│  54%            12W 10L  ·  3.1 KDA                            │
│  (40px, yellow) (14px, secondary)                              │
│                                                                │
│  ●●●●●●●●●●●●○○○○○○○○○○                                      │
│  (W/L strip — denser, more visual texture)                     │
└────────────────────────────────────────────────────────────────┘
```

**THIS SEASON** (plainest):
```
┌─ [default border] ────────────────────────────────────────────┐
│                                                                │
│  THIS SEASON                                                   │
│                                                                │
│  52%            148 games  ·  2.6 KDA                          │
│  (40px, yellow) (14px, secondary)                              │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

**Accessibility**:
- `aria-label="Today's session summary"` on card section
- `aria-label="Win"` / `aria-label="Loss"` on each W/L dot
- Win rate: numeric label always present (not color-only)
- Mural image: `alt=""` and `aria-hidden="true"` (decorative)

**Tests** (`test/unit/TodaySessionCard.spec.js`):
- [ ] Renders today's stats when `gamesToday > 0`
- [ ] Shows champion splash mural when best champion exists
- [ ] Left border uses success color when WR ≥ 55%
- [ ] Left border uses error color when WR < 45%
- [ ] Falls back to "THIS WEEK" when `gamesToday === 0` and `gamesThisWeek > 0`
- [ ] Falls back to "THIS SEASON" using `combinedStats` when no session data
- [ ] "THIS SEASON" state does not render W/L strip
- [ ] Shows loading skeleton when `loading` is true
