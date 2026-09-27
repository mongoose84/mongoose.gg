# Mongoose Pulse components

Guidelines for each component. Class names refer to components.css.

## ChampionHero

The page opener: the player's main champion splash with one headline about them.

- Provide: champion name, splash URL, the headline (one sentence, "Your {champion} …"), one supporting sentence, the player line (Riot ID · champion main · rank · LP), a primary action and up to two glass chips.
- Art is anchored right (`object-position` tuned per champion so the face stays visible), faded into `bg` by `mp-hero-scrim`. Text never sits on the art.
- When the player picks another champion card, swap the art, headline and chips together.
- Don't: put more than one hero on a page, or use it without a champion (fall back to a plain `surface` card with the headline).

## ScoreRing

A 0–100 score in a ring with its delta, inside a score card that lists what drove it.

- Provide: value (0–100), delta this week with ▲/▼, label (Laning, Teamfighting, Discipline), a one-line state, and 3 contributors each with a level word (High / Normal / Low).
- Fill: `primary` at 70+, `warn` below 70 (`mp-ring--warn`). Stroke 11px on a 140px ring (r = 58), round caps, starts at 12 o'clock; dasharray = value/100 × 364.4.
- Contributor levels are coloured by meaning: good → `positive-text`, neutral → `ink-soft`, needs work → `warn-text`.
- Mark it up as a meter: `role="meter"`, `aria-valuemin="0"`, `aria-valuemax="100"`, `aria-valuenow`, and `aria-valuetext` ("82 out of 100, strong").
- Each ring is its own card with its label beside it. Never nest rings inside each other or colour them red/green/blue; that is Apple's Activity rings, which Apple reserves for Move, Exercise and Stand.
- Three cards in a row, equal width. Don't show more than three rings on a page.

## AnimatedNumber

Counts a number up from 0 to its value once, when it first scrolls into view. Ported from Vue Bits `CountUp` (MIT + Commons Clause, no dependencies).

- Provide: the final value, optional prefix/suffix ("+", "%", " LP") and duration. Default duration 600ms, the same as the score-ring fill, with an ease-out curve.
- Use it for: the number inside a ScoreRing, the `score-xl` readiness number, stat chips on the hero, and real proof counters on public pages ("3.2M+ matches analysed").
- Don't use it for: table cells, match rows, LP in lists, or any number that updates while the player watches (use a plain number).
- The element always holds the final value in `aria-label`, so screen readers never hear the count. With `prefers-reduced-motion`, show the final value immediately.
- Animate once per page view, never again on re-render or tab switch.

## ChampionCard

A tall card for one champion in the player's pool, and a switch for the page's champion focus.

- Provide: name, centred art URL, win rate, matches, KDA, one strength tag ("Best laning"), and `aria-pressed` for the selected one.
- It is a `<button>`; the selected card gets a 2px `primary` border. Win-rate bar uses `primary`.
- Hover and keyboard focus add a soft `spotlight` that follows the pointer (`mp-spotlight`, ported from Vue Bits `SpotlightCard`): set `--mx` / `--my` on `pointermove`. This is the only glow in the system; static cards never get it.
- Show the top three champions as cards; everything else goes in a compact "Also played" list with 44px square icons.
- Don't: show more than three cards in a row, or place text over the art without the scrim.

## ReadinessMeter

The one highlight card: should the player keep queueing right now.

- Provide: a 0–100 value, a two-word verdict ("Good to go", "Take a break"), one sentence of advice, and 20 meter segments with round(value/5) switched on.
- Label both ends of the meter under it ("Take a break" on the left, "Good to go" on the right) so the scale explains itself, and mark it up as `role="meter"` with `aria-valuetext` ("71 out of 100, good to go").
- Uses `mp-card--highlight`; only one per view. Verdict under 40 switches segments to `warn`.
- Don't: add a chart inside it, or use it for anything but a present-tense recommendation.

## MatchRow

One match in a list: champion portrait, meta, KDA, result and LP change.

- Provide: champion icon, champion name, a meta line (queue · length · time ago), K / D / A, result (Victory / Defeat) and the signed LP change.
- Portrait border and result/LP text: win → `primary` / `positive-text`, loss → `warn` / `warn-text` (`mp-portrait--loss`, `mp-down`).
- The whole row is one `<a href>` to the match. Rows are separated by `divider` hairlines.

## InsightCard

A plain-language finding with its evidence and fix.

