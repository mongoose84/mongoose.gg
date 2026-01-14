# Product Backlog - Completed Tasks

This file contains tasks that have been completed and moved from the main [product_backlog.md](./product_backlog.md).

---

# Epic E: Database v2 & Analytics Schema (Completed Tasks)

### E1. [Database] Finalize Database v2 schema & DDL ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Architecture
**Estimate:** 3 points
**Labels:** `database`, `v2`, `epic-e`

#### Description

Finalize the Pulse Database v2 schema (tables, columns, indexes) based on `docs/database_schema_v2.md` for matches, participants, checkpoints, metrics, duo/team analytics and AI snapshots.

#### Acceptance Criteria

- [x] Consolidated ERD / schema documented in `docs/database_schema_v2.md`
- [x] Tables defined for: `matches`, `participants`, `participant_checkpoints`, `participant_metrics`, `team_objectives`, `participant_objectives`, `duo_metrics`, `team_match_metrics`, `team_role_responsibility`, `ai_snapshots`
- [x] `matches.queue_id` present (numeric Riot queue id) and used for queue filtering across v2 dashboards
- [x] Index strategy defined for common filters (puuid, queue_id, season/patch, team_id, minute_mark)

---

### E2. [Database] Create MySQL schema scripts for Database v2 ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Database Migration
**Estimate:** 2 points
**Depends on:** E1
**Labels:** `database`, `migration`, `v2`, `epic-e`

#### Description

Create SQL scripts (or migrations) to create all Database v2 tables and indexes in MySQL.

#### Acceptance Criteria

- [x] `schema-v2.sql` (or equivalent migration) creates all v2 tables and indexes
- [x] Script can be applied to a clean database without errors
- [x] Script is safe to re-run on an empty DB (idempotent for local dev)

---

### E3. [Repository] Implement v2 entities and repositories ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Depends on:** E1, E2
**Labels:** `repository`, `v2`, `epic-e`

#### Description

Add entity classes and repository types for Database v2 tables under `server/Infrastructure/External/Domain/Entities/` and `server/Infrastructure/External/Database/Repositories/`.

#### Acceptance Criteria

- [x] Entity classes created for all v2 tables
- [x] Repositories expose queries aligned with product needs (solo/duo/team summaries, timelines, derived metrics)
- [x] New repositories use `RepositoryBase` helpers and follow existing patterns

---

### E4. [Sync] Ingest match & participant core data into v2 tables ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Depends on:** E3
**Labels:** `sync`, `riot-api`, `v2`, `epic-e`

#### Description

Update `MatchHistorySyncJob` (and related logic) to write match- and participant-level data from Riot match info into the v2 `matches` and `participants` tables.

#### Acceptance Criteria

- [x] New writes to `matches` and `participants` occur for all synced matches
- [x] At least one test account can be fully synced into v2 tables
- [x] Basic solo stats queries using v2 repositories return expected values (win rate, KDA, CS, etc.)

---

### E5. [Sync] Ingest timeline & derived metrics into v2 tables ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points
**Depends on:** E4
**Labels:** `sync`, `timeline`, `statistics`, `v2`, `epic-e`

#### Description

Extend the sync pipeline to call Riot timeline endpoints and populate `participant_checkpoints`, `participant_metrics`, `team_objectives`, `participant_objectives`, `duo_metrics`, and `team_match_metrics`.

#### Acceptance Criteria

- [x] Timeline data fetched for synced matches (respecting rate limits)
- [x] Checkpoints stored at key minute marks (10/15/20/25 etc.)
- [x] Derived metrics (kill participation, damage share, death timings, gold leads, duo/team metrics) are persisted
- [x] Core solo/duo/team analytics can be served from v2 tables without additional Riot calls

---

# Epic F: API v2 (Completed Tasks)

### F1. [Design] Define API v2 surface & versioning ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Architecture
**Estimate:** 2 points
**Depends on:** E1
**Labels:** `api`, `v2`, `epic-f`

#### Description

Design the v2 API surface (routes, DTOs, versioning strategy) for solo, duo, team dashboards and AI/goal endpoints.

