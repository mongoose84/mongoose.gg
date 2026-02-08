# Product Backlog - Mongoose (mongoose.gg)

## Vision

> **"Mongoose is the solo queue improvement tracker—built to help you climb with better champ select picks and post-game takeaways, with a Premium mode that lets your duo or team set goals and improve together."**
>
> “Not just another builds app - Mongoose helps you improve between games and track progress over time.”

Crafted with love by the Agile Astronaut.
First 500 users get free Pro tier. Keep a counter on the landing page of how many free users are left.

## Pricing Model

**Key decision:** Keep pricing **simple** with **2 tiers** (Free + Pro). Collaboration is the main upsell; coaching/goals is the second.

**How Duo/Team works:**

- **Guests (Free users) can create and join Duo/Team spaces**
- **Guests only see their own data** inside the group (plus generic upgrade nudges)
- **Only Pro members** can access Duo/Team dashboards and use **shared goals** / collaboration features
- If a Pro user downgrades, they become a Guest and **do not break the group** for others

| Tier | Monthly | Annual | Features |
|------|---------|--------|----------|
| **Free** | €0 | - | Solo basics, last 20 games, champ-select personal matchup highlights, limited post-game takeaways |
| **Pro** | €4.99 | €3.99/mo | Full solo history, goal setting + tracking, deeper post-game coaching, Duo/Team spaces, Duo/Team dashboards, shared goals (Guests can join groups but can’t collaborate) |

**Primary upgrade moments to optimize for:**

1. **Collaboration lock:** user creates/joins a Duo/Team and hits locked team dashboard/shared goals → upgrade
2. **Goals lock:** user wants to set/track concrete improvement goals over the next X games → upgrade

---

## Epic Overview

| Epic | Description | Remaining Points | Completed Points |
|------|-------------|------------------|------------------|
| **B. AI Goal Recommendations** | LLM-powered improvement suggestions | 44 pts | 0 pts |
| **C. Subscription & Paywall** | Mollie integration, tiers, feature flags | 27 pts | 7 pts ✅ |
| **D. Analytics & Tracking** | User behavior tracking for product decisions | 19 pts | 0 pts |
| **E. Database & Analytics Schema** | Match/participant/timeline schema + ingestion | 0 pts | 20 pts ✅ |
| **F. API** | API surface aligned with schema and dashboards | 24 pts | 38 pts ✅ |
| **G. Frontend App & Marketing** | App shell, landing, and dashboards using API | 35 pts | 58 pts ✅ |

**Remaining:** 149 points | **Completed:** 123 points | **Grand Total:** 272 points

### G5 Epic: Frontend Solo Dashboard (Vertical slices)

The original G5 (5 points) has been split into **11 focused tasks** (35 points total), structured as vertical slices where possible:
- **G5a:** Dashboard Hub design (2 pts) ✅
- **G5b0:** Solo Dashboard design (2 pts) ✅
- **G5b1:** Empty Solo dashboard view & routing (1 pt) ✅
- **G5b2:** Profile header card + profile data (5 pts, FE+BE) ✅
- **G5b3:** Main Champion Card (3 pts, FE+BE) ✅
- **G5b4–G5b7:** Dashboard components **plus their backing data** (14 pts total; winrate chart, LP chart UI, matchups table, goals panel)
- **G5b8:** Database support for profile data (1 pt) ✅
- **G5b9:** Fetch and store profile data during account linking (2 pts) ✅
- **G5b10:** Update User dashboard endpoint with profile data (1 pt) ✅
- **G5b11:** Champion matchups endpoint (3 pts) ✅
    - **G5b12:** Main champions by role endpoint (2 pts) ✅

    Backend task **G5b15** is treated as a backend **subtask** of the corresponding vertical slices (G5b4–G5b7). In `docs/product_plan.md` its effort is rolled up into the main G5b4–G5b7 items; in this backlog it remains as a separate subsection to spell out backend behaviour and acceptance criteria.

Each vertical slice has clear acceptance criteria, enabling parallel frontend/backend work and incremental value delivery.

> Note: Platform epics (E–G) are prerequisites for most feature work (B–D) and should generally be completed first.
>
> **Completed tasks have been moved to [product_backlog_completed.md](./product_backlog_completed.md).**

## Cross-cutting requirements

- All dashboard endpoints and views support **queue filtering** (Ranked Solo/Duo, Ranked Flex, Normal, ARAM).
- Queue filtering is backed by the schema via `matches.queue_id` (numeric Riot queue id) and appropriate indexing.
- Frontend UX and page responsibilities should align with the contracts in `docs/ui-ux/ux-specification.md` (navigation model, page roles, non-negotiable rules). If a task needs to diverge, update that spec alongside this backlog.
- New UI components and flows should follow `docs/ui-ux/ui-design-guidelines.md`, reusing base components (BaseButton, BaseCard, etc.) and design tokens for colors, typography, spacing, and accessibility.

## Definition of Done (applies to all tasks)

A task is only considered complete when **all** of the following are true:

- [ ] All acceptance criteria for the task are met
- [ ] Code changes are committed and merged
- [ ] Update `docs/product_plan.md` to mark the task as ✅ complete
- [ ] Update `docs/product_backlog.md` to remove the completed task from the active backlog
- [ ] Update `docs/product_backlog_completed.md` to add the task with full details and checked acceptance criteria

---

# Epic B: AI-Powered Goal Recommendations

Enable users to receive personalized improvement goals powered by LLM analysis.

<!-- AI: START_EPIC_B_TASKS -->

## Issues

### B1. [Infrastructure] Create LLM provider abstraction layer

**Priority:** P0 - Critical
**Type:** Infrastructure
**Estimate:** 3 points
**Labels:** `infrastructure`, `ai`, `epic-b`

#### Description

Create abstraction layer for swapping LLM providers.

#### Acceptance Criteria

- [ ] Create `ILlmClient` interface with `CompleteAsync(LlmRequest, CancellationToken)`
- [ ] Create `LlmRequest` model (SystemPrompt, UserPrompt, MaxTokens, Temperature)
- [ ] Create `LlmResponse` model (Content, TokensUsed, FinishReason)
- [ ] Create `LlmClientFactory` to resolve provider from configuration
- [ ] Register in DI container

---

### B2. [Infrastructure] Implement OpenAI LLM client

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** B1
**Labels:** `infrastructure`, `ai`, `openai`, `epic-b`

#### Description

Implement OpenAI provider for LLM abstraction.

#### Acceptance Criteria

- [ ] Create `OpenAiClient : ILlmClient`
- [ ] Use Chat Completions API (`/v1/chat/completions`)
- [ ] Support GPT-4 and GPT-3.5-turbo via configuration
- [ ] Store API key securely
- [ ] Add configuration to `appsettings.json`

---

### B3. [Infrastructure] Add LLM rate limiting per user

**Priority:** P1 - High
**Type:** Infrastructure
**Estimate:** 2 points
**Depends on:** B1
**Labels:** `infrastructure`, `rate-limiting`, `epic-b`

#### Description

Rate limiting for LLM API usage costs.

#### Acceptance Criteria

- [ ] Create `ILlmRateLimiter` interface
- [ ] Track requests per user per time window
- [ ] Store state in database or cache
- [ ] Configurable limits via `appsettings.json`

---

### B4. [Database] Create Goal and GoalProgress tables

**Priority:** P0 - Critical
**Type:** Database Migration
**Estimate:** 2 points
**Labels:** `database`, `migration`, `epic-b`

#### Description

Database schema for user goals and progress tracking.

#### Acceptance Criteria

- [ ] Create `Goal` table (UserId, Puuid, ContextType, Title, Description, MetricName, MetricBaseline, MetricTarget, Status, CreatedAt, CompletedAt)
- [ ] Create `GoalProgress` table (GoalId, MetricValue, MatchCount, MeasuredAt)
- [ ] Create entity classes and `GoalRepository`

---

### B5. [Service] Create player stats aggregation service

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Labels:** `service`, `statistics`, `epic-b`

#### Description

Service to aggregate player statistics for LLM analysis.

#### Acceptance Criteria

- [ ] Create `IPlayerStatsAggregator` interface
- [ ] Gather: overall stats, recent trends, champion performance, role distribution, weakest areas
- [ ] Support solo, duo, and team contexts
- [ ] Return `PlayerStatsSnapshot` model
- [ ] Format for LLM consumption

---

### B6. [Service] Create goal recommendation prompt builder

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** B5
**Labels:** `service`, `ai`, `epic-b`

#### Description

Service to build effective prompts for LLM goal recommendations.

#### Acceptance Criteria

- [ ] Create `IGoalPromptBuilder` interface
- [ ] Implement templates for solo, duo, team contexts
- [ ] Request JSON output format
- [ ] Include LoL domain knowledge in system prompt
- [ ] Store prompts as configurable templates

---

### B7. [Service] Create goal recommendation service

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Depends on:** B1, B5, B6
**Labels:** `service`, `ai`, `epic-b`

#### Description

Orchestrate goal recommendation flow.

#### Acceptance Criteria

