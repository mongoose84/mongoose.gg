# Product Backlog - Pulse (lolcoach.gg/duolytics.gg/riftgoals.gg)

## Vision

> **"Pulse is the solo queue improvement tracker—built to help you climb with better champ select picks and post-game takeaways, with a Premium mode that lets your duo or team set goals and improve together."**
>
> “Not another builds app—Pulse helps you improve between games and track progress over time.”

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
| **C. Subscription & Paywall** | Mollie integration, tiers, feature flags | 34 pts | 0 pts |
| **D. Analytics & Tracking** | User behavior tracking for product decisions | 19 pts | 0 pts |
| **E. Database v2 & Analytics Schema** | New match/participant/timeline schema + ingestion | 4 pts | 16 pts ✅ |
| **F. API v2** | New API surface aligned with v2 schema and dashboards | 15 pts | 18 pts ✅ |
| **G. Frontend v2 App & Marketing** | New app shell, landing, and dashboards using v2 API | 10 pts | 15 pts ✅ |

**Remaining:** 126 points | **Completed:** 49 points | **Grand Total:** 175 points

> Note: Platform v2 epics (E–G) are prerequisites for most feature work (B–D) and should generally be completed first.
>
> **Completed tasks have been moved to [product_backlog_completed.md](./product_backlog_completed.md).**

## Cross-cutting requirements (v2)

- All v2 dashboard endpoints and views support **queue filtering** (Ranked Solo/Duo, Ranked Flex, Normal, ARAM).
- Queue filtering is backed by the v2 schema via `matches.queue_id` (numeric Riot queue id) and appropriate indexing.

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
- [ ] Store API key in `OpenAiSecret.txt` (gitignored)
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

Use the concepts in `/docs/rules_of_climbing.md` as domain context so the AI can interpret stats and suggest goals in line with your climbing philosophy.

#### Acceptance Criteria

- [ ] Summarize the key rules from `/docs/rules_of_climbing.md` into a stable, versioned system prompt (or configuration)  
- [ ] Ensure `IGoalPromptBuilder` includes this context for solo, duo and team prompts  
- [ ] Add tests or fixtures that verify the rules context is present in prompts so changes are explicit

---

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
- [ ] Store Mollie API keys in `MollieSecret.txt` (gitignored)
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

### C3. [Database] Add tier column to User table

**Priority:** P0 - Critical  
**Type:** Database Migration  
**Estimate:** 1 point  
**Labels:** `database`, `migration`, `epic-c`

#### Description

Add subscription tier to User for quick access.

#### Acceptance Criteria

- [ ] `ALTER TABLE User ADD COLUMN Tier ENUM('free', 'pro') DEFAULT 'free';`
- [ ] Update `User` entity
- [ ] Sync tier on subscription changes

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

### C10. [API] Add tier info to user endpoints

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 1 point  
**Depends on:** C3  
**Labels:** `api`, `endpoints`, `epic-c`

#### Description

Include subscription info in user API responses.

#### Acceptance Criteria

- [ ] Add to user profile response:
  ```json
  {
    "user": { ... },
    "subscription": {
      "tier": "pro",
      "status": "active",
      "periodEnd": "2026-02-03",
      "cancelAtPeriodEnd": false
    },
    "features": {
      "duoDashboard": true,
      "teamDashboard": false,
      "aiRecommendationsRemaining": 3
    }
  }
  ```

---

### C11. [Frontend] Create subscription status component

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** C7, C10  
**Labels:** `frontend`, `vue`, `epic-c`

#### Description

Vue component showing current subscription status.

#### Acceptance Criteria

- [ ] Create `SubscriptionStatus.vue`
- [ ] Display current tier, status, renewal date
- [ ] "Manage Subscription" button → in-app subscription management
- [ ] Show upgrade prompts for free users

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

---

### C13. [Frontend] Create pricing page

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** C7  
**Labels:** `frontend`, `vue`, `epic-c`

#### Description

Public pricing page with tier comparison.

#### Acceptance Criteria

- [ ] Create `PricingView.vue`
- [ ] Display three tiers with features
- [ ] Toggle monthly/annual pricing
- [ ] "Get Started" / "Upgrade" buttons
- [ ] Highlight current plan if logged in
- [ ] FAQ section

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