#### Acceptance Criteria

- [x] API v2 route scheme decided (e.g. `/api/v2/...`)
- [x] Request/response models defined for solo/duo/team summary endpoints
- [x] Response shapes optimized for frontend dashboards (minimal client-side aggregation)
- [x] Standardize optional queue filtering for v2 endpoints (e.g. `queueType=ranked_solo|ranked_flex|normal|aram|all`)

---

### F2. [API] Implement Solo dashboard v2 endpoint ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Depends on:** E3, E4, F1
**Labels:** `api`, `solo`, `v2`, `epic-f`

#### Description

Create a v2 endpoint that returns all data required for the Solo dashboard (overall stats, champion performance, role distribution, death efficiency, match duration, etc.) from Database v2.

#### Acceptance Criteria

- [x] Endpoint implemented (e.g. `GET /api/v2/solo/dashboard/{userId}`)
- [x] Uses only v2 repositories
- [x] Returns a single well-structured payload consumed by the new Solo dashboard view
- [x] Supports optional queue filtering via the standardized v2 queue filter

---

### F7. [Security] Implement authenticated access for backend ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Security
**Estimate:** 3 points
**Depends on:** F1
**Labels:** `security`, `auth`, `api`, `epic-f`

#### Description

Protect the .NET backend with cookie-based session authentication so only authorized clients can call the server.

#### Acceptance Criteria

- [x] Configure session middleware (httpOnly, secure, SameSite=Lax cookies) for all v2 endpoints
- [x] Create a login endpoint that validates credentials and sets a session cookie
- [x] Require valid session for non-public endpoints; missing/expired → `401 Unauthorized`
- [x] Session timeout configurable via appsettings (default 30 min)
- [x] Authorization policies can be applied per-route where needed
- [x] Local development can relax cookie.SecurePolicy via configuration
- [x] Authentication failures are logged without exposing user data

---

### F11. [API] Implement user auth, profile & social endpoints ✅ COMPLETE (Core Auth)

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points
**Labels:** `api`, `auth`, `users`, `epic-f`

#### Description

Provide API endpoints for user login, user profile data, and managing friends/duos/teams so dashboards and payments are associated with a user.

#### Acceptance Criteria

- [x] Implement basic user authentication endpoints (e.g. `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout`)
- [x] Expose a `GET /api/users/me` endpoint that returns the current user's profile, subscription tier and linked LoL accounts
- [ ] Provide endpoints to manage friends / duo partners and team members (e.g. add/remove friends, manage team roster) *(Moved to F11-social)*
- [ ] Provide a user search endpoint that lets you look up LoL accounts by Riot ID / game name + tagline when creating or linking a user *(Moved to F11-social)*
- [x] All new endpoints are protected by API key authentication and follow the unified error-handling conventions

**Note:** Social endpoints (friends, teams, user search) remain in the main backlog as incomplete work.

---

# Epic G: Frontend v2 App & Marketing (Completed Tasks)

### G9. [Frontend] Implement user login, signup, verification & `/app/user` shell ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points
**Depends on:** F7, F11, G2
**Labels:** `frontend`, `auth`, `users`, `epic-g`

#### Description

Provide working user authentication flows and a minimal in-app shell under `/app/user`, wired to the backend auth endpoints. Include a simple 6-digit verification page, a 7-day "keep me logged in" cookie-based session, and a basic user dropdown (logout + disabled settings) in the `/app/*` header.

#### Acceptance Criteria

- [x] `/auth` supports **login** and **signup** modes using `POST /api/auth/register` and `POST /api/auth/login` from F11
- [x] Signup requires `username`, `email`, and `password` and creates a user record with `emailVerified = false` (or equivalent)
- [x] Username is validated for uniqueness and length/format on the backend; the UI shows specific messages when:
  - Username is already taken
  - Username is too long / invalid
