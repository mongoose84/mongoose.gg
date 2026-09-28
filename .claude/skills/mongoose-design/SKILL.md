---
name: mongoose-design
description: 'Build UI in the Mongoose.gg design system (dark, purple-for-good / orange-for-needs-work, Clash Display + Satoshi, champion art, score rings, plain-language insights). Use when the user runs /mongoose-design with a page, component, feature or mockup to build.'
argument-hint: 'What to build, e.g. "the Matches page", "a goal progress card", "mockup of the champion detail page"'
disable-model-invocation: true
---

# Mongoose Design

Build whatever the user asked for (`$ARGUMENTS`) in the **Mongoose.gg** design system. Every colour, font, size, radius and component decision comes from the system. Never introduce values that are not in it.

## Source of truth

- Live system (Design System artifact): https://claude.ai/artifact/CV2Jr6dfMG8A8wv2JoMW2n
  - Read `project/README.md` first, then the `project/components/<Name>/README.md` files of the components you will use, with the Artifact tool (`action: "read"`, `path`). If the live system is newer than the snapshot below, follow the live one and refresh the snapshot files.
- Local snapshot, used when the artifact is unavailable and for all code work:
  - `reference/design-system.md`: principles, voice, colour, type, layout, champion-art and accessibility rules.
  - `reference/tokens.json`: every token with its value and usage note.
  - `reference/components.md`: guidelines for ChampionHero, ScoreRing, AnimatedNumber, ChampionCard, ReadinessMeter, MatchRow, StatTile, LaneRow, SplitBar, InsightCard, Skeleton, EmptyState, SyncProgress, Button, PillNav, SegmentedControl, Chip, Icon, EmailTemplate, TextField, MessageBox.
  - `reference/components.css`: reference CSS for those components (`mp-*` classes).
  - `reference/icons/`: the approved Lucide SVGs (ISC, `LICENSE` included); their meanings are in the Iconography table of `reference/design-system.md`.
  - `reference/fonts/`: Clash Display 600/700 and Satoshi 400/500/700 (`.woff2`, Fontshare free licence).
- Visual reference: the "B+ · Scores with champions" and "Font 1 · Clash Display + Satoshi" artboards on https://claude.ai/artifact/A8YZFeBwht3HtjYUHPxU3j.

Read `reference/design-system.md` and `reference/components.md` before building anything.

## Step 1: Decide the output

