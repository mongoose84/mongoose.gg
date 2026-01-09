# Copilot Instructions for Pulse (pulse.gg)

## Project Overview
- **Pulse** is a full-stack project with a Vue 3 + Vite client (in `client/`) and a C# .NET server (in `server/RiotProxy`).
- The client and server communicate via HTTP APIs. The server acts as a proxy to the Riot Games API and manages user/game data.
- Sensitive secrets (API keys, DB connection strings) are supplied via environment variables or .NET user-secrets (RIOT_API_KEY, LOL_DB_CONNECTIONSTRING, LOL_DB_CONNECTIONSTRING_V2); no secret files should be committed or required.

## Key Workflows
### Client (Vue 3 + Vite)
- Install dependencies: `cd client && npm install`
- Run dev server: `npm run dev`
- Run unit tests: `npm run test:unit`, `npm run test:unit:watch`, `npm run test:unit:coverage`
- Main entry: `client/src/main.js`, root component: `client/src/App.vue`
- Components: `client/src/components/`, Views: `client/src/views/`
- API logic: `client/src/assets/` (e.g., `getUsers.js`), composables in `client/src/composables/`

### Server (.NET 8/9)
- Build: `cd server && dotnet build`
- Run: `dotnet run`
- Publish (Windows): `dotnet publish -c Release -r win-x86 --self-contained true`
- Secrets: Provide via env vars or user-secrets (preferred) — `RIOT_API_KEY`, `LOL_DB_CONNECTIONSTRING`, `LOL_DB_CONNECTIONSTRING_V2`; legacy secret files are not used.
- Payments: Use Mollie (EU) for subscriptions; keep Mollie/API keys out of source (see DatabaseSecret.txt and other secrets files)
- Main entry: `server/Program.cs`
- Endpoints: `server/Application/Endpoints/`
- DTOs: `server/Application/DTOs/`
- Infrastructure: `server/Infrastructure/`

## Conventions & Patterns
- **Client**: Uses Vue SFCs, composition API, and composables for state/data logic. API calls are abstracted in `assets/`.
- **Server**: Endpoints are organized by resource in `Application/Endpoints/`. DTOs are used for request/response shapes. Secrets are not checked in. Use SOLID principles for maintainability.  
- **Testing**: Uses Vitest for client unit tests. No server-side test convention is documented.
- **Secrets**: Never commit secrets; use env vars/user-secrets (`RIOT_API_KEY`, `LOL_DB_CONNECTIONSTRING`, `LOL_DB_CONNECTIONSTRING_V2`).

## Integration Points
- Client <-> Server: HTTP API (see `getUsers.js`, `getGamers.js`, etc. for usage)
- Server <-> Riot API: Proxy logic in `RiotApiClient.cs`
- Server <-> Database: Connection via `LOL_DB_CONNECTIONSTRING`/`LOL_DB_CONNECTIONSTRING_V2` (env/user-secrets), logic in `Infrastructure/External/Database/`

## Examples
- Add a new API endpoint: create a new file in `Application/Endpoints/`, update `Program.cs` to register it.
- Add a new client view: create a `.vue` file in `src/views/`, add a route in `src/router/index.js`.

## References
- See [README.md](../README.md) for setup, build, and test commands (Pulse vision and domain: pulse.gg).
- Sensitive config: set via env vars or user-secrets (`RIOT_API_KEY`, `LOL_DB_CONNECTIONSTRING`, `LOL_DB_CONNECTIONSTRING_V2`)
- Main server logic: [server/Program.cs], [server/Application/Endpoints/]
- Main client logic: [client/src/], [client/src/assets/], [client/src/composables/]
- Riot API Documentation: https://developer.riotgames.com/
- Documentation for API v2: [docs/api_v2.md]
- Database schema: [docs/database_schema_v2.md][server/schema_v2.sql]

---
For new patterns or changes, update this file to keep AI agents productive and aligned with project conventions.