- [x] Auth endpoints treat all user input as parameters (no string-concatenated SQL); tests (either here or in F11) exercise common SQL injection payloads and assert no SQL errors or data leakage
- [x] After successful signup, the user is immediately redirected to a verification screen (e.g. `/auth/verify`) with a 6-digit input
- [x] For this first version, submitting any 6-digit code marks the user as verified in the database (Option A), then routes them into `/app/user`
- [x] Unverified users cannot access any `/app/*` routes; attempts redirect back to the verification screen with an explanatory message
- [x] Login form includes a "Keep me logged in for 30 days" checkbox:
  - When checked, the backend issues an HttpOnly, SameSite=Lax session cookie with a 7-day expiry
  - Each successful login resets the 7-day expiry (new cookie is issued)
  - When unchecked, session lifetime follows the shorter default from F7
- [x] On app load, the frontend calls `GET /api/users/me` (or equivalent) to restore auth state from the cookie-backed session and redirect appropriately
- [x] `/app` routing is wired through the G2 app shell and a route for `/app/user` is added
- [x] `/app/user` renders an initial, minimal user page (welcome text and placeholders for future content such as the login heatmap from D9 and friends list)
- [x] The `/app/*` header shows, in the upper-right corner:
  - User icon/avatar
  - Username
  - Subscription tier label (e.g. "Free"), with the free/solo tier displayed in grey when not paid
- [x] The header with user info and dropdown is only visible on `/app/*` routes (not on marketing routes)
- [x] Clicking the main app logo/icon:
  - Navigates to `/app/user` when the user is logged in
  - Navigates to `/` when the user is not logged in
- [x] Clicking the user icon/username opens a dropdown that includes:
  - A working **Logout** item that calls `POST /api/auth/logout`, clears the session cookie, and navigates back to `/` or `/auth`
  - A **Settings** item that is visible but visually disabled (e.g. greyed out, "Coming soon") and does not navigate yet

---

### G12. [Frontend] Implement Riot account linking on `/app/user` ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points
**Depends on:** G9, F12
**Labels:** `frontend`, `users`, `riot-api`, `epic-g`

#### Description

Allow authenticated users to link one or more Riot accounts to their profile from the `/app/user` page. Linking is non-blocking—users can skip and link later. When an account is linked, match sync starts automatically.

#### Acceptance Criteria

- [x] `/app/user` page displays linked Riot accounts (if any) in a card/list format showing:
  - Game Name#Tag
  - Region
  - Sync status badge (pending, syncing, completed, failed)
  - Progress bar when syncing
  - Last sync timestamp
- [x] The ui-design-guideline.md is used for the right feel
- [x] If no accounts are linked, show a prominent "Link Your Riot Account" card with a "+" button
- [x] User can dismiss/skip the prompt; preference stored in localStorage
- [x] Clicking "+" opens a `LinkRiotAccountModal.vue` with:
  - Game Name input (required)
  - Tag input (required)
  - Region dropdown (euw1, na1, kr, etc.)
  - Validation feedback
  - Submit button that calls `POST /api/v2/users/me/riot-accounts`
- [x] On successful link:
  - Modal closes
  - Account appears in list with "Syncing..." status
  - Match sync starts automatically (triggered by backend)
- [x] On error (account not found, already linked, etc.):
  - Modal shows clear error message
  - User can retry
- [x] Update `authStore` with:
  - `riotAccounts` computed property from user data
  - `hasLinkedAccount` getter
  - `linkRiotAccount(gameName, tagLine, region)` action
  - `refreshUser()` action to re-fetch user data
- [x] Update `GET /api/v2/users/me` response to include `riotAccounts` array (coordinate with F12)
- [x] "Add Another" button visible for users who may link multiple accounts (future feature, can be disabled initially)

---

### F12. [API] Implement Riot account linking endpoints ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points
**Depends on:** F7, F11
**Labels:** `api`, `users`, `riot-api`, `epic-f`

#### Description

Create v2 API endpoints for linking Riot accounts to authenticated users. Store linked accounts in a new `user_riot_accounts` table. Validate account existence via Riot API before linking.

#### Acceptance Criteria