---

### C17. [Product] Implement 2-tier pricing (Free + Pro) with Guests, Duo/Team collaboration paywall, and goal tracking paywall

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 5 points  
**Depends on:** C2, C3, C7, C8, C10, C12, C13, C14, F11-social  
**Labels:** `pricing`, `subscription`, `entitlements`, `backend`, `frontend`, `client_v2`, `epic-c`

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

**Frontend (`client_v2`)**

- [ ] Pricing page updated to show **2 tiers** (Free + Pro) with gamer-language value props
- [ ] Upgrade prompts updated (remove "Upgrade to Team"); Pro is the only paid upgrade
- [ ] Users can create/join Duo/Team spaces as Guests
- [ ] In-group collaboration modules (team dashboard/shared goals/voting) show locked state + upgrade nudges for Guests
- [ ] Goal setting/tracking UX is paywalled cleanly for Free users with upgrade path to Pro

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

## Issues

### D1. [Infrastructure] Set up analytics provider

**Priority:** P1 - High  
**Type:** Infrastructure  
**Estimate:** 2 points  
**Labels:** `infrastructure`, `analytics`, `epic-d`

#### Description

Integrate analytics service (PostHog recommended).

#### Acceptance Criteria

- [ ] Choose provider: PostHog (self-hosted or cloud)
- [ ] Add PostHog JS SDK to client
- [ ] Configure project and API keys
- [ ] Set up user identification on login
- [ ] GDPR compliance: cookie consent if needed

---

### D2. [Frontend] Implement core tracking events

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** D1  
**Labels:** `frontend`, `analytics`, `epic-d`

#### Description

Track core user actions.

#### Acceptance Criteria

- [ ] Create `useAnalytics` composable
- [ ] Track events:
  - `user_signed_up` (source, method)
  - `user_logged_in`
  - `dashboard_viewed` (type: solo/duo/team)
  - `ai_recommendation_requested`
  - `goal_created` (from_ai: true/false)
  - `goal_completed`
  - `goal_abandoned`
  - `subscription_started` (tier, annual)
  - `subscription_cancelled` (reason)
  - `feature_blocked` (feature, tier)
  - `upgrade_prompt_shown` (feature)
  - `upgrade_prompt_clicked` (feature)
- [ ] Include user properties: tier, signup_date, games_analyzed

---

### D3. [Frontend] Track page views and session data

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 1 point  
**Depends on:** D1  
**Labels:** `frontend`, `analytics`, `epic-d`

#### Description

Automatic page view and session tracking.

#### Acceptance Criteria

- [ ] Track page views on route change
- [ ] Session duration tracking
- [ ] Referrer tracking
- [ ] UTM parameter capture

---

### D4. [Backend] Create server-side event tracking

**Priority:** P2 - Medium  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** D1  
**Labels:** `backend`, `analytics`, `epic-d`

#### Description

Track critical events server-side for accuracy.

#### Acceptance Criteria

- [ ] Create `IAnalyticsService` interface
- [ ] Track:
  - `subscription_created` (server-side, accurate)
  - `subscription_cancelled`
  - `payment_failed`
  - `match_synced` (count per user)
  - `api_error` (endpoint, error type)
- [ ] Use PostHog server-side SDK or HTTP API

---

### D5. [Analytics] Create key dashboards

**Priority:** P2 - Medium  
**Type:** Configuration  
**Estimate:** 2 points  
**Depends on:** D2, D3, D4  
**Labels:** `analytics`, `dashboard`, `epic-d`

#### Description

Set up analytics dashboards in PostHog.

#### Acceptance Criteria

- [ ] **Acquisition Dashboard:**
  - Signups per day/week
  - Signup sources
  - Conversion funnel: Visit → Signup → First Dashboard → Paid
- [ ] **Engagement Dashboard:**
  - DAU/WAU/MAU
  - Feature usage breakdown
  - AI recommendations per user
  - Goals created/completed
- [ ] **Revenue Dashboard:**
  - MRR, subscriptions by tier
  - Churn rate
  - Upgrade/downgrade flow
- [ ] **Retention Dashboard:**
  - Cohort retention
  - Days to first goal
  - Feature correlation with retention

