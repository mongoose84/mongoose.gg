# Mongoose - Information Architecture & Routes

**Last Updated:** January 14, 2026
**Epic:** G1 - UX
**Status:** Production
**App Location:** `client/`

---

## Vision & User Evolution

> **"Mongoose is the only LoL improvement tracker built for duos and teams, powered by AI coaching that turns your stats into actionable goals you can actually achieve."**

### User Evolution Path
1. **Solo Player** → Creates account, buys Free/Pro tier, gets AI feedback
2. **Duo Partner** → Invites duo buddy (buddy creates account), both access shared duo dashboard
3. **Team Player** → 2-5 players pool together, one pays Team tier, all access team dashboard

**Key Insight:** Users can belong to multiple duos and teams simultaneously. Navigation must support context-switching.

---

## Route Map

### Public Routes (Unauthenticated)

| Route | Page | Purpose | Priority |
|-------|------|---------|----------|
| `/` | **Homepage** | Marketing landing, pricing tiers, features, contact info | **P0 - Launch** |
| `/auth` | **Auth Page** | Unified login/signup form (toggle mode) | **P0 - Launch** |
| `/verify-email` | **Email Verification** | Enter 6-digit code sent to email | **P0 - Launch** |
| `/reset-password` | **Password Reset** | Request password reset link | **P1 - Post-launch** |
| `/reset-password/:token` | **Set New Password** | Set new password via email token | **P1 - Post-launch** |

**Notes:**
- All public routes redirect to `/app` if user is already authenticated
- `/` homepage includes inline pricing (no separate pricing page)
- Footer on all public pages: Contact, Privacy Policy, Terms of Service

---

### Authenticated Routes (App Shell)

All authenticated routes use a shared app shell with:
- **Top Nav**: Logo, context switcher (Solo/Duo/Team), notifications bell, settings icon, avatar
- **Sidebar** (collapsible on mobile): Quick links to active contexts
- **Main Content Area**: Dashboard or settings content

| Route | Page | Purpose | Priority |
|-------|------|---------|----------|
| `/app` | **Dashboard Hub** | Summary of all active contexts (Solo + Duos + Teams) | **P0 - Launch** |
| `/app/solo` | **Solo Dashboard** | Personal stats, AI goals, performance analytics | **P0 - Launch** |
| `/app/duo/:duoId` | **Duo Dashboard** | Duo synergy stats, shared goals, matchups | **P1 - Post-launch** |
| `/app/team/:teamId` | **Team Dashboard** | Team coordination, role analysis, objectives | **P2 - Future** |
| `/app/settings` | **Settings** | Profile, security, subscription, preferences | **P0 - Launch** |
| `/app/notifications` | **Notifications Center** | All notifications (goals, invites, achievements) | **P1 - Post-launch** |
| `/app/invite/:inviteToken` | **Accept Invite** | Accept duo/team invite (redirects to context) | **P1 - Post-launch** |

**Notes:**
- Unauthenticated users redirected to `/auth`
- Routes respect tier-based feature flags (see Authorization below)

---

## User Flows

### 1. New User Signup & Onboarding

```
/ (Homepage)
  → Click "Get Started" / "Sign Up"
  → /auth (enter email, password, username)
  → POST /api/v2/auth/signup
  → /verify-email (enter 6-digit code)
  → POST /api/v2/auth/verify-email
  → /app (Dashboard Hub)
  → Onboarding wizard (optional):
     - Connect Riot account (enter summoner name + region)
     - Choose tier (Free → show limited features, Pro/Team → Mollie checkout)
  → /app/solo (first dashboard)
```

**Edge Cases:**
- Email already exists → show error, suggest login
- Code expired → resend code button
- Riot account not found → retry or skip (can add later in settings)

---

### 2. Existing User Login

```
/ (Homepage)
  → Click "Login"
  → /auth (toggle to login mode)
  → POST /api/v2/auth/login
  → /app (Dashboard Hub)
```

**Edge Cases:**
- Email not verified → redirect to `/verify-email`
- Forgot password → link to `/reset-password`

---

### 3. Solo → Duo Invitation Flow

```
User A (Pro tier):
  /app/solo
    → Click "Invite Duo Partner"
    → Modal: enter buddy's summoner name or generate invite link
    → Copy link: https://mongoose.gg/app/invite/abc123xyz
    → Share link (Discord, etc.)

User B (Buddy):
  Receives link → clicks
  → /app/invite/abc123xyz
  → If not logged in: redirect to /auth?redirect=/app/invite/abc123xyz
  → After login: auto-join duo, redirect to /app/duo/:duoId
  → Notification: "You joined [User A]'s duo!"
```