- [x] Validate the existing `riot_accounts` table schema meets requirements:
  | Column | Type | Nullable | Default |
  |--------|------|----------|---------|
  | puuid (PK) | varchar(78) | No | None |
  | user_id | bigint unsigned | No | None |
  | game_name | varchar(100) | No | None |
  | tag_line | varchar(10) | No | None |
  | summoner_name | varchar(100) | No | None |
  | region | varchar(10) | No | None |
  | is_primary | tinyint(1) | Yes | 0 |
  | sync_status | enum('pending','syncing','completed','failed') | Yes | 'pending' |
  | last_sync_at | timestamp | Yes | NULL |
  | created_at | timestamp | Yes | CURRENT_TIMESTAMP |
  | updated_at | timestamp | Yes | CURRENT_TIMESTAMP ON UPDATE |
- [x] Use the `V2RiotAccountsRepository` with CRUD operations
- [x] Create `POST /api/v2/users/me/riot-accounts` endpoint:
  - Request: `{ "gameName": "Faker", "tagLine": "KR1", "region": "euw1" }`
  - Validate Riot account exists via Riot API (`/riot/account/v1/accounts/by-riot-id/{gameName}/{tagLine}`)
  - Check account not already linked to any user (409 Conflict if so)
  - Store link in `riot_accounts` with `sync_status = 'pending'`
  - Trigger match sync job (enqueue or start immediately)
  - Response (201): `{ "puuid": "...", "gameName": "Faker", "tagLine": "KR1", "region": "euw1", "isPrimary": true, "syncStatus": "pending" }`
  - Error responses: 400 (invalid input), 404 (Riot account not found), 409 (already linked)
- [x] Update `GET /api/v2/users/me` to include `riotAccounts` array with sync status
- [x] Create `DELETE /api/v2/users/me/riot-accounts/{puuid}` endpoint
- [x] Create `POST /api/v2/users/me/riot-accounts/{puuid}/sync` endpoint to manually trigger sync retry
- [x] Create `GET /api/v2/users/me/riot-accounts/{puuid}/sync-status` endpoint for polling fallback

---

### F14. [Background] Implement V2 Match History Sync Job ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 8 points
**Depends on:** E4, E5, F12, F13
**Labels:** `background-job`, `sync`, `riot-api`, `epic-f`

#### Description

Create a match history sync job that fetches matches for linked Riot accounts, stores them in tables, and broadcasts progress via WebSocket. Supports automatic triggering on account link, on login, and after idle periods.

#### Acceptance Criteria

- [x] Create `MatchHistorySyncJob` background job that:
  - Picks up accounts with `sync_status = 'pending'` or due for refresh
  - Fetches up to 100 matches from Riot Match API
  - Stores match data in tables (matches, participants, checkpoints, metrics, etc.)
  - Updates `sync_progress` incrementally as matches are processed
  - Broadcasts progress via `IWebSocketBroadcaster` after each match
  - Sets `sync_status = 'completed'` and `last_sync_at = now()` on success
  - Sets `sync_status = 'failed'` with error details on failure
- [x] Sync triggers:
  - **On account link**: Immediately queue sync for new account (100 matches)
  - **On login**: Check if `last_sync_at` > 30 minutes ago; if so, queue sync for new matches only
  - **On manual retry**: `POST .../sync` endpoint queues sync
- [x] Rate limit handling:
  - Respect Riot API rate limits
  - On 429 response, pause and retry with exponential backoff
  - Update `sync_status` to indicate waiting (optional: broadcast wait message)
- [x] Error handling:
  - Partial failure: save progress, mark last successful point
  - Allow resume from last successful match on retry
  - Log errors with context for debugging
- [x] Job runs in background without blocking API responses
- [x] Multiple accounts can sync concurrently (with rate limit awareness)
- [x] Unit tests cover core sync logic (claim pending, update status, progress tracking, stuck job reset)

**Note:** WebSocket broadcasting (`IWebSocketBroadcaster`) will be integrated after F13 is complete. The sync job currently logs progress but does not broadcast yet.

---

# Epic G: Frontend v2 App & Marketing (Completed Tasks)

### G1. [UX] Define app v2 information architecture & routes ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** UX
**Estimate:** 2 points
**Labels:** `frontend`, `ux`, `epic-g`

#### Description