- [ ] Create `IGoalRecommendationService` interface
- [ ] Implement: gather stats → build prompt → call LLM → parse response
- [ ] Handle malformed LLM responses gracefully
- [ ] Log LLM interactions

---

### B8. [API] Create goal recommendation endpoint

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** B7
**Labels:** `api`, `endpoints`, `epic-b`

#### Description

API endpoint for goal recommendations.

#### Acceptance Criteria

- [ ] Create endpoint: `POST /api/goals/recommend`
- [ ] Request: puuid, context (solo/duo/team), contextPuuids
- [ ] Response: array of recommendations with title, description, metric, currentValue, targetValue, priority
- [ ] Check rate limits and require authentication

---

### B9. [API] Create goal CRUD endpoints

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 2 points
**Depends on:** B4
**Labels:** `api`, `endpoints`, `epic-b`

#### Description

Endpoints to manage user goals.

#### Acceptance Criteria

- [ ] `POST /api/goals` - Create goal
- [ ] `GET /api/goals` - List user's goals
- [ ] `GET /api/goals/{id}` - Get goal with progress
- [ ] `PATCH /api/goals/{id}` - Update status
- [ ] `DELETE /api/goals/{id}` - Delete goal

---

### B10. [Service] Create goal progress tracking service

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 3 points
**Depends on:** B4, B5
**Labels:** `service`, `progress`, `epic-b`

#### Description

Automatically track goal progress as matches sync.

#### Acceptance Criteria

- [ ] Create `IGoalProgressService` interface
- [ ] Calculate current metric value
- [ ] Compare to baseline, detect completion
- [ ] Support metrics: cs_per_min, deaths_per_game, kda, win_rate, gold_per_min

---

### B11. [Background] Create goal progress update job

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 2 points
**Depends on:** B10
**Labels:** `background-job`, `epic-b`

#### Description

Background job to update goal progress after match sync.

#### Acceptance Criteria

- [ ] Hook into match sync job completion
- [ ] Calculate and record progress for active goals
- [ ] Check for goal completion

---

### B12. [API] Create goal progress endpoint

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 1 point
**Depends on:** B10
**Labels:** `api`, `endpoints`, `epic-b`

#### Description

Endpoint for goal progress history.

#### Acceptance Criteria

- [ ] `GET /api/goals/{id}/progress`
- [ ] Return: baseline, target, current, progressPercent, history array
- [ ] Include trend direction

---

### B13. [Frontend] Create goal recommendation UI component

**Priority:** P2 - Medium
**Type:** Feature
**Estimate:** 3 points
**Depends on:** B8
**Labels:** `frontend`, `vue`, `epic-b`

#### Description

Vue component for goal recommendations.

#### Acceptance Criteria

- [ ] Create `GoalRecommendations.vue`
- [ ] "Get AI Recommendations" button
- [ ] Loading state, error handling
- [ ] Display recommendations as cards with "Set as Goal" button

---

### B14. [Frontend] Create active goals display component

**Priority:** P2 - Medium
**Type:** Feature
**Estimate:** 3 points
**Depends on:** B9, B12
**Labels:** `frontend`, `vue`, `epic-b`

#### Description

Vue component for active goals with progress.

#### Acceptance Criteria

- [ ] Create `ActiveGoals.vue`
- [ ] Progress bar (baseline → current → target)
- [ ] Trend indicator, actions (complete, abandon, delete)

---

### B15. [Frontend] Create goal progress chart component

**Priority:** P2 - Medium
**Type:** Feature
**Estimate:** 2 points
**Depends on:** B12, B14
**Labels:** `frontend`, `vue`, `charts`, `epic-b`

#### Description

Chart showing goal progress over time.

#### Acceptance Criteria

- [ ] Create `GoalProgressChart.vue`
- [ ] Line chart with baseline and target lines
- [ ] Highlight when target reached

---

### B16. [Infrastructure] Implement Anthropic Claude LLM client

**Priority:** P3 - Low
**Type:** Feature
**Estimate:** 2 points
**Depends on:** B1
**Labels:** `infrastructure`, `ai`, `anthropic`, `epic-b`

#### Description

Anthropic Claude provider for LLM abstraction.

#### Acceptance Criteria

- [ ] Create `AnthropicClient : ILlmClient`
- [ ] Use Anthropic Messages API
- [ ] Support Claude 3 models

---

### B17. [Feature] Add conversational follow-up support

**Priority:** P3 - Low
**Type:** Feature
**Estimate:** 5 points
**Depends on:** B7
**Labels:** `feature`, `ai`, `conversation`, `epic-b`

#### Description

Enable follow-up questions about goals.

#### Acceptance Criteria

- [ ] Create `ConversationSession` model
- [ ] Store conversation context
- [ ] Endpoint: `POST /api/goals/chat`
- [ ] Frontend chat UI

---

### B18. [AI] Add rules-of-climbing domain context for recommendations

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 2 points
**Depends on:** B5, B6, B7
**Labels:** `ai`, `prompting`, `epic-b`

#### Description

Use the concepts in `./rules_of_climbing.md` as domain context so the AI can interpret stats and suggest goals in line with your climbing philosophy.

#### Acceptance Criteria

- [ ] Summarize the key rules from `./rules_of_climbing.md` into a stable, versioned system prompt (or configuration)
- [ ] Ensure `IGoalPromptBuilder` includes this context for solo, duo and team prompts
- [ ] Add tests or fixtures that verify the rules context is present in prompts so changes are explicit

---

### B19. [Discovery] Clarify AI goals vs coaching product direction

**Priority:** P1 - High
**Type:** Discovery
**Estimate:** 3 points
**Labels:** `product`, `strategy`, `ai`, `epic-b`

#### Description

Clarify how far the AI experience should go beyond one-off goal recommendations toward a fuller "coaching" product. Capture concrete use cases, guardrails, and business goals so future AI work (B20, B21 and others) has a clear target and we avoid over-building in the wrong direction.

#### Acceptance Criteria

- [ ] Talk to at least 3–5 representative users (or prospects) about their expectations from "AI help" vs. human-like coaching
- [ ] Map out a simple spectrum from "lightweight guidance" → "full coaching" and decide explicitly where mongoose.gg should sit for the next 6–12 months
- [ ] Document 3–5 primary AI use cases (e.g. post-game feedback, champion select advice, long-term goals) and 3–5 things that are explicitly out of scope for now
- [ ] Capture constraints (latency, cost per user, data needed) that will influence technical design for AI features
- [ ] Produce a short written brief (1–2 pages) that is linked from this task and referenced by B20 and B21

---

### B20. [Feature] Post-game AI feedback (Pro, on-demand)

**Priority:** P2 - Medium
**Type:** Feature
**Estimate:** 5 points
**Depends on:** B5, B6, B7, B8, B19, B21
**Labels:** `ai`, `goals`, `post-game`, `pro`, `epic-b`

#### Description

Allow Pro users to request AI feedback after a specific match. The AI reviews the match stats/timeline and returns a concise explanation of what went well, what went wrong, and 2–3 concrete, actionable tips for the next games, aligned with the product direction defined in B19.

#### Acceptance Criteria

- [ ] Add an internal API contract for requesting post-game feedback (e.g. match id + context such as lane/role, primary goals) built on top of the existing AI goal/analysis pipeline from Epic B
- [ ] Feedback includes:
  - 1–2 sentences of overall summary
  - Bullet list of 2–3 key mistakes or improvement areas grounded in match stats (CS, gold, deaths, objectives, etc.)
  - 2–3 specific, testable suggestions for next games (e.g. "aim for 7 CS/min by 10 minutes", "ward river before pushing past mid lane by 6 minutes")
- [ ] Clearly labelled as a **Pro** feature in the UI; Free users see an upgrade CTA instead of triggering the AI call
- [ ] Rate limiting and usage tracking are in place so we can control cost per user (e.g. max N feedback requests per day/week)
- [ ] Feedback is explicitly framed in the context of recent trends (e.g. last 10–20 games) so that single-match insights are clearly part of a multi-game pattern, not isolated judgments
- [ ] From the feedback UI, users can easily navigate to longer-term improvement views (e.g. goals or analysis pages) so goal management remains centralized rather than handled ad-hoc per match
- [ ] Copy avoids deterministic win/loss predictions and instead focuses on preparation, trend awareness, and actionable next steps
- [ ] UX is designed so users can easily request feedback from the match details view without getting lost (discoverable entry point and clear loading/empty states)

---

### B21. [Infrastructure] Extend database for AI coaching metrics

**Priority:** P1 - High
**Type:** Infrastructure
**Estimate:** 5 points
**Depends on:** B4, E5
**Labels:** `database`, `ai`, `analytics`, `epic-b`

#### Description

Extend the database schema so we can persist the extra metrics and derived data needed for richer AI coaching, such as lane-specific gold/XP diffs, early objective control, and repeated mistake patterns over time. This underpins B20 and future AI coaching features.

#### Acceptance Criteria

