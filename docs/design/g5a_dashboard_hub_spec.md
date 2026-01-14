# G5a — Dashboard Hub (/app/user) UX Specification

Status: Draft (Option A selected)

## Summary

Option A: Use a unified grid that mixes Duos and Teams in the same collection, with a single view toggle that switches the shared collection between Grid and Table. Notifications show pending badges and a dropdown preview (up to 3 items) before a "View All" action. Users without a linked Riot account cannot be in a Duo/Team; show a blocking CTA to link accounts. Data refreshes on login/reload and pushes near real-time updates via WebSocket after match completion.

## Layout & Navigation

- Top bar: App shell header with Notification badge, account menu.
- Main content sections in order:
  - Solo card (personal context summary)
  - Duo/Team unified grid (with global Grid/Table toggle)
  - Create Duo/Team CTAs (shown when none exist)

### Solo Card
- Fields: profile icon, Riot ID `gameName#tagline`, summoner level, overall winrate (respecting queue defaults), solo/duo rank, flex rank.
- Interactions:
  - Click card → opens Solo dashboard at `/app/solo`.
  - Shows sync indicator when background sync is running.
  - If no linked Riot account → block with CTA "Link Riot Account" (cannot enter duos/teams).

### Duo/Team Unified Grid
- A single collection composed of Duo and Team items.
- Global view toggle (Grid/Table) applies to the entire collection.
- Sorting: recent activity (syncs/matches) desc by default; secondary: alphabetical by name.
- Card sizing: medium density; show key stats at-a-glance without truncation.

#### Duo Card (grid mode)
- Visual: two member icons side-by-side; duo name; winrate; games played.
- Badges: pending invites count, pending actions, or syncing indicator.
- Click → opens Duo dashboard (Pro gating applies: show upgrade prompt if feature not available).

#### Team Card (grid mode)
- Visual: up to five member icons; team name; winrate; games played.
- Badges: pending invites/actions, syncing indicator.
- Click → opens Team dashboard (Pro gating applies).

#### Table mode
- Columns: Type (Duo/Team), Name, Members (compact list), Games, Winrate, Status (invites/sync), Actions.
- Responsive: horizontal scroll on small screens; sticky header.

## Notifications

- Badge in top nav: shows count of pending invites/goals/important sync updates.
- Dropdown preview: show up to 3 newest items; then a "View All" link to the notification center.
- Item types and shape:
  - Invite: `{ id, type: 'invite', context: 'duo|team', fromUser, groupName, createdAt, href }`
  - Goal: `{ id, type: 'goal', title, progressPercent, createdAt, href }`
  - Sync/update: `{ id, type: 'sync', message, createdAt, href }`
- Accessibility: keyboard navigable, focus trapping in dropdown, ARIA roles for list items.

### Sync Progress in Notifications
- Include `sync_progress`, `sync_complete`, `sync_error` items in the preview list when active.
- Preview shows up to 3 newest sync-related items before "View All".
- Clicking a sync item focuses the relevant account or group and opens details (or a retry action for errors).

## Empty States & Entitlements

- No linked Riot account:
  - Blocking state above all content with explanation: "Link your Riot account to start."
  - CTA: "Link Riot Account" → opens existing link flow.
  - Duo/Team creation disabled; show tooltip: "Link an account first".
- No Duos/Teams:
  - Prominent CTAs: "Create Duo", "Create Team".
  - Contextual help text: "Collaborate with friends to track shared performance."
- Feature gating:
  - Duo/Team dashboards and collaboration features require Pro.
  - Show upgrade prompt when accessing gated views from cards.

## Syncing UX (WebSocket-driven)

- Global indicator:
  - When any linked account is syncing, show a subtle spinner badge in the top bar next to the Notifications icon.
  - Tooltip: "Syncing matches…" with a brief status.
- SoloCard:
  - Displays a compact progress bar (e.g., 45/100) and a syncing badge when active.
  - Shows "Last synced" timestamp when idle; switches to spinner + progress during active sync.
- Duo/Team Cards:
  - Cards show a syncing badge and a compact progress bar if any member's account is currently syncing.
  - Status text: "Syncing" / "Waiting" / "Completed"; error state shows a retry affordance.
- Live updates:
  - Progress is streamed from `SyncProgressHub` via WebSocket.
  - UI updates are non-blocking; cards remain clickable with current data while new data streams in.
- Idle detection:
  - When the app becomes visible after idle, trigger a lightweight refresh that may start a sync if needed (see AppLayout behavior).

## Data Refresh & Live Updates

- On login/reload: auto-check for new matches and profile changes (see F14) and update UI accordingly.
- Near real-time after match completion:
  - Note: Riot API has no push/webhook for matches; implement periodic polling with safe cooldowns, then push updates to the session via WebSocket.
  - Poll strategy: check for latest match id per linked `puuid` at a conservative interval (e.g., 2–5 min) respecting rate limits; debounce frequent checks.
  - When new match detected → trigger sync job → stream progress to client via `SyncProgressHub`.