Define the high-level navigation for v2, including marketing pages and in-app routes (e.g. `/`, `/pricing`, `/app/solo`, `/app/duo`, `/app/team`, `/app/settings`).

#### Acceptance Criteria

- [x] Route map documented (public vs app routes)
- [x] Decisions on authentication and how users enter the dashboards
- [x] Mapping from legacy routes to new routes defined

---

### G2. [Frontend] Implement new app shell & navigation ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 3 points
**Depends on:** G1
**Labels:** `frontend`, `layout`, `epic-g`

#### Description

Create a shared layout component for all `/app/*` routes with header, navigation (solo/duo/team/goals/settings), and consistent styling.

#### Acceptance Criteria

- [x] New layout component created and used by all app routes
- [x] Navigation clearly shows Free/Pro/Team feature boundaries (copy can be simple initially)
- [x] Works responsively on desktop and common laptop resolutions

---

### F13. [API] Implement WebSocket endpoint for sync progress ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points
**Depends on:** F12
**Labels:** `api`, `websocket`, `sync`, `epic-f`

#### Description

Create a WebSocket endpoint that broadcasts real-time match sync progress to connected clients. Authenticate connections using the existing session cookie.

#### Acceptance Criteria

- [x] Create WebSocket endpoint at `/ws/sync`
- [x] Authenticate WebSocket connections using session cookie (same auth as HTTP endpoints)
- [x] Reject unauthenticated connections with appropriate close code (4401)
- [x] Implement message types from server to client:
  - `sync_progress`: progress updates during sync
  - `sync_complete`: sync completed successfully
  - `sync_error`: sync failed with error message
- [x] Implement message types from client to server:
  - `subscribe`: subscribe to updates for a specific puuid
  - `unsubscribe`: unsubscribe from updates for a specific puuid
- [x] Create `IWebSocketBroadcaster` service that sync job can call to push updates
- [x] Handle client disconnection gracefully (remove from subscription list)
- [x] Support multiple clients per user (e.g., user has app open in two tabs)

#### Implementation Notes

- Created `IWebSocketBroadcaster` interface in `Infrastructure/WebSocket/`
- Implemented `SyncProgressHub` WebSocket handler with concurrent connection management
- Added message DTOs: `SyncProgressMessage`, `SyncCompleteMessage`, `SyncErrorMessage`
- Integrated broadcaster into `MatchHistorySyncJob`
- Added WebSocket middleware to `Program.cs` with 2-minute keep-alive
- Unit tests in `RiotProxy.Tests/SyncProgressHubTests.cs`

---

### G13. [Frontend] Implement real-time match sync progress via WebSocket ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points
**Depends on:** G12, F13, F14
**Labels:** `frontend`, `websocket`, `sync`, `epic-g`

#### Description

Provide real-time progress feedback when matches are being synced for a linked Riot account. Use WebSocket for live updates. Handle idle detection to trigger sync checks when user returns after inactivity.

#### Acceptance Criteria

- [x] Create `useSyncWebSocket` composable that:
  - Connects to `/ws/sync` WebSocket endpoint on mount
  - Auto-reconnects on disconnect with exponential backoff
  - Provides reactive `syncProgress` Map keyed by puuid
  - Exposes `subscribe(puuid)` to listen for specific account updates
  - Handles message types: `sync_progress`, `sync_complete`, `sync_error`
- [x] `/app/user` page shows real-time progress for syncing accounts:
  - Progress bar updates live as matches are synced
  - Shows "45 / 100 matches synced" text
  - On `sync_complete`: progress bar fills, badge changes to "Completed"
  - On `sync_error`: show error message with "Retry" button
- [x] Implement idle detection in `AppLayout.vue`:
  - Track last active time in localStorage
  - On `visibilitychange` to visible, check idle duration
  - If idle > 30 minutes, call `authStore.refreshUser()` to trigger sync check

#### Implementation Notes

- Created `useSyncWebSocket` composable in `composables/useSyncWebSocket.js`
- Updated `UserPage.vue` with progress bar and real-time status display
- Added idle detection in `AppLayout.vue` with 30-minute threshold
- Unit tests in `test/unit/useSyncWebSocket.spec.js` (19 tests)