---

### D6. [Backend] Create internal metrics endpoint

**Priority:** P2 - Medium  
**Type:** Feature  
**Estimate:** 2 points  
**Labels:** `backend`, `api`, `metrics`, `epic-d`

#### Description

Admin endpoint for real-time metrics.

#### Acceptance Criteria

- [ ] Create `GET /admin/metrics` endpoint (admin auth required)
- [ ] Return:
  ```json
  {
    "users": { "total": 500, "free": 400, "pro": 80, "team": 20 },
    "mrr": 599.20,
    "activeGoals": 150,
    "aiRequestsToday": 45,
    "matchesSynced": { "today": 1200, "total": 50000 }
  }
  ```
- [ ] Cache metrics (refresh every 5 min)

---

### D7. [Infrastructure] Set up error tracking

**Priority:** P2 - Medium  
**Type:** Infrastructure  
**Estimate:** 2 points  
**Labels:** `infrastructure`, `monitoring`, `epic-d`

#### Description

Track errors for debugging and reliability.

#### Acceptance Criteria

- [ ] Choose provider: Sentry (recommended) or PostHog errors
- [ ] Add Sentry SDK to client and server
- [ ] Configure source maps for client
- [ ] Set up error alerts (email/Slack)
- [ ] Track: exceptions, API errors, Stripe webhook failures

---

### D8. [Analytics] Implement A/B testing framework

**Priority:** P3 - Low  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** D1  
**Labels:** `analytics`, `ab-testing`, `epic-d`

#### Description

Framework for A/B testing features and pricing.

#### Acceptance Criteria

- [ ] Use PostHog feature flags for A/B tests
- [ ] Create `useExperiment` composable
- [ ] First test: pricing page variants
- [ ] Track experiment exposure and conversion

---

### D9. [Frontend] Show login activity heatmap on user page

**Priority:** P2 - Medium  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** D1, D2, G9  
**Labels:** `frontend`, `analytics`, `epic-d`

#### Description

Give users a GitHub-style contribution view of how often they log in over time.

#### Acceptance Criteria

- [ ] Use existing `user_logged_in` tracking (or add it) to store daily login counts per user  
- [ ] Implement a heatmap component that renders a 12-month day-by-day matrix similar to GitHub's contribution graph  
- [ ] Surface the heatmap on the user account page (Epic G9)  
- [ ] Provide basic tooltips (date + number of logins) and a legend for intensity levels

---

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

---

# Epic E: Database v2 & Analytics Schema

Modernize the Pulse database to match `docs/database_schema_v2.md` and support advanced solo/duo/team analytics.

> **Completed tasks (E1–E5) have been moved to [product_backlog_completed.md](./product_backlog_completed.md).**

## Issues

### E6. [Validation] Validate Database v2 metrics against Riot for sample accounts

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** E5  
**Labels:** `validation`, `v2`, `epic-e`

#### Description

Spot-check v2 metrics for a small set of accounts against Riot and/or existing dashboards to ensure correctness.

#### Acceptance Criteria

- [ ] Define 3–5 representative test accounts (roles, elos)  
- [ ] For each, verify key metrics (win rate, CS@10/15, kill participation, deaths timings, gold leads) match expectations  
- [ ] Any discrepancies are either fixed or documented with rationale

---

### E7. [Cleanup] Remove v1 database tables and repositories

**Priority:** P2 - Medium  
**Type:** Chore  
**Estimate:** 2 points  
**Depends on:** E6, F6  
**Labels:** `cleanup`, `database`, `v2`, `epic-e`

#### Description

Once all consumers have been migrated to v2, remove legacy tables, repositories and any unused DTOs/entities.

#### Acceptance Criteria

- [ ] No remaining code paths depend on v1 tables  
- [ ] Legacy repositories and entities are deleted  
- [ ] Database v2 is the only schema used in production

---

# Epic F: API v2

Expose a new HTTP API surface aligned with Database v2 and the new dashboards.

> **Completed tasks (F1, F2, F7, F11 core auth, F12) have been moved to [product_backlog_completed.md](./product_backlog_completed.md).**

## Issues

### F3. [API] Implement Duo dashboard v2 endpoint

