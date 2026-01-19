# Product Plan

This document provides an overview of the project plan, sprint schedule, and task status.

For full task details, see:
- [product_backlog.md](./product_backlog.md) - Active tasks
- [product_backlog_completed.md](./product_backlog_completed.md) - Completed tasks with full acceptance criteria

---

## Vision

> **"Mongoose is the solo queue improvement tracker—built to help you climb with better champ select picks and post-game takeaways, with a Premium mode that lets your duo or team set goals and improve together."**
>
> “Not just another builds app - Mongoose helps you improve between games and track progress over time.”

## Completed Tasks

| ID | Title | Epic | Points | Status |
|----|-------|------|--------|--------|
| C3 | Add tier column to User | Subscription | 1 | ✅ |
| C10 | Add tier info to user endpoints | Subscription | 1 | ✅ |
| C11 | Create subscription status component | Subscription | 2 | ✅ |
| C13 | Create pricing page | Subscription | 3 | ✅ |
| E1 | Finalize database schema & DDL | Database | 3 | ✅ |
| E2 | Create MySQL schema scripts | Database | 2 | ✅ |
| E3 | Implement entities and repositories | Database | 3 | ✅ |
| E4 | Ingest match & participant core data | Database | 3 | ✅ |
| E5 | Ingest timeline & derived metrics | Database | 5 | ✅ |
| E6 | Validate database metrics against Riot | Database | 2 | ✅ |
| E7 | Remove legacy database tables and repositories | Database | 2 | ✅ |
| F1 | Define API surface & versioning | API | 2 | ✅ |
| F2 | Implement Solo dashboard endpoint | API | 3 | ✅ |
| F6 | Deprecate or migrate legacy endpoints | API | 2 | ✅ |
| F7 | Implement authenticated access for backend | API | 3 | ✅ |
| F11 | Implement user auth endpoints (core) | API | 5 | ✅ |
| F12 | Implement Riot account linking endpoints | API | 5 | ✅ |
| F13 | Implement WebSocket endpoint for sync progress | API | 5 | ✅ |
| F14 | Implement Match History Sync Job | API | 8 | ✅ |
| G1 | Define app IA & routes | Frontend | 2 | ✅ |
| G2 | Implement new app shell & navigation | Frontend | 3 | ✅ |
| G3 | Implement new public landing page | Frontend | 2 | ✅ |
| G4 | Implement pricing page | Frontend | 2 | ✅ |
| G5a | Dashboard Hub design (/app/user) | Frontend | 2 | ✅ |
| G5b0 | Solo Dashboard design | Frontend | 2 | ✅ |
| G5b1 | Create empty Solo dashboard view & routing | Frontend | 1 | ✅ |
| G5b2 | Profile header card + profile data (FE+BE) | Frontend / API | 5 | ✅ |
| G5b3 | Main champion card + main champions data (FE+BE) | Frontend / API | 5 | ✅ |
| G5b8 | Add profile_icon_id and summoner_level to riot_accounts | Database | 1 | ✅ |
| G5b9 | Fetch and store profile data during account linking | Backend | 2 | ✅ |
| G5b10 | Update User dashboard endpoint with profile data | Backend | 1 | ✅ |
| G5b11 | Create champion matchups endpoint | Backend | 3 | ✅ |
| G5b12 | Fetch main champions by role for Solo dashboard | Backend | 2 | ✅ |
| G5b16 | Update database on login (FE+BE) | Frontend / API | 2 | ✅ |
| G5b17 | Implement ranked data display in ProfileHeaderCard (FE+BE) | Frontend / API | 5 | ✅ |
| G9 | Implement user login, signup, verification & `/app/user` shell | Frontend | 5 | ✅ |
| G12 | Implement Riot account linking on `/app/user` | Frontend | 5 | ✅ |
| G13 | Implement real-time match sync progress via WebSocket | Frontend | 5 | ✅ |