---

### G5a. [Design] Dashboard Hub (/app/user) - Context switcher & account management ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Design
**Estimate:** 2 points
**Depends on:** G2, G9
**Labels:** `design`, `frontend`, `ux`, `epic-g`

#### Description

Design the `/app/user` (Dashboard Hub) page: summarize Solo + Duos + Teams and enable quick context switching. The final design selects Option A (unified Duo/Team grid) with a single Grid/Table toggle for both collections.

#### Key Decisions

- Unified Duo/Team grid with one global Grid/Table toggle.
- Notifications badge + dropdown preview (shows up to 3 items, then "View All").
- Users without a linked Riot account cannot be in Duos/Teams; show a blocking CTA to link accounts.
- Near real-time updates via polling + WebSocket (Riot API has no push webhooks); also check for new data on reload/login.

#### Acceptance Criteria

- [x] Complete UX specification document created describing:
  - [x] Card layouts (solo, duo, team) with sketches or wireframes
  - [x] Empty states (no account, no duos, no teams)
  - [x] Notification badge + dropdown interaction flow
  - [x] Mobile responsiveness strategy
  - [x] Accessibility considerations
- [x] Design decisions documented (grid vs. table toggle, card size, data density)
- [x] Component breakdown identified (DuoCard, TeamCard, SoloCard, NotificationBadge, etc.)
- [x] Data shapes defined (what fields each card needs from the backend)
- [x] Spec saved at: `docs/design/g5a_dashboard_hub_spec.md`

#### Completion Notes

- Spec file: [docs/design/g5a_dashboard_hub_spec.md](../design/g5a_dashboard_hub_spec.md)
- This completion unlocks G5b1 and downstream frontend tasks for the hub and solo views.

---

### G5b0. [Design] Solo Dashboard design 

**Priority:** P0 - Critical
**Type:** Design
**Estimate:** 2 points
**Labels:** `frontend`, `design`, `epic-g`

#### Description

Design the Solo Dashboard layout and user experience for v2.

#### Acceptance Criteria

- [x] Wireframes created for Solo Dashboard
- [x] Design reviewed and approved by stakeholders
- [x] Design handed off to frontend team

---

### G5b1. [Frontend] Create empty Solo dashboard view & routing

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 1 point
**Depends on:** G2, G5b0
**Labels:** `frontend`, `solo`, `dashboard`, `epic-g`

#### Description

Create the base `/app/solo` route and view component with basic structure (header, placeholder sections, no data yet). This establishes the layout skeleton so subsequent tasks can fill in the sections.

#### Acceptance Criteria

- [x] Route `/v2/app/solo` added to router configuration
- [x] `SoloDashboard.vue` created with basic structure (divs/sections for each upcoming section)
- [x] View is protected by authentication (unauthenticated users redirected to `/v2/auth`)
- [x] Rendered within the app shell (G2) with correct header and sidebar
- [x] Queue filter dropdown and time range dropdown created (no functionality yet, just UI)
- [x] Placeholder text or loading state for each section visible
- [x] No data displayed (all sections empty until subsequent tasks)

---

### G5b8. [Backend] Add profile_icon_id and summoner_level to riot_accounts table ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Database Migration
**Estimate:** 1 point
**Depends on:** None
**Labels:** `database`, `migration`, `epic-g`

#### Description

Add two columns to the `riot_accounts` table: `profile_icon_id` (string) and `summoner_level` (integer). These are fetched from the Riot API during account linking and displayed on the Solo dashboard Profile Header.

#### Acceptance Criteria

- [x] SQL migration created and tested: `ALTER TABLE riot_accounts ADD COLUMN profile_icon_id VARCHAR(255), ADD COLUMN summoner_level INT`
- [x] Migration is idempotent (safe to run multiple times)
- [x] Entity class updated to include these fields
- [x] No data loss or errors on existing records

---

### G5b2. [FE+BE] Profile header button + profile data (User dashboard) ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 5 points (2 FE + 3 BE; rolls up backend work from G5b9 & G5b10)
**Depends on:** G5b1, F2 (user endpoint needs icon/level/rank data)
**Labels:** `frontend`, `user`, `dashboard`, `component`, `epic-g`