**Priority:** P0 - Critical  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** E3, E5, F1  
**Labels:** `api`, `duo`, `v2`, `epic-f`

#### Description

Create a v2 endpoint that returns duo synergy stats, matchup data, shared objective participation and win rates from Database v2.

#### Acceptance Criteria

- [ ] Endpoint implemented (e.g. `GET /api/v2/duo/dashboard/{userId}`)  
- [ ] Returns per-duo aggregates needed for the Duo dashboard  
- [ ] Uses duo-related tables/metrics from Database v2
- [ ] Supports optional queue filtering via the standardized v2 queue filter

---

### F4. [API] Implement Team dashboard v2 endpoint

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** E3, E5, F1  
**Labels:** `api`, `team`, `v2`, `epic-f`

#### Description

Create a v2 endpoint that exposes team-level metrics (games played, win rate, queue type, gold leads, role composition, champion combos, role pair effectiveness).

#### Acceptance Criteria

- [ ] Endpoint implemented (e.g. `GET /api/v2/team/dashboard/{userId}`)  
- [ ] Returns all data needed by the Team dashboard v2  
- [ ] Uses team-related tables/metrics from Database v2
- [ ] Supports optional queue filtering via the standardized v2 queue filter

---

### F5. [API] Implement AI snapshot/goal input endpoint

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** E5, B1, F1  
**Labels:** `api`, `ai`, `v2`, `epic-f`

#### Description

Expose an endpoint that aggregates player stats into an AI-friendly snapshot (`ai_snapshots`), to be consumed by the AI goal recommendation flow (Epic B).

#### Acceptance Criteria

- [ ] Endpoint implemented (e.g. `POST /api/v2/ai/snapshot`)  
- [ ] Returns or stores an `ai_snapshots` record for the requested context (solo/duo/team)  
- [ ] Contracts align with `IPlayerStatsAggregator` and `IGoalRecommendationService`

---

### F6. [API] Deprecate or migrate v1 endpoints to v2

**Priority:** P2 - Medium  
**Type:** Chore  
**Estimate:** 2 points  
**Depends on:** F2, F3, F4, G5, G6, G7  
**Labels:** `api`, `cleanup`, `v2`, `epic-f`

#### Description

Once the new frontend is migrated to API v2, mark v1 endpoints as deprecated and remove any unused routes.

#### Acceptance Criteria

- [ ] Frontend uses only v2 endpoints for dashboards  
- [ ] v1 routes removed or clearly marked as internal/testing-only  
- [ ] API documentation updated to reference v2 only

---

### F8. [API] Implement unified error handling & problem responses

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** F1  
**Labels:** `api`, `error-handling`, `epic-f`

#### Description

Provide consistent, user-friendly error responses from the API v2 surface and avoid leaking implementation details.

#### Acceptance Criteria

- [ ] Add global exception handling middleware/filter that returns a standard error shape (e.g. RFC 7807-style problem details)  
- [ ] Map validation and domain errors to 4xx responses with clear, structured error information  
- [ ] Map unexpected failures to 5xx responses with a generic message and correlation id  
- [ ] Integrate with error tracking (Epic D7) so important failures are logged with context  
- [ ] Update a representative set of endpoints to use the standardized error patterns

---

### F9. [Testing] Add backend tests with focus on security

**Priority:** P1 - High  
**Type:** Testing  
**Estimate:** 3 points  
**Depends on:** F7, F8  
**Labels:** `testing`, `security`, `epic-f`

#### Description

Introduce automated tests for the .NET server, with emphasis on security-sensitive flows.

#### Acceptance Criteria

- [ ] Create a test project for the backend (unit and/or integration tests)  
- [ ] Tests cover API key authentication happy-path and failure scenarios  
- [ ] Tests cover at least one representative endpoint from each major area (goals, subscriptions, dashboards)  
- [ ] Tests verify error handling behavior (4xx vs 5xx, response shape)  
- [ ] Tests run as part of the standard CI pipeline

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

# Epic G: Frontend v2 App & Marketing

Create a new, professional user experience with a landing page, pricing, and app shell consuming API v2.

> **Completed tasks (G1, G2, G9, G12) have been moved to [product_backlog_completed.md](./product_backlog_completed.md).**

