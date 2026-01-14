# Product Plan

This document provides an overview of the project plan, sprint schedule, and task status.

For full task details, see:
- [product_backlog.md](./product_backlog.md) - Active tasks
- [product_backlog_completed.md](./product_backlog_completed.md) - Completed tasks with full acceptance criteria

---

## Vision

> **"Pulse is the solo queue improvement tracker—built to help you climb with better champ select picks and post-game takeaways, with a Premium mode that lets your duo or team set goals and improve together."**
>
> “Not another builds app—Pulse helps you improve between games and track progress over time.”

## Completed Tasks

| ID | Title | Epic | Points | Status |
|----|-------|------|--------|--------|
| E1 | Finalize Database v2 schema & DDL | Database v2 | 3 | ✅ |
| E2 | Create MySQL schema scripts for Database v2 | Database v2 | 2 | ✅ |
| E3 | Implement v2 entities and repositories | Database v2 | 3 | ✅ |
| E4 | Ingest match & participant core data into v2 | Database v2 | 3 | ✅ |
| E5 | Ingest timeline & derived metrics into v2 | Database v2 | 5 | ✅ |
| F1 | Define API v2 surface & versioning | API v2 | 2 | ✅ |
| F2 | Implement Solo dashboard v2 endpoint | API v2 | 3 | ✅ |
| F7 | Implement authenticated access for backend | API v2 | 3 | ✅ |
| F11 | Implement user auth endpoints (core) | API v2 | 5 | ✅ |
| F12 | Implement Riot account linking endpoints | API v2 | 5 | ✅ |
| F13 | Implement WebSocket endpoint for sync progress | API v2 | 5 | ✅ |
| F14 | Implement V2 Match History Sync Job | API v2 | 8 | ✅ |
| G1 | Define app v2 IA & routes | Frontend v2 | 2 | ✅ |
| G2 | Implement new app shell & navigation | Frontend v2 | 3 | ✅ |
| G5a | Dashboard Hub design (/app/user) | Frontend v2 | 2 | ✅ |
| G5b0 | Solo Dashboard design | Frontend v2 | 2 | ✅ |
| G5b1 | Create empty Solo dashboard view & routing | Frontend v2 | 1 | ✅ |
| G5b8 | Add profile_icon_id and summoner_level to riot_accounts | Database v2 | 1 | ✅ |
| G9 | Implement user login, signup, verification & `/app/user` shell | Frontend v2 | 5 | ✅ |
| G12 | Implement Riot account linking on `/app/user` | Frontend v2 | 5 | ✅ |
| G13 | Implement real-time match sync progress via WebSocket | Frontend v2 | 5 | ✅ |

**Completed Total:** 73 points

---

## All Issues by Priority

### P0 - Critical (MVP)