#### Description

Create the Profile Header Card that replaces the button navigating to the solo dashboard. It must contain the user's overall stats at the top of the user dashboard (/app/user): profile icon, summoner name + tag, level, solo/duo rank, flex rank, overall winrate (respects queue filter). When clicked it should navigate to the solo dashboard.

#### Acceptance Criteria

- [x] ProfileHeaderCard component created with icon, name#tagline, level badge, rank badges
- [x] Component styled consistently with app theme (/docs/design/ui-design-guidelines.md)
- [x] Icon loads correctly from Riot CDN
- [x] Responsive: stacks vertically on mobile
- [x] Backend subtasks G5b9 and G5b10 are complete
- [x] User dashboard endpoint returns profileIconId and summonerLevel

---

### G5b9. [Backend] Fetch and store profile data during account linking ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 2 points
**Depends on:** None (G5b8 complete)
**Labels:** `backend`, `riot-api`, `epic-g`

#### Description

Update the account linking flow to fetch summoner profile data (icon ID, level) from Riot API and store it in the `riot_accounts` table.

#### Acceptance Criteria

- [x] Account linking endpoint calls `GetSummonerByPuuIdAsync` (Riot API)
- [x] Extracts `profileIconId` and `summonerLevel` from response
- [x] Stores both in `riot_accounts` table
- [x] If API call fails, gracefully falls back (stores NULL and logs error)

---

### G5b10. [Backend] Update User dashboard endpoint to include profile data ✅ COMPLETE

**Priority:** P0 - Critical
**Type:** Feature
**Estimate:** 1 point
**Depends on:** G5b9, F2
**Labels:** `backend`, `api`, `epic-g`

#### Description

Update the User dashboard endpoint (`/api/users/me`) to include `profileIconId` and `summonerLevel` in the response. These are pulled from the `riot_accounts` table for the user's primary account.

#### Acceptance Criteria

- [x] Endpoint response includes `profileIconId` (integer) and `summonerLevel` (integer)
- [x] Data fetched from `riot_accounts` table
- [x] If no primary account, returns null (no error)

---

## Summary of Completed Work

| Epic | Task | Points | Completed |
|------|------|--------|-----------|
| E | E1 - Database v2 schema & DDL | 3 | ✅ |
| E | E2 - MySQL schema scripts | 2 | ✅ |
| E | E3 - V2 entities and repositories | 3 | ✅ |
| E | E4 - Match & participant ingestion | 3 | ✅ |
| E | E5 - Timeline & derived metrics ingestion | 5 | ✅ |
| F | F1 - API v2 surface design | 2 | ✅ |
| F | F2 - Solo dashboard v2 endpoint | 3 | ✅ |
| F | F7 - Session authentication | 3 | ✅ |
| F | F11 - User auth endpoints (core) | 5 | ✅ |
| F | F12 - Riot account linking endpoints | 5 | ✅ |
| F | F13 - WebSocket endpoint for sync progress | 5 | ✅ |
| F | F14 - V2 Match History Sync Job | 8 | ✅ |
| G | G1 - App v2 IA & routes | 2 | ✅ |
| G | G2 - App shell & navigation | 3 | ✅ |
| G | G9 - Login, signup, verification & user shell | 5 | ✅ |
| G | G12 - Riot account linking on `/app/user` | 5 | ✅ |
| G | G13 - Real-time match sync progress via WebSocket | 5 | ✅ |
| G | G5b0 - Solo Dashboard design | 2 | ✅ |
| G | G5b1 - Create empty Solo dashboard view & routing | 1 | ✅ |
| G | G5b2 - Profile header button + profile data (FE+BE) | 5 | ✅ |
| G | G5b8 - Add profile_icon_id and summoner_level to riot_accounts | 1 | ✅ |
| G | G5b9 - Fetch and store profile data during account linking | 2 | ✅ |
| G | G5b10 - Update User dashboard endpoint with profile data | 1 | ✅ |

**Total Completed Points:** 79