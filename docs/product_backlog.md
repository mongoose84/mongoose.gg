# Product Backlog - LoL Improvement Tracker

## Vision

> **"The only LoL improvement tracker also built for duos and teams, powered by AI coaching that turns your stats into actionable goals you can actually achieve."**

## Pricing Model

| Tier | Monthly | Annual | Features |
|------|---------|--------|----------|
| **Free** | €0 | - | Solo stats only, last 20 games, no AI |
| **Pro** | €4.99 | €3.99/mo | Full solo/duo stats, 5 AI recommendations/week, goal tracking |
| **Team** | €8.99 | €6.99/mo | Everything + team dashboard, shared goals, unlimited AI |

---

## Epic Overview

| Epic | Description | Total Points |
|------|-------------|--------------|
| **A. Queue Type Filtering** | Filter stats by Ranked/Normal/ARAM | 27 pts |
| **B. AI Goal Recommendations** | LLM-powered improvement suggestions | 42 pts |
| **C. Subscription & Paywall** | Stripe integration, tiers, feature flags | 35 pts |
| **D. Analytics & Tracking** | User behavior tracking for product decisions | 18 pts |

**Grand Total:** 122 points

---

# Epic A: Queue Type Filtering & Death Timeline

Enable filtering of statistics by queue type (Ranked Solo/Duo, Ranked Flex, Normal, ARAM).

## Issues

### A1. [Database] Add QueueId column to LolMatch table

**Priority:** P0 - Critical  
**Type:** Database Migration  
**Estimate:** 1 point  
**Labels:** `database`, `migration`, `epic-a`

#### Description

Add `QueueId` (INT, nullable) column to the `LolMatch` table to store Riot's queue identifier.

#### Acceptance Criteria

- [x] Create SQL migration: `ALTER TABLE LolMatch ADD COLUMN QueueId INT NULL;`
- [x] Add index: `CREATE INDEX idx_lolmatch_queueid ON LolMatch(QueueId);`
- [x] Update `LolMatch` entity with `public int? QueueId { get; set; }`
- [x] Update `LolMatchRepository` insert/update methods to include QueueId
- [x] Migration is idempotent

#### Queue ID Reference

| Queue ID | Queue Type |
|----------|------------|
| 420 | Ranked Solo/Duo |
| 440 | Ranked Flex |
| 450 | ARAM |
| 400 | Normal Draft |
| 430 | Normal Blind |

---

### A2. [API] Extract QueueId from Riot Match API response

**Priority:** P0 - Critical  
**Type:** Feature  
**Estimate:** 1 point  
**Labels:** `api`, `riot-api`, `epic-a`

#### Description

Update `MatchHistorySyncJob` to extract and persist `queueId` from Riot API match info response.

#### Acceptance Criteria

- [x] Add `queueId` to `LolMatch` entity from `GetMatchInfoAsync`
- [x] Update `MapToLolMatchEntity` to set `match.QueueId`
- [x] Handle missing queueId gracefully (set to null)

---

### A3. [Infrastructure] Create reusable backfill job framework

**Priority:** P0 - Critical  
**Type:** Infrastructure  
**Estimate:** 3 points  
**Labels:** `infrastructure`, `backfill`, `epic-a`

#### Description

Create a generic backfill job infrastructure for future data migrations.

#### Acceptance Criteria

- [x] Create `IBackfillJob` interface
- [x] Create `BackfillJobRunner` with batch processing, progress tracking, error recovery
- [x] Support configurable batch size and delay
- [x] Log progress (e.g., "Processed 500/10000 matches")
- [x] Respect `IRiotLimitHandler` rate limits

---

### A4. [Backfill] Implement QueueId backfill job

**Priority:** P0 - Critical  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** A2, A3  
**Labels:** `backfill`, `epic-a`

#### Description

Backfill job to populate QueueId for existing matches.

#### Acceptance Criteria