- [ ] Identify which additional metrics beyond the current schema are required for post-game AI feedback and medium-term coaching (e.g. lane gold difference at 10/15 minutes, jungle proximity, warding patterns)
- [ ] Propose schema changes (new tables or columns) and add them to `../architecture/database-schema.md` before implementation
- [ ] Implement the schema changes via MySQL migrations and repository updates
- [ ] Backfill or derive the new metrics for existing matches where feasible, or explicitly document which metrics are only available for newly synced games
- [ ] Verify that the new metrics can be queried efficiently enough for AI requests without causing performance issues on the main dashboards

---

<!-- AI: END_EPIC_B_TASKS -->

# Epic C: Subscription & Paywall System

Implement tiered subscriptions with Mollie integration (European payment provider) and feature flags.

## Issues

### C1. [Infrastructure] Set up Mollie integration

**Priority:** P0 - Critical
**Type:** Infrastructure
**Estimate:** 3 points
**Labels:** `infrastructure`, `payments`, `mollie`, `epic-c`

#### Description

Set up Mollie SDK and configuration for payment processing (European payment provider).

#### Acceptance Criteria

- [ ] Add Mollie.Api NuGet package
- [ ] Store Mollie API keys securely (e.g., environment variables, user-secrets)
- [ ] Configure Mollie client in DI container
- [ ] Set up webhook endpoint for Mollie events
- [ ] Add to `appsettings.json`:
  ```json
  "Mollie": {
    "ApiKey": "live_...",
    "WebhookSecret": "..."
  }
  ```

---

### C2. [Database] Create subscription tables

**Priority:** P0 - Critical
**Type:** Database Migration
**Estimate:** 2 points
**Labels:** `database`, `migration`, `epic-c`

#### Description

Database schema for subscriptions and billing.

#### Acceptance Criteria

- [ ] Create `Subscription` table:
  ```sql
  CREATE TABLE Subscription (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    MollieCustomerId VARCHAR(100),
    MollieSubscriptionId VARCHAR(100),
    Tier ENUM('free', 'pro', 'team') DEFAULT 'free',
    Status ENUM('active', 'cancelled', 'past_due', 'trialing') DEFAULT 'active',
    CurrentPeriodStart DATETIME,
    CurrentPeriodEnd DATETIME,
    CancelAtPeriodEnd BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES User(Id),
    UNIQUE INDEX idx_subscription_user (UserId),
    INDEX idx_subscription_mollie (MollieSubscriptionId)
  );
  ```
- [ ] Create `SubscriptionEvent` table for audit log
- [ ] Create entity classes and `SubscriptionRepository`

---



### C4. [Service] Create Mollie customer service

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** C1, C2
**Labels:** `service`, `mollie`, `epic-c`

#### Description

Service to manage Mollie customers.

#### Acceptance Criteria

- [ ] Create `IMollieCustomerService` interface
- [ ] `CreateCustomerAsync(User user)` - create Mollie customer
- [ ] `GetOrCreateCustomerAsync(User user)` - idempotent customer creation
- [ ] Store MollieCustomerId in Subscription table
- [ ] Handle Mollie API errors

---

### C5. [Service] Create subscription management service

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Depends on:** C2, C4
**Labels:** `service`, `stripe`, `epic-c`

#### Description

Service to manage subscription lifecycle.

#### Acceptance Criteria

- [ ] Create `ISubscriptionService` interface
- [ ] `CreateCheckoutSessionAsync(UserId, Tier)` - generate Mollie payment link
- [ ] `GetSubscriptionAsync(UserId)` - get current subscription
- [ ] `CancelSubscriptionAsync(UserId)` - cancel at period end
- [ ] Handle upgrade/downgrade flows
- [ ] Note: Mollie doesn't have a built-in customer portal like Stripe; manage subscriptions via app UI

---

### C6. [API] Create Mollie webhook handler

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Depends on:** C1, C5
**Labels:** `api`, `mollie`, `webhook`, `epic-c`

#### Description

Handle Mollie webhook events for subscription updates.

#### Acceptance Criteria

- [ ] Create endpoint: `POST /api/webhooks/mollie`
- [ ] Verify webhook signature
- [ ] Handle events:
  - `payment.paid` → activate subscription
  - `subscription.updated` → update status/tier
  - `subscription.cancelled` → downgrade to free
  - `payment.failed` → mark past_due
- [ ] Log all events to `SubscriptionEvent` table

---

### C7. [API] Create subscription endpoints

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** C5
**Labels:** `api`, `endpoints`, `epic-c`

#### Description

API endpoints for subscription management.

#### Acceptance Criteria

- [ ] `GET /api/subscription` - get current subscription status
- [ ] `POST /api/subscription/checkout` - create checkout session for upgrade
  - Request: `{ "tier": "pro" }`
  - Response: `{ "checkoutUrl": "https://www.mollie.com/checkout/..." }`
- [ ] `POST /api/subscription/cancel` - cancel subscription (handle via app UI instead of external portal)
- [ ] Require authentication

---

### C8. [Service] Create feature flag service

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** C3
**Labels:** `service`, `feature-flags`, `epic-c`

#### Description

Service to check feature access based on subscription tier.

#### Acceptance Criteria

- [ ] Create `IFeatureFlagService` interface
- [ ] Define feature flags:
  ```csharp
  public enum Feature
  {
      DuoDashboard,
      TeamDashboard,
      AiRecommendations,
      GoalTracking,
      FullMatchHistory,
      UnlimitedAi
  }
  ```
- [ ] `HasAccessAsync(UserId, Feature)` - check if user can access feature
- [ ] Feature → Tier mapping:
  | Feature | Free | Pro | Team |
  |---------|------|-----|------|
  | Solo Dashboard | ✅ | ✅ | ✅ |
  | Last 20 Games | ✅ | ✅ | ✅ |
  | Full Match History | ❌ | ✅ | ✅ |
  | Duo Dashboard | ❌ | ✅ | ✅ |
  | Team Dashboard | ❌ | ❌ | ✅ |
  | AI Recommendations | ❌ | 5/week | Unlimited |
  | Goal Tracking | ❌ | ✅ | ✅ |

---

### C9. [Middleware] Create feature gate middleware

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 2 points
**Depends on:** C8
**Labels:** `middleware`, `feature-flags`, `epic-c`

#### Description

Middleware/attribute to protect endpoints by feature.

#### Acceptance Criteria

- [ ] Create `[RequireFeature(Feature.DuoDashboard)]` attribute
- [ ] Create middleware to check feature access
- [ ] Return 403 with upgrade message if feature not available
- [ ] Response: `{ "error": "upgrade_required", "requiredTier": "pro", "feature": "duo_dashboard" }`

---

### C12. [Frontend] Create upgrade prompt component

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 2 points
**Depends on:** C7
**Labels:** `frontend`, `vue`, `epic-c`

#### Description

Component to prompt users to upgrade when hitting feature limits.

#### Acceptance Criteria

- [ ] Create `UpgradePrompt.vue`
- [ ] Props: feature name, required tier
- [ ] Display benefits of upgrading
- [ ] "Upgrade to Pro" / "Upgrade to Team" buttons
- [ ] Redirect to Mollie checkout
- [ ] Default usage pattern is non-blocking on core flows (e.g. Overview, Solo dashboard, match details): prompts appear as inline cards, panels, or sidebars so the primary task remains usable even if the user ignores the upgrade CTA
- [ ] Visual style and copy follow the dark, tool-like aesthetic from `docs/ui-ux/ui-design-guidelines.md` rather than feeling like a separate marketing page

---

### C14. [Frontend] Gate features based on tier

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 2 points
**Depends on:** C8, C10
**Labels:** `frontend`, `vue`, `epic-c`

#### Description

Implement frontend feature gating.

#### Acceptance Criteria

- [ ] Create `useSubscription` composable
- [ ] `hasFeature(feature)` method
- [ ] Hide/disable features user can't access
- [ ] Show upgrade prompt instead of blocked features
- [ ] Blur/overlay for teaser content
- [ ] When a feature is gated, prefer inline locked states (blurred content, inline `UpgradePrompt`) over full-screen blocks for core orientation pages (e.g. Overview and main dashboards), keeping basic tool functionality available to Free users
- [ ] Gated experiences consistently use `UpgradePrompt.vue` for messaging so paywall UX is uniform across the app

---

### C17. [Product] Implement 2-tier pricing (Free + Pro) with Guests, Duo/Team collaboration paywall, and goal tracking paywall

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 5 points
**Depends on:** C2, C3, C7, C8, C10, C12, C13, C14, F11-social
**Labels:** `pricing`, `subscription`, `entitlements`, `backend`, `frontend`, `epic-c`

#### Description

Ship the updated monetization model in the product:

- **Only 2 tiers:** Free + Pro (remove Team tier from UI/backend logic)
- **Guests can create and join Duo/Team spaces**, but **Guests only see their own data** (plus upgrade nudges)
- **Pro unlocks collaboration** (shared goals + duo/team dashboards) and **goal setting/tracking**
- **Downgrade behavior:** if a Pro user downgrades, they become a Guest and must not break groups for others

This aligns with the product positioning: not a builds app; focused on champ-select personal matchup highlights + post-game takeaways + progress over time.

#### Database impact (analyze + implement if needed)

Evaluate the current schema and add migrations as required to support:

- A reliable source of truth for user access (e.g., `users.tier` = `free|pro`, subscription status/renewal metadata from C2)
- Duo/Team grouping and invites (if not already covered by existing social tables/endpoints):
  - Group entity (type: `duo|team`)
  - Membership table (user_id, group_id, role/created_by, joined_at, left_at)
  - Invite table (inviter, invitee/email, token, expires_at, accepted_at)
- Shared goals/collaboration artifacts (if not already planned elsewhere): shared goals + optional voting/comments metadata

#### Acceptance Criteria

**Backend**

- [ ] Tier model supports only **Free** and **Pro**; legacy "Team" references removed/mapped safely
- [ ] Feature gating implemented server-side for:
  - [ ] **Goal setting + tracking** (Pro)
  - [ ] **Duo/Team dashboards** (Pro)
  - [ ] **Shared goals / collaboration actions** (Pro)
- [ ] Guest access enforced: Guests in a group can only access **their own** data; no teammate stats are returned
- [ ] Downgrade does not break groups: Pro→Free transitions preserve memberships; user becomes Guest in groups
- [ ] API returns clear, consistent errors for gated features (e.g., `403` + error code like `TIER_REQUIRED`)

**Frontend**

- [ ] Pricing page updated to show **2 tiers** (Free + Pro) with gamer-language value props
- [ ] Upgrade prompts updated (remove "Upgrade to Team"); Pro is the only paid upgrade
- [ ] Users can create/join Duo/Team spaces as Guests
- [ ] In-group collaboration modules (team dashboard/shared goals/voting) show locked state + upgrade nudges for Guests
- [ ] Goal setting/tracking UX is paywalled cleanly for Free users with upgrade path to Pro
- [ ] Paywall UX for collaboration, goals, and AI features follows the shared patterns from C12/C14: non-blocking on core orientation flows (e.g. Overview, Solo dashboard) and implemented via consistent `UpgradePrompt` + inline locked states rather than full-screen walls

**Validation**

- [ ] Add/update tests for tier gating (guest vs pro) for at least one goal endpoint and one duo/team endpoint
- [ ] Verify that no endpoint leaks teammate data to Guests

---

### C15. [Service] Create founding member pricing

**Priority:** P2 - Medium
**Type:** Feature
**Estimate:** 2 points
**Depends on:** C5
**Labels:** `service`, `pricing`, `epic-c`

#### Description

Special pricing for first 100 users.

#### Acceptance Criteria

- [ ] Track founding member count
- [ ] Implement founding member discount logic (€2.99 forever) in subscription creation
- [ ] Auto-apply discount for qualifying users
- [ ] Display "X spots remaining" on pricing page
- [ ] Lock in price for founding members permanently

---

### C16. [Database] Create referral tracking

**Priority:** P3 - Low
**Type:** Feature
**Estimate:** 2 points
**Labels:** `database`, `referral`, `epic-c`

#### Description

Track referrals for future referral program.

#### Acceptance Criteria

- [ ] Create `Referral` table (ReferrerId, ReferredUserId, CreatedAt, ConvertedAt)
- [ ] Generate unique referral codes per user
- [ ] Track referral on signup
- [ ] Future: reward referrers with free weeks

---

# Epic D: Analytics & User Tracking

Track user behavior to inform product decisions.

<!-- AI: START_EPIC_D_TASKS -->

## Issues

### D10. [Frontend] Implement cookie consent & preferences

### D10. [Frontend] Implement cookie consent & preferences

**Priority:** P2 - Medium
**Type:** Feature
**Estimate:** 2 points
**Depends on:** D1, F7
**Labels:** `frontend`, `analytics`, `privacy`, `epic-d`

#### Description

Provide a cookie consent banner and preferences so users can control analytics cookies while keeping authentication/session cookies as strictly necessary.

#### Acceptance Criteria

- [ ] On first visit, show a cookie banner that explains the difference between strictly necessary cookies (e.g. auth/session, CSRF) and optional analytics cookies (PostHog or similar from D1)
- [ ] Banner offers at least "Accept all" and "Use only necessary cookies" actions; a "Customize" flow can be implemented as a simple preferences dialog or follow-up view
- [ ] Authentication/session cookies used for login (F7/F11) are treated as strictly necessary and remain enabled even when the user chooses "only necessary cookies"
- [ ] Analytics tracking code is only initialized after the user has granted consent for analytics cookies, and respects the stored preference on subsequent visits
- [ ] Cookie/consent preferences are stored (e.g. in a consent cookie or `localStorage`) and can be changed later via a "Cookie settings" link in the footer or account/settings area
- [ ] The implementation is wired into the analytics work from D1/D2 so that events are not sent when analytics cookies have been declined
- [ ] Consent UI is implemented as a non-blocking banner or slim sheet (not a full-screen modal) on first visit so the app retains its fast, tool-like feel and users can quickly reach core pages like Overview

---

### D11. [Research] Evaluate Betterlytics analytics platform

**Priority:** P2 - Medium
**Type:** Research
**Estimate:** 2 points
**Depends on:** D1
**Labels:** `analytics`, `evaluation`, `epic-d`

#### Description

Do a short, focused evaluation of Betterlytics as a potential analytics provider or complement to the current stack. The goal is to understand whether it solves real pain points (limits, pricing, features) compared to existing tools, not to fully migrate.

#### Acceptance Criteria

- [ ] Set up a small, non-production test project with Betterlytics (or go through an interactive demo) using a subset of mongoose.gg events
- [ ] Compare pricing, event limits, and key features (funnels, retention, user paths, etc.) against the current analytics setup
- [ ] Identify any hard blockers (e.g. lack of EU hosting, missing features we rely on) and any must-have advantages
- [ ] Produce a brief written recommendation (stay with current stack vs. pilot Betterlytics alongside it) including a rough estimate of events/month and expected cost
- [ ] Document the outcome in `docs/analytics/` and link from this task

---

<!-- AI: END_EPIC_D_TASKS -->

# Epic E: Database & Analytics Schema ✅ COMPLETE

Modernize the mongoose database to match `docs/database_schema.md` and support advanced solo/duo/team analytics.

> **All tasks completed (E1–E7) have been moved to [product_backlog_completed.md](./product_backlog_completed.md).**

---

# Epic F: API

Expose the HTTP API surface aligned with the database schema and dashboards.

> **Completed tasks (F1, F2, F6, F7, F11 core auth, F12, F13, F14) have been moved to [product_backlog_completed.md](./product_backlog_completed.md).**

<!-- AI: START_EPIC_F_TASKS -->

## Issues

### F3. [API] Implement Duo dashboard endpoint

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Depends on:** E3, E5, F1
**Labels:** `api`, `duo`, `epic-f`

#### Description

Create an endpoint that returns duo synergy stats, matchup data, shared objective participation and win rates.

#### Acceptance Criteria

- [ ] Endpoint implemented (e.g. `GET /api/v2/duo/dashboard/{userId}`)
- [ ] Returns per-duo aggregates needed for the Duo dashboard
- [ ] Uses duo-related tables/metrics from database
- [ ] Supports optional queue filtering via the standardized queue filter

---

### F4. [API] Implement Team dashboard endpoint

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 3 points
**Depends on:** E3, E5, F1
**Labels:** `api`, `team`, `epic-f`

#### Description

Create an endpoint that exposes team-level metrics (games played, win rate, queue type, gold leads, role composition, champion combos, role pair effectiveness).

#### Acceptance Criteria

- [ ] Endpoint implemented (e.g. `GET /api/v2/team/dashboard/{userId}`)
- [ ] Returns all data needed by the Team dashboard
- [ ] Uses team-related tables/metrics from database
- [ ] Supports optional queue filtering via the standardized queue filter

---

### F5. [API] Implement AI snapshot/goal input endpoint

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 3 points
**Depends on:** E5, B1, F1
**Labels:** `api`, `ai`, `epic-f`

#### Description

Expose an endpoint that aggregates player stats into an AI-friendly snapshot (`ai_snapshots`), to be consumed by the AI goal recommendation flow (Epic B).

#### Acceptance Criteria

- [ ] Endpoint implemented (e.g. `POST /api/v2/ai/snapshot`)
- [ ] Returns or stores an `ai_snapshots` record for the requested context (solo/duo/team)
- [ ] Contracts align with `IPlayerStatsAggregator` and `IGoalRecommendationService`

---

### F8. [API] Implement unified error handling & problem responses

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 3 points
**Depends on:** F1
**Labels:** `api`, `error-handling`, `epic-f`

#### Description

Provide consistent, user-friendly error responses from the API surface and avoid leaking implementation details.

#### Acceptance Criteria

- [ ] Add global exception handling middleware/filter that returns a standard error shape (e.g. RFC 7807-style problem details)
- [ ] Map validation and domain errors to 4xx responses with clear, structured error information
- [ ] Map unexpected failures to 5xx responses with a generic message and correlation id
- [ ] Integrate with error tracking (Epic D7) so important failures are logged with context
- [ ] Update a representative set of endpoints to use the standardized error patterns

---


### F10. [Reliability] Audit async methods for CancellationToken usage

