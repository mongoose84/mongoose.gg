# Mongoose.gg design system components

Guidelines for each component. Class names refer to components.css.

## ChampionHero

The page opener: the player's main champion splash with one headline about them.

- Provide: champion name, splash URL, the headline (one sentence, "Your {champion} …"), one supporting sentence, the player line (Riot ID · champion main · rank · LP), a primary action and up to two glass chips.
- Art is anchored right (`object-position` tuned per champion so the face stays visible), faded into `bg` by `mp-hero-scrim`. Text never sits on the art.
- When the player picks another champion card, swap the art, headline and chips together.
- **Match banner** (`mp-hero--match`): on the Matches page the open match gets the hero instead of the page. 240px tall, the eyebrow carries the result and time ("Victory · 2 hours ago", `positive-text` for a win, `warn-text` for a loss), the title is the champion and role ("Ahri, Mid") in a 40px `h2` (the page headline stays the one `h1`), a meta line (queue · length), and up to three glass chips: K / D / A, the signed LP change, and the rank after the match with its tier-colour dot. No primary action. On phones the art fills the banner with a bottom scrim and the text sits at the bottom. Opening another match swaps the art without animation.
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

- Provide: champion icon, champion name, a meta line (queue · length · time ago, plus the Riot ID in Overall mode), K / D / A, result (Victory / Defeat / Remake) and the signed LP change when known.
- Portrait border and result/LP text: win → `primary` / `positive-text`, loss → `warn` / `warn-text` (`mp-portrait--loss`, `mp-down`). A remake (ended before 5 minutes) is neither: `track-strong` border and the word "Remake" in `ink-muted` (`mp-portrait--remake`, `mp-result--remake`).
- The whole row is one `<a href>` to the match (`/app/matches/<matchId>`), so a match can be shared and the back button works. On desktop the match opens beside the list, which stays in place; below 900px it opens as its own page with an "All matches" link. Opening a match never animates. Rows are separated by `divider` hairlines.
- The open match's row carries `aria-current="page"` and sits on `surface-selected`, reaching 12px into the card padding so the columns stay aligned; the hairlines around it disappear.
- In a narrow list column (the Matches page list, 420px on desktop) wrap the rows in `mp-match-list`: below 480px of container width the KDA column moves into the meta line (`mp-match-kda` hides, `mp-match-meta-kda` shows), as on phones.
- Don't: add badges or chips to a row (a match's finding belongs on the open match), or colour a remake as a loss.

## StatTile

One stat of a match inside a card: label, number and how it compares with the player's own average.

- Provide: a label in sentence case ("Gold lead at 15", "CS per minute"), the value, and optionally one note: the comparison ("+1.2 vs your average") or a verdict word ("Won lane", "Safe early game").
- Good notes are `positive-text` led by ▲ (`mp-up`), needs-work notes `warn-text` led by ▼ (`mp-down`), neutral notes `ink-soft` with no arrow. The arrow is `aria-hidden`; the words carry the meaning. Never tint the tile itself.
- Tiles sit in `mp-stat-grid` (3 columns; 2 below 600px) on `surface-raised` with `radius-md`, inside a `surface` card whose title states the takeaway ("2 of 6 match-deciding stats went your way").
- Numbers use the real minus (−2.9) and `tabular-nums`; a missing value shows "—" with a word why ("Ended before 15 minutes").
- Don't: use it outside a card, put more than ten in one card, or use it for a 0–100 score (that is a ScoreRing).

## LaneRow

One lane of a match: role, your side against the opponent, who won the lane, and a button that opens the lane's details.

- Provide: the role (Riot role emblem as an image plus its name; a "You" Strength chip on your own lane), each side's square champion icon (36px, `radius-md`, the champion name as alt text since no name is written) and K / D / A, and the lane result in words: "Won lane" (`mp-up`), "Lost lane" (`mp-down`) or "Even" (`ink-muted`). A lane is won at 300 or more gold ahead at 10 minutes.
- The row is a real `<button class="mp-lane-row">` with `aria-expanded` and `aria-controls` pointing at the details under it; a 20px `chevron-down` turns over when open. Opening never animates.
- The details under an open row: one sentence on how the lane went (finding first), then early laning (gold lead, CS lead, deaths) and match impact (damage share, kill participation, vision score) as small tables, leads in `positive-text`, deficits in `warn-text`.
- Rows sit in a `surface` card whose title counts lanes ("Your team won 3 of 5 lanes"); hairlines are `divider`. Below 600px the role takes its own line.
- ARAM has no lanes: list both teams by damage share instead, "Your team" / "Enemy team" as eyebrows in `positive-text` / `warn-text`.
- Don't: use ✓ / ✗ or colour alone for the result, or open more than one lane at a time.