| ID | Title | Epic | Points | Status |
|----|-------|------|--------|--------|
| B1 | Create LLM provider abstraction | AI Goals | 3 | |
| B2 | Implement OpenAI client | AI Goals | 2 | |
| B4 | Create Goal database tables | AI Goals | 2 | |
| B5 | Create player stats aggregator | AI Goals | 3 | |
| B6 | Create goal prompt builder | AI Goals | 2 | |
| B7 | Create goal recommendation service | AI Goals | 3 | |
| B8 | Create recommendation endpoint | AI Goals | 2 | |
| C1 | Set up Mollie integration | Subscription | 3 | |
| C2 | Create subscription tables | Subscription | 2 | |
| C3 | Add tier column to User | Subscription | 1 | |
| C4 | Create Mollie customer service | Subscription | 2 | |
| C5 | Create subscription management service | Subscription | 3 | |
| C6 | Create Mollie webhook handler | Subscription | 3 | |
| C7 | Create subscription endpoints | Subscription | 2 | |
| C8 | Create feature flag service | Subscription | 2 | |
| E1 | Finalize Database v2 schema & DDL | Database v2 | 3 | ✅ |
| E2 | Create MySQL schema scripts for Database v2 | Database v2 | 2 | ✅ |
| E3 | Implement v2 entities and repositories | Database v2 | 3 | ✅ |
| E4 | Ingest match & participant core data into v2 | Database v2 | 3 | ✅ |
| E5 | Ingest timeline & derived metrics into v2 | Database v2 | 5 | ✅ |
| F1 | Define API v2 surface & versioning | API v2 | 2 | ✅ |
| F2 | Implement Solo dashboard v2 endpoint | API v2 | 3 | ✅ |
| F3 | Implement Duo dashboard v2 endpoint | API v2 | 3 | |
| F7 | Implement authenticated access for backend | API v2 | 3 | ✅ |
| F11 | Implement user auth endpoints (core) | API v2 | 5 | ✅ |
| F12 | Implement Riot account linking endpoints | API v2 | 5 | ✅ |
| F13 | Implement WebSocket endpoint for sync progress | API v2 | 5 | ✅ |
| F14 | Implement V2 Match History Sync Job | API v2 | 8 | ✅ |
| G1 | Define app v2 IA & routes | Frontend v2 | 2 | ✅ |
| G2 | Implement new app shell & navigation | Frontend v2 | 3 | ✅ |
| G5a | Dashboard Hub design (/app/user) | Frontend v2 | 2 | ✅ |
| G5b0 | Solo Dashboard design | Frontend v2 | 2 | ✅ |
| G5b1 | Create empty Solo dashboard view & routing | Frontend v2 | 1 | ✅ |
| G5b2 | Profile header button + profile data (FE+BE, User dashboard) | Frontend v2 / API v2 | 5 | ✅ |
| G5b3 | Main champion card + main champions data (FE+BE) | Frontend v2 / API v2 | 5 | |
| G5b4 | Winrate Over Time chart + trend data (FE+BE) | Frontend v2 / API v2 | 5 | |
| G5b5 | LP Over Time chart (frontend UI) | Frontend v2 | 2 | |
| G5b6 | Champion matchups table + v2 endpoint (FE+BE) | Frontend v2 / API v2 | 6 | |
| G5b8 | Add profile_icon_id and summoner_level to riot_accounts | Database v2 | 1 | ✅ |
| G5b16 | Update database on login (FE+BE) | Frontend v2 / API v2 | 2 | |
| G9 | Implement user login, signup, verification & `/app/user` shell | Frontend v2 | 5 | ✅ |
| G12 | Implement Riot account linking on `/app/user` | Frontend v2 | 5 | ✅ |
| G13 | Implement real-time match sync progress via WebSocket | Frontend v2 | 5 | ✅ |

**P0 Remaining:** 63 points | **P0 Completed:** 73 points | **P0 Total:** 136 points

### P1 - High

| ID | Title | Epic | Points | Status |
|----|-------|------|--------|--------|
| B3 | Add LLM rate limiting | AI Goals | 2 | |
| B9 | Create goal CRUD endpoints | AI Goals | 2 | |
| B10 | Create progress tracking service | AI Goals | 3 | |
| B11 | Create progress update job | AI Goals | 2 | |
| B12 | Create progress endpoint | AI Goals | 1 | |
| B18 | Add rules-of-climbing domain context for recommendations | AI Goals | 2 | |
| C9 | Create feature gate middleware | Subscription | 2 | |
| C10 | Add tier info to user endpoints | Subscription | 1 | |
| C11 | Create subscription status component | Subscription | 2 | |
| C12 | Create upgrade prompt component | Subscription | 2 | |
| C13 | Create pricing page | Subscription | 3 | |
| C14 | Gate features based on tier | Subscription | 2 | |
| C17 | Implement 2-tier pricing (Free + Pro) + Guests + collaboration/goal paywalls | Subscription | 5 | |
| D1 | Set up analytics provider | Analytics | 2 | |
| D2 | Implement core tracking events | Analytics | 3 | |
| D3 | Track page views and sessions | Analytics | 1 | |
| E6 | Validate Database v2 metrics against Riot | Database v2 | 2 | |
| F4 | Implement Team dashboard v2 endpoint | API v2 | 3 | |
| F5 | Implement AI snapshot/goal input endpoint | API v2 | 3 | |
| F8 | Implement unified error handling & problem responses | API v2 | 3 | |
| F9 | Add backend tests with focus on security | API v2 | 3 | |
| F11-social | Implement social endpoints (friends, teams, search) | API v2 | 3 | |
| F13-lp | Implement Riot League API for rank/LP data | API v2 | 5 | |
| F14-login | Check for new matches on user login and auto-sync | API v2 | 3 | |
| G3 | Implement new public landing page | Frontend v2 | 2 | |
| G4 | Implement pricing page | Frontend v2 | 2 | |
| G5b7 | Goals panel + goals data on Solo dashboard (FE+BE) | Frontend v2 / Backend | 4 | |
| G5b14 | Fetch LP trend data for Solo dashboard | Backend | 2 | |
| G6 | Implement Duo dashboard v2 view | Frontend v2 | 5 | |
| G7 | Implement Team dashboard v2 view | Frontend v2 | 5 | |
| G10 | Implement user dropdown details & account settings page | Frontend v2 | 8 | |