### Decision (Jan 2026): Build a standalone `client_v2` Vue app
- **Architecture**: Create a completely independent Vue 3 + Vite application in `/client_v2/` directory with its own `package.json`, `node_modules`, and build configuration. This is **not** nested within the legacy `/client` folder.
- **Rationale**: New styling was difficult to merge into the legacy client without disrupting v1. A fresh, standalone app allows v2 to ship independently while the current client remains untouched and operational.
- **Style direction**: Start with the Vercel developer aesthetic (dark, sharp, neon-tinged) but keep theme tokens configurable via CSS variables so we can swap looks later without restructuring code.
- **Development workflow**: 
  - Scaffold `/client_v2/` as a new Vue 3 + Vite project
  - Implement app shell (G2), landing page (G3), and solo dashboard (G5) in parallel with backend (F2)
  - Once solo dashboard is feature-complete and tested locally, deploy v2 app to production
  - Existing `/client` (v1) continues running unchanged; users can access either app during transition
- **Scope**: Marketing landing page + app shell + solo experience first; duo/team dashboards follow once solo v2 is stable in production.

## Issues

### G3. [Frontend] Implement new public landing page

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 2 points  
**Labels:** `frontend`, `marketing`, `epic-g`

#### Description

Redesign the `/` route as a marketing landing page describing the product, key benefits, and CTAs to create a dashboard or sign up.

#### Acceptance Criteria

- [ ] Hero section with concise value proposition  
- [ ] Explanation of Free vs Pro vs Team tiers at a high level  
- [ ] Primary CTA leading into the app (e.g. create dashboard / log in)

---

### G4. [Frontend] Implement pricing page

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** C1, C2, C7 (for final details)  
**Labels:** `frontend`, `pricing`, `epic-g`

#### Description

Create a `/pricing` view that presents the Free, Pro, and Team plans and integrates with the subscription endpoints once available.

#### Acceptance Criteria

- [ ] Pricing cards for Free, Pro, Team with key features  
- [ ] Buttons wired to subscription/checkout flows when those are implemented  
- [ ] Clear explanation of what is included in each tier

---

### G5. [Frontend] Implement Solo dashboard v2 view

**Priority:** P0 - Critical  
**Type:** Feature  
**Estimate:** 5 points  
**Depends on:** F2, G2  
**Labels:** `frontend`, `solo`, `dashboard`, `epic-g`

#### Description

Create a new Solo dashboard screen under `/app/solo` that consumes the Solo dashboard v2 endpoint and presents the key solo stats (overview, champion performance, role distribution, death efficiency, match duration, etc.).

#### Acceptance Criteria

- [ ] Solo v2 view implemented and wired to API v2  
- [ ] Queue filter control implemented (Ranked Solo/Duo, Ranked Flex, Normal, ARAM, All) and wired into API requests
- [ ] Old Solo dashboard route either redirects or is clearly deprecated  
- [ ] Layout matches the new app shell and feels consistent with the product branding

---

### G6. [Frontend] Implement Duo dashboard v2 view

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 5 points  
**Depends on:** F3, G2  
**Labels:** `frontend`, `duo`, `dashboard`, `epic-g`

#### Description

Create a new Duo dashboard screen under `/app/duo` that consumes the Duo dashboard v2 endpoint.

#### Acceptance Criteria

- [ ] Duo v2 view implemented and wired to API v2  
- [ ] Shows duo synergy, matchups, shared objectives, and relevant duo KPIs  
- [ ] Old Duo dashboard route either redirects or is clearly deprecated

---

### G7. [Frontend] Implement Team dashboard v2 view

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 5 points  
**Depends on:** F4, G2  
**Labels:** `frontend`, `team`, `dashboard`, `epic-g`

#### Description

Create a new Team dashboard screen under `/app/team` that consumes the Team dashboard v2 endpoint.

#### Acceptance Criteria

- [ ] Team v2 view implemented and wired to API v2  
- [ ] Shows team composition, win rate, role composition, combos, and performance trends  
- [ ] Old Team dashboard route either redirects or is clearly deprecated

---

### G8. [Cleanup] Remove legacy dashboard views & routes

