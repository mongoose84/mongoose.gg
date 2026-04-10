# Cookie Consent Banner — Feature Spec (v2)

> **Status**: Proposed  
> **Date**: April 10, 2026  
> **Author**: UX/UI Design Review  
> **Revision**: v2 — incorporates GDPR/ePrivacy compliance review findings

---

## 1. Problem Statement

Mongoose.gg uses cookies for authentication (session/persistent login cookies). EU ePrivacy Directive and GDPR require informed consent before setting non-strictly-necessary cookies, and transparency about strictly necessary ones. Currently there is no cookie consent mechanism — cookies are set without user awareness.

The "Remember Me" feature (30-day persistent cookie) is controlled by an explicit user choice on the login form. Users who do not check "Remember Me" get a session cookie that clears on browser close or 30-minute idle timeout. This checkbox is itself a consent mechanism for cookie duration.

---

## 2. Cookie Inventory

| Cookie | Type | Purpose | Duration | Strictly Necessary? |
|--------|------|---------|----------|---------------------|
| `mongoose-auth` | Authentication | Encrypted auth ticket (login state) | Session or 30 days | Yes (for logged-in functionality) |
| `mongoose_active_account` | Preference | localStorage, not a cookie — active Riot account selection | Persistent | N/A (localStorage) |

**Key insight**: Mongoose.gg currently only uses one cookie (`mongoose-auth`), and it is a **strictly necessary** authentication cookie. Under GDPR/ePrivacy, strictly necessary cookies do not require consent — but users must be **informed** about them.

The session-only variant and the 30-day persistent variant both serve the same authentication function that the user explicitly requests by logging in. The "Remember Me" checkbox on the login form provides the user with a clear, granular choice about cookie duration. The cookie consent banner therefore operates as an **informational notice with opt-out**, not a multi-tier granular consent mechanism.

---

## 3. Consent Model

### Two-tier approach

| Choice | Label | What happens | Cookie behavior |
|--------|-------|-------------|-----------------|
| **Accept Cookies** | "Accept Cookies" | Full experience — authentication works, "Remember Me" available on login | `mongoose-auth` set per login form preference (session or 30-day persistent) |
| **Reject Cookies** | "Reject Cookies" | No cookies set at all | Auth will not work — user stays on public pages only. Informed of this limitation in the banner. |

### Why two tiers instead of three

The previous three-tier model ("Accept All" / "Functional Only" / "Reject All") attempted to separate session-only auth cookies from persistent auth cookies as different consent categories. This creates a regulatory grey area:

- European DPAs (CNIL, ICO) classify strictly necessary cookies by whether they are essential to a service the user explicitly requested. A user who clicks "Log in" has explicitly requested authentication. Whether that auth lasts for the session or 30 days is an implementation detail of the same service.
- The "Remember Me" checkbox on the login form already provides granular user choice about cookie duration — layering a separate banner category on top creates redundant consent for the same function.
- CNIL's 2020 recommendations consider persistent login cookies as extensions of the authentication function, not a separate preference category.