**P1 Total:** 85 points

### P2 - Medium

| ID | Title | Epic | Points | Status |
|----|-------|------|--------|--------|
| B13 | Create goal recommendation UI | AI Goals | 3 | |
| B14 | Create active goals display | AI Goals | 3 | |
| B15 | Create goal progress chart | AI Goals | 2 | |
| C15 | Create founding member pricing | Subscription | 2 | |
| D4 | Server-side event tracking | Analytics | 2 | |
| D5 | Create key dashboards | Analytics | 2 | |
| D6 | Create internal metrics endpoint | Analytics | 2 | |
| D7 | Set up error tracking | Analytics | 2 | |
| D9 | Show login activity heatmap on user page | Analytics | 3 | |
| D10 | Implement cookie consent & preferences | Analytics | 2 | |
| E7 | Remove v1 database tables and repositories | Database v2 | 2 | |
| F6 | Deprecate or migrate v1 endpoints to v2 | API v2 | 2 | |
| F10 | Audit async methods for CancellationToken usage | API v2 | 3 | |
| G8 | Remove legacy dashboard views & routes | Frontend v2 | 1 | |
| G11 | Implement friends management UI scaffolding | Frontend v2 | 3 | |

**P2 Total:** 34 points

### P3 - Low

| ID | Title | Epic | Points | Status |
|----|-------|------|--------|--------|
| B16 | Implement Anthropic client | AI Goals | 2 | |
| B17 | Conversational follow-up | AI Goals | 5 | |
| C16 | Create referral tracking | Subscription | 2 | |
| D8 | Implement A/B testing | Analytics | 2 | |

**P3 Total:** 11 points

---

## Grand Totals

| Category | Points |
|----------|--------|
| **Remaining** | 187 pts |
| **Completed** | 73 pts |
| **Grand Total** | 260 pts |

---

## Recommended Sprint Plan

### Sprint 0: Platform v2 Foundation ✅ IN PROGRESS
**Focus:** Database v2 + API v2 + Solo dashboard v2 + Auth + Account Linking + Real-time Sync
**Points:** ~98 (79 completed, 19 remaining)

- ✅ E1, E2, E3 (Database v2 schema & repositories)
- ✅ E4, E5 (v2 ingestion: matches, participants, timeline & metrics)
- ✅ F1, F2 (API v2 design + Solo dashboard endpoint)
- ✅ F7, F11 core (Session auth + User auth endpoints)
- ✅ F12 (Riot account linking endpoints)
- ✅ F13 (WebSocket sync progress endpoint)
- ✅ F14 (V2 Match History Sync Job)
- ✅ G1, G2 (App v2 IA & shell)
- ✅ G5a, G5b0 (Dashboard Hub & Solo Dashboard design)
- ✅ G5b1 (Empty Solo dashboard view & routing)
- ✅ G5b2 (Profile header card + profile data FE+BE)
- ✅ G5b8 (Add profile_icon_id and summoner_level columns)
- ✅ G5b9, G5b10 (Fetch & expose profile data on backend)
- ✅ G9 (User login, signup, verification & `/app/user` shell)
- ✅ G12 (Riot account linking UI)
- ✅ G13 (Real-time sync progress UI)
- ⬜ G5b3-b6 (User dashboard frontend components)
- ⬜ G5b11-b13 (User dashboard backend endpoints)
- ⬜ G5b16 (Update database on login)

### Sprint 1: Foundation (P0 Core)
**Focus:** Database + Stripe + Basic AI
**Points:** ~20

- C1, C2, C3 (Stripe + DB setup)
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
**Points:** ~25

- C9, C10, C11, C12, C13, C14 (Frontend subscription)
- C17 (2-tier pricing model + Guests + collaboration/goal paywalls)
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
| Services | `server/Application/Services/` |
| DTOs | `server/Application/DTOs/` |
| Vue Components | `client_v2/src/components/` |
| Vue Views | `client_v2/src/views/` |
| Composables | `client_v2/src/composables/` |
| API Client | `client_v2/src/api/` |