**Priority:** P2 - Medium  
**Type:** Chore  
**Estimate:** 1 point  
**Depends on:** G5, G6, G7  
**Labels:** `frontend`, `cleanup`, `epic-g`

#### Description

Remove old dashboard views, components and routes that are no longer used after migration to v2.

#### Acceptance Criteria

- [ ] Legacy Solo/Duo/Team views are removed or replaced by thin redirect stubs  
- [ ] No dead routes or components remain for v1 dashboards  
- [ ] Router configuration reflects only the v2 navigation structure

---

### G10. [Frontend] Implement user dropdown details & account settings page

**Priority:** P1 - High
**Type:** Feature
**Estimate:** 8 points
**Depends on:** G9, G12, C7, C10, F11, F12
**Labels:** `frontend`, `auth`, `users`, `settings`, `subscription`, `riot-api`, `epic-g`

#### Description

Extend the user dropdown to show key account information and link to a new `/app/settings` page. On this settings page, users can update their username, email, password, profile icon, subscription plan, and manage their linked game accounts (Riot accounts). Changing email reuses the verification flow and sets a flag in the database to unverified until validation is complete.

#### Acceptance Criteria

- [ ] User dropdown displays current username, email, and subscription tier/status using data from the profile endpoint (enriched by C10)
- [ ] The Settings item in the dropdown is now active and navigates to `/app/settings`
- [ ] `/app/settings` is only accessible to authenticated users and is rendered inside the G2 app shell
- [ ] Users can update their **username**:
  - Uses the same uniqueness and length/format validation rules as signup
  - Shows clear error messages when the username is already taken or invalid
- [ ] Users can update their **email**:
  - Changing email updates the stored email and sets `emailVerified = false` (or equivalent)
  - The UI clearly indicates the email is pending verification and prompts the user to complete the 6-digit verification flow again
- [ ] Users can change their **password** using a form that includes current password, new password, and confirmation; failures (wrong current password or policy violations) are handled with clear messages
- [ ] Users can change their **profile icon** by selecting from a predefined set of avatar icons (no file upload yet); the chosen icon is persisted and reflected in the `/app/*` header and dropdown
- [ ] **Subscription Management** section on `/app/settings`:
  - Shows current subscription tier (Free, Pro) and status (active, cancelled, past_due)
  - Shows billing period end date if subscribed
  - "Upgrade Plan" button opens a plan selection modal or routes to `/pricing`
  - "Change Plan" button (for existing subscribers) allows switching between tiers
  - "Cancel Subscription" button with confirmation dialog; calls backend to cancel at period end
  - "Reactivate" button if subscription is cancelled but still within billing period
  - Payment method display (last 4 digits of card) with "Update Payment Method" link to Stripe portal
- [ ] **Game Accounts** section on `/app/settings`:
  - Lists all linked Riot accounts showing: Game Name#Tag, Region, Sync status, Last synced timestamp
  - "Link New Account" button opens `LinkRiotAccountModal.vue` (reused from G12)
  - Per-account actions:
    - "Set as Primary" button to designate main account for dashboard defaults
    - "Unlink" button with confirmation dialog; calls `DELETE /api/v2/users/me/riot-accounts/{accountId}`
    - "Refresh Sync" button to manually trigger match sync for that account
  - Empty state with prominent CTA if no accounts are linked
  - Visual indicator for primary account
- [ ] Update or add backend tests (in this issue or F11) to confirm that username/email/password update endpoints:
  - Use parameterized queries / ORM APIs (no dynamic SQL)
  - Do not leak SQL error details when given malicious input
- [ ] Add backend endpoints if not already present:
  - `PATCH /api/v2/users/me` for profile updates (username, email, avatar)
  - `POST /api/v2/users/me/change-password` for password changes
  - `DELETE /api/v2/users/me/riot-accounts/{accountId}` for unlinking game accounts
  - `PATCH /api/v2/users/me/riot-accounts/{accountId}` for setting primary account
  - `POST /api/v2/users/me/riot-accounts/{accountId}/sync` for manual sync trigger

---

### G11. [Frontend] Implement friends management UI scaffolding

