Mongoose Pulse is the design language of Mongoose.gg, a League of Legends improvement coach. It reads like a calm health tracker (scores, readiness, plain-language insights) and dresses itself in the player's own champions. Dark only, purple for good, orange for needs work, champion art for identity.

## Principles

1. **Summary before detail.** Every page opens with one sentence a player can act on, then scores, then the evidence. Numbers support the sentence, never replace it.
2. **Purple means good, orange means needs work.** `primary` / `positive-text` for strengths, wins and gains; `warn` / `warn-text` for weaknesses, losses and drops. Never use red/green pairs. Every state also carries a word or arrow (Victory, Defeat, ▲ 5, ▼ 7, High, Low) so colour is never the only signal.
3. **The champion is the identity.** Splash art, centred art and icons carry the player's personality. Purple stays the brand; the art brings the variety.
4. **Calm, not loud.** Flat surfaces, generous radius, one highlight card per view, no gradients except the scrims that fade art into `bg`, and no glow except the soft `spotlight` hover on clickable champion cards.

## Voice and copy

- Talk to the player as "you", like a coach sitting next to them: "Your Ahri laning is elite. Your late-game discipline is not."
- Lead with the finding, then the evidence, then the fix: "Vision has dropped three sessions in a row." → "Worst on Syndra at 0.8 per minute." → "A control ward on first back fixes most of it."
- Use League vocabulary players use themselves: LP, CS, KDA, first back, Baron, roam, AP mid, champion pool. Never internal names (PUUID, match ID, sync job).
- Sentence case everywhere. Uppercase only for `eyebrow` labels.
- Numbers: whole-number scores out of 100, percentages without decimals (64%), per-minute stats with one decimal (0.8 per minute), LP signed with a real minus (+19 LP, −17 LP).
- No emoji. No exclamation marks. No filler praise.
- Match the tone to the moment: direct and calm for errors and losses ("We couldn't reach Riot's servers"), warm for real milestones ("Promoted to Emerald I"). Never cheer a loss or scold a player.
- Use one word per idea, from this glossary:

| Say | Not | Meaning |
|---|---|---|
| match | game, round | One League match, everywhere: the Matches page, "Today's matches", "22 matches", "Syncing 12 of 40 matches". League phrases like "late game" and "early game" keep their word. |
| Riot ID | summoner name, username, PUUID | `Name#TAG` |
| sync | import, fetch, refresh | Pulling new matches from Riot |
| score | rating, grade, index | A 0–100 Pulse score (Laning, Teamfighting, Discipline) |
| readiness | tilt meter, mood | The Queue readiness score |
| champion pool | roster, mains list | The champions a player plays |
| insight | tip, alert, notification | A finding with evidence and a fix |
| goal | target, objective | Something the player chose to improve. "Objectives" means dragons, Baron and towers. |

## Headlines, buttons and first screens

These rules come from the sites that lead this niche (OP.GG, U.GG, Mobalytics, Blitz, Porofessor). They share them; weaker sites (LeagueSpy, LoLalytics, DeepLoL) break them.

1. **The first screen does the job.** The main control on the first screen is the product's core action, not a sign-up form. Public pages lead with a Riot ID search that shows a real result without an account; in the app, the first screen is the player's own data. Never put a login or join form in front of the first result.
2. **Placeholders teach the exact input.** Show the format and pre-select the region: `Game name + #EUW`. Offer the shortcut players use when there is one ("Paste your lobby chat to scout all five").
3. **Headlines name the player's outcome in the player's nouns.** Use League words (LP, climb, match history, champ select, builds, counters), and "you/your" in every headline: "Climb faster with coaching from your own match history". Never define the product by what it is not ("Not just another builds app"), and never lead with a category label ("League of Legends Analytics").
4. **Headline short, subline concrete.** Headline at most 10 words. The line under it lists two to four concrete things the player gets: "Champion picks for your pool, a readiness score before you queue, and one fix after every match."
5. **Buttons are a verb plus the player's object, three words at most.** "Search", "Link Riot account", "See today's matches", "Get the app". Say "free" when it is free ("Start free"). Never "Start now", "Learn more", "Submit" or "Click here".
6. **Proof sits next to the main button, as real numbers.** Players, matches analysed or downloads, rounded down with a unit and "+": "3.2M+ matches analysed". Only counts the backend really reports; when the count is small, show a real example result instead of a small number.
7. **Show the rules are respected.** One short line near the main button says the product is safe to use, only if it is true: "Built on Riot's official API". Never claim Riot approval or endorsement without it in writing.
8. **Show the patch.** Any meta, tier or champion stat claim carries the current patch next to it: "Patch 16.19". Stale-looking data loses trust faster than missing data.
9. **Real data on the first screen, not pictures of it.** Show a real result card (a Pulse score card or champion card from a public example account, marked "Example"), not an illustration or a screenshot mock.
10. **Make it personal.** Stats sites say "stats"; coaching sites say "your strengths and weaknesses". Every first-screen promise is about the player's own matches, never about the meta in general.