**Edge Cases:**
- Invite expired → show error, suggest contacting inviter
- Already in duo → show confirmation modal before joining
- Inviter doesn't have Pro tier → block invite creation

---

### 4. Duo → Team Invitation Flow

```
User A (Team tier payer):
  /app/duo/:duoId
    → Click "Upgrade to Team"
    → Mollie checkout (Team tier)
    → After payment: /app/team/:teamId (new team created)
    → Click "Invite Team Members" (up to 5 total)
    → Generate invite links × 3 (for remaining slots)
    → Share links

Users B, C, D, E:
  Receive link → /app/invite/team-xyz789
  → Same flow as duo invite
  → Redirect to /app/team/:teamId
```

**Edge Cases:**
- Team full (5 members) → disable invite link generation
- Member leaves → slot opens, can invite again

---

### 5. Context Switching (Solo ↔ Duo ↔ Team)

```
User is on /app/solo
  → Top nav: click context switcher dropdown
  → Shows:
     - Solo (you) ✓ current
     - Duo with [Partner A]
     - Duo with [Partner B]
     - Team "[Team Name]"
  → Select "Duo with [Partner A]"
  → Navigate to /app/duo/:duoId
```

**Notes:**
- Active context highlighted in nav
- Quick-switch keyboard shortcut (Cmd+K / Ctrl+K) opens context picker

---

## Authorization & Feature Gating

### Tier-Based Access Matrix

| Feature | Free | Pro | Team |
|---------|------|-----|------|
| **Solo Dashboard** | ✅ Last 20 games, basic stats | ✅ Unlimited games, AI goals | ✅ Full access |
| **Duo Dashboard** | ❌ | ✅ Can create & invite | ✅ Full access |
| **Team Dashboard** | ❌ | ❌ | ✅ Can create & invite |
| **AI Goal Recommendations** | ❌ | ✅ 5/week | ✅ Unlimited |
| **Goal Tracking** | ❌ | ✅ | ✅ |
| **Advanced Analytics** | ❌ | ✅ | ✅ |
| **Queue Filtering** | ✅ Ranked only | ✅ All queues | ✅ All queues |
| **Invited Access** | N/A | ✅ Can join others' duos | ✅ Can join others' teams |