**Priority:** P2 - Medium  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** G9 (and future social endpoints from F11)  
**Labels:** `frontend`, `users`, `social`, `epic-g`

#### Description

Introduce a first-pass friends/social area in the app UI that defines the layout, navigation, and empty states for friends/duos/teams, but with no real backend integration yet. All actions are disabled or marked as "coming soon" so a later iteration can implement the detailed flows.

#### Acceptance Criteria

- [ ] Add a Friends section on the `/app/user` page that visually groups social functionality (e.g. a "Friends & Teams" card or tab)  
- [ ] The Friends section includes:
  - An empty state message when there are no friends (e.g. "You don't have any friends added yet")  
  - UI scaffolding for a friends list (list area or table), search field, "Add friend" button, and per-friend actions (e.g. remove/manage)  
  - Optional sub-tabs or headings for Friends, Duos, and Teams, even if they contain no real data yet  
- [ ] All social actions (search, add, remove, manage) are disabled or trigger only local "Coming soon" messages and do not call any backend endpoints yet  
- [ ] A simple frontend abstraction (e.g. `useFriendsStore` or `useSocialStore`) is introduced and used by the Friends UI to obtain data and actions, with TODO comments describing how it will later connect to social endpoints from F11

---

---

---

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
| C1 | Set up Stripe integration | Subscription | 3 |
| C2 | Create subscription tables | Subscription | 2 |
| C3 | Add tier column to User | Subscription | 1 |
| C4 | Create Stripe customer service | Subscription | 2 |
| C5 | Create subscription management service | Subscription | 3 |
| C6 | Create Stripe webhook handler | Subscription | 3 |
| C7 | Create subscription endpoints | Subscription | 2 |
| C8 | Create feature flag service | Subscription | 2 |

| E1 | Finalize Database v2 schema & DDL | Database v2 | 3 |
| E2 | Create MySQL schema scripts for Database v2 | Database v2 | 2 |
| E3 | Implement v2 entities and repositories | Database v2 | 3 |
| E4 | Ingest match & participant core data into v2 | Database v2 | 3 |
| E5 | Ingest timeline & derived metrics into v2 | Database v2 | 5 |
| F1 | Define API v2 surface & versioning | API v2 | 2 |
| F2 | Implement Solo dashboard v2 endpoint | API v2 | 3 |
| F3 | Implement Duo dashboard v2 endpoint | API v2 | 3 |
| F7 | Implement authenticated access for backend | API v2 | 3 |
| F11 | Implement user auth, profile & social endpoints | API v2 | 5 |
| G1 | Define app v2 IA & routes | Frontend v2 | 2 |
| G2 | Implement new app shell & navigation | Frontend v2 | 3 |
| G5 | Implement Solo dashboard v2 view | Frontend v2 | 5 |
| G9 | Implement user login, signup, verification & `/app/user` shell | Frontend v2 | 5 |
| G12 | Implement Riot account linking on `/app/user` | Frontend v2 | 5 |
| F12 | Implement Riot account linking endpoints | API v2 | 5 |

**P0 Total:** 102 points (18 points completed: F14, F13, G13)

### P1 - High

| ID | Title | Epic | Points |
|----|-------|------|--------|
| B3 | Add LLM rate limiting | AI Goals | 2 |
| B9 | Create goal CRUD endpoints | AI Goals | 2 |
| B10 | Create progress tracking service | AI Goals | 3 |
| B11 | Create progress update job | AI Goals | 2 |
| B12 | Create progress endpoint | AI Goals | 1 |
| B18 | Add rules-of-climbing domain context for recommendations | AI Goals | 2 |
| C9 | Create feature gate middleware | Subscription | 2 |
| C10 | Add tier info to user endpoints | Subscription | 1 |
| C11 | Create subscription status component | Subscription | 2 |
| C12 | Create upgrade prompt component | Subscription | 2 |
| C13 | Create pricing page | Subscription | 3 |
| C14 | Gate features based on tier | Subscription | 2 |
| D1 | Set up analytics provider | Analytics | 2 |
| D2 | Implement core tracking events | Analytics | 3 |
| D3 | Track page views and sessions | Analytics | 1 |