## SplitBar

Two sides of one total in a single bar: your team's share against the enemy team's (damage, gold).

- Provide: both totals, written out next to the bar ("Your team 54.2k", "Enemy team 45.8k") with a small key dot each, and the bar with the first side's width as a percentage.
- Your team is `primary`, the enemy team `warn`, the sides split by a 4px gap, `radius-pill` ends, 10px tall. The card title states the takeaway ("Your team dealt 54% of the damage").
- The bar is `role="img"` with an `aria-label` that holds both percentages ("Damage: your team 54%, enemy team 46%").
- Don't: show it without the numbers, split it into more than two sides, or animate it when a match opens.

## FormStrip

The player's recent results at a glance: one bar per match, oldest on the left, newest on the right.

- Provide: the results of the last matches in the list (up to 20, the same range as the list), and the LP change over them when the queue is ranked ("+86 LP over 20").
- Wins are tall `primary` bars (`is-win`), losses short `warn` bars, remakes a 6px `track-strong` stub (`is-remake`). Height carries the result too, so colour is never the only signal.
- Label the ends under it with `mp-form-scale` ("20 matches ago", "Latest"). The strip is `role="img"` with an `aria-label` that states the sequence and the totals ("Last 20 matches, oldest to newest: … 12 wins, 8 losses").
- It sits beside the page headline on the Matches page, which says the takeaway ("12 wins in your last 20"). The range counts matches, never days.
- Don't: make the bars clickable (the rows below are the links), show more than 20, or animate it on a filter change.

## UsualMeter

One stat of a match against the player's own usual: a bar for this match and a tick for what they normally do.

- Provide: a label in sentence case ("Gold lead at 10"), this match's value, the scale maximum, the player's usual value, and the note under it ("Your usual: +180"; or one "Your usual" key under a group of meters).
- The fill is `primary` when this match beats the usual and `warn` (`mp-usual__fill--warn`) when it falls short; the `ink` tick marks the usual. For stats where less is better (deaths), flip the comparison, not the direction of the bar.
- Mark it up as `role="meter"` with `aria-valuemin`, `aria-valuemax`, `aria-valuenow` and an `aria-valuetext` that says both numbers ("+1,240 gold, your usual is +180").
- Use three side by side (one per row on phones) as the evidence inside the "What decided it" highlight card, under a one-line finding. Track is `track-strong` because it sits on `surface-highlight`.
- Don't: use it for a 0–100 score (ScoreRing) or the readiness verdict (ReadinessMeter), show more than three in one card, or leave out the usual tick.

## LaneBar
One lane of a match as a picture: your side's gold difference at 10 minutes, growing right when ahead and left when behind.

- Provide: both champions' square icons (36px, `radius-md`), the signed gold difference at 10 ("+1,240", "−650" with a real minus), which lane is the player's, and the lane name for the label.
- Ahead grows right in `primary` (`mp-lane-bar__ahead`), behind grows left in `warn` (`mp-lane-bar__behind`) from a `track-strong` centre axis; scale the length to a fixed range (±1,500 gold fills the half). Under 300 either way is even: no bar, the number in `ink-muted`. The number carries the colour of its side, so direction, number and colour all say the same thing.
- The player's own icon gets a 2px `primary` ring (`is-you`). The enemy icon sits on the right.
- Each row is a `<button class="mp-lane-bar">` that opens the lane's details (like LaneRow), with an `aria-label` that says it in words ("Mid (you): won lane by 1,240 gold at 10 minutes"); the icons are `alt=""` inside it.
- Rows sit in a card titled with the count ("3 of 5 lanes won") and a "Gold at 10 min" caption. It replaces LaneRow where the layout is visual first; LaneRow stays for text-first lists.
- Reused without icons for LP per champion on the Solo page: one row per champion (36px icon, matches and win rate on the left), the bar for the signed LP (or net wins), gains right in `primary` and losses left in `warn`, scaled to the largest value in the list. The row is not a button there, and the bar is `role="img"` with the value in words.
- Don't: use it for anything but a two-sided difference around zero, or drop the number.

## ColumnChart

A small chart that compares two to four groups on one measure, such as win rate by start time.

