# Statistical Research: League of Legends Team Analytics Metrics

> **Research Date**: May 2026  
> **Purpose**: Identify science-backed metrics for invited 2-5 player teams on Mongoose.gg  
> **Scope**: Duo to full-premade team analytics for display pages (not coaching flows yet)  
> **Sources**: MOBA analytics papers, esports/team-performance research, Riot-data-derived studies

---

## Executive Summary

This document synthesizes scientific and applied research to identify:
1. Which team metrics are most predictive of winning
2. Which metrics are most actionable for invited teams of size 2-5
3. Which metrics should differ by team size and role pairing
4. How to approximate team chemistry and consistency with in-game telemetry

**Key Finding**: Team analytics should be built around three pillars:
- **Macro conversion** (gold/objectives into wins)
- **Synergy execution** (pair and role coordination quality)
- **Stability and consistency** (roster, roles, and variance control)

The strongest evidence remains that gold and objective control are top predictors of wins, while team-specific value comes from how reliably premades convert those leads together.

---

## 1. Research Grounding: What Team Science Says

## 1.1 Macro Advantage Still Dominates

Across LoL and broader MOBA analyses, team gold lead and objective control remain the highest-signal win predictors. Team pages should therefore keep:
- Gold lead checkpoints (10/15/20)
- Objective control and first-objective capture
- Lead conversion rates (win when ahead)

This aligns with your current analytics entities (`gold_lead_at_15`, `win_when_ahead_at_20`, team objectives).

## 1.2 Team Design Matters: Proficiency vs Congruency

Research on LoL team design ("proficiency-congruency dilemma") shows teams perform best when they balance:
- **Individual proficiency**: players on comfort champions/roles
- **Team congruency**: composition that fits together

Implication for Mongoose team analytics:
- Track whether teams over-prioritize comfort picks at the cost of comp fit
- Surface when role/champion familiarity improves or harms team outcomes

## 1.3 Familiarity and Coordination Effects

Team-performance literature (including online team settings) consistently shows:
- Repeated collaboration improves coordination efficiency
- Shared routines reduce execution mistakes under pressure
- Stable role allocation improves collective performance

Implication:
- Measure roster stability and role stability directly
- Compare "stable roster" vs "mixed roster" performance segments

## 1.4 Communication and Shared Mental Models

General team cognition research and esports communication studies suggest better teams exhibit:
- Faster convergence on decisions
- Higher participation in coordinated actions
- Lower variance in execution quality

Since direct voice-comms telemetry is unavailable, proxy these via in-game synchronized behavior (objective attendance, assist chains, response-time proxies around map events).

---

## 2. Metric Families for Team Analytics

## 2.1 Family A: Macro Conversion Metrics (All Team Sizes)

| Metric | Why It Matters | Suggested Calculation |
|--------|----------------|-----------------------|
| Gold Lead @ 10/15/20 | Most robust win predictor across studies | Team gold delta at checkpoints |
| Win When Ahead @ 15/20 | Measures conversion quality, not just early lead | wins in games with positive lead / games with positive lead |
| Objective Control Rate | Team-level macro discipline | (dragons + heralds + barons + towers secured) / total contestable |
| First Objective Conversion | Early coordination quality | win rate conditional on first dragon/first tower |
| Throw Resistance | Stability under pressure | win rate when behind at 15 |

## 2.2 Family B: Synergy Metrics (Pair and Subgroup Focus)

| Metric | Team Sizes | Why It Matters | Suggested Calculation |
|--------|------------|----------------|-----------------------|
| Assist Synergy % | 2-5 | Direct combat coordination signal | shared kill participation / team kills |
| Shared Objective Participation % | 2-5 | Presence at high-value decisions | shared objective events / team objective events |
| Pair Proximity Involvement (proxy) | 2-3 mainly | Reveals active pairing (bot duo, mid-jungle) | pair co-participation in kills/objectives |
| Follow-up Reliability | 2-5 | Measures whether calls become team actions | % events where >= N teammates join within event window |
| Trade Efficiency | 2-5 | Captures coordinated skirmish execution | (kills-for-deaths ratio in 30-60s post-contact windows) |

## 2.3 Family C: Stability and Consistency Metrics

| Metric | Why It Matters | Suggested Calculation |
|--------|----------------|-----------------------|
| Roster Stability Index | Team familiarity effect | % games with same invited members |
| Role Stability Index | Shared expectations and cleaner macro | % games each member keeps primary role |
| Performance Variance | Reliability over peak-only play | rolling std dev of gold@15, deaths, objective control |
| Session Drift | Fatigue and coordination drop | metric delta from game 1 to later games in same session |
| Comp Cohesion Score | Proficiency-congruency balance | champion comfort score + comp-fit heuristics |

