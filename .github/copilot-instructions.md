
# Copilot Instructions for Pulse (pulse.gg)

## Project Overview
- **Pulse** is a full-stack project with two clients:
	- **Legacy client**: Vue 3 + Vite app in `client/`
	- **Primary client (v2)**: Standalone Vue 3 + Vite app in `client_v2/` (see docs/information_architecture_v2.md)
- The backend is a C# .NET server in `server/RiotProxy`.
- Client(s) and server communicate via HTTP APIs (see docs/api_v2_design.md). The server proxies the Riot Games API and manages user/game data.
- Sensitive secrets (API keys, DB connection strings, Mollie API keys) are supplied via environment variables or .NET user-secrets (RIOT_API_KEY, LOL_DB_CONNECTIONSTRING, LOL_DB_CONNECTIONSTRING_V2, Mollie keys). Optional local secret files (RiotSecret.txt, DatabaseSecret.txt) are supported for local development only and must never be committed.

## Key Workflows
### Client (Vue 3 + Vite)

### Client v2 (Vue 3 + Vite)
- Install dependencies: `cd client_v2 && npm install`
- Run dev server: `npm run dev`
- Run unit tests: `npm run test:unit`, `npm run test:unit:watch`, `npm run test:unit:coverage`
- Main entry: `client_v2/src/main.js`, root component: `client_v2/src/App.vue`
- Components: `client_v2/src/components/`, Views: `client_v2/src/views/`
- API logic: `client_v2/src/services/`, composables in `client_v2/src/composables/`
- See docs/information_architecture_v2.md for route map and app structure.

### Server (.NET 8/9)

## Conventions & Patterns

## Conventions & Patterns
- **Client v2**: Uses Vue SFCs, composition API, composables for state/data logic. API calls are abstracted in `services/`. Follows UI design guidelines in `docs/design/ui-design-guidelines.md`.
- **Legacy Client**: Same conventions, but API logic in `assets/`.
- **Server**: Endpoints are organized by resource in `Application/Endpoints/`. DTOs are used for request/response shapes. SOLID principles for maintainability. Backend tests in `RiotProxy.Tests/`.
- **Testing**: Uses Vitest for client and client_v2 unit tests. Backend tests are present in `server/RiotProxy.Tests/`.
- **Secrets**: Never commit secrets; use env vars/user-secrets (`RIOT_API_KEY`, `LOL_DB_CONNECTIONSTRING`, `LOL_DB_CONNECTIONSTRING_V2`, Mollie API keys).

## Integration Points

## Integration Points
- Client(s) <-> Server: HTTP API (see docs/api_v2_design.md for endpoints, queue filtering, and response shapes)
- Server <-> Riot API: Proxy logic in `RiotApiClient.cs`
- Server <-> Database: Connection via `LOL_DB_CONNECTIONSTRING`/`LOL_DB_CONNECTIONSTRING_V2` (env/user-secrets), logic in `Infrastructure/External/Database/`

## Examples

## Examples
- Add a new API endpoint: create a new file in `Application/Endpoints/`, update `Program.cs` to register it.
- Add a new client_v2 view: create a `.vue` file in `client_v2/src/views/`, add a route in `client_v2/src/router/index.js`.
- Add a new legacy client view: create a `.vue` file in `client/src/views/`, add a route in `client/src/router/index.js`.

## References

## References
- See [README.md](../README.md) for setup, build, and test commands (Pulse vision and domain: pulse.gg).
- Sensitive config: set via env vars or user-secrets (`RIOT_API_KEY`, `LOL_DB_CONNECTIONSTRING`, `LOL_DB_CONNECTIONSTRING_V2`, Mollie API keys)
- Main server logic: [server/Program.cs], [server/Application/Endpoints/]
- Main client_v2 logic: [client_v2/src/], [client_v2/src/services/], [client_v2/src/composables/]
- Main legacy client logic: [client/src/], [client/src/assets/], [client/src/composables/]
- Riot API Documentation: https://developer.riotgames.com/
- API v2 Design: [docs/architecture/api_v2_design.md]
- Database schema: [docs/architecture/database_schema_v2.md], [server/schema-v2.sql]
- Information architecture: [docs/architecture/information_architecture_v2.md]
- UI design guidelines: [docs/design/ui-design-guidelines.md]

For new patterns or changes, update this file to keep AI agents productive and aligned with project conventions.