## Charts

- Use a chart only when the shape of the data is the point (a trend, a comparison). A list or table is better for looking things up.
- The chart title says the takeaway, not the metric: "Up from 61 to 74 in 30 days", with the metric as the subtitle.
- One or two series at most. More detail comes on demand (a segmented 7D / 30D / Split control, a tap for a single match), never all at once.
- Lines use `primary` 2.5–3px with a highlighted last point; grid lines `divider`; axis labels `ink-faint` at 12px; no fills, no 3D, no legends when a label next to the line will do.
- Every chart has a text alternative: an `aria-label` that states the range and the change ("Performance score, 30 days, from 61 to 74"), and the numbers are available as a table or list for screen readers.

## Colour

- Page ground is `bg`. Cards are `surface`. Pill tracks are `surface-raised`, active pills are `surface-selected`.
- Exactly one card per view may use `surface-highlight` with a 1px `border-highlight`: the thing to act on now (Queue readiness).
- Text: `ink` for titles and numbers, `ink-soft` for body, `ink-muted` for meta, `ink-faint` only for chart axes and dates (never on `surface-selected`).
- `primary` is for marks (ring fills, bars, meter segments, win borders, the selected champion's ring, trend lines), not body text. Text meaning "good" uses `positive-text`.
- Filled buttons use `primary-strong` with `on-primary` text.
- Ring colour rule: score ≥ 70 → `primary`, below 70 → `warn`.
- Win-rate colour: `winrate-terrible` < 40%, `winrate-bad` 40–47%, `winrate-average` 48–52%, `winrate-good` 53–59%, `winrate-great` 60%+.
- Rank badges keep their tier colours (`rank-iron` … `rank-challenger`); they are the only other hues allowed, and only on rank marks.

## Type

- Two families: **Clash Display** (`--font-display`) for headlines, titles, champion names and every big number; **Satoshi** (`--font-body`) for everything else. Both are free for commercial use from Fontshare, weights 600/700 and 400/500/700.
- Numbers are always Clash Display and tabular (`font-variant-numeric: tabular-nums`).
- One `headline` per page. Card titles use `title`. Scores use `score-lg` in rings and `score-xl` for the single hero number.
- Keep running text under 65 characters wide.

## Layout and spacing

- Desktop page gutter `space-14` (56px), phones 16px. Content max width 1328px.
- Cards pad `space-7` (28px); insight cards pad `space-6`. Grids gap `space-5` (20px). Page sections stack with `space-7`.
- Standard Overview order: nav → champion hero → three score rings → your champions → readiness + today's matches → insights.
- Grids: 3 equal columns for score rings and insights; champion cards are 3 equal columns plus a 300px "Also played" list; readiness is a fixed 420px column beside a flexible list.
- Below 900px everything stacks to one column; the hero art moves behind the text with a full-width scrim.

## Radius and depth

- `radius-xl` (24px) for cards, `radius-2xl` (28px) for the champion hero only, `radius-lg` (20px) for insight cards, `radius-md` (12px) for square champion icons, `radius-pill` for every control, chip and round portrait.
- No drop shadows. Depth comes from the `bg` → `surface` → `surface-selected` steps. The only shadow is `shadow-focus`.

## Champion art

- Source at runtime from Riot Data Dragon, through `client/src/utils/leagueAssets.js`: square icons (`/cdn/<version>/img/champion/<Name>.png`), splash (`/cdn/img/champion/splash/<Name>_0.jpg`), centred (`/cdn/img/champion/centered/<Name>_0.jpg`).
- Splash art: only in the champion hero, anchored right, faded into `bg` from the left so text sits on solid ground.
- Centred art: champion cards, faded into `bg` from the bottom.
- Icons: round 48px portraits in match rows (2px border: `primary` for a win, `warn` for a loss); 44–48px squares with `radius-md` in lists and insight cards; round 28–44px for the player avatar.
- Every image gets real alt text when it carries meaning ("Ahri splash art") and `alt=""` when a name sits next to it.
- The assets under Champions in this system are examples for previews only.

## Motion

- Score rings fill over 600ms with an ease-out curve on first view, and the number inside counts up with them (AnimatedNumber, same 600ms). Hover transitions are 150ms colour changes. Nothing bounces. The only loop is the Skeleton pulse while data loads.
- Motion must earn its place: it shows a change (a score filling, a new state), never decorates. Never animate things people do often (switching tabs or champions, opening a row), and never make anyone wait for an animation before they can act.
- Entrances: sections fade in from 8px below over 300ms, 60ms apart, the first time they scroll into view. Never re-animate on re-render.
- Respect `prefers-reduced-motion`: no ring or number animation, no entrances, no skeleton pulse, instant states.

## States and accessibility

- Show something at once: the page frame and Skeletons render immediately, and each card fills in as its data arrives. Never block the whole page on one request.
- Syncing never blocks the app. Show sync as determinate progress when the count is known ("Syncing 12 of 40 matches") with SyncProgress, and let people keep using what has already loaded. On a long first sync, use the wait to explain what each score means.
- Teach in context: a first-time tip sits next to the thing it explains (one sentence and "Got it"), shown once and findable again from the card's info button. No multi-screen tutorials; linking the Riot account is the only required step.
- Every data card has four states: Skeleton while loading, EmptyState when there is nothing yet, an inline error line with a retry button when a request fails, and the content.
- Every interactive element shows `shadow-focus` on keyboard focus.
- Touch targets are at least 44px tall (buttons 48px, nav pills 36px inside a 44px track).
- Use real `<button>`, `<a href>` and `aria-pressed` / `aria-current`. Icon-only buttons get `aria-label`.
- Text pairs are checked at 4.5:1 or better on every ground listed in their token notes.
- Scores and meters are real meters for assistive tech: `role="meter"` with `aria-valuemin`, `aria-valuemax`, `aria-valuenow` and an `aria-valuetext` that says it in words ("82 out of 100, strong").
- Respect the player's text size: build type and spacing in `rem` (1rem = 16px) so browser font-size and 200% zoom work without clipping or overlap.
- Respect "increase contrast": under `@media (prefers-contrast: more)` use the High contrast theme values (brighter muted text, stronger dividers and tracks).
- Mongoose is dark only by choice: champion art and scores read best on a dark stage, as media apps do. Don't add a light theme without redesigning the art treatment.

## Iconography

- **One set: Lucide** (lucide.dev, ISC licence), taken as SVG files from Iconify (`https://api.iconify.design/lucide/<name>.svg`). Never mix in another set; Heroicons and hand-drawn inline SVGs in the app get replaced by the Lucide equivalent over time.
- **Style:** 24 × 24 grid, 2px stroke, round caps and joins, no fill, `stroke="currentColor"` so the icon takes the text colour. Never recolour paths or change stroke width per icon.
- **Sizes:** 16px inside chips and dense rows, 20px in buttons, inputs and list rows, 24px in navigation and empty states. Nothing larger; big visuals are champion art, not icons.
- **Colour:** `ink-muted` by default, `ink` when active or selected, `positive-text` / `warn-text` only when the icon sits next to text of that meaning.
- **Always with a word.** Icons support labels; they never replace them. The only icon-only buttons are close (`x`), copy and overflow controls, and each gets an `aria-label`. Decorative icons get `aria-hidden="true"`.
- **Use the vocabulary below.** One meaning per icon across the whole app. Need a concept that is not listed? Pick the closest Lucide icon, add it here and to the Icons asset group before using it.
- **Not Lucide:** champion, item, rune, summoner-spell, role and rank emblems come from Riot (Data Dragon / Community Dragon) as images, never redrawn as icons.
- **Brand mark:** the mongoose logo in Logos (`mongoose.png` wordmark-shaped, `mongoose_square.png` square). Its body is off-white with a purple stripe; it sits on `bg` or `surface` only.
- No emoji. Arrows ▲ ▼ still mark deltas inside text.

| Group | Icon (Lucide name) and meaning |
|---|---|
| Navigation | `house` Overview / Today, `swords` Matches, `shield` Champions, `chart-line` Solo stats and trends, `users` Team, `target` Goals, `settings` Settings, `message-square` Feedback, `log-out` Log out |
| Match stats | `sword` Kills, `skull` Deaths, `handshake` Assists, `wheat` CS / farming, `coins` Gold, `zap` Damage, `eye` Vision and wards, `castle` Objectives and towers, `crown` Rank and LP, `trophy` Win, `flame` Win streak, `clock` Match length, time of day, `moon` Late-night matches |
| Insights | `trending-up` Strength, rising score, `trending-down` Falling score, `repeat` Pattern, `activity` Trend |
| Actions and status | `search` Search, `refresh-cw` Sync matches, `filter` Filter, `calendar` Date range, `copy` Copy, `link` Link Riot account, `external-link` Opens outside the app, `arrow-right` Go to, `chevron-right` Open detail, `chevron-down` Expand, dropdown, `check` Done, selected, `x` Close, remove, `info` More information, `triangle-alert` Error or warning, `lock` Pro feature, `user` Profile |

## Building with this system

- CSS variables come from this system's `tokens.css`; component classes (`mp-*`) come from `components/bundle.css`.
- In the Vue app, map these tokens onto the existing `--color-*` variables in `client/src/style.css` and Tailwind names in `client/tailwind.config.js` rather than adding a second set.

## Third-party components

Two libraries were reviewed. Use them as described here and nowhere else.

**Vue Bits** (vue-bits.dev, the Vue port of React Bits; MIT + Commons Clause). Components may be copied into the app and changed. They may not be resold or redistributed as components, so never publish them as a package, template or kit.

| Component | Status | Used as |
|---|---|---|
| `CountUp` | Adopted | AnimatedNumber |
| `SpotlightCard` | Adopted | `spotlight` hover on ChampionCard |
| `AnimatedContent`, `FadeContent` | Pattern only | Section entrances, rebuilt with CSS and `IntersectionObserver`; they need GSAP, which is not worth adding for a fade |
| `AnimatedList`, `Stepper`, `Counter`, `RotatingText` | Not yet | Need `motion-v`. Reconsider only if a feature needs several of them (for example a Riot-account onboarding stepper) |
| All Backgrounds, cursor effects, glitch/shiny/fuzzy text, `BorderGlow`, `MagicBento`, 3D and WebGL pieces | Rejected | Break "Calm, not loud" and cost performance |

**Canvas UI** (canvasui.dev; WebGL/WebGPU effects such as Blaze, Liquid, Glass, Shatter, Particle Reveal, VHS; has Vue builds; MIT + Commons Clause). Rejected: the effects are the opposite of "Calm, not loud", run on the GPU on every page they touch, and the full effect relies on Chrome's experimental HTML-in-canvas API (origin trial), so most players would see a partial version. Reconsider only for a single, rare celebration moment (for example a promotion to a new rank), with a reduced-motion fallback, and only with the user's agreement.

**21st.dev** (React + shadcn registry; each component has its own author and licence; free tier allows 2 copies a day). It is React-only, so nothing installs into this Vue app. Use it as a pattern library: browse Empty States, Skeletons and Spinner Loaders, Onboarding, Toasts, Search Bars and Command Palette, Stats & KPIs for structure and behaviour, then rebuild in Vue with Pulse tokens. Skeleton and EmptyState in this system came from that review.

### Rules for bringing a component in

1. **Check the system first.** If a Pulse component covers the need, use it. Take outside code only for behaviour the system lacks.
2. **No new runtime dependencies without a reason.** Prefer components that import only `vue`. GSAP, `motion-v`, three.js, OGL and icon packs each need the user's agreement first.
3. **Port, don't paste.** Rewrite to the app's conventions: `<script setup>` in plain JavaScript (not TypeScript), explicit `defineProps` / `defineEmits`, no `className` props, file in `client/src/components/base/` (or the feature folder) with a PascalCase name.
4. **Re-skin with tokens.** Replace every hard-coded colour, radius, shadow and duration with Pulse tokens (`var(--…)` or Tailwind names mapped to them). Default white glows become `spotlight`; default durations become 150ms (hover), 300ms (entrance) or 600ms (rings and numbers).
5. **Add what the library leaves out.** `prefers-reduced-motion` handling, keyboard focus (`shadow-focus`), real buttons and links, `aria-label` holding final values, `data-testid` on interactive parts.
6. **Credit and licence.** Put a comment at the top of the file: source, original author, licence (for Vue Bits: "Adapted from Vue Bits <name> by David Haz, MIT + Commons Clause"). For 21st.dev components, check the individual licence before copying any code; if it is unclear, rebuild from the pattern without copying.
7. **Test it.** Add a Vitest unit test for its logic (final value rendered, reduced-motion path, emitted events).
8. **Record it.** Add the component to this system (README and preview) and to the table above, so the next build finds it.

