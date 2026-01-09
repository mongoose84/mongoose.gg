# Configuration Issue: Missing Environment Variables in Production

## Problem
The live app is returning `400 Bad Request` with message "Invalid operation when getting users" when calling `/api/v1.0/users`. 

The root cause is that **the database connection string environment variable is not being set in your production environment**.

## Root Cause Analysis

The error originates from this chain:

1. Frontend calls `GET /api/v1.0/users`
2. Server's `UsersEndpoint` calls `userRepo.GetAllUsersAsync()`
3. Repository tries to create a database connection via `DbConnectionFactory.CreateConnection()`
4. `DbConnectionFactory` checks `Secrets.DatabaseConnectionString`
5. Since it's not configured, it throws `InvalidOperationException` with message "Database connection string is not configured..."
6. The endpoint catches this exception and returns `BadRequest("Invalid operation when getting users")`

## Solution: Set Environment Variables

Your production/hosting environment **must** set these environment variables:

```bash
# Required for v1 API (existing database)
export LOL_DB_CONNECTIONSTRING="Server=<your-mysql-server>;Port=3306;Database=<your-db>;User=<user>;Password=<pass>"

# Required for v2 API (new database schema)
export LOL_DB_CONNECTIONSTRING_V2="Server=<your-mysql-server>;Port=3306;Database=<your-db-v2>;User=<user>;Password=<pass>"

# Required for Riot API integration
export RIOT_API_KEY="<your-riot-api-key>"
```

### Configuration Priority (Checked in order)

For **DatabaseConnectionString** (`LOL_DB_CONNECTIONSTRING`):
1. `appsettings.json` → `ConnectionStrings:Default`
2. `appsettings.json` → `ConnectionStrings:Database`
3. `appsettings.json` → `Database:ConnectionString`
4. Environment variable: `LOL_DB_CONNECTIONSTRING`
5. Environment variable via .NET configuration: `Database__ConnectionString` (for Docker)
6. Local file: `DatabaseSecret.txt` (development only, not checked in)

For **DatabaseConnectionStringV2** (`LOL_DB_CONNECTIONSTRING_V2`):
1. `appsettings.json` → `ConnectionStrings:DatabaseV2`
2. `appsettings.json` → `ConnectionStrings:Database`
3. `appsettings.json` → `Database:ConnectionStringV2`
4. Environment variable: `LOL_DB_CONNECTIONSTRING_V2`
5. Environment variable via .NET configuration: `Database__ConnectionStringV2`
6. Local file: `DatabaseSecretV2.txt` (development only, not checked in)

## How to Fix

### Option 1: CI/CD Pipeline (Recommended)
Add the environment variables to your CI/CD deployment configuration:

**For GitHub Actions (.github/workflows/):**
```yaml
env:
  LOL_DB_CONNECTIONSTRING: ${{ secrets.LOL_DB_CONNECTIONSTRING }}
  LOL_DB_CONNECTIONSTRING_V2: ${{ secrets.LOL_DB_CONNECTIONSTRING_V2 }}
  RIOT_API_KEY: ${{ secrets.RIOT_API_KEY }}
```

**For systemd service file:**
```ini
[Service]
Environment="LOL_DB_CONNECTIONSTRING=Server=...;Password=..."
Environment="LOL_DB_CONNECTIONSTRING_V2=Server=...;Password=..."
Environment="RIOT_API_KEY=..."
```

**For Docker:**
```dockerfile
ENV LOL_DB_CONNECTIONSTRING="Server=...;Password=..."
ENV LOL_DB_CONNECTIONSTRING_V2="Server=...;Password=..."
ENV RIOT_API_KEY="..."
```

**For docker-compose.yml:**
```yaml
services:
  api:
    environment:
      - LOL_DB_CONNECTIONSTRING=Server=...;Password=...
      - LOL_DB_CONNECTIONSTRING_V2=Server=...;Password=...
      - RIOT_API_KEY=...
```

### Option 2: appsettings.json (Less Secure)
Add to `appsettings.Production.json` (but **DO NOT commit secrets**):
```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Password=...",
    "DatabaseV2": "Server=...;Password=..."
  },
  "Riot": {
    "ApiKey": "..."
  }
}
```

**⚠️ WARNING:** Never commit actual secrets to version control. Use environment variables instead.

## Verification

### Check Configuration Status
Visit the diagnostics endpoint to verify all required configuration is loaded:

```bash
curl https://lol-api.agileastronaut.com/api/v1.0/diagnostics
```

Expected response when everything is configured:
```json
{
  "environment": "Production",
  "timestamp": "2026-01-10T12:34:56Z",
  "configuration": {
    "apiKeyConfigured": true,
    "databaseConfigured": true,
    "databaseV2Configured": true,
    "allConfigured": true
  },
  "notes": [...]
}
```

If `allConfigured` is `false`, one or more required environment variables are missing.

### Test Users Endpoint
Once configured:
```bash
curl https://lol-api.agileastronaut.com/api/v1.0/users
```

Should return a JSON array of users, not a 400 error.

## Implementation Steps

1. **Identify your hosting platform** (GitHub Actions, systemd, Docker, etc.)
2. **Add environment variables** to deployment configuration with your actual connection strings and API keys
3. **Rebuild and redeploy** the application
4. **Visit diagnostics endpoint** to verify configuration loaded
5. **Test `/api/v1.0/users`** to confirm it now returns data

## Additional Improvements Made

1. **Enhanced Error Messages**: `DbConnectionFactory` now provides clearer error messages about missing configuration
2. **Configuration Logging**: `Secrets.Initialize()` logs which configurations are loaded and which are missing
3. **Diagnostics Endpoint**: Added `/api/v1.0/diagnostics` endpoint to verify configuration status without requiring authentication

## References

- [README.md - Server Setup](../../README.md#server-part)
- [Secrets Configuration Code](../Infrastructure/Configuration/Secrets.cs)
- [Database Factory Code](../Infrastructure/External/Database/DbConnectionFactory.cs)
- [Pulse Instructions - Secrets](../.github/copilot-instructions.md)
