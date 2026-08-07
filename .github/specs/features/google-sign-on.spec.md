# Feature: Google Sign-On

> **Purpose**: Status note and activation checklist for Google Sign-On. Implementation is complete and merged on branch `497-add-social-media-login`; this doc tracks the one remaining step — obtaining real Google OAuth credentials and flipping the feature flags — so it can be picked up in a later session.
>
> **Triage**: implemented, pending activation

## Status

Implemented, gated off by default (same pattern as Riot Sign-On). Nothing more to build — the only remaining work is operational: register a Google OAuth client and turn the flags on.

## What's done

- `GoogleSignOnEndpoint` (`GET /api/v2/auth/google/login`, `GET /api/v2/auth/google/callback`) — OAuth 2.0 authorization code flow, CSRF state cookie, rate-limited callback (10/15min/IP). See `.github/specs/architecture.spec.md` §6.2b.
- `IGoogleSignOnClient` / `GoogleSignOnClient` (`server/Mongoose.Api/Infrastructure/Google/`) — server-side code exchange + Google userinfo lookup. Never trusts client-supplied identity.
- Shared `user_identity_providers` table (also used by Riot Sign-On) — see `.github/specs/database-schema.spec.md`. Migration `003_AddUserIdentityProviders.sql` already applied to local dev DB.
- Auto-link behavior: a first-time Google sign-in with a Google-verified email matching an existing local account links to that account instead of creating a duplicate; unverified emails always create a new account.
- Frontend: `featureFlags.googleSignOn`, "Sign in with Google" button on `AuthPage.vue`, error-code surfacing for the callback redirect.
- Full test coverage: `GoogleSignOnEndpointTests.cs` (backend), `AuthPage.spec.js` (frontend).

## Activation checklist (remaining)

1. **Google Cloud Console** — create an OAuth 2.0 Client ID (Web application type) under a Google Cloud project:
   - Authorized redirect URI (local dev): `http://localhost:5164/api/v2/auth/google/callback` — must match `Auth:Google:RedirectUri` exactly.
   - Add the production redirect URI too once a prod domain is ready.
   - Scopes requested by the flow: `openid email profile` (no extra consent-screen scopes needed).
2. **Backend config** — set real credentials, then flip the flag:
   - `Auth:EnableGoogleSignOn` → `true` (currently `false` in `server/Mongoose.Api/appsettings.Development.json`)
   - `Auth:Google:ClientId` / `Auth:Google:ClientSecret` — or the env vars `GSO_CLIENT_ID` / `GSO_CLIENT_SECRET` (preferred so the secret isn't in a config file)
   - `Auth:Google:RedirectUri` and `Auth:Google:ClientBaseUrl` are already set correctly for local dev
3. **Frontend config** — set `VITE_FEATURE_GOOGLE_SIGNON=true` in `client/.env.development` (and `client/.env` when ready for production) to show the button.
4. **Manual smoke test** — with both flags on and the backend running: click "Sign in with Google" on `/auth`, complete Google's consent screen, confirm redirect to `/app/overview` with a session cookie set, and confirm a row appears in `user_identity_providers` with `provider = 'google'`.
5. Re-run `client/test/unit` and `server/Mongoose.Api.Tests` after flipping flags locally, just as a sanity check that nothing in config wiring broke — the suites already pass with flags off.

## References

- `.github/specs/architecture.spec.md` §6.2b (endpoint contract)
- `.github/specs/database-schema.spec.md` — `user_identity_providers` table
- Riot Sign-On follows the identical activation pattern (`Auth:EnableRiotSignOn`, `VITE_FEATURE_RIOT_SIGNON`) if useful as a side-by-side reference