- [x] Create `QueueIdBackfillJob : IBackfillJob`
- [x] Query matches where `QueueId IS NULL AND InfoFetched = TRUE`
- [x] Fetch match info from Riot API (using rate limiter)
- [x] Add endpoint: `POST /admin/backfill/queue-id`

---

### A5. [Database] Create QueueType lookup/enum for filtering

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 1 point  
**Labels:** `database`, `epic-a`

#### Description

Create mapping between QueueId integers and user-friendly filter categories.

#### Acceptance Criteria

- [ ] Create `QueueTypeMapper` utility class
- [ ] Map: `RankedSoloDuo` → [420], `RankedFlex` → [440], `Ranked` → [420, 440], `Normal` → [400, 430], `ARAM` → [450], `All` → null
- [ ] Expose available filters via API endpoint

---

### A6. [Repository] Add QueueId filtering to all statistics queries

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 5 points  
**Depends on:** A1, A5  
**Labels:** `repository`, `refactor`, `epic-a`

#### Description

Update repository methods to support optional QueueId filtering.

#### Acceptance Criteria

- [ ] Add `int[]? queueIds` parameter to repository methods
- [ ] Update SQL JOINs to filter by `LolMatch.QueueId IN (@queueIds)`
- [ ] Update `LolMatchParticipantRepository`, `SoloStatsRepository`
- [ ] Deprecate `GameMode` filtering

---

### A7. [API] Add queue type filter to statistics endpoints

**Priority:** P1 - High  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** A5, A6  
**Labels:** `api`, `endpoints`, `epic-a`

#### Description

Expose queue type filtering in API endpoints.

#### Acceptance Criteria

- [ ] Add `?queueType=ranked_solo` query parameter
- [ ] Validate against allowed filter names
- [ ] Default to `All` if not specified

---

### A8. [Database] Add death timing table for timeline data

**Priority:** P2 - Medium  
**Type:** Database Migration  
**Estimate:** 2 points  
**Labels:** `database`, `migration`, `epic-a`

#### Description

Create table for per-death timing and position data.

#### Acceptance Criteria

- [ ] Create `LolMatchDeath` table with MatchId, VictimPuuid, KillerPuuid, TimestampMs, PositionX, PositionY
- [ ] Create `LolMatchDeath` entity class
- [ ] Create `LolMatchDeathRepository`

---

### A9. [API] Add Match Timeline API client method

**Priority:** P2 - Medium  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** A8  
**Labels:** `api`, `riot-api`, `epic-a`

#### Description

Add method to fetch match timeline data from Riot API.

#### Acceptance Criteria

- [ ] Add `GetMatchTimelineAsync(string matchId, CancellationToken ct)` to `IRiotApiClient`
- [ ] Use endpoint `/lol/match/v5/matches/{matchId}/timeline`
- [ ] Respect rate limits

---

### A10. [Sync] Extract and persist death events from timeline

**Priority:** P2 - Medium  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** A8, A9  
**Labels:** `sync`, `epic-a`

#### Description

Update match sync job to fetch and store death timeline data.

#### Acceptance Criteria

- [ ] Add configuration flag to enable/disable timeline fetching
- [ ] Parse timeline JSON for `CHAMPION_KILL` events
- [ ] Persist via `LolMatchDeathRepository`

---

### A11. [Backfill] Implement death timeline backfill job

**Priority:** P3 - Low  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** A3, A10  
**Labels:** `backfill`, `epic-a`

#### Description

Backfill job for death timeline data.

#### Acceptance Criteria

- [ ] Create `DeathTimelineBackfillJob : IBackfillJob`
- [ ] Add endpoint: `POST /admin/backfill/death-timeline`

---

### A12. [Repository] Add death timing statistics methods

**Priority:** P3 - Low  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** A8  
**Labels:** `repository`, `statistics`, `epic-a`

#### Description

Add repository methods for death timing statistics.

#### Acceptance Criteria