---

## 3. Team-Size-Specific Research Recommendations

## 3.1 Two-Player Teams (Duo)

Primary use-cases: bot/support and mid/jungle.

### Bot/Support Priority Metrics
1. Shared objective participation (especially dragon windows)
2. Assist synergy and kill conversion in lane skirmishes
3. Death synchronization risk (double deaths shortly before objective spawns)
4. Vision-to-objective conversion (wards placed -> objective secured)

### Mid/Jungle Priority Metrics
1. Early gold delta at 10/15 after first jungle-mid interactions
2. Joint participation in first herald/dragon contests
3. Roam conversion rate (mid roam events resulting in objective/kill advantage)
4. Countergank/counterplay success proxy (net kill-gold in response windows)

## 3.2 Three-Player Teams

Focus on "triangle control" (typically jungle-mid-support or jungle-mid-top):
- Multi-role objective setup quality
- Cross-map response consistency
- First major objective preparation score (vision + attendance + tempo)

## 3.3 Four-Player Teams

Focus on integration gaps around the non-premade player:
- Win rate delta when random fills weak-side vs strong-side role
- Coordination robustness when one role is unstable
- Teamfight participation spread (is one role systematically isolated)

## 3.4 Five-Player Teams

Focus on full-system execution and consistency:
- Role responsibility balance (gold share, damage share, deaths share)
- Objective sequencing quality (dragon -> tower -> vision reset patterns)
- Lead protection (gold swing post-20)
- Comeback structure (loss prevention and throw minimization)

---

## 4. Role-Synergy Research Translation

## 4.1 Bot/Support (Highest Duo Priority)

Why evidence supports this focus:
- Bot lane has constant paired interaction, high communication demands, and strong objective adjacency (dragon-side play).
- Pair-level execution errors are easier to detect and more actionable than team-level aggregate outcomes.

Recommended headline metrics:
- Assist Synergy %
- Shared Dragon Participation %
- Lane Death Risk Before 14 min
- 2v2 Outcome Stability (variance-based)

## 4.2 Mid/Jungle (Highest Early-Macro Priority)

Why evidence supports this focus:
- Mid-jungle coordination influences map tempo, neutral objective pressure, and side-lane stability.
- Strong early pairing often amplifies team macro through priority and vision access.

Recommended headline metrics:
- Early Gold Delta 10/15 (pair-influenced)
- First Herald/Dragon Joint Participation
- Skirmish Conversion Rate Around River Objectives
- Post-Play Map Gain Proxy (objective/tower taken after successful action)

## 4.3 Full Team Role Structure (5 players)

Use role-balance analytics, not raw totals alone:
- Deaths Share % by role (risk concentration)
- Gold Share % vs expected role baseline
- Damage Share % and objective participation alignment
- Role-overload alerts (single role carrying unsustainable burden)

---

## 5. Measuring Chemistry and Consistency Without Voice Data

Because voice data is unavailable, use behavior-based proxies.

## 5.1 Chemistry Proxies

| Proxy | Interpretation |
|-------|----------------|
| Repeated pair/trio co-participation in decisive events | Shared decision patterns |
| Low hesitation around objective contests | Faster team alignment |
| High assist chaining with low overextension deaths | Trust + execution quality |
| Stable response patterns to enemy plays | Shared mental models |

## 5.2 Consistency Proxies

| Proxy | Interpretation |
|-------|----------------|
| Low week-to-week variance in objective control | Process consistency |
| Small performance drop across sessions | Fatigue resilience |
| Similar outcomes across patches/champ pools | Adaptability and stable fundamentals |
| Lower frequency of extreme gold swings | Better risk management |

**Important**: Chemistry scores should be shown as composite indicators with transparent components, not opaque black-box values.

---

## 6. Metric Tiers by Actionability for Team Pages

### Tier 1: High Impact + Immediately Actionable

| Metric | Why Priority #1 | Team Sizes |
|--------|------------------|------------|
| Win When Ahead @ 15/20 | Directly exposes conversion weakness | 2-5 |
| Shared Objective Participation % | Coordination quality in meaningful moments | 2-5 |
| Deaths Before Objectives | Preventable and high-leverage | 2-5 |
| Gold Lead @ 15 + Variance | Performance + stability in one view | 2-5 |