**Priority:** P2 - Medium
**Type:** Chore
**Estimate:** 3 points
**Labels:** `reliability`, `async`, `epic-f`

#### Description

Ensure all important async operations in the backend respect `CancellationToken` so requests and background jobs can be cancelled cleanly.

#### Acceptance Criteria

- [ ] Identify public async methods in the Application, Endpoints, and Infrastructure layers
- [ ] Add `CancellationToken` parameters where missing and thread them through to HTTP and database calls
- [ ] Wire up tokens from ASP.NET request pipeline and background job scheduler
- [ ] Add at least a couple of tests that verify cancellation is honored for long-running operations

---

### F11-social. [API] Implement social endpoints (friends, teams, user search)

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 3 points
**Depends on:** F11 (core auth - completed)
**Labels:** `api`, `social`, `users`, `epic-f`

#### Description

Provide API endpoints for managing friends/duos/teams and searching for LoL accounts. This is the remaining work from F11 after core auth was completed.

#### Acceptance Criteria

- [ ] Provide endpoints to manage friends / duo partners and team members (e.g. add/remove friends, manage team roster)
- [ ] Provide a user search endpoint that lets you look up LoL accounts by Riot ID / game name + tagline when creating or linking a user
- [ ] All new endpoints are protected by session authentication and follow the unified error-handling conventions

---

### F15. [Bug] Preserve username casing while keeping login case-insensitive

**Priority:** P2 - Medium
**Type:** Bug
**Estimate:** 1 point
**Labels:** `api`, `auth`, `users`, `epic-f`

#### Description

Usernames are currently being lowercased so a user who signs up as `DoendW` is shown as `doendw` in the UI. We want to preserve the exact casing as entered when storing and displaying the username, while still treating login and uniqueness checks as case-insensitive.

#### Acceptance Criteria

