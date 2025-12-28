# Augment Instructions for lol-app

## Project Overview
- **lol-app** ("Do End") is a League of Legends cross-account statistics tracker
- Full-stack project with a Vue 3 + Vite client (in `client/`) and a C# .NET 9.0 server (in `server/RiotProxy`)
- The client and server communicate via HTTP APIs. The server acts as a proxy to the Riot Games API and manages user/game data
- Sensitive secrets (API keys, DB connection strings) are stored in plaintext files in the server directory and are gitignored
- Database: MySQL for storing users, gamers, matches, and match participants

## Key Workflows
### Client (Vue 3 + Vite)
- Install dependencies: `cd client && npm install`
- Run dev server: `npm run dev` (runs on http://localhost:5173)
- Run unit tests: 
  - `npm run test:unit` - Run all test suites once
  - `npm run test:unit:watch` - Watch mode for development
  - `npm run test:unit:coverage` - Run with coverage report
- Build for production: `npm run build`
- Main entry: `client/src/main.js`, root component: `client/src/App.vue`
- Components: `client/src/components/` (reusable UI components)
- Views: `client/src/views/` (page-level components)
- API logic: `client/src/assets/` (e.g., `getUsers.js`, `getGamers.js`, `getPerformance.js`)
- Composables: `client/src/composables/` (shared reactive state/logic)
- Routing: `client/src/router/index.js`

### Server (.NET 9.0)
- Build: `cd server/RiotProxy && dotnet build`
- Run: `dotnet run` (runs on configured port, typically 5000/5001)
- Publish for Windows: `dotnet publish -c Release -r win-x86 --self-contained true`
- Publish for Linux: `dotnet publish -c Release -r linux-x64 --self-contained false`
- Secrets: Place `RiotSecret.txt` (Riot API key) and `DatabaseSecret.txt` (MySQL connection string) in `server/RiotProxy/`
- Main entry: `server/RiotProxy/Program.cs`
- Application layer: `server/RiotProxy/Application/`
  - Endpoints: `Application/Endpoints/` (API endpoint definitions)
  - DTOs: `Application/DTOs/` (request/response models)
- Infrastructure layer: `server/RiotProxy/Infrastructure/`
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
- Server: No formal test convention documented (opportunity for improvement)
- Always run tests after making changes to ensure nothing breaks

## Integration Points
- **Client <-> Server**: HTTP API calls
  - Base path: `/api/v1.0/`
  - See `getUsers.js`, `getGamers.js`, `getComparison.js`, `getPerformance.js`, etc.
- **Server <-> Riot API**: Proxy logic in `RiotApiClient.cs`
  - Fetches summoner data, match history, game versions
  - Rate limiting and error handling
- **Server <-> Database**: Connection via `DatabaseSecret.txt`
  - Logic in `Infrastructure/External/Database/`
  - Uses Dapper for data access

## Key Features
1. **Multi-Account Dashboards**: Solo, Duo, and Team views
2. **Performance Metrics**: Winrate, KDA, CS/min, Gold/min, Games played
3. **Performance Timeline**: Time-series data with rolling averages (1w, 1m, 3m, 6m, all)
4. **Account Comparison**: Compare stats across multiple accounts
5. **Automated Match Sync**: Background job syncs match history periodically

## Common Tasks
### Add a new API endpoint
1. Create a new file in `server/RiotProxy/Application/Endpoints/` implementing `IEndpoint`
2. Register it in `server/RiotProxy/Application/RiotProxyApplication.cs`
3. Create corresponding DTO in `Application/DTOs/` if needed
4. Add client-side API call function in `client/src/assets/`
5. Update views/components to use the new endpoint

### Add a new client view
1. Create a `.vue` file in `client/src/views/`
2. Add a route in `client/src/router/index.js`
3. Link to it from appropriate navigation/components

### Add a new database repository method
1. Add method to appropriate repository in `Infrastructure/External/Database/Repositories/`
2. Use Dapper for SQL queries
3. Follow async/await pattern
4. Handle null cases appropriately

## Important Notes
- **Never commit secrets**: `RiotSecret.txt` and `DatabaseSecret.txt` are gitignored
- **CORS**: Configured for localhost:5173 (dev) and lol.agileastronaut.com (prod)
- **API Rate Limiting**: Riot API has rate limits; handle 429 responses gracefully
- **Background Jobs**: MatchHistorySyncJob runs periodically to fetch new matches
- **Error Handling**: Always handle errors gracefully on both client and server
- **Performance**: Consider pagination for large datasets
- **Security**: Never expose Riot API key to client; always proxy through server

## File Structure Reference
```
lol/
├── client/                    # Vue 3 frontend
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
└── server/
    └── RiotProxy/            # .NET 9.0 backend
        ├── Application/      # Application layer
        │   ├── DTOs/        # Data transfer objects
        │   └── Endpoints/   # API endpoints
        ├── Infrastructure/   # Infrastructure layer
        │   ├── External/    # External services (DB, Riot API)
        │   └── Jobs/        # Background jobs
        └── Program.cs       # Entry point
```

## References
- Setup instructions: [README.md](../README.md)
- Sensitive config: `server/RiotProxy/RiotSecret.txt`, `server/RiotProxy/DatabaseSecret.txt`
- Main server logic: `server/RiotProxy/Program.cs`, `server/RiotProxy/Application/`
- Main client logic: `client/src/`, `client/src/assets/`, `client/src/composables/`
- Riot API Documentation: https://developer.riotgames.com/

---
For new patterns or changes, update this file to keep AI agents productive and aligned with project conventions.