- Provide: a takeaway title ("You lose most after 11pm"), the measure and range as the caption ("Win rate by start time, last 20 matches"), and for each group its label, value and share of the scale.
- Columns are `primary`; the group the finding is about is `warn` (`mp-columns__bar--warn`) when it is the weak spot, with its value in `warn-text`. The value sits on top of each column in Clash Display, the label under it in `ink-muted`. Rounded top (`radius-md`), no axis, no grid lines, no legend.
- The chart is `role="img"` with an `aria-label` that lists every group and value ("Win rate by start time: afternoon 67 percent, evening 64 percent, after 11pm 25 percent").
- Groups need enough matches to mean something: leave a group out under 3 matches rather than show a 0% or 100% from one match.
- Don't: use it for trends over time (that is a line chart), show more than four columns, or colour every column differently.

## TrendTile

One stat's trend over a range of matches: the current average, a verdict, and a sparkline of every match with the rolling average.

- Provide: a label in sentence case ("Vision per minute"), the current 10-match average and its unit, the verdict word ("Improving", "Slipping", "Steady", or "Needs 20 matches"), the earlier average for "was …", the per-match values, the rolling average points and, when there is one, a benchmark ("Your season average").
- It is a StatTile (`mp-stat mp-trend`) with an SVG sparkline under the text: one `track-strong` dot per match (`mp-trend__dot`), the rolling average as a 2.5px `primary` line with a highlighted last point (`mp-trend__line`, `mp-trend__last`), and the benchmark as a dashed `ink-muted` line (`mp-trend__benchmark`). The y scale covers all three so nothing is clipped.
- The note follows StatTile: a good verdict is `positive-text`, a needs-work verdict `warn-text`. The arrow shows the direction of the number, the colour and the word show whether that is good: fewer deaths is ▼ in purple, "Improving". "Lower is better" stats flip the verdict, never the chart.
- Six tiles sit in `mp-stat-grid` inside a card titled with the count ("4 of 6 match-deciding stats improved"), with a one-line key under the grid (`mp-trend-key`: Your average, One match, Your season average). Below 900px each tile becomes a row, text left and the sparkline right.
- The tile is `role="group"` with an `aria-label` that says the range, the change and the benchmark ("Vision per minute, last 50 matches: up from 0.7 to 0.9, your season average 0.8"); the SVG is `aria-hidden`.
- Don't: draw fewer than two points as a line, add axes or grid lines, or colour the dots by result.

## WinFactorRow

What decides the player's matches: their win rate when they hit a mark against when they missed it, as two dots on one 0–100% track.

- Provide: the mark in words ("Ahead in gold at 15", "0.9+ vision per minute"), the win rate in matches that hit it, the win rate in matches that missed it, and the match counts for the text alternative.
- The hit rate is a filled `primary` dot (`mp-win-factor__dot is-hit`), the missed rate a `warn` ring on `surface` (`is-miss`), on a 2px `track` line; the stretch between them is `track-strong` (`mp-win-factor__span`). Both rates sit above the track in Clash Display, hit in `positive-text`, missed in `warn-text`, with "vs" between. Shape and position carry the meaning too: filled vs ring, right vs left.
- A negative gap draws the same way, with the hit dot left of the missed one: the row still tells the truth.
- Rows sit in a card titled with the takeaway ("Your gold lead at 15 decides your matches most"), sorted by gap, largest first. Each side needs 5 matches; show a row only when both have them.
- The track is `role="img"` with an `aria-label` that says both rates and counts ("Ahead in gold at 15: you won 68% of 22 matches, and 38% of 28 matches when you weren't"); the rates above it are `aria-hidden`.
- Don't: make the rows clickable, draw bars instead of dots, or show a row with under 5 matches on a side.

## GoalStrip

Whether the player hit one mark in each of their last matches: one cell per match, oldest on the left.

- Provide: the results of the last 20 matches as hit, missed or not counted (the mark doesn't apply, such as a support's CS), and the mark in words for the text alternative ("0.9+ vision per minute").
- Hit is a filled `primary` cell (`is-hit`), missed a 2px `warn` ring (`is-miss`), not counted a 6px `track-strong` stub (`is-none`). Filled, ring and stub differ in shape, so colour is never the only signal. It is the FormStrip's shape with the WinFactorRow's dot language.
- Label the ends under it with `mp-form-scale` ("20 matches ago", "Latest"). The strip is `role="img"` with an `aria-label` that lists every match and the total ("… Hit in 9 of 18").
- It sits in the focus card (`mp-card--highlight`) between the evidence and the "Next match" fix.
- Don't: make the cells clickable, show more than 20, or use it for a win/loss record (that is a FormStrip).

