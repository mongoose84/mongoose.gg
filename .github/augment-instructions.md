
# Augment Instructions for lol-app

## Project Overview
- **lol-app** (Pulse) is a League of Legends cross-account improvement and statistics tracker.
- Full-stack project with two clients:
  - **Legacy client**: Vue 3 + Vite app in `client/`
  - **Primary client (v2)**: Standalone Vue 3 + Vite app in `client_v2/` (see docs/information_architecture_v2.md)
- The backend is a C# .NET 8/9 server in `server/RiotProxy`.
- Client(s) and server communicate via HTTP APIs (see docs/api_v2_design.md). The server proxies the Riot Games API and manages user/game data.
- Sensitive secrets (API keys, DB connection strings, Mollie API keys) are supplied via environment variables or .NET user-secrets (RIOT_API_KEY, LOL_DB_CONNECTIONSTRING, LOL_DB_CONNECTIONSTRING_V2, Mollie keys). Optional local secret files (RiotSecret.txt, DatabaseSecret.txt) are supported for local development only and must never be committed.
- Database: MySQL for storing users, gamers, matches, and match participants.


## Key Workflows
### Client v2 (Vue 3 + Vite)
- Install dependencies: `cd client_v2 && npm install`
- Run dev server: `npm run dev`
- Run unit tests: `npm run test:unit`, `npm run test:unit:watch`, `npm run test:unit:coverage`
- Main entry: `client_v2/src/main.js`, root component: `client_v2/src/App.vue`
- Components: `client_v2/src/components/`, Views: `client_v2/src/views/`
- API logic: `client_v2/src/services/`, composables in `client_v2/src/composables/`
- See docs/information_architecture_v2.md for route map and app structure.

### Legacy Client (Vue 3 + Vite)
- Install dependencies: `cd client && npm install`
- Run dev server: `npm run dev`
- Main entry: `client/src/main.js`, root component: `client/src/App.vue`
- Components: `client/src/components/`, Views: `client/src/views/`
- API logic: `client/src/assets/`, composables in `client/src/composables/`

### Server (.NET 8/9)
- Build: `cd server && dotnet build`
- Run: `dotnet run`
- Publish (Windows): `dotnet publish -c Release -r win-x86 --self-contained true`
- Publish (Linux): `dotnet publish -c Release -r linux-x64 --self-contained false`
- Secrets: Provide via env vars or user-secrets (preferred) — `RIOT_API_KEY`, `LOL_DB_CONNECTIONSTRING`, `LOL_DB_CONNECTIONSTRING_V2`, Mollie API keys. Optional local secret files (RiotSecret.txt, DatabaseSecret.txt) are supported for local development only and must not be committed.
- Payments: Use Mollie (EU) for subscriptions; keep Mollie/API keys out of source (see DatabaseSecret.txt and other secrets files)
- Main entry: `server/Program.cs`
- Endpoints: `server/Application/Endpoints/`
- DTOs: `server/Application/DTOs/`
- Infrastructure: `server/Infrastructure/`
- Backend tests: `server/RiotProxy.Tests/` (see product_plan.md for test coverage)

### Server (.NET 9.0)
- Build: `cd server && dotnet build`
- Run: `dotnet run` (runs on configured port, typically 5000/5001)
- Publish for Windows: `dotnet publish -c Release -r win-x86 --self-contained true`
- Publish for Linux: `dotnet publish -c Release -r linux-x64 --self-contained false`
- Secrets: Place `RiotSecret.txt` (Riot API key) and `DatabaseSecret.txt` (MySQL connection string) in `server/`
- Main entry: `server/Program.cs`
- Application layer: `server/Application/`
  - Endpoints: `Application/Endpoints/` (API endpoint definitions)
  - DTOs: `Application/DTOs/` (request/response models)
- Infrastructure layer: `server/Infrastructure/`
  - Database: `Infrastructure/External/Database/` (repositories, connection factory)
  - Riot API: `Infrastructure/External/Riot/` (Riot API client)
- Background jobs: `Infrastructure/Jobs/` (e.g., MatchHistorySyncJob)

## Conventions & Patterns
### General
- Follows RESTful API design principles
- Handles errors and edge cases gracefully
- Uses async/await for asynchronous operations
- Adheres to SOLID principles for maintainability
- Consistent naming and formatting conventions
- Consistent code style and formatting
- Consistent use of comments and documentation
- Consistent use of logging and monitoring
- Use Spec-Driven development (specs in `server/RiotProxy.Tests/`)

### Client
- Uses Vue 3 Single File Components (SFCs) with Composition API
- Composables for shared state/data logic (e.g., `useGamers.js`)
- API calls are abstracted in `assets/` directory with dedicated files per endpoint
- Uses Axios for HTTP requests
- Testing with Vitest and Vue Test Utils
- Styling: Custom CSS in `assets/main.css`


### Server
- Endpoints are organized by resource in `Application/Endpoints/`
- Each endpoint implements `IEndpoint` interface with a `Configure` method
- DTOs (Data Transfer Objects) are used for request/response shapes
- Repository pattern for database access
- Dependency injection for services and repositories
- CORS configured for Vue dev server and production domains
- Background jobs using IHostedService
- Backend tests in `RiotProxy.Tests/`
- Secrets are never checked into version control


### Database
- MySQL database with repositories for:
  - `UserRepository` - User/dashboard management
  - `GamerRepository` - League of Legends account data
  - `UserGamerRepository` - Many-to-many relationship between users and gamers
  - `LolMatchRepository` - Match metadata
  - `LolMatchParticipantRepository` - Individual player performance in matches