- Provide: a champion icon (the champion the finding is about; the player's main if none), a chip (Strength / Pattern / Trend), a one-sentence finding as the title, and one or two sentences of evidence and advice.
- Three per row, strongest finding first. Title uses the `insight` style, body `ink-muted`.
- Don't: write findings without a number behind them, or stack more than six.

## Skeleton

The loading state of a card: grey shapes in the exact layout of the content that is coming. Pattern from 21st.dev's Skeleton and Spinner Loaders categories, built on Pulse tokens.

- Provide: the same card, grid and sizes as the loaded content, with `mp-skeleton` blocks in place of text, rings and portraits.
- Every card that loads data shows a skeleton, never a spinner in the middle of an empty card. The container gets `aria-busy="true"` and a visually hidden "Loading your matches" label.
- Shapes use `track`; they pulse gently (1.6s) and stay still with `prefers-reduced-motion`.
- Don't: show a skeleton for less than 300ms (wait, then show it), or skeleton the page chrome (nav, headers).

## EmptyState

What a card or page shows when there is nothing to show yet, written as the next step. Pattern from 21st.dev's Empty States category, built on Pulse tokens and copy rules.

- Provide: a `title` sentence that says what is missing in the player's words, one line of why or what happens next, and one button that fixes it.
- Examples: "No ranked matches this week" → "Play a ranked match and your scores update within a few minutes." → "Sync matches". "Link your Riot account to see your scores" → "Link Riot account".
- Uses `mp-empty` (dashed `border-highlight`) in the same slot the content would fill.
- Don't: use illustrations, mascots or jokes; don't say "No data" or "Nothing here".

## SyncProgress

Shows new matches coming in from Riot as determinate progress, without blocking the page.

- Provide: matches synced so far, the total when known, and one line about what happens next. With no total yet, show "Looking for new matches…" and an indeterminate bar.
- Sits at the top of the page content, inline (not a modal or overlay). Everything already loaded stays usable underneath.
- The bar is `role="progressbar"` with `aria-valuenow` / `aria-valuemax`; the text updates politely (`aria-live="polite"`), at most every few matches.
- On a first sync (more than a few seconds), add one sentence that explains a score while people wait.
- When finished, the bar is replaced by "Synced 40 matches · just now" for a few seconds, then disappears.

## Button

Pill-shaped action. Use `mp-btn--primary` for the one main action in a view ("See today's matches"), `mp-btn--secondary` for alternatives, `mp-btn--ghost` for inline text actions like "All matches".

- Provide: a `<button>` (actions) or `<a href>` (navigation) with the class, and a verb-first label.
- One primary button per view. Height 48px (`mp-btn--sm` 36px only inside dense rows).
- Primary fill is `primary-strong` with `on-primary` text; hover `primary-strong-hover`.
- Don't: icon-only buttons without `aria-label`, uppercase labels, more than one primary side by side.

## PillNav

Top-level navigation as pills in a `surface-raised` track.

- Provide: `<nav class="mp-nav" aria-label="Main">` with `<a href>` children; mark the current page with `aria-current="page"`.
- Five items at most: Today, Matches, Champions, Goals, Team.
- Sits in an 80px header with the logo left and the player's champion avatar right.
- Don't: icons inside pills, badges on pills, two pill navs on one page.

## SegmentedControl

Switches the time range or view of one card (7D / 30D / Split).

- Provide: `<div class="mp-seg" role="group" aria-label="…">` with `<button type="button">` children; the active one has `aria-pressed="true"`.
- 2–4 short options. Sits top-right of the card it controls.
- Don't: use it for page navigation (use PillNav).

## Chip

A small label that states the type of a finding, or a stat on top of art.

- `mp-chip--strength` (purple) for good findings, `mp-chip--pattern` / `mp-chip--trend` (orange) for things to work on, `mp-chip--glass` for stats over splash art ("64% win rate").
- Provide: a `<span>` with one or two words, sentence case, led by its 16px Lucide icon (Strength `trending-up`, Pattern `repeat`, Trend `activity`).
- Don't: use chips as buttons, stack more than three in a row.

## Icon

A Lucide line icon as inline SVG, sized 16, 20 or 24px, coloured by the text around it.

- Provide: the Lucide name from the vocabulary in the brand book, a size (16 / 20 / 24), and either a visible label next to it or an `aria-label` on the button that holds it.
- Inline the SVG (not `<img>`) so `currentColor` works: `<svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true" focusable="false">…paths…</svg>` with the Lucide paths unchanged.
- In the Vue app use `BaseIcon` (`<BaseIcon name="swords" :size="20" />`), which reads the SVG files from `client/src/assets/icons/`.
- Chips carry an icon at 16px before the word: Strength `trending-up`, Pattern `repeat`, Trend `activity`.
- Don't: use an icon without a word (except close, copy, overflow), use two icons for one meaning, or colour an icon differently from its label.