**Completed Total:** 113 points

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
| C3 | Add tier column to User | Subscription | 1 | ✅ |
| C4 | Create Mollie customer service | Subscription | 2 | |
| C5 | Create subscription management service | Subscription | 3 | |
| C6 | Create Mollie webhook handler | Subscription | 3 | |
| C7 | Create subscription endpoints | Subscription | 2 | |
| C8 | Create feature flag service | Subscription | 2 | |
| E1 | Finalize database schema & DDL | Database | 3 | ✅ |
| E2 | Create MySQL schema scripts | Database | 2 | ✅ |
| E3 | Implement entities and repositories | Database | 3 | ✅ |
| E4 | Ingest match & participant core data | Database | 3 | ✅ |
| E5 | Ingest timeline & derived metrics | Database | 5 | ✅ |
| F1 | Define API surface & versioning | API | 2 | ✅ |
| F2 | Implement Solo dashboard endpoint | API | 3 | ✅ |
| F3 | Implement Duo dashboard endpoint | API | 3 | |
| F7 | Implement authenticated access for backend | API | 3 | ✅ |
| F11 | Implement user auth endpoints (core) | API | 5 | ✅ |
| F12 | Implement Riot account linking endpoints | API | 5 | ✅ |
| F13 | Implement WebSocket endpoint for sync progress | API | 5 | ✅ |
| F14 | Implement Match History Sync Job | API | 8 | ✅ |
| G1 | Define app IA & routes | Frontend | 2 | ✅ |
| G2 | Implement new app shell & navigation | Frontend | 3 | ✅ |
| G3 | Implement new public landing page | Frontend | 2 | ✅ |
| G5a | Dashboard Hub design (/app/user) | Frontend | 2 | ✅ |
| G5b0 | Solo Dashboard design | Frontend | 2 | ✅ |
| G5b1 | Create empty Solo dashboard view & routing | Frontend | 1 | ✅ |
| G5b2 | Profile header card + profile data (FE+BE) | Frontend / API | 5 | ✅ |
| G5b3 | Main champion card + main champions data (FE+BE) | Frontend / API | 5 | ✅ |
| G5b4 | Winrate Over Time chart + trend data (FE+BE) | Frontend / API | 5 | |
| G5b5 | LP Over Time chart (frontend UI) | Frontend | 2 | |
| G5b6 | Champion matchups table + endpoint (FE+BE) | Frontend / API | 6 | |
| G5b8 | Add profile_icon_id and summoner_level to riot_accounts | Database | 1 | ✅ |
| G5b9 | Fetch and store profile data during account linking | Backend | 2 | ✅ |
| G5b10 | Update User dashboard endpoint with profile data | Backend | 1 | ✅ |
| G5b11 | Create champion matchups endpoint | Backend | 3 | ✅ |
| G5b12 | Fetch main champions by role for Solo dashboard | Backend | 2 | ✅ |
| G5b16 | Update database on login (FE+BE) | Frontend / API | 2 | ✅ |
| G5b17 | Implement ranked data display in ProfileHeaderCard (FE+BE) | Frontend / API | 5 | ✅ |
| G9 | Implement user login, signup, verification & `/app/user` shell | Frontend | 5 | ✅ |
| G12 | Implement Riot account linking on `/app/user` | Frontend | 5 | ✅ |
| G13 | Implement real-time match sync progress via WebSocket | Frontend | 5 | ✅ |

**P0 Remaining:** 39 points | **P0 Completed:** 109 points | **P0 Total:** 148 points

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
| C10 | Add tier info to user endpoints | Subscription | 1 | ✅ |
| C11 | Create subscription status component | Subscription | 2 | ✅ |
| C12 | Create upgrade prompt component | Subscription | 2 | |
| C13 | Create pricing page | Subscription | 3 | ✅ |
| C14 | Gate features based on tier | Subscription | 2 | |
| C17 | Implement 2-tier pricing (Free + Pro) + Guests + collaboration/goal paywalls | Subscription | 5 | |
| D1 | Set up analytics provider | Analytics | 2 | |
| D2 | Implement core tracking events | Analytics | 3 | |
| D3 | Track page views and sessions | Analytics | 1 | |
| E6 | Validate database metrics against Riot | Database | 2 | ✅ |
| F4 | Implement Team dashboard endpoint | API | 3 | |
| F5 | Implement AI snapshot/goal input endpoint | API | 3 | |
| F8 | Implement unified error handling & problem responses | API | 3 | |
| F9 | Add backend tests with focus on security | API | 3 | |
| F11-social | Implement social endpoints (friends, teams, search) | API | 3 | |
| F13-lp | Implement Riot League API for rank/LP data | API | 5 | |
| F14-login | Check for new matches on user login and auto-sync | API | 3 | |
| G3 | Implement new public landing page | Frontend | 2 | ✅ |
| G4 | Implement pricing page | Frontend | 2 | ✅ |
| G5b7 | Goals panel + goals data on Solo dashboard (FE+BE) | Frontend / Backend | 4 | |
| G5b13 | Fetch winrate trend data for Solo dashboard | Backend | 2 | |
| G5b14 | Fetch LP trend data for Solo dashboard | Backend | 2 | |
| G5b15 | Goals array in Solo endpoint | Backend | 2 | |
| G6 | Implement Duo dashboard view | Frontend | 5 | |
| G7 | Implement Team dashboard view | Frontend | 5 | |
| G10 | Implement user dropdown details & account settings page | Frontend | 8 | |