- Connection string format: `Server=host;Port=port;Database=db;User Id=user;Password=pass;SslMode=Preferred;`


### Testing
- Client: Vitest for unit tests, Vue Test Utils for component testing
- Client v2: Vitest for unit tests
- Server: Backend tests in `RiotProxy.Tests/`
- Always run tests after making changes to ensure nothing breaks


## Integration Points
- Client(s) <-> Server: HTTP API (see docs/api_v2_design.md for endpoints, queue filtering, and response shapes)
- Server <-> Riot API: Proxy logic in `RiotApiClient.cs`
- Server <-> Database: Connection via `LOL_DB_CONNECTIONSTRING`/`LOL_DB_CONNECTIONSTRING_V2` (env/user-secrets), logic in `Infrastructure/External/Database/`


## Key Features
1. **Multi-Account Dashboards**: Solo, Duo, and Team views
2. **Performance Metrics**: Winrate, KDA, CS/min, Gold/min, Games played
3. **Performance Timeline**: Time-series data with rolling averages (1w, 1m, 3m, 6m, all)
4. **Account Comparison**: Compare stats across multiple accounts
5. **Automated Match Sync**: Background job syncs match history periodically
6. **Subscription & Paywall**: Mollie integration for Pro tier, feature gating, and upgrade prompts


## Common Tasks
### Add a new API endpoint
1. Create a new file in `server/Application/Endpoints/` implementing `IEndpoint`
2. Register it in `server/Application/RiotProxyApplication.cs` and/or `Program.cs`
3. Create corresponding DTO in `Application/DTOs/` if needed
4. Add client-side API call function in `client_v2/src/services/` (or `client/src/assets/` for legacy)
5. Update views/components to use the new endpoint

### Add a new client_v2 view
1. Create a `.vue` file in `client_v2/src/views/`
2. Add a route in `client_v2/src/router/index.js`
3. Link to it from appropriate navigation/components

### Add a new legacy client view
1. Create a `.vue` file in `client/src/views/`
2. Add a route in `client/src/router/index.js`
3. Link to it from appropriate navigation/components

### Add a new database repository method
1. Add method to appropriate repository in `Infrastructure/External/Database/Repositories/`
2. Use Dapper for SQL queries
3. Follow async/await pattern
4. Handle null cases appropriately


## Important Notes
- **Never commit secrets**: Use env vars or user-secrets for all sensitive config; `RiotSecret.txt` and `DatabaseSecret.txt` are gitignored and only used for local dev
- **CORS**: Configured for localhost:5173 (dev) and production domains
- **API Rate Limiting**: Riot API has rate limits; handle 429 responses gracefully
- **Background Jobs**: MatchHistorySyncJob runs periodically to fetch new matches
- **Error Handling**: Always handle errors gracefully on both client and server
- **Performance**: Consider pagination for large datasets
- **Security**: Never expose Riot API key to client; always proxy through server

lol/

## File Structure Reference
```

├── client/                    # Legacy Vue 3 frontend
│   ├── src/
│   │   ├── assets/           # API calls, CSS, static assets
│   │   ├── components/       # Reusable Vue components
│   │   ├── composables/      # Shared reactive logic
│   │   ├── router/           # Vue Router configuration
│   │   ├── views/            # Page-level components
│   │   ├── App.vue           # Root component
│   │   └── main.js           # Entry point
│   ├── test/                 # Unit tests
│   └── package.json
├── client_v2/                 # Standalone Vue 3 frontend (primary)
│   ├── src/
│   │   ├── components/       # Reusable Vue components
│   │   ├── composables/      # Shared logic
│   │   ├── layouts/          # App layouts
│   │   ├── router/           # Vue Router configuration
│   │   ├── services/         # API logic
│   │   ├── stores/           # Pinia stores
│   │   ├── views/            # Page-level components
│   │   ├── App.vue           # Root component
│   │   └── main.js           # Entry point
│   ├── test/                 # Unit tests
│   └── package.json
└── server/                    # .NET 8/9 backend
  ├── Application/          # Application layer
  │   ├── DTOs/             # Data transfer objects
  │   └── Endpoints/        # API endpoints
  ├── Infrastructure/       # Infrastructure layer
  │   ├── External/         # External services (DB, Riot API)
  │   └── Jobs/             # Background jobs
  ├── RiotProxy.Tests/      # Backend tests
  └── Program.cs            # Entry point
```

## References
- Setup instructions: [README.md](../README.md)
- Sensitive config: set via env vars or user-secrets (`RIOT_API_KEY`, `LOL_DB_CONNECTIONSTRING`, `LOL_DB_CONNECTIONSTRING_V2`, Mollie API keys)
- Main server logic: `server/Program.cs`, `server/Application/`, `server/Application/Endpoints/`, `server/RiotProxy.Tests/`
- Main client_v2 logic: `client_v2/src/`, `client_v2/src/services/`, `client_v2/src/composables/`
- Main legacy client logic: `client/src/`, `client/src/assets/`, `client/src/composables/`
- Riot API Documentation: https://developer.riotgames.com/
- API v2 Design: [docs/architecture/api_v2_design.md]
- Database schema: [docs/architecture/database_schema_v2.md], [server/schema-v2.sql]
- Information architecture: [docs/architecture/information_architecture_v2.md]
- UI design guidelines: [docs/design/ui-design-guidelines.md]

For new patterns or changes, update this file to keep AI agents productive and aligned with project conventions.