## DeathMap

Where the player's deaths cost them: an outline of Summoner's Rift with one circle per zone, a zone list that filters the breakdowns, and three breakdown bars.

- Provide: up to five zones (key, label, deaths, objectives lost after them, a fixed anchor on the map, and a timing note such as "27 of them after 25 minutes" or "Mostly before 14 minutes"), plus counts per phase ("When"), per kind of death ("How") and per objective lost ("What it cost"), for all zones and for each one.
- The map (`mp-death-map`, an SVG on a 0–100 grid) draws the ground in `surface-raised`, both bases in `track`, the three lanes in `track-strong` and the river as a dashed `divider` line. The player's base is always bottom left: deaths on the red side are mirrored. Zones are named regions, not clusters, so they stay put between visits.
- Each zone is a circle at its anchor (`mp-death-map__zone`), its area following the deaths (radius 4 to 8 on the grid), `primary` at 55% opacity, `warn` when 30% or more of its deaths cost an objective (`is-costly`); the selected zone is filled more and outlined in `ink` (`is-selected`). The map is `role="img"` with every zone in its `aria-label`; the list is the interactive part.
- The zone list (`mp-zone-list`) is one `<button class="mp-zone">` per zone with `aria-pressed`: a dot matching the circle, the zone name, "12 deaths · 5 objectives lost" and the timing note. Pressing one filters the breakdowns; pressing it again returns to all zones. The breakdowns' heading names the filter ("All zones", "Top river") and is `aria-live`.
- The breakdowns (`mp-breakdown`) are three short groups of labelled bars on a `track`: When (Before 14 min, 14–25 min, After 25 min), How (Ganked in lane, Caught alone, In a teamfight, Lane fights and skirmishes) in `primary`, and What it cost (Dragon, Tower, Baron, Herald) in `warn` (`is-warn`). Each bar is scaled to the group's largest.
- The card is titled with the takeaway ("Deaths in the top river cost you the most objectives" when the top zone lost 3 or more, else "You die most in your top-side jungle") and needs 30 deaths with detail; until then it shows the SyncProgress-style bar while older matches are read, or an EmptyState. Layout: map, list and breakdowns in three columns; the breakdowns go below at 1100px and everything stacks below 900px.
- Don't: draw a heatmap or individual death dots, add more than five zones, or colour zones by anything but cost.

## InsightCard

A plain-language finding with its evidence and fix.