### Special Cases
- **Invited Users**: Can access Duo/Team dashboards even if they only have Free tier (inviter must have Pro/Team)
- **Downgrade**: If Pro user downgrades to Free, existing duo dashboards become read-only (can't create new duos)
- **Team Payer Cancels**: Team becomes read-only until someone else upgrades

---

## Settings Page Structure

### `/app/settings`

**Tab-based layout** (side navigation on desktop, top tabs on mobile):

#### 1. Profile
- Display name (editable)
- Avatar upload
- Email (display only, change via "Account Security")
- Riot accounts (list connected summoner names + regions)
  - Add new account button
  - Set primary account (used for solo dashboard)
  - Remove account

#### 2. Account Security
- **Change Email**
  - Enter new email → sends verification code → confirm
- **Change Password**
  - Enter current password, new password, confirm
- **Two-Factor Authentication** (P2 - future)
  - Enable/disable 2FA

#### 3. Subscription Management
- **Current Tier**: Free / Pro / Team (badge + icon)
- **Billing Info**:
  - Payment method (card last 4 digits)
  - Billing address
  - Update payment method → Mollie modal
- **Billing History**:
  - Table: Date, Amount, Status, Invoice (download PDF)
- **Actions**:
  - Upgrade tier button (if Free/Pro)
  - Cancel subscription button (if Pro/Team)
    - Confirmation modal: "Cancel at end of period?" vs "Cancel immediately?"

#### 4. Preferences
- **Game Preferences**:
  - Primary region (dropdown: EUW, NA, KR, etc.)
  - Primary role (Top, Jungle, Mid, ADC, Support, Fill)
- **Notification Preferences**:
  - Email notifications (toggle):
    - Goal achievements
    - Duo/team invites
    - Weekly progress report
  - In-app notifications (toggle):
    - Real-time goal progress
    - New duo/team activity
- **Display Preferences**:
  - Theme: Light / Dark / Auto (P2 - future)
  - Language: English (P2 - future, i18n)

#### 5. Data & Privacy
- **Data Export**:
  - Download all your data (JSON format)
- **Account Deletion**:
  - Permanently delete account
  - Confirmation modal with password re-entry

---

## Notifications System

### Notification Types

| Type | Trigger | Action | Priority |
|------|---------|--------|----------|
| **Goal Achievement** | Goal target reached | Link to goal details | High |
| **Goal Progress** | Significant progress (25%, 50%, 75%) | Link to goal | Medium |
| **Duo Invite** | Invited to duo | Accept/decline buttons | High |
| **Team Invite** | Invited to team | Accept/decline buttons | High |
| **Duo Joined** | Partner accepted duo invite | Link to duo dashboard | Medium |
| **Team Member Joined** | Member joined team | Link to team dashboard | Low |
| **Subscription Change** | Payment success/failure | Link to settings | High |
| **Weekly Report** | Every Monday (optional) | Link to solo dashboard | Low |

### Notification UI

**Bell Icon (Top Nav)**:
- Badge count (unread notifications)
- Dropdown shows last 5 notifications
- "View All" link → `/app/notifications`

**Notifications Center** (`/app/notifications`):
- Filterable by type (All, Invites, Goals, System)
- Mark as read/unread
- Archive old notifications
- Real-time updates (WebSocket or polling)

---

## Navigation Patterns

### Top Navigation (Authenticated)

```
┌─────────────────────────────────────────────────────────────┐
│ [Logo]  [Context Switcher ▼]    🔔(3)  ⚙️  [Avatar ▼]     │
└─────────────────────────────────────────────────────────────┘
```

**Components**:
1. **Logo**: Click → `/app` (Dashboard Hub)
2. **Context Switcher**: Dropdown showing Solo, Duos, Teams
3. **Notifications Bell**: Badge count, dropdown preview
4. **Settings Icon**: Click → `/app/settings`
5. **Avatar Dropdown**: Profile, Settings, Logout

---

### Sidebar (Collapsible)

**Dashboard Hub** (`/app`):
- No sidebar (uses card-based layout)

**Dashboard Views** (`/app/solo`, `/app/duo/:id`, `/app/team/:id`):
```
┌────────────────┐
│ Overview       │ ← Active
│ Champions      │
│ Performance    │
│ Goals          │ (Pro/Team only)
│ Timeline       │
│ Settings       │ (context settings, not global)
└────────────────┘
```

**Mobile**: Sidebar collapses to hamburger menu (top-left)

---

### Breadcrumbs

Show current location for nested routes:
- `/app/duo/123`: **Dashboard Hub** > **Duo with [Partner]**
- `/app/team/456/goals`: **Dashboard Hub** > **Team [Name]** > **Goals**

---

## Technical Stack (Frontend)

### Core Framework
- **Vue 3** (Composition API only, `<script setup>`)
- **Vite** (fast builds, HMR, optimized for Vue)
- **Vue Router 4** (route-based code splitting)

### Styling & Design
- **Tailwind CSS** (utility-first, fast iteration)
  - CSS variables for theming (light/dark mode prep)
  - Tailwind config as design-token layer (colors, spacing, typography)
- **PostCSS** (Tailwind + autoprefixer)

### Component Libraries
- **Headless UI (Vue)** or **Radix Vue** (accessible primitives):
  - Dropdown menus
  - Modals/dialogs
  - Tabs
  - Toggles/switches
  - No heavy UI kits — build custom look on top
- **Heroicons** (Vue 3, Tailwind-optimized icons)

### Motion & Micro-interactions
- **Motion One** or **VueUse Motion**:
  - Progress bar animations
  - Goal achievement celebrations
  - Delta indicators (↑↓ stats)
  - Page transitions
  - Hover states

### State Management & Data Fetching
- **Pinia** (Vue 3 official state library)
  - User store (auth, tier, profile)
  - Notification store (real-time updates)
  - UI state (sidebar collapsed, active context, theme)
- **Vue Query** (TanStack Query for Vue)
  - API caching & invalidation
  - Automatic refetching & background updates
  - Optimistic updates for goals/stats
  - Query keys: `['solo', userId]`, `['duo', duoId]`, `['team', teamId]`

### API Client
- **Axios** (consistent with legacy client)
- Abstracted in `client/src/api/` (e.g., `auth.js`, `solo.js`, `duo.js`)
- Wrapped with Vue Query hooks (e.g., `useSoloStats()`, `useDuoStats()`)

### Forms & Validation
- **VeeValidate** or **Vuelidate** (Vue 3 compatible)
- Zod schemas for type-safe validation

### Charts & Visualizations
- **Chart.js** (existing pattern) or **Apache ECharts** (more powerful)
- Custom Vue wrappers for radar, line, bar charts

### Testing
- **Vitest** (existing, fast, Vite-native)
- **@vue/test-utils** (Vue 3 testing library)

---

## Implementation Priorities

### P0 - Launch (MVP)
1. Public routes: Homepage (`/`), Auth (`/auth`), Email Verification
2. Authenticated routes: Dashboard Hub (`/app`), Solo Dashboard, Settings
3. Authentication: Login, Signup, Email verification, Session management
4. Subscription: Mollie checkout, tier gating, settings page (subscription tab)
5. Core UI components: Top nav, context switcher, notification bell (badge only)
6. Solo Dashboard: Basic stats, chart cards (using API endpoints, see `docs/api_design.md`)

### P1 - Post-Launch
1. Duo Dashboard: Synergy stats, duo-specific goals
2. Invitation system: Generate links, accept/decline, notifications
3. Notifications Center: Full UI, real-time updates
4. Settings: Full profile, security, preferences tabs
5. Password reset flow

### P2 - Future
1. Team Dashboard: Role analysis, team objectives
2. AI Goal Recommendations: LLM integration, conversational follow-ups
3. Advanced analytics: Timeline deep dives, matchup analysis
4. Theming: Light/dark mode toggle
5. i18n: Multi-language support

---

## Decision Log

### Decisions Made
2. ✅ **Auth flow**: Unified `/auth` page (toggle login/signup)
3. ✅ **Email verification**: Required after signup (6-digit code)
4. ✅ **Pricing**: Inline on homepage (no separate pricing page)
5. ✅ **Invitations**: Shareable links (no username search for MVP)
6. ✅ **Notifications**: Bell icon + dedicated center page
7. ✅ **Settings**: Tab-based, 5 sections (Profile, Security, Subscription, Preferences, Privacy)
8. ✅ **Context switching**: Dropdown in top nav + sidebar links
9. ✅ **Tech stack**: Vue 3 + Tailwind + Headless UI + Motion One

### Open Questions
1. ⏳ **Auth page**: Separate login/signup pages OR unified toggle page? *(User to decide)*
2. ⏳ **Onboarding wizard**: Required after signup, or optional? *(Suggest optional, can skip)*
3. ⏳ **Duo/Team naming**: Can users name their duos/teams? *(Nice-to-have for P1)*
4. ⏳ **Real-time updates**: WebSocket or polling for notifications? *(Polling for MVP, WebSocket for P1)*
5. ⏳ **Chart library**: Stick with Chart.js or migrate to ECharts? *(Stick with Chart.js for consistency)*

---

## Next Steps

### For Product Owner (You)
1. **Review this document**: Approve route map, user flows, settings structure
2. **Decide on open questions**: Especially unified vs separate auth pages
3. **Prioritize features**: Confirm P0/P1/P2 split aligns with business goals
4. **Visual design**: Create wireframes or mockups for key pages (Homepage, Dashboard Hub, Solo Dashboard)

### For Development Team
1. **Initialize project structure**: Update boilerplate (src, public, vite.config.js, package.json)
2. **Scaffold routes**: Define Vue Router routes for all pages in `client/src/router/`
3. **Build app shell**: Top nav, sidebar, layout components in `client/src/components/`
4. **Implement auth flow**: Signup, login, email verification (frontend + backend) using API endpoints
5. **Connect to API**: Use backend endpoints (see `docs/api_design.md`)

---

## References
- **Product Backlog**: [docs/product_backlog.md](./product_backlog.md)
- **API Design**: [docs/api_design.md](./api_design.md)
- **Database Schema**: [docs/database_schema.md](./database_schema.md)
- **Copilot Instructions**: [.github/copilot-instructions.md](../.github/copilot-instructions.md)
- **Existing Login Endpoint**: [server/Application/Endpoints/Auth/LoginEndpoint.cs](../server/Application/Endpoints/Auth/LoginEndpoint.cs)

---

**Document Status**: ✅ Current
**Last Updated**: January 14, 2026