### Tier 2: High Impact + Moderate Complexity

| Metric | Why It Matters | Team Sizes |
|--------|----------------|------------|
| Role Stability Index | Coordination gains from stable responsibilities | 3-5 |
| Assist Synergy % by Pair | Identifies strongest/weakest links | 2-5 |
| Gold Swing Post-20 | Teamfight/macro discipline under pressure | 4-5 |
| Comp Cohesion Score | Draft quality vs comfort bias | 5 |

### Tier 3: Experimental but Valuable

| Metric | Value | Notes |
|--------|-------|-------|
| Chemistry Composite Score | Easy summary for users | Must remain explainable |
| Follow-up Reliability | Strong proxy for comm quality | Needs robust event windows |
| Team Decision Latency Proxy | Potentially high insight | Requires careful timeline modeling |

---

## 7. Recommended Goals for Team Contexts

### Duo Goals (2-player invites)
1. Increase shared objective participation by +10 percentage points
2. Reduce duo-linked deaths before dragon/herald timers by 20%
3. Improve win-when-ahead@15 by +8 percentage points

### Partial Team Goals (3-4 player invites)
1. Raise first-objective contest attendance consistency above 75%
2. Improve role stability index over 20 games
3. Reduce gold swing post-20 volatility by 15%

### Full Team Goals (5-player invites)
1. Improve lead conversion (ahead@20 -> win) to target threshold by rank
2. Balance deaths share across roles (avoid single-role failure concentration)
3. Improve objective sequencing success (first objective -> next macro gain)

---

## 8. Implementation Recommendations for Mongoose.gg Data Layer

### Metrics to Track Prominently (Team Page)
1. Gold lead checkpoints and conversion rates
2. Shared objective participation and assist synergy
3. Roster/role stability indexes
4. Gold swing and variance metrics (consistency)
5. Role responsibility distribution (deaths/gold/damage share)

### Existing Data Model Alignment
Your current schema/entities already support much of this:
- `duo_metrics`: `assist_synergy_pct`, `shared_objective_participation_pct`, early gold deltas
- `team_match_metrics`: `gold_lead_at_15`, `gold_swing_post_20`, `win_when_ahead_at_20`
- `team_objectives`: dragons/heralds/barons/towers
- `team_role_responsibility`: deaths/gold/damage role shares

### Suggested Additions (Future)
- Team consistency snapshots (rolling variance table/materialized view)
- Event-window participation table for follow-up reliability
- Objective-phase tagging (pre-spawn setup, contest, post-objective conversion)

---

## 9. Academic and Research Sources

## Core MOBA / LoL-Relevant Sources
1. **Kim et al. (2017)** - The Proficiency-Congruency Dilemma: Virtual Team Design and Performance in League of Legends (team design trade-off: comfort vs composition fit).
2. **Sapienza et al. (2018)** - Performance Deterioration in Team-Based Online Games (session drift and within-session decline patterns).
3. **Zhang & Naidu (2024)** - SIDO Performance Model (phase-based win predictors including gold and damage features).
4. **MOBA match outcome modeling literature (multiple studies, 2014-2024)** - Repeated findings that early advantage and objective control strongly predict outcomes.

## Team Science Foundations Used for Metric Design
5. **Shared Mental Models literature** (organizational/team cognition) - teams perform better when members align on task/role understanding.
6. **Team Familiarity and Coordination literature** - repeated collaboration improves execution and lowers coordination cost.
7. **Collective efficacy / team cohesion studies in sport and esports-adjacent contexts** - stable role clarity and coordinated behavior improve outcomes.

## Notes on Evidence Quality
- LoL-specific papers are strongest for macro predictors and team design trade-offs.
- Communication/chemistry constructs often come from broader team science and should be implemented as transparent in-game proxies.
- Where exact effect sizes vary by dataset/patch, this document prioritizes directionally stable findings over patch-specific coefficients.

---

## 10. Key Takeaways

1. Team pages should center on **conversion**, not just accumulation: lead/objective -> win.
2. Duo and team value comes from **coordinated participation metrics** more than isolated KDA totals.
3. **Bot/support** and **mid/jungle** should have dedicated synergy lenses in 2-player contexts.
4. For 5-player groups, **stability and variance control** are core predictors of reliable climb.
5. Chemistry can be approximated credibly through timeline participation patterns if components are explicit.
6. Your current schema already supports a strong v1 of science-backed team analytics with limited new data requirements.