## Component Breakdown

- `SoloCard` — personal summary + link to solo dashboard.
- `DuoTeamGrid` — unified collection with global Grid/Table toggle.
- `DuoCard`, `TeamCard` — item renderers for the unified grid.
- `GridTableToggle` — persistent toggle control for the collection view.
- `NotificationBadge` + `NotificationsDropdown` — badge & preview in top nav.
- `SyncStatusBadge` + `SyncProgressBar` — reusable sync indicators for cards and header.
- `CreateGroupCtas` — guided creation area when no duos/teams exist.

## Persistence & Settings

- View mode persistence: `localStorage['hubViewMode'] = 'grid' | 'table'` (default: `grid`).
- Remember last sort choice per user (optional, future enhancement).

## Data Shapes (Frontend contracts)

- Solo summary:
  ```json
  {
    "riotId": { "gameName": "Fidel", "tagLine": "EUW" },
    "profileIconId": 1234,
    "summonerLevel": 271,
    "overallWinRate": 54.2,
    "rankedSolo": { "tier": "PLATINUM", "division": "IV", "lp": 37 },
    "rankedFlex": { "tier": "GOLD", "division": "I", "lp": 12 },
    "syncStatus": "idle|pending|running|error",
    "lastUpdated": "2026-01-13T20:00:00Z"
  }
  ```

- Duo summary:
  ```json
  {
    "id": "duo_123",
    "type": "duo",
    "name": "Bot Lane Bros",
    "members": [
      { "puuid": "...", "riotId": "PlayerA#EUW", "profileIconId": 3456 },
      { "puuid": "...", "riotId": "PlayerB#EUW", "profileIconId": 7890 }
    ],
    "games": 42,
    "winRate": 57.1,
    "pendingInvitesCount": 0,
    "status": "ok|syncing|invite_pending",
    "updatedAt": "2026-01-13T20:00:00Z"
  }
  ```

- Team summary:
  ```json
  {
    "id": "team_456",
    "type": "team",
    "name": "Late Game Kings",
    "members": [
      { "puuid": "...", "riotId": "Topper#EUW", "profileIconId": 1111 },
      { "puuid": "...", "riotId": "Jungle#EUW", "profileIconId": 2222 },
      { "puuid": "...", "riotId": "Mid#EUW", "profileIconId": 3333 },
      { "puuid": "...", "riotId": "Adc#EUW", "profileIconId": 4444 },
      { "puuid": "...", "riotId": "Support#EUW", "profileIconId": 5555 }
    ],
    "games": 28,
    "winRate": 50.0,
    "pendingInvitesCount": 1,
    "status": "ok|syncing|invite_pending",
    "updatedAt": "2026-01-13T20:00:00Z"
  }
  ```

- Notification preview item:
  ```json
  {
    "id": "notif_789",
    "type": "invite|goal|sync_progress|sync_complete|sync_error",
    "title": "Team invite from PlayerA",
    "createdAt": "2026-01-13T19:40:00Z",
    "href": "/app/notifications"
  }
  ```

## Acceptance Notes (Sync UX)
- Top bar shows a sync spinner when any account is syncing.
- SoloCard and Duo/Team cards display progress bars during sync.
- Notifications preview lists sync items (up to 3) and links to details.
- UI remains interactive; data refreshes progressively without hard blocking.

## Responsive Behavior

- Mobile:
  - Solo card stacks vertically; badges reposition below title.
  - Grid view becomes single-column cards; Table view enables horizontal scroll with compact columns.
  - Notifications dropdown uses a full-screen sheet pattern for readability.
- Tablet/Desktop:
  - Grid uses 2–4 columns depending on width; dynamic wrapping.
  - Table maintains sticky header with min column widths.

## Accessibility

- Color contrast meets WCAG AA; avoid color-only status indicators.
- Keyboard support for toggles, dropdowns, and card actions.
- ARIA roles/labels for badge counts and notification list.

## Backend Dependencies & Notes

- Relies on API endpoints (`/api/v2/`) for:
  - Solo summary (F2) including `profileIconId` and `summonerLevel` (see G5b8–G5b10).
  - Duo/Team summaries (F3, F4) for aggregates and invite status.
  - Social endpoints (F11-social) for invites/memberships.
  - Sync progress WebSocket (`SyncProgressHub`) for live updates.
- Auto-sync behavior on login/reload via F14.
- Near real-time updates require safe polling; adhere to Riot API rate limits.

## Open Questions (tracked)

- Final sort/pin behavior for Duo/Team cards (manual pinning?) — future iteration.
- Whether to include recent goals directly on the hub or keep them in Solo/Duo/Team dashboards only.
