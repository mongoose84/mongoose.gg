---
type: "always_apply"
---

# Augment Instructions for lol-app

## Project Overview
- **lol-app** (Mongoose) is a League of Legends cross-account improvement and statistics tracker.
- Full-stack project with a single client:
  - **Primary client**: Vue 3 + Vite + Tailwind CSS + Headless UI app in `client/`
- The backend is a C# .NET 9 server in `server/` following Clean Architecture principles.
- Client and server communicate via HTTP APIs (see docs/api-design-guidelines.md). The server proxies the Riot Games API and manages user/game data.
- Sensitive secrets (API keys, DB connection strings, Mollie API keys) are supplied via environment variables or .NET user-secrets (RIOT_API_KEY, Database_test, Database_production, Mollie keys).
- Database: MySQL for storing users, riot accounts, matches, and match participants.


## Key Workflows
### Client (Vue 3 + Vite + Tailwind + Headless UI)
- Install dependencies: `cd client && npm install`
- Run dev server: `npm run dev`
- Run unit tests: `npm run test:unit`, `npm run test:unit:watch`, `npm run test:unit:coverage`
- Run e2e tests: `npm run test:e2e`, `npm run test:e2e:ui`, `npm run test:e2e:headed`
- Main entry: `client/src/main.js`, root component: `client/src/App.vue`
- Components: `client/src/components/` (feature components + `base/` for primitives)
- Views: `client/src/views/` (page-level components)
- Layouts: `client/src/layouts/` (e.g., `AppLayout.vue`)
- API services: `client/src/services/` (Axios-based API clients)
- Composables: `client/src/composables/` (shared reactive logic)
- Stores: `client/src/stores/` (Pinia state management)
- Utils: `client/src/utils/` (helper functions like formatters)
- Styling: `client/src/style.css` (CSS variables + Tailwind directives)
- Tailwind config: `client/tailwind.config.js`

### Server (.NET 9 - Clean Architecture)
- Build: `cd server && dotnet build`
- Run: `dotnet run` (runs on configured port, typically 5000/5001)
- Publish for Windows: `dotnet publish -c Release -r win-x86 --self-contained true`
- Publish for Linux: `dotnet publish -c Release -r linux-x64 --self-contained false`
- Main entry: `server/Program.cs`

#### Architecture Layers:
- **Core Layer** (`server/Core/`): Domain entities, interfaces, value objects, enums
  - Entities: `Core/Entities/` (e.g., User, RiotAccount, Match, Participant)
  - Interfaces: `Core/Interfaces/` (repository interfaces)
  - Value Objects: `Core/ValueObjects/`
  - Query Models: `Core/QueryModels/`
- **Application Layer** (`server/Application/`): Use cases and API endpoints
  - Endpoints: `Application/Endpoints/` (organized by feature: Auth, Overview, Solo, Matches, etc.)
  - DTOs: `Application/DTOs/` (request/response models by feature)
  - Services: `Application/Services/` (application-level services)
  - Query Models: `Application/QueryModels/`
- **Infrastructure Layer** (`server/Infrastructure/`): External concerns
  - Database: `Infrastructure/Database/` (repositories, connection factory)
  - Riot API: `Infrastructure/Riot/` (API client, mappers, rate limiting)
  - Email: `Infrastructure/Email/` (SMTP service, verification)
  - Security: `Infrastructure/Security/` (encryption)
  - Jobs: `Infrastructure/Jobs/` (background jobs like MatchHistorySyncJob)
  - WebSocket: `Infrastructure/WebSocket/` (real-time sync progress)
  - Middleware: `Infrastructure/Middleware/` (exception handling)
  - Telemetry: `Infrastructure/Telemetry/` (metrics)
- Backend tests: `server/Mongoose.Api.Tests/`

## Conventions & Patterns
### General
- Follows RESTful API design principles
- Handles errors and edge cases gracefully
- Uses async/await for asynchronous operations
- Adheres to SOLID principles for maintainability
- Clean Architecture with clear layer separation (Core → Application → Infrastructure)
- Consistent naming and formatting conventions
- Consistent use of comments and documentation
- Consistent use of logging and monitoring
- Use Spec-Driven development (specs in `server/Mongoose.Api.Tests/`)