- Provide: a champion icon (the champion the finding is about; the player's main if none), a chip (Strength / Pattern / Trend), a one-sentence finding as the title, and one or two sentences of evidence and advice.
- Three per row, strongest finding first. Title uses the `insight` style, body `ink-muted`.
- Don't: write findings without a number behind them, or stack more than six.

## Skeleton

The loading state of a card: grey shapes in the exact layout of the content that is coming. Pattern from 21st.dev's Skeleton and Spinner Loaders categories, built on design-system tokens.

- Provide: the same card, grid and sizes as the loaded content, with `mp-skeleton` blocks in place of text, rings and portraits.
- Every card that loads data shows a skeleton, never a spinner in the middle of an empty card. The container gets `aria-busy="true"` and a visually hidden "Loading your matches" label.
- Shapes use `track`; they pulse gently (1.6s) and stay still with `prefers-reduced-motion`.
- Don't: show a skeleton for less than 300ms (wait, then show it), or skeleton the page chrome (nav, headers).

## EmptyState

What a card or page shows when there is nothing to show yet, written as the next step. Pattern from 21st.dev's Empty States category, built on design-system tokens and copy rules.

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
- Items, in order: Overview, Champion Select, Matches, Solo, and Advanced (Team and Goals combined, when it ships). Five at most.
- Sits in an 80px header with the logo left and the player's champion avatar right; the avatar opens a menu with Settings, Feedback and Log out. This header is the only app navigation: no sidebar.
- Phones (below 900px): the header shrinks to 56px (logo and avatar) and the items move to a fixed bottom tab bar, 64px plus the safe-area inset, `surface-raised` with a `divider` hairline on top. Each tab is an icon above a short label ("Champ Select" on phones); the active tab uses `positive-text` and `aria-current="page"`. Class: `.mp-tabbar` on the same `<nav aria-label="Main">`. This is the one place icons appear in the navigation.
- How Pro-only items are marked is decided when Pro is built (never a badge on a pill).
- Don't: icons inside desktop pills, badges on pills, two pill navs on one page.

## SegmentedControl

Switches the time range or view of one card (Last 20 / Last 50 / Season). Ranges count matches, not days.

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

## EmailTemplate

The layout of every transactional email (email verification, password reset): one card on the dark ground with the code in the single highlight box.

- Provide: a subject, a preheader (the inbox preview line: when the code expires), a title (sentence case, the action: "Verify your email", "Reset your password"), one sentence that starts "Hi {username}," and says what to do with the code, the code, its label (`eyebrow`: "VERIFICATION CODE", "RESET CODE"), the expiry line, an optional next step, and a line for people who did not ask for the email.
- Layout: `bg` ground; the logo and "Mongoose.gg" above the card; one `surface` card with a `divider` border and `radius-xl`; the code in a `surface-highlight` box with `border-highlight` and `radius-lg`, its digits in Clash Display 40px with wide letter spacing; the footer in `ink-faint` with "Not affiliated with Riot Games" and the year.
- Text colours: title and code `ink`, body `ink-soft`, the ignore line and code label `ink-muted`.
- Width: fluid up to 560px; below 480px the card padding drops from 40/36px to 28/20px. Mark it `color-scheme: dark` so clients do not invert it.
- Fonts: `@font-face` loads Clash Display and Satoshi from the site (`https://beta.mongoose.gg/fonts/` for now; the site serves them with `Access-Control-Allow-Origin: *`). Apple Mail and iOS Mail use them; Gmail, Outlook and most others fall back to system fonts, so the design must hold up in the fallback.
- Always send a plain-text part with the same copy next to the HTML part.
- Code lives in `server/Mongoose.Api/Infrastructure/Email/EmailTemplates.cs`; HTML-encode every user value.
- Don't: add buttons that log the player in, images other than the logo, tracking pixels, exclamation marks or emoji; put the code in the subject line.

## TextField

A labelled single-line input: pill-shaped, 48px tall, on the page ground.

- Provide: a visible `<label>` above the field (sentence case: "Username", "Email"), the input with the right `type` and `autocomplete`, and optionally a hint or an error line below it. Required fields show a `*` in `ink-muted` after the label.
- Look: `bg` fill (so it reads as a hole in the `surface` card), 1px `divider` border, `radius-pill`, 20px side padding, Satoshi 16px `ink`; placeholder in `ink-faint` shows the format ("you@example.com"), never the label.
- States: hover border `border-highlight`; focus border `primary` plus `shadow-focus`; error border `error` with the message below in `error` 13px, tied to the input with `aria-describedby` and `aria-invalid`; disabled at 45% opacity.
- Hint: 13px `ink-muted` under the field; an error replaces the hint.
- Stack fields 20px apart (`space-5`); a label sits 8px above its field.
- In the Vue app: `BaseInput`. Class: `mp-field` with `mp-field__label`, `mp-field__input`, `mp-field__hint`, `mp-field__error` and `mp-field--error` on the wrapper.
- Don't: use the placeholder as the only label, put icons inside the field without a word next to them, or colour a valid field purple.

## MessageBox

An inline box at the top of a form or card that tells the player something went wrong or needs their attention. Two kinds: error and notice.

- Provide: a Lucide icon (20px), one or two sentences in plain "you" language, and optionally one text action ("Update cookie preferences", "Try again").
- **Error** (`mp-message--error`): a failed request or a form that could not be sent ("Wrong username or password", "We couldn't reach Riot's servers"). `error-soft` fill, 1px `error-border`, text `ink`, icon `triangle-alert` in `error`. Use `role="alert"`.
- **Notice** (`mp-message--notice`): neutral information that blocks or changes an action but is not a failure ("You've rejected cookies. Logging in needs an authentication cookie."). `surface-raised` fill, 1px `divider` border, text `ink-soft`, icon `info` in `ink-muted`.
- Look: `radius-md`, padding 12px 16px, 12px between icon and text, Satoshi 14px. The action is a text button in `positive-text`, 700, below the sentence.
- Place it directly above the form fields it concerns, inside the same card; one message box at a time.
- Say what happened and what to do next, calmly ("We couldn't reach Riot's servers. Try again in a minute."). No "Oops", no exclamation marks, no raw error codes.
- Field-level problems go under the field (TextField error), not in a message box.
- Don't: use it for match or performance news (that is an InsightCard), use `warn` orange for system errors, or stack several boxes.