- **App code (default).** The user names a page, view, component or feature of Mongoose.gg → build it in the Vue app under `client/src/`.
- **Mockup.** The user says mockup, design, concept, prototype or "show me" → build it on a Design canvas artifact. Create a new canvas unless they name an existing one. Install this system on it (the Design type's design-system install step, address https://claude.ai/artifact/CV2Jr6dfMG8A8wv2JoMW2n). Use the system's fonts and tokens, and upload any champion art as assets from Data Dragon.

If it is unclear which one they want, build app code.

## Step 2 (app code only): Make sure the foundation is in the app

Check `client/src/style.css` for `--font-display` and `--color-surface-selected`. If both are there, skip to Step 3. If not, set up the foundation first and tell the user you did:

1. Copy `reference/fonts/*.woff2` to `client/public/fonts/`. Replace the Inter `@import` in `client/src/style.css` with `@font-face` rules for Clash Display (600, 700) and Satoshi (400, 500, 700), `font-display: swap`.
2. In `:root` of `client/src/style.css`, keep the existing variable names so current components keep working, and set their values from the system:

   | App variable | Design token | Value |
   |---|---|---|
   | `--color-bg` | `bg` | `#0a0810` |
   | `--color-surface` | `surface` | `#120f19` |
   | `--color-elevated` | `surface-raised` | `#15111d` |
   | `--color-text` | `ink` | `#f1eef7` |
   | `--color-text-secondary` | `ink-muted` | `#a39cb3` |
   | `--color-border` | `divider` | `#1f1929` |
   | `--color-primary` | `primary-strong` | `#6d28d9` |
   | `--color-primary-light` | `primary-strong-hover` | `#7c3aed` |
   | `--color-primary-accent` | `primary` | `#a855f7` |
   | `--color-primary-soft` | `primary-soft` | `rgba(168,85,247,0.2)` |
   | `--color-winrate-*` | `winrate-*` | see tokens.json (average becomes `#cfc9da`) |
   | `--color-heatmap-1/2/3` | `primary` at 30% / 60% / 100% | |

   Then add the design-system tokens that have no app equivalent as `--color-<token>`: `surface-selected`, `surface-selected-hover`, `surface-highlight`, `border-highlight`, `track`, `track-strong`, `ink-soft`, `ink-faint`, `positive-text`, `positive-text-strong`, `warn`, `warn-text`, `warn-soft`, `glass`, `focus-ring`. Add `--font-display` and `--font-body` and set `body { font-family: var(--font-body); }`. Keep the rank-tier colours as they are.
3. Semantic colour rule: game and performance meaning (win/loss, good/bad stat, deltas) uses purple (`primary`, `positive-text`) and orange (`warn`, `warn-text`), never green/red. The existing `--color-error` / `--color-success` stay only for system states: form validation, failed requests, destructive confirmations.
   Add the High contrast values from `reference/tokens.json` (the `contrast` theme) inside `@media (prefers-contrast: more) { :root { … } }`.
   Define the type scale and spacing in `rem` (1rem = 16px), not px, so browser text size and 200% zoom work.
4. In `client/tailwind.config.js`, expose the new variables under the existing colour groups, and add `fontFamily: { display: 'var(--font-display)', body: 'var(--font-body)' }`, radii `xl: 24px`, `2xl: 28px` and `pill: 999px` if missing.
5. In `.github/specs/ui-ux.spec.md` §2 (Visual Design System), update the "Migration status" line to say the foundation (fonts, tokens, Tailwind mapping) is in the app. Never add visual values to that spec; it only points here.
6. Run `npm run build` in `client/` and fix anything that breaks.

## Step 3: Build

- Plan the layout from the system's standard order: summary sentence → scores → champions → act-now card → lists → insights. Use only the parts the request needs.
- Map each piece of the request to a system component (`reference/components.md`). Build missing pieces from the same tokens, radii and type styles, and say which new patterns you added.
- **Vue rules** (from `client/src/CLAUDE.md`): `<script setup>`, explicit props/emits, loading / error / empty / content states, API calls through `services/`, `data-testid` on interactive and assertion-critical elements, Tailwind for layout and sizing, CSS variables for themed values. Put reusable pieces in `client/src/components/base/` or the feature folder that already fits. Reuse existing components before creating new ones.
- **States:** every data card gets Skeleton (loading), EmptyState (nothing yet, with the button that fixes it), an inline error with retry, and content.
- **Motion:** numbers in rings and the readiness score use AnimatedNumber; sections fade in once on first view (CSS + `IntersectionObserver`, no GSAP); ChampionCard gets the `spotlight` hover. Everything has a `prefers-reduced-motion` path.
- **Third-party components:** follow "Third-party components" in `reference/design-system.md`:
  - Vue Bits (https://vue-bits.dev, source https://github.com/DavidHDev/vue-bits) is the only library code may be copied from. Adopted: `CountUp` → `AnimatedNumber.vue`, `SpotlightCard` → spotlight hover. Fetch the source with `curl https://raw.githubusercontent.com/DavidHDev/vue-bits/main/src/content/<Category>/<Name>/<Name>.vue`.
  - Port it: plain-JS `<script setup>`, explicit props/emits, tokens instead of hard-coded values, reduced-motion and focus handling added, `data-testid`, a credit comment ("Adapted from Vue Bits <Name> by David Haz, MIT + Commons Clause"), and a unit test.
  - Never add GSAP, `motion-v`, three.js, OGL or icon packs without asking the user first. Never use Vue Bits backgrounds, cursor effects or glitch/shiny text.
  - 21st.dev is React-only: use it to study structure (empty states, onboarding, toasts, command palette, stats cards), then rebuild in Vue with design-system tokens. Copy code from it only after checking that component's own licence.
  - After adding any new component, add it to the design system (README + preview) and the adopted table, and refresh `reference/`.
- **Icons:** Lucide only, as SVG files, meanings from the Iconography table.
  - First time: copy `reference/icons/*.svg` and `LICENSE` to `client/src/assets/icons/`, and create `client/src/components/base/BaseIcon.vue`: props `name` (required) and `size` (16 / 20 / 24, default 20); load the files with `import.meta.glob('@/assets/icons/*.svg', { query: '?raw', import: 'default', eager: true })`; render the SVG inline with `width`/`height` set to `size`, `aria-hidden="true"`, `focusable="false"`, so `currentColor` follows the text colour. Add a unit test (renders the named icon, sets size, warns on an unknown name).
  - Missing icon: download it from `https://api.iconify.design/lucide/<name>.svg` into both `client/src/assets/icons/` and `reference/icons/`, and add it to the vocabulary in the design system.
  - When touching a component that uses `@heroicons/vue` or a hand-drawn inline `<svg>`, switch it to `BaseIcon` with the vocabulary's icon. Don't rewrite untouched files just for icons.
  - Icons never replace words; icon-only buttons (close, copy, overflow) need `aria-label`.
- **Champion art:** use `getChampionIconUrl` and `getChampionSplashUrl` from `client/src/utils/leagueAssets.js`. If you need centred art, add a `getChampionCenteredUrl` helper there (`https://ddragon.leagueoflegends.com/cdn/img/champion/centered/<Name>_0.jpg`) with a unit test, following the splash helper.
- **Apple HIG lessons (already in the system):** use the glossary words (match — never "game" for a single match —, Riot ID, sync, score, readiness, insight, goal); chart titles state the takeaway and every chart has a text alternative; scores and meters use `role="meter"` with `aria-valuetext`; sync shows determinate progress (SyncProgress) and never blocks the page; first-time tips sit next to the thing they explain; no motion on frequent interactions.
- **Copy:** follow the voice rules in `reference/design-system.md`: "you", finding → evidence → fix, League vocabulary, sentence case, signed LP with a real minus, no emoji. Never show raw PUUIDs or internal IDs.
- **Accessibility:** real buttons and links, `aria-pressed` / `aria-current`, `shadow-focus` on focus, 44px touch targets, text only on the grounds its token note allows. Respect `prefers-reduced-motion`.
- **Responsive:** 56px gutters on desktop, 16px on phones, grids collapse to one column below 900px, and the hero art moves behind the text with a full-width scrim.

## Step 4: Validate

- App code: add or update unit tests for new logic and components (`client/test/unit/`, see its CLAUDE.md), run `npx vitest run <changed test files>`, then `npm run build`. Report failures with their output.
- Check the result against the system: no hard-coded colours outside tokens, Clash Display for numbers and titles, Satoshi for text, purple/orange semantics, one highlight card per view, one headline per page.
- Mockups: share the canvas link and say what you assumed.

## Step 5: Keep the system and the mockup in sync

Whenever this run changes the design system itself (a new or changed token, component, copy rule or adopted third-party component):

1. Update the live system (https://claude.ai/artifact/CV2Jr6dfMG8A8wv2JoMW2n) and refresh the `reference/` snapshot to match.
2. Update the mockup canvas (https://claude.ai/artifact/A8YZFeBwht3HtjYUHPxU3j) so the user can see the change: edit the matching artboard in the "Mongoose.gg — current system" row (`Current.dc.html` Overview, `CurrentStates.dc.html` loading/empty/error, `CurrentLanding.dc.html` landing first screen, `CurrentIcons.dc.html` icon vocabulary), or add a new `Current*` artboard to that row when nothing fits. Read `project/canvas.json` and the artboard first; leave the older exploration artboards as they are.

## Report

Tell the user what you built, which system components you used, any new patterns you introduced (and whether they should be added to the design system), and whether the Step 2 foundation was set up in this run.
