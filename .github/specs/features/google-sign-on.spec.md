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

## Local activation (done)

- `Auth:EnableGoogleSignOn` → `true` in `server/Mongoose.Api/appsettings.Development.json`.
- `VITE_FEATURE_GOOGLE_SIGNON=true` in `client/.env.development`.
- `Auth:Google:RedirectUri` (`http://localhost:5164/api/v2/auth/google/callback`) and `Auth:Google:ClientBaseUrl` (`http://localhost:5174`) already set for local dev.
- Both flags default to off in production (`server/Mongoose.Api/appsettings.json` has no `EnableGoogleSignOn` key at all — unset binds to `false`; `client/.env` has `VITE_FEATURE_GOOGLE_SIGNON=false`), so nothing here leaks to prod until the steps below are done.

## Deployment shape (how this repo ships to production)

There is no separate CD pipeline to design — `.github/workflows/ci-server.yml` and `ci-client.yml` already deploy on every push to `main` (path-filtered to `server/**` / `client/**`):

- **Backend**: self-contained `win-x86` publish → FTPS to an IIS host. Secrets are injected by generating `Mongoose.Api/web.config` from GitHub Actions repo secrets (`Settings → Secrets and variables → Actions`), which IIS's AspNetCoreModule exposes as process env vars.
- **Frontend**: `npm run build` (reads `client/.env`, the production env file) → FTPS `dist/` to the same host.
- Production origins: frontend `https://mongoose.gg`, API `https://api.mongoose.gg` (see `client/src/services/apiConfig.js`).
- No auto-migration runner exists — `.sql` files under `server/Mongoose.Api/Infrastructure/Database/Migrations/` are applied by hand against the production database.

## Activation checklist (remaining — do in order)

1. **Google Cloud Console** — on the same OAuth 2.0 Client ID (Web application type) used for local dev, add the production redirect URI: `https://api.mongoose.gg/api/v2/auth/google/callback`. Also set the OAuth consent screen's publishing status to "In production" (not "Testing") if it isn't already, or only allow-listed test users will be able to sign in. Scopes stay `openid email profile`.
2. **GitHub repo secrets** — add `GSO_CLIENT_ID` and `GSO_CLIENT_SECRET` (the real values from step 1) under `Settings → Secrets and variables → Actions`. `GoogleSignOnClient` reads these directly as flat env vars, same as `Auth:Google:ClientId`/`ClientSecret` — no `Auth__Google__` prefix needed for these two.
3. **CI workflow wiring** — done in this pass: `ci-server.yml` now validates `GSO_CLIENT_ID`/`GSO_CLIENT_SECRET` are present and writes `Auth__EnableGoogleSignOn=true`, `GSO_CLIENT_ID`, `GSO_CLIENT_SECRET`, `Auth__Google__RedirectUri=https://api.mongoose.gg/api/v2/auth/google/callback`, and `Auth__Google__ClientBaseUrl=https://mongoose.gg` into the generated `web.config`. (Riot Sign-On has no equivalent wiring yet — it's still off in prod; not in scope here.)
4. **Database migration** — run `003_AddUserIdentityProviders.sql` against the production MySQL database (the one behind the `DB_CONNECTIONSTRING` secret) before or in the same window as the deploy. This table is shared with Riot Sign-On but hasn't been created in prod yet.
5. **Frontend flag** — set `VITE_FEATURE_GOOGLE_SIGNON=true` in `client/.env`. Do this last, once steps 1–4 are confirmed ready, since merging to `main` deploys immediately — the button shouldn't go live before the backend can actually complete the flow.
6. **Merge to `main`** — merging `497-add-social-media-login` triggers both `ci-server.yml` and `ci-client.yml` (it touches both `server/**` and `client/**`). Watch both workflow runs in the Actions tab.
7. **Production smoke test** — on `https://mongoose.gg/auth`, click "Sign in with Google", complete Google's consent screen, confirm redirect to `/app/overview` with a session cookie set, and confirm a row appears in production `user_identity_providers` with `provider = 'google'`.
8. If anything fails after deploy, the fast rollback is flipping `VITE_FEATURE_GOOGLE_SIGNON` back to `false` in `client/.env` and re-merging — the backend endpoint stays inert client-side without the button, and `Auth:EnableGoogleSignOn` can be turned off the same way by editing the workflow's generated value if needed.

## References

- `.github/specs/architecture.spec.md` §6.2b (endpoint contract)
- `.github/specs/database-schema.spec.md` — `user_identity_providers` table
- `.github/workflows/ci-server.yml`, `.github/workflows/ci-client.yml` — deploy pipelines
- Riot Sign-On uses the identical local-activation pattern (`Auth:EnableRiotSignOn`, `VITE_FEATURE_RIOT_SIGNON`) but has not been wired into the production deploy pipeline yet