### Client
- Uses Vue 3 Single File Components (SFCs) with Composition API (`<script setup>`)
- **Tailwind CSS** for utility-first styling with CSS variables for design tokens
- **Headless UI** (`@headlessui/vue`) for accessible, unstyled UI primitives (modals, dropdowns)
- **Heroicons** (`@heroicons/vue`) for consistent iconography
- Base components in `components/base/` (BaseButton, BaseCard, BaseModal, BaseInput)
- Feature components organized by domain (e.g., `components/matches/`, `components/overview/`)
- Composables for shared reactive logic (e.g., `useSyncWebSocket.js`, `useWinRateColor.js`)
- Pinia stores for global state (`stores/authStore.js`, `stores/uiStore.js`)
- API services in `services/` directory with dedicated files per feature
- Uses Axios for HTTP requests
- Testing: Vitest + Vue Test Utils for unit tests, Playwright for e2e tests
- TanStack Vue Query for server state management
- Chart.js + vue-chartjs for data visualization

### Server
- **Clean Architecture** with three layers: Core, Application, Infrastructure
- Core layer contains domain entities, interfaces, value objects, and enums
- Application layer contains endpoints, DTOs, and application services
- Infrastructure layer contains external concerns (database, Riot API, email, etc.)
- Endpoints organized by feature in `Application/Endpoints/` (Auth, Overview, Solo, Matches, etc.)
- Each endpoint implements `IEndpoint` interface with `Route` property and `Configure` method
- DTOs (Data Transfer Objects) organized by feature in `Application/DTOs/`
- Repository pattern with `RepositoryBase` providing common database operation helpers
- Dependency injection for all services and repositories
- CORS configured for Vue dev server and production domains
- Background jobs using `IHostedService` (e.g., `MatchHistorySyncJob`)
- WebSocket support via SignalR (`SyncProgressHub`) for real-time updates
- Backend tests in `Mongoose.Api.Tests/`
- Secrets are never checked into version control

### Database
- MySQL database with Clean Architecture repository pattern
- All repositories extend `RepositoryBase` for consistent database access patterns
- Key repositories:
  - `UsersRepository` - User account management
  - `RiotAccountsRepository` - League of Legends account data
  - `MatchesRepository` - Match metadata
  - `ParticipantsRepository` - Individual player performance in matches
  - `LpSnapshotsRepository` - LP history tracking
  - `OverviewStatsRepository` - Aggregated dashboard statistics
  - `SoloStatsRepository` - Solo queue statistics
- Connection string format: `Server=host;Port=port;Database=db;User Id=user;Password=pass;SslMode=Preferred;`

### Testing
- Client: Vitest for unit tests, Playwright for e2e tests
- Server: Backend tests in `Mongoose.Api.Tests/` with xUnit
- Always run tests after making changes to ensure nothing breaks


## Integration Points
- Client <-> Server: HTTP API (see docs/api-design-guidelines.md for endpoints, queue filtering, and response shapes)
- Server <-> Riot API: Proxy logic in `Infrastructure/Riot/RiotApiClient.cs`
- Server <-> Database: Connection via `Database_test`, `Database_production` (env/user-secrets), logic in `Infrastructure/Database/`


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
2. Register it in `server/Application/MongooseApiApplication.cs` and/or `Program.cs`
3. Create corresponding DTO in `Application/DTOs/` if needed
4. Add client-side API call function in `client/src/services/` (or `client/src/assets/` for legacy)
5. Update views/components to use the new endpoint

### Add a new client view
1. Create a `.vue` file in `client/src/views/`
2. Add a route in `client/src/router/index.js`
3. Link to it from appropriate navigation/components

### Add a new database repository method
1. Add interface method to appropriate interface in `Core/Interfaces/`
2. Implement method in repository in `Infrastructure/Database/Repositories/`
3. Use `RepositoryBase` helper methods (`ExecuteListAsync`, `ExecuteSingleAsync`, `ExecuteScalarAsync`, etc.)
4. Follow async/await pattern
5. Handle null cases appropriately