| E6 | Validate Database v2 metrics against Riot | Database v2 | 2 |
| F4 | Implement Team dashboard v2 endpoint | API v2 | 3 |
| F5 | Implement AI snapshot/goal input endpoint | API v2 | 3 |
| F8 | Implement unified error handling & problem responses | API v2 | 3 |
| F9 | Add backend tests with focus on security | API v2 | 3 |
| G3 | Implement new public landing page | Frontend v2 | 2 |
| G4 | Implement pricing page | Frontend v2 | 2 |
| G6 | Implement Duo dashboard v2 view | Frontend v2 | 5 |
| G7 | Implement Team dashboard v2 view | Frontend v2 | 5 |

| G10 | Implement user dropdown details & account settings page | Frontend v2 | 5 |

**P1 Total:** 63 points

### P2 - Medium

| ID | Title | Epic | Points |
|----|-------|------|--------|
| B13 | Create goal recommendation UI | AI Goals | 3 |
| B14 | Create active goals display | AI Goals | 3 |
| B15 | Create goal progress chart | AI Goals | 2 |
| C15 | Create founding member pricing | Subscription | 2 |
| D4 | Server-side event tracking | Analytics | 2 |
| D5 | Create key dashboards | Analytics | 2 |
| D6 | Create internal metrics endpoint | Analytics | 2 |
| D7 | Set up error tracking | Analytics | 2 |
| D9 | Show login activity heatmap on user page | Analytics | 3 |
| D10 | Implement cookie consent & preferences | Analytics | 2 |

| E7 | Remove v1 database tables and repositories | Database v2 | 2 |
| F6 | Deprecate or migrate v1 endpoints to v2 | API v2 | 2 |
| F10 | Audit async methods for CancellationToken usage | API v2 | 3 |
| G8 | Remove legacy dashboard views & routes | Frontend v2 | 1 |
| G11 | Implement friends management UI scaffolding | Frontend v2 | 3 |

**P2 Total:** 34 points

### P3 - Low

| ID | Title | Epic | Points |
|----|-------|------|--------|
| B16 | Implement Anthropic client | AI Goals | 2 |
| B17 | Conversational follow-up | AI Goals | 5 |
| C16 | Create referral tracking | Subscription | 2 |
| D8 | Implement A/B testing | Analytics | 2 |

**P3 Total:** 11 points

---

## Recommended Sprint Plan

### Sprint 0: Platform v2 Foundation
**Focus:** Database v2 + API v2 + Solo dashboard v2 + Auth + Account Linking
**Points:** ~58

- E1, E2, E3 (Database v2 schema & repositories)
- E4, E5 (v2 ingestion: matches, participants, timeline & metrics)
- F1, F2 (API v2 design + Solo dashboard endpoint)
- G1, G2, G5 (App v2 IA, shell, Solo dashboard view)
- G9 (User login, signup, verification & `/app/user` shell)
- F12, F13, F14 (Riot account linking + WebSocket sync + v2 sync job)
- G12, G13 (Riot account linking UI + real-time sync progress)

### Sprint 1: Foundation (P0 Core)
**Focus:** Database + Mollie + Basic AI
**Points:** ~20

- C1, C2, C3 (Mollie + DB setup)
- B1, B2 (LLM abstraction)

### Sprint 2: Subscriptions (P0 Payments)
**Focus:** Complete payment flow  
**Points:** ~18

- C4, C5, C6, C7, C8 (Subscription services + endpoints)
- B4 (Goal tables)

### Sprint 3: AI Goals MVP (P0 AI)
**Focus:** AI recommendations working  
**Points:** ~10

- B5, B6, B7, B8 (AI goal flow)

### Sprint 4: Polish (P1)
**Focus:** Feature gates + analytics  
**Points:** ~20

- C9, C10, C11, C12, C13, C14 (Frontend subscription)
- D1, D2, D3 (Analytics)
- B3 (LLM rate limiting)

### Sprint 5: Goal Tracking (P1)
**Focus:** Complete goal lifecycle  
**Points:** ~16

- B9, B10, B11, B12 (Goal CRUD + progress)
- D4, D5, D6, D7 (Server analytics + monitoring)

### Sprint 6+: Enhancements (P2/P3)
**Focus:** UI polish, advanced features  
**Points:** ~41

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