- [ ] `GetDeathTimingsByPuuidAsync`
- [ ] `GetAverageDeathTimeByPuuidAsync`
- [ ] `GetDeathsByGamePhaseAsync`
- [ ] Support queue type filtering

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

# Epic C: Subscription & Paywall System

Implement tiered subscriptions with Stripe integration and feature flags.

## Issues

### C1. [Infrastructure] Set up Stripe integration

**Priority:** P0 - Critical  
**Type:** Infrastructure  
**Estimate:** 3 points  
**Labels:** `infrastructure`, `payments`, `stripe`, `epic-c`

#### Description

Set up Stripe SDK and configuration for payment processing.

#### Acceptance Criteria

- [ ] Add Stripe.net NuGet package
- [ ] Store Stripe API keys in `StripeSecret.txt` (gitignored)
- [ ] Configure Stripe client in DI container
- [ ] Set up webhook endpoint for Stripe events
- [ ] Add to `appsettings.json`:
  ```json
  "Stripe": {
    "PublishableKey": "pk_...",
    "WebhookSecret": "whsec_..."
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
    StripeCustomerId VARCHAR(100),
    StripeSubscriptionId VARCHAR(100),
    Tier ENUM('free', 'pro', 'team') DEFAULT 'free',
    Status ENUM('active', 'cancelled', 'past_due', 'trialing') DEFAULT 'active',
    CurrentPeriodStart DATETIME,
    CurrentPeriodEnd DATETIME,
    CancelAtPeriodEnd BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES User(Id),
    UNIQUE INDEX idx_subscription_user (UserId),
    INDEX idx_subscription_stripe (StripeSubscriptionId)
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

- [ ] `ALTER TABLE User ADD COLUMN Tier ENUM('free', 'pro', 'team') DEFAULT 'free';`
- [ ] Update `User` entity
- [ ] Sync tier on subscription changes

---

### C4. [Service] Create Stripe customer service

**Priority:** P0 - Critical  
**Type:** Feature  
**Estimate:** 2 points  
**Depends on:** C1, C2  
**Labels:** `service`, `stripe`, `epic-c`

#### Description

Service to manage Stripe customers.

#### Acceptance Criteria

- [ ] Create `IStripeCustomerService` interface
- [ ] `CreateCustomerAsync(User user)` - create Stripe customer
- [ ] `GetOrCreateCustomerAsync(User user)` - idempotent customer creation
- [ ] Store StripeCustomerId in Subscription table
- [ ] Handle Stripe API errors

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
- [ ] `CreateCheckoutSessionAsync(UserId, Tier)` - generate Stripe Checkout URL
- [ ] `CreatePortalSessionAsync(UserId)` - generate Stripe Customer Portal URL
- [ ] `GetSubscriptionAsync(UserId)` - get current subscription
- [ ] `CancelSubscriptionAsync(UserId)` - cancel at period end
- [ ] Handle upgrade/downgrade flows

---

### C6. [API] Create Stripe webhook handler

**Priority:** P0 - Critical  
**Type:** Feature  
**Estimate:** 3 points  
**Depends on:** C1, C5  
**Labels:** `api`, `stripe`, `webhook`, `epic-c`

#### Description

Handle Stripe webhook events for subscription updates.

#### Acceptance Criteria

- [ ] Create endpoint: `POST /api/webhooks/stripe`
- [ ] Verify webhook signature
- [ ] Handle events:
  - `checkout.session.completed` → create subscription
  - `customer.subscription.updated` → update status/tier
  - `customer.subscription.deleted` → downgrade to free
  - `invoice.payment_failed` → mark past_due
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
  - Response: `{ "checkoutUrl": "https://checkout.stripe.com/..." }`
- [ ] `POST /api/subscription/portal` - get customer portal URL
- [ ] `POST /api/subscription/cancel` - cancel subscription
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
- [ ] "Manage Subscription" button → Stripe Portal
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
- [ ] Redirect to Stripe Checkout

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
- [ ] Create Stripe coupon for founding member discount (€2.99 forever)
- [ ] Auto-apply coupon for qualifying users
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

# Summary

## All Issues by Priority

### P0 - Critical (MVP)

| ID | Title | Epic | Points |
|----|-------|------|--------|
| A1 | Add QueueId column to LolMatch | Queue Filtering | 1 |
| A2 | Extract QueueId from Riot API | Queue Filtering | 1 |
| A3 | Create reusable backfill framework | Queue Filtering | 3 |
| A4 | Implement QueueId backfill job | Queue Filtering | 2 |
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

**P0 Total:** 43 points

### P1 - High

| ID | Title | Epic | Points |
|----|-------|------|--------|
| A5 | Create QueueType lookup/enum | Queue Filtering | 1 |
| A6 | Add QueueId filtering to repositories | Queue Filtering | 5 |
| A7 | Add queue type filter to endpoints | Queue Filtering | 2 |
| B3 | Add LLM rate limiting | AI Goals | 2 |
| B9 | Create goal CRUD endpoints | AI Goals | 2 |
| B10 | Create progress tracking service | AI Goals | 3 |
| B11 | Create progress update job | AI Goals | 2 |
| B12 | Create progress endpoint | AI Goals | 1 |
| C9 | Create feature gate middleware | Subscription | 2 |
| C10 | Add tier info to user endpoints | Subscription | 1 |
| C11 | Create subscription status component | Subscription | 2 |
| C12 | Create upgrade prompt component | Subscription | 2 |
| C13 | Create pricing page | Subscription | 3 |
| C14 | Gate features based on tier | Subscription | 2 |
| D1 | Set up analytics provider | Analytics | 2 |
| D2 | Implement core tracking events | Analytics | 3 |
| D3 | Track page views and sessions | Analytics | 1 |

**P1 Total:** 38 points

### P2 - Medium

| ID | Title | Epic | Points |
|----|-------|------|--------|
| A8 | Add death timing table | Queue Filtering | 2 |
| A9 | Add Timeline API client | Queue Filtering | 2 |
| A10 | Extract death events from timeline | Queue Filtering | 3 |
| B13 | Create goal recommendation UI | AI Goals | 3 |
| B14 | Create active goals display | AI Goals | 3 |
| B15 | Create goal progress chart | AI Goals | 2 |
| C15 | Create founding member pricing | Subscription | 2 |
| D4 | Server-side event tracking | Analytics | 2 |
| D5 | Create key dashboards | Analytics | 2 |
| D6 | Create internal metrics endpoint | Analytics | 2 |
| D7 | Set up error tracking | Analytics | 2 |

**P2 Total:** 25 points

### P3 - Low

| ID | Title | Epic | Points |
|----|-------|------|--------|
| A11 | Death timeline backfill job | Queue Filtering | 2 |
| A12 | Death timing statistics methods | Queue Filtering | 3 |
| B16 | Implement Anthropic client | AI Goals | 2 |
| B17 | Conversational follow-up | AI Goals | 5 |
| C16 | Create referral tracking | Subscription | 2 |
| D8 | Implement A/B testing | Analytics | 2 |

**P3 Total:** 16 points

---

## Recommended Sprint Plan

### Sprint 1: Foundation (P0 Core)
**Focus:** Database + Stripe + Basic AI  
**Points:** ~25

- A1, A2, A3 (Queue infrastructure)
- C1, C2, C3 (Stripe + DB setup)
- B1, B2 (LLM abstraction)

### Sprint 2: Subscriptions (P0 Payments)
**Focus:** Complete payment flow  
**Points:** ~20

- C4, C5, C6, C7, C8 (Subscription services + endpoints)
- A4 (QueueId backfill)
- B4 (Goal tables)

### Sprint 3: AI Goals MVP (P0 AI)
**Focus:** AI recommendations working  
**Points:** ~18

- B5, B6, B7, B8 (AI goal flow)
- A5, A6, A7 (Queue filtering)

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
**Focus:** Death timing, UI polish, advanced features  
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
