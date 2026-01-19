
# Copilot Instructions for Mongoose (mongoose.gg)

## Project Overview
- **Mongoose** is a full-stack project with one client:
	- **Primary client**: Standalone Vue 3 + Vite app in `client/` (see docs/information_architecture.md)
- The backend is a C# .NET server in `server/RiotProxy`.
- Client(s) and server communicate via HTTP APIs (see docs/api_design.md). The server proxies the Riot Games API and manages user/game data.
- Sensitive secrets (API keys, DB connection strings, Mollie API keys) are supplied via environment variables or .NET user-secrets (RIOT_API_KEY, LOL_DB_CONNECTIONSTRING_V2, Mollie keys). 

## Key Workflows

### Client (Vue 3 + Vite)
- Install dependencies: `cd client && npm install`
- Run dev server: `npm run dev`
- Run unit tests: `npm run test:unit`, `npm run test:unit:watch`, `npm run test:unit:coverage`
- Main entry: `client/src/main.js`, root component: `client/src/App.vue`
- Components: `client/src/components/`, Views: `client/src/views/`
- API logic: `client/src/services/`, composables in `client/src/composables/`
- See docs/information_architecture.md for route map and app structure.

### Server (.NET 9)

## Conventions & Patterns
- **Client**: Uses Vue SFCs, composition API, composables for state/data logic. API calls are abstracted in `services/`. Follows UI design guidelines in `docs/design/ui-design-guidelines.md`.
- **Server**: Endpoints are organized by resource in `Application/Endpoints/`. DTOs are used for request/response shapes. SOLID principles for maintainability. Backend tests in `RiotProxy.Tests/`.
- **Testing**: Uses Vitest for client unit tests. Backend tests are present in `server/RiotProxy.Tests/`.
- **Secrets**: Never commit secrets; use env vars/user-secrets (`RIOT_API_KEY`, `LOL_DB_CONNECTIONSTRING_V2`, Mollie API keys).

## Integration Points
- Client(s) <-> Server: HTTP API (see docs/api_design.md for endpoints, queue filtering, and response shapes)
- Server <-> Riot API: Proxy logic in `RiotApiClient.cs`
- Server <-> Database: Connection via `LOL_DB_CONNECTIONSTRING_V2` (env/user-secrets), logic in `Infrastructure/External/Database/`

## Examples
- Add a new API endpoint: create a new file in `Application/Endpoints/`, update `Program.cs` to register it.
- Add a new client view: create a `.vue` file in `client/src/views/`, add a route in `client/src/router/index.js`.


## References
- See [README.md](../README.md) for setup, build, and test commands (Mongoose vision and domain: mongoose.gg).
- Sensitive config: set via env vars or user-secrets (`RIOT_API_KEY` `LOL_DB_CONNECTIONSTRING_V2`, Mollie API keys)
- Main server logic: [server/Program.cs], [server/Application/Endpoints/]
- Main client logic: [client/src/], [client/src/services/], [client/src/composables/]
- Riot API Documentation: https://developer.riotgames.com/
- API Design: [docs/architecture/api_design.md]
- Database schema: [docs/architecture/database_schema.md], [server/schema.sql]
- Information architecture: [docs/architecture/information_architecture.md]
- UI design guidelines: [docs/design/ui-design-guidelines.md]

For new patterns or changes, update this file to keep AI agents productive and aligned with project conventions.