- [ ] Usernames are stored with the exact casing entered at signup (e.g. `DoendW` remains `DoendW` in the database and UI)
- [ ] Login remains case-insensitive: entering `doendw` or `DOENDW` still logs in the same user account
- [ ] Uniqueness checks for usernames remain case-insensitive so `DoendW` and `doendw` cannot coexist as separate accounts
- [ ] Existing users with lowercased display names are either migrated or handled so they see their preferred casing going forward (with a reasonable default for those who don't care)
- [ ] Add or update tests around signup/login and username normalization rules

---

### F16. [Chore] Rename RiotProxy backend to Mongoose.Api ✅

**Priority:** P3 - Low
**Type:** Chore
**Estimate:** 3 points
**Status:** Complete
**Labels:** `backend`, `naming`, `maintenance`, `epic-f`

#### Description

Standardize the backend naming from "RiotProxy" to "Mongoose.Api" across the solution so the project name matches the product branding and reduces confusion in logs, deployments, and documentation.

#### Acceptance Criteria

- [ ] Rename the main backend project/assembly from RiotProxy to Mongoose.Api (or equivalent) in the .NET solution
- [ ] Update namespaces, configuration, and startup code references where needed so the app still builds and runs
- [ ] Update scripts, deployment configs, and documentation that reference the old RiotProxy name
- [ ] Ensure logging/telemetry identifiers reflect the new name where appropriate
- [ ] All backend tests still pass after the rename

---

### F17. [API] Implement feedback endpoint and GitHub integration ✅

**Priority:** P2 - Medium
**Type:** Feature
**Estimate:** 5 points
**Status:** Complete
**Depends on:** F1, F8
**Labels:** `api`, `feedback`, `github`, `epic-f`

#### Description

Secure backend endpoint (`POST /api/v2/feedback`) that accepts structured in-app feedback (bugs and feature requests) and creates corresponding GitHub issues in the internal backlog repository using server-side credentials. Includes validation, error handling per F8, and 12 unit tests covering happy paths and failure scenarios.

<!-- AI: END_EPIC_F_TASKS -->


# Epic G: Frontend App & Marketing

Create a professional user experience with a landing page, pricing, and app shell consuming the API.

> **Completed tasks (G1, G2, G3, G4, G5a, G5b0-G5b3, G5b8-G5b12, G5b16, G5b17, G9, G12, G13) have been moved to [product_backlog_completed.md](./product_backlog_completed.md).**

### Architecture Notes
- **Framework**: Vue 3 + Vite application in `/client/` directory
- **Style direction**: Vercel developer aesthetic (dark, sharp, neon-tinged) with theme tokens configurable via CSS variables
- **Scope**: Marketing landing page + app shell + solo experience first; duo/team dashboards follow once solo is stable in production.

<!-- AI: START_EPIC_G_TASKS -->

## G5 Epic: Frontend Solo Dashboard

> **Solo v1 MVP scope:** Summary Stats Card (G5b18), LP Trend Chart (G5b5), Winrate Trend Chart (G5b4). These 3 components answer the core user questions: "Am I climbing?", "Am I improving?", "How's my season going?"
>
> **Layout:** Summary stats (full width, lightweight context row) → LP chart (left) + Winrate chart (right) side-by-side, both defaulting to last 20 games with an expand button to view the full season. Charts switch data range within the same half-width space (no modal, no full-width expansion). Uses shared `AnalysisLayout.vue` zone system (G5b19).
>
> **Architecture:** Solo, Duo, and Team are **separate pages** with distinct routes (`/app/solo`, `/app/duo`, `/app/team`). Duo and Team are gated behind Pro tier. All three use the same zone-based layout component but fill zones with context-specific content.
>
> **Solo v2 (deferred):** Goals Panel (G5b7), Matchups Table (G5b6), Performance by Phase, Danger Zones Map, Main Champions Card on Solo (stays on Champion Select).

---

### G5b18. [Frontend] Implement Summary Stats Card

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 1 point
**Depends on:** G5b1, F2
**Labels:** `frontend`, `solo`, `dashboard`, `component`, `epic-g`, `solo-v1-mvp`

#### Description

Create a Summary Stats Card that provides quick orientation on the Solo dashboard. This is a 5-second scan component answering "How am I doing overall?"

The component displays key aggregate stats from the existing Solo performance endpoint. No new backend work required.

#### Acceptance Criteria

- [ ] Component created at `client/src/components/solo/SummaryStatsCard.vue`
- [ ] Displays the following stats in a compact, scannable layout:
  - Games Played (total count)
  - Winrate (percentage with color coding: green ≥52%, yellow 48-52%, red <48%)
  - Average KDA (formatted as X.XX)
- [ ] Uses `BaseCard` component following `docs/ui-ux/ui-design-guidelines.md`
- [ ] Responsive: stats stack on mobile, inline on desktop
- [ ] Loading state with skeleton placeholders
- [ ] Empty state when no games played: "No games found for this filter"
- [ ] Reacts to queue filter and time range filter changes
- [ ] Unit tests covering component rendering and edge cases

---

### G5b4. [Frontend + Backend] Winrate Over Time Chart

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points (3 FE + 2 BE via G5b13)
**Depends on:** G5b1, G5b19 (AnalysisLayout), G5b13 (winrate trend data)
**Labels:** `frontend`, `backend`, `solo`, `dashboard`, `chart`, `epic-g`, `solo-v1-mvp`

#### Description

Create a Winrate Over Time chart answering "Am I improving?" Displays rolling average winrate as a line chart. Placed **side-by-side with the LP chart** (right half of Zone 3) in the shared `AnalysisLayout` zone system.

Defaults to **last 20 games** for quick momentum reading. Includes an expand button that switches the data range to the full season within the same half-width space (no modal or full-width expansion).

The chart component is reusable across Solo, Duo, and Team pages.

#### Acceptance Criteria

- [ ] Component created at `client/src/components/solo/WinrateChart.vue`
- [ ] Renders a Chart.js line chart showing rolling average winrate over games
- [ ] Default data range: last 20 games
- [ ] Expand button in chart header switches to full season data (all available games)
- [ ] Data range switch happens within the same half-width space (Option C) – no modal, no full-width expansion
- [ ] Chart readable at half-width (side-by-side with LP chart in a 2-column grid)
- [ ] Winrate axis shows percentage (0–100% or contextual range)
- [ ] Color coding: green trend line when winrate ≥ 52%, red when < 48%, neutral otherwise
- [ ] Supports `matchId` query parameter to highlight a specific game point (for "View Analysis" from Matches page)
- [ ] Reacts to queue filter and time range filter changes
- [ ] Loading state with skeleton placeholder
- [ ] Empty state when no data available
- [ ] Unit tests covering rendering, data range switching, and highlight behavior

---

### G5b5. [Frontend] LP Over Time Chart

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** G5b1, G5b19 (AnalysisLayout), G5b14 (LP trend data)
**Labels:** `frontend`, `solo`, `dashboard`, `chart`, `epic-g`, `solo-v1-mvp`

#### Description

Create an LP Over Time chart answering "Am I climbing?" Displays LP progression as a line chart with promotion/demotion markers. Placed **side-by-side with the Winrate chart** (left half of Zone 3) in the shared `AnalysisLayout` zone system.

Defaults to **last 20 games** for quick momentum reading. Includes an expand button that switches the data range to the full season within the same half-width space (no modal or full-width expansion).

The chart component is reusable across Solo and Duo pages (Team may use a winrate-only variant).

#### Acceptance Criteria

- [ ] Component created at `client/src/components/solo/LpTrendChart.vue`
- [ ] Renders a Chart.js line chart showing LP progression over games
- [ ] Default data range: last 20 games
- [ ] Expand button in chart header switches to full season data (all available games)
- [ ] Data range switch happens within the same half-width space (Option C) – no modal, no full-width expansion
- [ ] Chart readable at half-width (side-by-side with Winrate chart in a 2-column grid)
- [ ] Promotion markers (visual indicator when player promotes)
- [ ] Demotion markers (visual indicator when player demotes)
- [ ] Win/loss coloring on data points (green dot = win, red dot = loss)
- [ ] Supports `matchId` query parameter to highlight a specific game point (for "View Analysis" from Matches page)
- [ ] Reacts to queue filter and time range filter changes
- [ ] Loading state with skeleton placeholder
- [ ] Empty state when no data available
- [ ] Unit tests covering rendering, data range switching, promotion/demotion markers, and highlight behavior

---

### G5b19. [Frontend] Create AnalysisLayout zone component

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** G5b1
**Labels:** `frontend`, `layout`, `shared`, `epic-g`, `solo-v1-mvp`

#### Description

Create a shared `AnalysisLayout.vue` component that defines the zone-based layout structure used by Solo, Duo, and Team analysis pages. Each page fills the zones with context-specific content via named slots.

**Zone model:**

| Zone | Purpose | v1 | v2 |
|------|---------|----|----|
| Zone 1 | Context bar (filters, time range) | ✅ Queue toggle + time range | Same |
| Zone 2 | Summary stats (lightweight context row) | ✅ Games, Winrate, KDA | Per-context stats |
| Zone 3 | Trend charts (2-column grid) | ✅ LP chart (left) + Winrate chart (right) | Same |
| Zone 4 | Deep analysis | Not rendered | Danger Zones, Champion Matrix |
| Zone 5 | Goals | Not rendered | Active goals with progress |

Zones 4 and 5 are defined as slots but not rendered in v1 (no content). The layout component handles the zone spacing, grid structure, and responsive behavior.

#### Acceptance Criteria

- [ ] Component created at `client/src/components/shared/AnalysisLayout.vue`
- [ ] Defines 5 named slots: `context-bar`, `summary`, `trend-charts`, `deep-analysis`, `goals`
- [ ] `trend-charts` slot renders as a 2-column CSS grid (equal width)
- [ ] Zones 4 and 5 only render when slot content is provided (conditional rendering)
- [ ] Consistent spacing between zones using design tokens
- [ ] Supports `matchId` prop for "View Analysis" match-highlight mode (passed to child components)
- [ ] Used by `SoloPage.vue` in v1; will be used by `DuoPage.vue` and `TeamPage.vue` when implemented
- [ ] Unit tests covering slot rendering and conditional zone visibility

---

### G5b7. [Frontend] Implement Goals Panel (basic display)

**Priority:** P2 - Medium (deferred to Solo v2)
**Type:** Feature
**Estimate:** 2 points
**Depends on:** G5b1, F2, B9 (goals CRUD endpoints)
**Labels:** `frontend`, `solo`, `dashboard`, `component`, `epic-g`, `solo-v2`

#### Description

> **Note:** Deferred to Solo v2. Depends on goals system (B9) which is not yet implemented.

Create a Goals Panel that displays active goals (if Pro tier) or shows an upgrade CTA (if Free tier). Goals show progress bar, current value, target value, and an estimated completion date. "Set New Goal" button opens a modal (future task). No create/edit/delete logic yet, just display.

#### Backend Requirements

- [ ] Solo dashboard endpoint (F2) should return `activeGoals` array (empty if Free tier):
  - `goalId`, `title`, `description`, `metric`, `currentValue`, `targetValue`, `baselineValue`
  - `progress` (percentage), `estimatedCompletionDate`

---

### G21. [Frontend] Add in-app Feedback page and sidebar entry ✅

**Priority:** P2 - Medium
**Type:** Feature
**Estimate:** 5 points
**Status:** Complete
**Depends on:** G2, F17
**Labels:** `frontend`, `feedback`, `forms`, `epic-g`

#### Description

In-app feedback page at `/app/feedback` with bug/feature request forms, inline validation, and integration with F17 backend endpoint. Includes sidebar navigation entry, success/error handling, and responsive design following UI guidelines.

---

### G6. [Frontend] Implement Duo Dashboard Page (Separate Gated Page)

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 5 points
**Depends on:** G5b19 (AnalysisLayout), F3 (Duo dashboard endpoint), C9 (feature gate middleware), C12 (upgrade prompt)
**Labels:** `frontend`, `duo`, `dashboard`, `gated`, `epic-g`

#### Description

Create a dedicated Duo dashboard page at `/app/duo` as a **separate route** (not a tab or toggle on the Solo page). This page is gated behind Pro tier for full access.

Uses the shared `AnalysisLayout.vue` zone component (G5b19) and fills zones with duo-specific content: pair summary stats, LP/winrate trends for the duo, and (v2) Champion Matrix + Danger Zones with multi-player color coding.

**Free user experience:** When a free user navigates to `/app/duo`, they see a preview/teaser page with a description of duo analysis features and a clear upgrade CTA. They do NOT see a blank wall or a generic 403.

**Architecture decision:** Separate pages (Solo/Duo/Team) were chosen over a shared page with toggles for three reasons:
1. Better perceived value when upgrading ("You get Duo Analysis and Team Analysis" vs "You get two extra tabs")
2. Content will diverge significantly in v2 (Champion Matrix, team comp patterns)
3. Cleaner gating UX – locked page with preview is a well-understood pattern

#### Acceptance Criteria

**Page & Routing**
- [ ] Page created at `client/src/views/DuoPage.vue`
- [ ] Route registered at `/app/duo` with `meta: { requiresPro: true }`
- [ ] Sidebar shows "Duo" as a separate top-level nav item (not under Analysis submenu)
- [ ] Sidebar shows a lock/badge icon on Duo for free users

**Free User (Gated) View**
- [ ] Free users see a preview/teaser page (not a 403 or blank wall)
- [ ] Preview describes duo analysis features: pair synergy, champion combos, shared improvement tracking
- [ ] Clear upgrade CTA with link to pricing/upgrade page
- [ ] Preview may include blurred/dimmed mockup of what the page looks like with data

**Pro User View**
- [ ] Uses `AnalysisLayout.vue` zone system
- [ ] Zone 1: Queue toggle + time range filter
- [ ] Zone 2: Pair summary stats (games played together, winrate, KDA per player)
- [ ] Zone 3: LP chart (left) + Winrate chart (right), scoped to duo games, last 20 games default
- [ ] Zones 4-5: Not rendered in v1 (Champion Matrix and Danger Zones are v2)
- [ ] Supports `matchId` query parameter for "View Analysis" from Matches page

**v2 placeholders (not implemented in v1)**
- Champion Matrix: which champion combos the duo plays and their success rates
- Danger Zones: death heatmap with both players in different colors

---

### G7. [Frontend] Implement Team Dashboard Page (Separate Gated Page)

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 5 points
**Depends on:** G5b19 (AnalysisLayout), F4 (Team dashboard endpoint), C9 (feature gate middleware), C12 (upgrade prompt)
**Labels:** `frontend`, `team`, `dashboard`, `gated`, `epic-g`

#### Description

Create a dedicated Team dashboard page at `/app/team` as a **separate route** (not a tab or toggle). This page is gated behind Pro tier for full access.

Uses the shared `AnalysisLayout.vue` zone component (G5b19) and fills zones with team-specific content: team summary stats, winrate trends, and (v2) team composition patterns + Danger Zones with multi-player color coding.

**Free user experience:** Same pattern as Duo – preview/teaser page with upgrade CTA.

#### Acceptance Criteria

**Page & Routing**
- [ ] Page created at `client/src/views/TeamPage.vue`
- [ ] Route registered at `/app/team` with `meta: { requiresPro: true }`
- [ ] Sidebar shows "Team" as a separate top-level nav item (not under Analysis submenu)
- [ ] Sidebar shows a lock/badge icon on Team for free users

**Free User (Gated) View**
- [ ] Free users see a preview/teaser page (not a 403 or blank wall)
- [ ] Preview describes team analysis features: team synergy, composition patterns, shared improvement tracking
- [ ] Clear upgrade CTA with link to pricing/upgrade page

**Pro User View**
- [ ] Uses `AnalysisLayout.vue` zone system
- [ ] Zone 1: Queue toggle + time range filter
- [ ] Zone 2: Team summary stats (games played, winrate, KDA per player)
- [ ] Zone 3: Winrate chart (may be single full-width if LP chart doesn't apply to team context)
- [ ] Zones 4-5: Not rendered in v1 (team comp patterns and Danger Zones are v2)
- [ ] Supports `matchId` query parameter for "View Analysis" from Matches page

**v2 placeholders (not implemented in v1)**
- Team composition patterns: which team comps work best
- Danger Zones: death heatmap with all players in different colors
- Role pair effectiveness

---

<!-- AI: END_EPIC_G_TASKS -->

# Summary

## All Issues by Priority

### P0 - Critical (MVP)

| ID | Title | Epic | Points |
|----|-------|------|--------|
| B1 | Create LLM provider abstraction | AI Goals | 3 |
| B2 | Implement OpenAI client | AI Goals | 2 |
| B4 | Create Goal database tables | AI Goals | 2 |
| B5 | Create player stats aggregator | AI Goals | 3 |
| B6 | Create goal prompt builder | AI Goals | 2 |
| B7 | Create goal recommendation service | AI Goals | 3 |
| B8 | Create recommendation endpoint | AI Goals | 2 |
| C1 | Set up Mollie integration | Subscription | 3 |
| C2 | Create subscription tables | Subscription | 2 |
| C4 | Create Mollie customer service | Subscription | 2 |
| C5 | Create subscription management service | Subscription | 3 |
| C6 | Create Mollie webhook handler | Subscription | 3 |
| C7 | Create subscription endpoints | Subscription | 2 |
| C8 | Create feature flag service | Subscription | 2 |
| F3 | Implement Duo dashboard endpoint | API | 3 |
| G5b4 | Winrate Over Time chart + trend data (FE+BE) | Frontend / API | 5 |
| G5b5 | LP Over Time chart (frontend UI) | Frontend | 2 |
| G5b18 | Summary Stats Card | Frontend | 1 |
| G5b19 | AnalysisLayout zone component | Frontend | 2 |
| G15 | Allow cancelling or switching account during email verification | Frontend | 2 |

**P0 Remaining Total:** 49 points

### P1 - High

| ID | Title | Epic | Points |
|----|-------|------|--------|
| B3 | Add LLM rate limiting | AI Goals | 2 |
| B9 | Create goal CRUD endpoints | AI Goals | 2 |
| B10 | Create progress tracking service | AI Goals | 3 |
| B11 | Create progress update job | AI Goals | 2 |
| B12 | Create progress endpoint | AI Goals | 1 |
| B18 | Add rules-of-climbing domain context for recommendations | AI Goals | 2 |
| B19 | Clarify AI goals vs coaching product direction | AI Goals | 3 |
| B21 | Extend database for AI coaching metrics | AI Goals | 5 |
| C9 | Create feature gate middleware | Subscription | 2 |
| C12 | Create upgrade prompt component | Subscription | 2 |
| C14 | Gate features based on tier | Subscription | 2 |
| C17 | Implement 2-tier pricing (Free + Pro) + Guests + collaboration/goal paywalls | Subscription | 5 |
| D1 | Set up analytics provider | Analytics | 2 |
| D2 | Implement core tracking events | Analytics | 3 |
| D3 | Track page views and sessions | Analytics | 1 |
| F4 | Implement Team dashboard endpoint | API | 3 |
| F5 | Implement AI snapshot/goal input endpoint | API | 3 |
| F8 | Implement unified error handling & problem responses | API | 3 |
| F9 | Add backend tests with focus on security | API | 3 |
| F11-social | Implement social endpoints (friends, teams, search) | API | 3 |
| F13-lp | Implement Riot League API for rank/LP data | API | 5 |
| F14-login | Check for new matches on user login and auto-sync | API | 3 |
| G5b13 | Fetch winrate trend data for Solo dashboard | Backend | 2 |
| G5b14 | Fetch LP trend data for Solo dashboard | Backend | 2 |
| G6 | Implement Duo Dashboard Page (Separate Gated Page) | Frontend | 5 |
| G7 | Implement Team Dashboard Page (Separate Gated Page) | Frontend | 5 |
| G10 | Implement user dropdown details & account settings page | Frontend | 8 |
| G19 | Implement session expiry handling (global handler + UX) | Frontend | 5 |

**P1 Remaining Total:** 85 points

### P2 - Medium

| ID | Title | Epic | Points |
|----|-------|------|--------|
| B13 | Create goal recommendation UI | AI Goals | 3 |
| B14 | Create active goals display | AI Goals | 3 |
| B15 | Create goal progress chart | AI Goals | 2 |
| B20 | Post-game AI feedback (Pro, on-demand) | AI Goals | 5 |
| C15 | Create founding member pricing | Subscription | 2 |
| D4 | Server-side event tracking | Analytics | 2 |
| D5 | Create key dashboards | Analytics | 2 |
| D6 | Create internal metrics endpoint | Analytics | 2 |
| D7 | Set up error tracking | Analytics | 2 |
| D9 | Show login activity heatmap on user page | Analytics | 3 |
| D10 | Implement cookie consent & preferences | Analytics | 2 |
| D11 | Evaluate Betterlytics analytics platform | Analytics | 2 |
| F10 | Audit async methods for CancellationToken usage | API | 3 |
| F15 | Preserve username casing while keeping login case-insensitive | API | 1 |
| G8 | Remove legacy dashboard views & routes | Frontend | 1 |
| G11 | Implement friends management UI scaffolding | Frontend | 3 |
| G17 | Design and implement manual match refresh entry point | Frontend | 2 |
| G18 | Multi-account Riot support & aggregated stats | Frontend | 5 |
| G5b6 | Champion matchups table + endpoint (FE+BE) | Frontend / API | 6 |
| G5b7 | Goals panel + goals data on Solo dashboard (FE+BE) | Frontend / Backend | 4 |
| G5b15 | Goals array in Solo endpoint | Backend | 2 |

**P2 Remaining Total:** 57 points

### P3 - Low

| ID | Title | Epic | Points |
|----|-------|------|--------|
| B16 | Implement Anthropic client | AI Goals | 2 |
| B17 | Conversational follow-up | AI Goals | 5 |
| C16 | Create referral tracking | Subscription | 2 |
| D8 | Implement A/B testing | Analytics | 2 |
| G16 | Improve Match narrative header spacing and "You" button | Frontend | 1 |
| G20 | Add optional pre-expiry session warning toast | Frontend | 2 |

**P3 Remaining Total:** 14 points

## Summary of Completed Work

| Epic | Task | Points | Completed |
|------|------|--------|-----------|
| C | C3 - Add tier column to User | 1 | ✅ |
| C | C10 - Add tier info to user endpoints | 1 | ✅ |
| C | C11 - Create subscription status component | 2 | ✅ |
| C | C13 - Create pricing page | 3 | ✅ |
| E | E1 - Database schema & DDL | 3 | ✅ |
| E | E2 - MySQL schema scripts | 2 | ✅ |
| E | E3 - Entities and repositories | 3 | ✅ |
| E | E4 - Match & participant ingestion | 3 | ✅ |
| E | E5 - Timeline & derived metrics ingestion | 5 | ✅ |
| E | E6 - Validate database metrics against Riot | 2 | ✅ |
| E | E7 - Remove legacy database tables and repositories | 2 | ✅ |
| F | F1 - API surface design | 2 | ✅ |
| F | F2 - Solo dashboard endpoint | 3 | ✅ |
| F | F6 - Deprecate or migrate legacy endpoints | 2 | ✅ |
| F | F7 - Session authentication | 3 | ✅ |
| F | F11 - User auth endpoints (core) | 5 | ✅ |
| F | F12 - Riot account linking endpoints | 5 | ✅ |
| F | F13 - WebSocket endpoint for sync progress | 5 | ✅ |
| F | F14 - Match History Sync Job | 8 | ✅ |
| G | G1 - App IA & routes | 2 | ✅ |
| G | G2 - App shell & navigation | 3 | ✅ |
| G | G3 - Implement new public landing page | 2 | ✅ |
| G | G4 - Implement pricing page | 2 | ✅ |
| G | G5a - Dashboard Hub design | 2 | ✅ |
| G | G5b0 - Solo Dashboard design | 2 | ✅ |
| G | G5b1 - Create empty Solo dashboard view & routing | 1 | ✅ |
| G | G5b2 - Profile header button + profile data (FE+BE) | 5 | ✅ |
| G | G5b3 - Main Champion Card (FE+BE) | 3 | ✅ |
| G | G5b8 - Add profile_icon_id and summoner_level to riot_accounts | 1 | ✅ |
| G | G5b9 - Fetch and store profile data during account linking | 2 | ✅ |
| G | G5b10 - Update User dashboard endpoint with profile data | 1 | ✅ |
| G | G5b11 - Create champion matchups endpoint | 3 | ✅ |
| G | G5b12 - Fetch main champions by role for Solo dashboard | 2 | ✅ |
| G | G5b16 - Update database on login (FE+BE) | 2 | ✅ |
| G | G5b17 - Implement ranked data display in ProfileHeaderCard (FE+BE) | 5 | ✅ |
| G | G9 - Login, signup, verification & user shell | 5 | ✅ |
| G | G12 - Riot account linking on `/app/user` | 5 | ✅ |
| G | G13 - Real-time match sync progress via WebSocket | 5 | ✅ |
| G | G14b - OverviewPlayerHeader component | 1 | ✅ |
| G | G14c - RankSnapshot component | 2 | ✅ |
| G | G14d - LastMatchCard component | 1 | ✅ |
| G | G14g - AnalysisStatusCard component and persisted analysis status | 2 | ✅ |
| G | G14h - Add global "analysis in progress" sidebar indicator | 1 | ✅ |
| G | G14i - ChampionSelectCTA card on Overview | 1 | ✅ |
| G | G14j - Restructure Overview page layout for new components | 2 | ✅ |
| F | F16 - Rename RiotProxy backend to Mongoose.Api | 3 | ✅ |
| F | F17 - Implement feedback endpoint & GitHub integration | 5 | ✅ |
| G | G21 - In-app Feedback page & sidebar entry | 5 | ✅ |

**Total Completed Points:** 187

## Grand Totals

| Category | Points |
|----------|--------|
| **Remaining** | 206 pts |
| **Completed** | 135 pts |
| **Grand Total** | 341 pts |

---

## Recommended Sprint Plan

### Sprint 0: Platform Foundation ✅ COMPLETE
**Focus:** Database + API + Solo dashboard + Auth + Account Linking
**Points:** ~58 (completed)

- ✅ E1, E2, E3 (Database schema & repositories)
- ✅ E4, E5, E6, E7 (Ingestion: matches, participants, timeline & metrics + validation + cleanup)
- ✅ F1, F2 (API design + Solo dashboard endpoint)
- ✅ G1, G2, G5a-G5b3 (App IA, shell, Solo dashboard design + profile header + main champions)
- ✅ G9 (User login, signup, verification & `/app/user` shell)
- ✅ F12, F13, F14 (Riot account linking + WebSocket sync + sync job)
- ✅ G12, G13 (Riot account linking UI + real-time sync progress)
- ✅ G5b8-G5b12 (Profile data storage, fetching, endpoints, matchups, main champions)

### Sprint 1: Solo v1 MVP
**Focus:** Ship core Solo dashboard answering "Am I climbing? Am I improving?"
**Points:** ~10

- G5b19 (AnalysisLayout zone component) - 2 pts
- G5b18 (Summary Stats Card) - 1 pt
- G5b4, G5b5 (Winrate chart + LP chart, side-by-side, last 20 games default) - 7 pts
- G5b13, G5b14 (Backend: winrate trend, LP trend data) - 4 pts

> **Layout:** Summary stats (full width) → LP chart (left) + Winrate chart (right) side-by-side. Charts default to last 20 games with expand button to view full season (Option C: data range switch within same half-width space).
>
> **Deferred to Solo v2:** G5b3 (Main Champions Card – stays on Champion Select page only), G5b6 (Matchups table), G5b7 (Goals panel), G5b15 (Goals data). These require goals system (B9) or add complexity without addressing core user questions.

### Sprint 2: Subscriptions (P0 Payments)
**Focus:** Mollie + payment flow
**Points:** ~20

- C1, C2, C4, C5, C6, C7, C8 (Mollie + subscription services + endpoints)
- B1, B2 (LLM abstraction)

### Sprint 3: AI Goals MVP (P0 AI)
**Focus:** AI recommendations working
**Points:** ~14

- B4 (Goal tables)
- B5, B6, B7, B8 (AI goal flow)

### Sprint 4: Polish (P1)
**Focus:** Feature gates + analytics
**Points:** ~18

- C9, C12, C14, C17 (Frontend subscription + 2-tier pricing)
- D1, D2, D3 (Analytics)
- B3 (LLM rate limiting)

### Sprint 5: Goal Tracking (P1)
**Focus:** Complete goal lifecycle
**Points:** ~16

- B9, B10, B11, B12 (Goal CRUD + progress)
- D4, D5, D6, D7 (Server analytics + monitoring)

### Sprint 6+: Enhancements (P2/P3)
**Focus:** UI polish, advanced features
**Points:** ~30

- Remaining P2 and P3 items

---

## Quick Reference: File Locations

| Area | Location |
|------|----------|
| Entities | `server/Infrastructure/External/Domain/Entities/` |
| Repositories | `server/Infrastructure/External/Database/Repositories/` |
| Endpoints | `server/Application/Endpoints/` |
| Services | `server/Application/Services/` (create) |
| DTOs | `server/Application/DTOs/` |
| Vue Components | `client/src/components/` |
| Vue Views | `client/src/views/` |
| Composables | `client/src/composables/` |
| API Client | `client/src/api/` |

---

## G5 Epic: Task Dependency Graph & Implementation Timeline

Below is a visual guide to dependencies and recommended implementation order for G5:

```
┌─────────────────────────────────────────────────────────┐
│ PHASE 0: DESIGN (No code, enables all other tasks)      │
├─────────────────────────────────────────────────────────┤
│ G5a: Dashboard Hub Design ──┐                           │
│ G5b0: Solo Dashboard Design │                           │
└──────────────┬──────────────┴──────────────┬────────────┘
               │                             │
┌──────────────▼──────────────────┐ ┌────────▼─────────────┐
│ PHASE 1: INFRASTRUCTURE         │ │ PHASE 1B: DATABASE   │
├──────────────────────────────────┤ ├─────────────────────┤
│ G5b1: Empty Solo Dashboard View  │ │ G5b8: Add Columns   │
│     └─ Depends on: G2, G5b0      │ │    to riot_accounts │
│                                  │ │                     │
│ G5b2: Profile Header Card        │ │ G5b9: Fetch Profile │
│     ├─ Depends on: G5b1, F2      │ │    Data at Linking  │
│     └─ Blocks: UI visible        │ │     Depends on: G5b8│
│                                  │ │                     │
│ G5b3: Main Champion Card (roles) │ │ G5b10: Update F2    │
│     ├─ Depends on: G5b1, F2      │ │   Endpoint with     │
│     └─ Blocks: UI visible        │ │   Profile Fields    │
│                                  │ │  Depends on: G5b9   │
│ G5b4: Winrate Over Time Chart    │ │                     │
│     ├─ Depends on: G5b1, F2      │ └─────────────────────┘
│     ├─ Reusable for Duo/Team     │
│     └─ Blocks: Chart visible     │
│                                  │
│ G5b5: LP Over Time Chart         │
│     ├─ Depends on: G5b1, F2      │
│     └─ Blocks: Chart visible     │
│                                  │
│ G5b6: Champion Matchups Table    │
│     ├─ Depends on: G5b1, G5b11   │
│     └─ Blocks: Table visible     │
│                                  │
│ G5b7: Goals Panel (display)      │
│     ├─ Depends on: G5b1, F2      │
│     └─ Blocks: Goals visible     │
└──────────────────────────────────┘

┌──────────────────────────────────────┐
│ PHASE 2: BACKEND ENDPOINTS           │
├──────────────────────────────────────┤
│ G5b11: Champion Matchups Endpoint    │
│     ├─ Depends on: E3               │
│     └─ Enables: G5b6                │
│                                      │
│ G5b12: Main Champions by Role       │
│     ├─ Depends on: E3, F2           │
│     └─ Enables: G5b3 to show data   │
│                                      │
│ G5b13: Winrate Trend Data           │
│     ├─ Depends on: E3, F2           │
│     └─ Enables: G5b4 to show data   │
│                                      │
│ G5b14: LP Trend Data                │
│     ├─ Depends on: E3, F2           │
│     └─ Enables: G5b5 to show data   │
│                                      │
│ G5b15: Goals Array in Solo Endpoint │
│     ├─ Depends on: F2, B4           │
│     └─ Enables: G5b7 to show data   │
└──────────────────────────────────────┘

PARALLEL WORK STREAMS:
─────────────────────
Frontend & Backend can work in parallel:
  • Design team → G5a, G5b0 (blocking all others)
  • Backend team → G5b8, G5b9, G5b10, G5b11-b15 (can start immediately)
  • Frontend team → G5b1 (needs G5b0), then G5b2-b7 (can start once backend endpoints ready)

CRITICAL PATH (for fastest delivery):
────────────────────────────────────
G5a → G5b0 → G5b1 → G5b2 → [parallel: G5b3-b7] + [parallel: G5b8-b15]
```

### Recommended Implementation Timeline

**Week 1: Design Phase**
- G5a & G5b0: Design specifications created, reviewed, and approved
- Output: Component breakdown, data shapes, responsive mockups

**Week 2: Infrastructure & Setup**
- Backend: G5b8, G5b9, G5b10 (database columns, profile data fetching, endpoint updates)
- Frontend: G5b1 (empty Solo dashboard view + routing)
- Output: Foundation for data display

**Week 3: Core Components**
- Frontend: G5b2 (profile header), G5b3 (main champions card)
- Backend: G5b11, G5b12 (matchups endpoint, main champions aggregation)
- Output: User profile data and champion performance visible

**Week 4: Charts & Analytics**
- Frontend: G5b4 (winrate chart), G5b5 (LP chart)
- Backend: G5b13, G5b14 (winrate and LP trend data endpoints)
- Output: Trend analysis visible on dashboard

**Week 5: Tables & Features**
- Frontend: G5b6 (matchups table), G5b7 (goals panel)
- Backend: G5b15 (goals data in endpoint)
- Output: Complete solo dashboard ready for testing