## Important Notes
- **Never commit secrets**: Use env vars or user-secrets for all sensitive config
- **CORS**: Configured for localhost:5173 (dev) and production domains
- **API Rate Limiting**: Riot API has rate limits; handle 429 responses gracefully
- **Background Jobs**: MatchHistorySyncJob runs periodically to fetch new matches
- **Error Handling**: Always handle errors gracefully on both client and server
- **Performance**: Consider pagination for large datasets
- **Security**: Never expose Riot API key to client; always proxy through server


## File Structure Reference
```
├── client/                   # Vue 3 + Tailwind + Headless UI frontend
│   ├── src/
│   │   ├── components/       # Reusable Vue components
│   │   │   ├── base/         # Primitives (BaseButton, BaseCard, BaseModal, BaseInput)
│   │   │   ├── matches/      # Match-related components
│   │   │   └── overview/     # Overview dashboard components
│   │   ├── composables/      # Shared reactive logic
│   │   ├── layouts/          # App layouts (AppLayout.vue)
│   │   ├── router/           # Vue Router configuration
│   │   ├── services/         # API services (Axios-based)
│   │   ├── stores/           # Pinia state stores
│   │   ├── utils/            # Helper functions
│   │   ├── views/            # Page-level components
│   │   ├── style.css         # CSS variables + Tailwind
│   │   ├── App.vue           # Root component
│   │   └── main.js           # Entry point
│   ├── test/                 # Unit tests
│   ├── e2e/                  # Playwright e2e tests
│   ├── tailwind.config.js    # Tailwind configuration
│   └── package.json
└── server/                   # .NET 9 Clean Architecture backend
    ├── Core/                 # Domain layer
    │   ├── Entities/         # Domain entities
    │   ├── Interfaces/       # Repository interfaces
    │   ├── ValueObjects/     # Value objects
    │   ├── Enums/            # Enumerations
    │   └── QueryModels/      # Query-specific models
    ├── Application/          # Application layer
    │   ├── Endpoints/        # API endpoints (by feature)
    │   │   ├── Auth/         # Authentication endpoints
    │   │   ├── Overview/     # Overview dashboard
    │   │   ├── Solo/         # Solo dashboard
    │   │   ├── Matches/      # Match list & details
    │   │   └── Shared/       # Shared endpoints & IEndpoint
    │   ├── DTOs/             # Request/response models
    │   ├── Services/         # Application services
    │   └── QueryModels/      # Query-specific models
    ├── Infrastructure/       # Infrastructure layer
    │   ├── Database/         # Database access
    │   │   └── Repositories/ # Repository implementations
    │   ├── Riot/             # Riot API client & mappers
    │   ├── Email/            # Email service
    │   ├── Security/         # Encryption
    │   ├── Jobs/             # Background jobs
    │   ├── WebSocket/        # SignalR hubs
    │   ├── Middleware/       # Exception handling
    │   └── Telemetry/        # Metrics
    ├── Mongoose.Api.Tests/   # Backend tests (xUnit)
    └── Program.cs            # Entry point
```

## References
- Setup instructions: [README.md](../README.md)
- Sensitive config: set via env vars or user-secrets (`RIOT_API_KEY`, `Database_test`, `Database_production`, Mollie API keys)
- Main server logic: `server/Program.cs`, `server/Application/`, `server/Application/Endpoints/`, `server/Mongoose.Api.Tests/`
- Main client logic: `client/src/`, `client/src/services/`, `client/src/composables/`
- Riot API Documentation: https://developer.riotgames.com/
- API Design: [docs/api-design-guidelines.md]
- Database schema: [docs/database-schema.md], [server/schema.sql]
- UI design guidelines: [docs/ui-design-guidelines.md]

For new patterns or changes, update this file to keep AI agents productive and aligned with project conventions.

## Behavior
Act as a professional full stack developer with many years of experience. Feel free to challenge the inputs if they do not live up to your standard of good engineering software practices and/or security.