**P1 Remaining:** 72 points | **P1 Completed:** 13 points | **P1 Total:** 85 points

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
| E7 | Remove legacy database tables and repositories | Database | 2 | ✅ |
| F6 | Deprecate or migrate legacy endpoints | API | 2 | ✅ |
| F10 | Audit async methods for CancellationToken usage | API | 3 | |
| G8 | Remove legacy dashboard views & routes | Frontend | 1 | |
| G11 | Implement friends management UI scaffolding | Frontend | 3 | |

**P2 Remaining:** 30 points | **P2 Completed:** 4 points | **P2 Total:** 34 points

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
| **Remaining** | 151 pts |
| **Completed** | 113 pts |
| **Grand Total** | 264 pts |

---

## Recommended Sprint Plan

### Sprint 0: Platform Foundation ✅ COMPLETE
**Focus:** Database + API + Solo dashboard + Auth + Account Linking + Real-time Sync
**Points:** ~109 (all completed)

- ✅ E1, E2, E3 (Database schema & repositories)
- ✅ E4, E5, E6, E7 (Ingestion: matches, participants, timeline & metrics + validation + cleanup)
- ✅ F1, F2 (API design + Solo dashboard endpoint)
- ✅ F6 (Deprecate legacy endpoints)
- ✅ F7, F11 core (Session auth + User auth endpoints)
- ✅ F12 (Riot account linking endpoints)
- ✅ F13 (WebSocket sync progress endpoint)
- ✅ F14 (Match History Sync Job)
- ✅ G1, G2 (App IA & shell)
- ✅ G3, G4 (Public landing page + pricing page)
- ✅ G5a, G5b0 (Dashboard Hub & Solo Dashboard design)
- ✅ G5b1 (Empty Solo dashboard view & routing)
- ✅ G5b2 (Profile header card + profile data FE+BE)
- ✅ G5b3 (Main champion card + main champions data FE+BE)
- ✅ G5b8 (Add profile_icon_id and summoner_level columns)
- ✅ G5b9, G5b10 (Fetch & expose profile data on backend)
- ✅ G5b11, G5b12 (Champion matchups + main champions endpoints)
- ✅ G9 (User login, signup, verification & `/app/user` shell)
- ✅ G12 (Riot account linking UI)
- ✅ G13 (Real-time sync progress UI)
- ✅ G5b16 (Update database on login)
- ✅ G5b17 (Implement ranked data display in ProfileHeaderCard)
- ✅ C3, C10, C11, C13 (Tier column, tier info, subscription status, pricing page)

### Sprint 1: Solo Dashboard Completion
**Focus:** Complete Solo dashboard components + backend data
**Points:** ~14

- G5b4, G5b5, G5b6, G5b7 (Winrate chart, LP chart, Matchups table, Goals panel)
- G5b13, G5b14, G5b15 (Backend: winrate trend, LP trend, goals data)

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

- C9, C12, C14, C17 (Feature gates + upgrade prompts + 2-tier pricing)
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
| Services | `server/Application/Services/` |
| DTOs | `server/Application/DTOs/` |
| Vue Components | `client/src/components/` |
| Vue Views | `client/src/views/` |
| Composables | `client/src/composables/` |
| API Client | `client/src/api/` |