The two-tier model is cleaner: accept (auth works, "Remember Me" is the user's granular control) or reject (no cookies, public pages only).

---

## 4. UX Design

### 4.1 Banner Layout

**Position**: Fixed bottom of viewport (not top — top is reserved for `SessionExpiredBanner` at z-400).  
**Z-index**: 500 (above everything — this is a legal/consent overlay).  
**Backdrop**: Semi-transparent dark overlay behind the banner to draw focus, but NOT a modal — page content remains visible and scrollable.

```
┌──────────────────────────────────────────────────────────────────────┐
│                        (page content, dimmed)                        │
│                                                                      │
│                                                                      │
│                                                                      │
│                                                                      │
├──────────────────────────────────────────────────────────────────────┤
│  🍪  We use cookies                                                  │
│                                                                      │
│  Mongoose.gg uses an authentication cookie to keep you logged in.    │
│  Without cookies, login and analytics features won't be available.   │
│                                                                      │
│  Learn more in our [Cookie Policy] and [Privacy Policy].             │
│                                                                      │
│  ┌───────────────────┐  ┌───────────────────┐                       │
│  │  Reject Cookies   │  │  Accept Cookies   │                       │
│  └───────────────────┘  └───────────────────┘                       │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**Key design decision — no confirmation step on Reject**: The EDPB Guidelines 05/2020 require that withdrawing or refusing consent must not be more burdensome than giving it. A confirmation step on "Reject" but not on "Accept" creates asymmetric friction (2 clicks vs 1 click) that regulators flag as a dark pattern. Instead, the consequence of rejection is stated clearly in the banner body text *before* the user chooses, so both options are a single click.

### 4.2 Visual Specification

| Element | Token / Style |
|---------|--------------|
| Banner background | `var(--color-surface)` with `backdrop-filter: blur(10px)` |
| Banner border | `border-top: 1px solid var(--color-border)` |
| Banner padding | `var(--spacing-lg)` (24px) vertical, `var(--spacing-xl)` (32px) horizontal |
| Heading | `var(--font-size-lg)` (18px), `font-weight: 600`, `var(--color-text)` |
| Body text | `var(--font-size-sm)` (14px), `var(--color-text-secondary)` |
| Privacy/Cookie Policy link | `var(--color-primary)`, underline on hover |
| "Accept Cookies" button | `BaseButton variant="primary"` — filled purple |
| "Reject Cookies" button | `BaseButton variant="secondary"` — outlined, clearly visible and equally prominent |
| Button order (LTR) | Reject Cookies → Accept Cookies (positive action on the right, matching natural reading flow) |
| Max width | `max-w-[800px]`, centered |
| Border radius | `var(--radius-lg)` (12px) on the banner container (bottom corners: 0) |
| Shadow | `var(--shadow-lg)` upward |
| Z-index | `500` (new layer, above toasts/banners) |
| Entrance animation | Slide up from bottom, 300ms ease-out (matches existing `slide-down` pattern but inverted) |

**Equal prominence requirement (CNIL/EDPB)**: "Accept Cookies" and "Reject Cookies" must have **equal visual weight**. "Accept" uses `variant="primary"` (filled) and "Reject" uses `variant="secondary"` (outlined with visible border/background). Both buttons must be the same size, in the same row, with clear clickable affordance. A reasonable user must not perceive "Reject" as harder to find or less actionable than "Accept."

### 4.3 Responsive Behavior

| Viewport | Layout |
|----------|--------|
| Desktop (>1024px) | Horizontal button row, text above buttons |
| Tablet (640–1024px) | Same, buttons may wrap to second line |
| Mobile (<640px) | Full-width banner, buttons stack vertically (Accept Cookies on top, Reject Cookies on bottom), both full-width to maintain equal prominence |

---

## 5. Persistence & Storage

### 5.1 Where to store consent

**`localStorage`** key: `mongoose_cookie_consent`

| Value | Meaning |
|-------|---------|
| `"accepted"` | Accepted cookies |
| `"rejected"` | Rejected all cookies |
| *(key absent)* | Not yet decided — show the banner |

**Why localStorage and not a cookie**: Using a cookie to remember "I rejected cookies" is ironic and arguably non-compliant. `localStorage` is not a cookie and is not transmitted to the server — it's client-side storage only.

### 5.2 Consent expiry

The banner should re-appear after **6 months** (183 days). This follows the strictest current DPA guidance (CNIL recommends a maximum of 6 months for consent validity). Store `mongoose_cookie_consent_date` as an ISO timestamp. On app load, if the consent is older than 183 days, clear both keys and re-show the banner.

### 5.3 Ability to change preference

Add a "Cookie Preferences" link in:
1. **Footer of all public pages** — next to existing Privacy Policy / Terms of Service links
2. **User Settings page** (`/app/user`) — as a button in settings
3. **Authenticated app layout** — in the user dropdown menu or sidebar footer, reachable in 1–2 clicks from any authenticated page

GDPR Article 7(3) requires: *"It shall be as easy to withdraw consent as to give it."* Consent withdrawal must be accessible from within the authenticated app shell, not only on public pages or the settings page.

Clicking "Cookie Preferences" from any location clears the stored consent and re-shows the banner.

### 5.4 Cross-tab synchronization

The `useCookieConsent` composable **must** listen to `window.addEventListener('storage', ...)` events and reactively update consent state when another tab changes it. Without this, a tab opened before a consent change would continue operating under stale consent, which has compliance implications. When a `storage` event fires for the `mongoose_cookie_consent` key, the composable should update its reactive refs immediately.

---

## 6. Integration Points

### 6.1 Impact on "Remember Me" checkbox

| Consent level | "Remember Me" on login form | Auth cookie behavior |
|---------------|----------------------------|---------------------|
| `"accepted"` | Visible, functional | Per user's checkbox choice: `IsPersistent=true` with 30-day `ExpiresUtc`, or `IsPersistent=false` session cookie |
| `"rejected"` | N/A — login form not accessible (shown with warning) | No cookie set |

### 6.2 Impact on login flow

When consent is `"rejected"`:
- The login form should display an inline info banner: _"You've rejected cookies. Login requires an authentication cookie. [Update cookie preferences]"_
- The "Update cookie preferences" link re-opens the consent banner
- Alternatively, the auth page can show the consent banner automatically if consent is `"rejected"` and the user navigates to `/auth`

### 6.3 Impact on `authStore.initialize()`

On app initialization:
1. Check `localStorage` for `mongoose_cookie_consent`
2. If absent → show banner, do NOT call `getCurrentUser()` (no cookie exists anyway)
3. If `"rejected"` → skip `getCurrentUser()`, set `isAuthenticated = false`
4. If `"accepted"` → proceed normally

### 6.4 Server-side consent enforcement

The consent model is primarily client-side (`localStorage`), but the server must also enforce consent to ensure compliance is not purely cosmetic:

- The login API request body should include a `consentLevel` field (e.g., `"accepted"` or `"rejected"`).
- If `consentLevel` is `"rejected"` or absent, the server must refuse to set the auth cookie and return a `400` with an appropriate error: `{ "error": "Cookie consent is required for login.", "code": "COOKIE_CONSENT_REQUIRED" }`.
- This prevents scenarios where a modified client or direct API call bypasses the frontend consent logic.
- The `LoginRequest` record becomes: `LoginRequest(username, password, rememberMe?, consentLevel?)`.

### 6.5 Where to render

In `App.vue`, alongside `SessionExpiredBanner`:

```
<CookieConsentBanner />    ← new, z-500, bottom-fixed
<SessionExpiredBanner />   ← existing, z-400, top-fixed
<RouterView />
```

The two banners occupy different screen edges and different z-layers, so they can coexist without conflict.

---

## 7. Component Architecture

### New components

| Component | Location | Responsibility |
|-----------|----------|----------------|
| `CookieConsentBanner.vue` | `client/src/components/` | The banner UI, consent logic, localStorage read/write |

### New composable

| Composable | Location | Responsibility |
|------------|----------|----------------|
| `useCookieConsent.js` | `client/src/composables/` | Reactive consent state, `getConsent()`, `setConsent()`, `resetConsent()`, `isConsentExpired()`, cross-tab `storage` event listener |

### New page (required)

| Page | Location | Responsibility |
|------|----------|----------------|
| Cookie Policy page | `client/src/views/CookiePolicyPage.vue` | Dedicated `/cookies` page listing each cookie with name, purpose, type, duration, and data controller (see §10) |

### Modified components

| Component | Change |
|-----------|--------|
| `App.vue` | Add `<CookieConsentBanner />` |
| `AuthPage.vue` | Show info banner when consent is `"rejected"`, link to re-open consent banner |
| `authStore.js` | Check consent before `initialize()` calls `getCurrentUser()` |
| `LandingPage.vue` | Add "Cookie Preferences" footer link |
| `UserSettingsPage.vue` | Add "Cookie Preferences" button in settings |
| Authenticated layout | Add "Cookie Preferences" link in user dropdown or sidebar footer |
| `LoginEndpoint.cs` | Accept `consentLevel` field; refuse login if consent not given |

---

## 8. Accessibility Requirements

| Requirement | Implementation |
|-------------|---------------|
| Focus trap | When banner is visible, tab order should include banner buttons. Not a full trap (page is still scrollable), but banner should receive initial focus. |
| `role="dialog"` | On the banner container, with `aria-label="Cookie consent"` |
| `aria-describedby` | Link banner description text to the dialog |
| Button labels | Clear, action-oriented: "Accept Cookies", "Reject Cookies" |
| Keyboard | Both buttons reachable via Tab. Enter/Space activates. |
| Screen reader | Announce banner on appearance via `aria-live="polite"` |
| Contrast | All text meets WCAG AA (existing design tokens already satisfy this on dark backgrounds) |

---

## 9. Edge Cases

| Scenario | Behavior |
|----------|----------|
| User clears localStorage | Banner re-appears on next visit |
| User has existing session cookie but no consent in localStorage | Show banner to collect **prospective** consent. Do NOT log them out — the existing session continues. This is a one-time transitional measure for users who had sessions before the consent mechanism was deployed. Do NOT frame this as "retroactive consent" in any legal documentation — the banner collects consent for future cookie use. |
| Private/incognito browsing | Banner shows every visit (localStorage is ephemeral). This is expected. |
| User rejects, then clicks login link | Show banner again with explanation that login requires cookies |
| Consent expires (>6 months) | Banner re-appears. Existing session is not interrupted — consent is re-collected for future sessions. |
| Multiple tabs | All tabs share the same `localStorage`. Consent in one tab applies everywhere. The `useCookieConsent` composable listens to `storage` events and reactively updates state in all open tabs. |
| Direct API call bypasses frontend | Server-side enforcement (§6.4) rejects login requests without valid `consentLevel`. |

---

## 10. Cookie Policy Page (Required)

A dedicated `/cookies` route is **required** (not optional) under best practice guidance from CNIL, the ICO, and the Belgian DPA. The cookie consent banner serves as the "first layer" of a two-layer notice. The cookie policy page is the "second layer."

### Required content

| Field | Value |
|-------|-------|
| Cookie name | `mongoose-auth` |
| Purpose | Authentication — maintains logged-in session |
| Type | First-party, HTTP-only, Secure, SameSite=Strict |
| Duration | Session (cleared on browser close) or 30 days (when "Remember Me" is checked at login) |
| Data controller | Mongoose.gg (include contact information) |
| Legal basis | Strictly necessary cookie under ePrivacy Directive Article 5(3); processing under GDPR Article 6(1)(b) (performance of contract — providing the authenticated service the user requested) |

The banner links to this page via "Cookie Policy" and to the existing Privacy Policy via "Privacy Policy."

---

## 11. Legal/Compliance Notes

- The authentication cookie (`mongoose-auth`) is **strictly necessary** for the core service (login). Under GDPR Article 6(1)(b) (performance of contract) and ePrivacy Directive Article 5(3), strictly necessary cookies do not require consent — but users must be **informed** about them.
- Both the session-only and 30-day persistent variants serve the same authentication function. The "Remember Me" checkbox on the login form is the user's granular control over cookie duration, not the consent banner.
- The banner text must not use dark patterns: no pre-selected checkboxes, no misleading button colors, no hidden reject option.
- **Equal prominence (CNIL/EDPB)**: "Accept Cookies" and "Reject Cookies" have equal visual weight — same size, same row, both clearly clickable. "Accept" is `variant="primary"` (filled) and "Reject" is `variant="secondary"` (outlined with visible affordance). This satisfies the CNIL 2022 updated guidelines requiring equal prominence for accept and refuse options.
- **No asymmetric friction**: Both "Accept" and "Reject" are single-click actions. There is no confirmation step on either path, complying with EDPB Guidelines 05/2020 that withdrawal/refusal must not be more burdensome than giving consent.
- **Consent validity**: 6 months (183 days), following the strictest current DPA guidance (CNIL).
- **Server-side enforcement**: The login API validates consent, preventing bypass of the frontend consent mechanism.
- **Consent withdrawal**: Accessible from public pages (footer), user settings, and the authenticated app shell (user dropdown/sidebar), satisfying GDPR Article 7(3) that withdrawal must be as easy as giving consent.

---

## 12. Implementation Priority

1. `useCookieConsent.js` composable (consent state management + cross-tab sync)
2. `CookieConsentBanner.vue` component
3. `App.vue` integration
4. `CookiePolicyPage.vue` — dedicated `/cookies` route
5. `AuthPage.vue` — info banner when consent rejected
6. `authStore.js` — consent-aware initialization
7. `LoginEndpoint.cs` — server-side consent enforcement
8. Footer / settings / app shell links for preference management
9. Update Privacy Policy page with cookie details and link to Cookie Policy

---

## 13. Open Questions

1. **Analytics cookies**: If Mongoose.gg adds third-party analytics (e.g., PostHog, Plausible) in the future, a third consent tier ("Analytics") should be added. The consent model should be designed to accommodate this expansion — the `mongoose_cookie_consent` localStorage value can evolve from a simple string to a JSON object (e.g., `{ "essential": true, "analytics": false }`) when needed.
2. **Geolocation-based display**: **Resolved — show globally.** Simpler implementation, builds trust with all users, avoids geo-detection complexity and mistakes.
3. **Cookie policy page**: **Resolved — required.** See §10. A dedicated `/cookies` page listing each cookie with name, purpose, type, duration, and data controller is required under DPA best practice guidance.
