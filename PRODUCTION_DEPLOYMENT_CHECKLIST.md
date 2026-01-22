# Production Deployment Troubleshooting Guide

## Current Issue
Your production server diagnostics show:
- ✅ `apiKeyConfigured`: true
- ❌ `databaseConfigured`: false
- ❌ `smtpConfigured`: false
- ✅ `emailEncryptionKeyConfigured`: true

## ✅ ROOT CAUSE IDENTIFIED

**The `appsettings.Production.json` file was NOT being copied to the publish output directory.**

The GitHub Actions workflow was:
1. ✅ Generating `appsettings.Production.json` with all secrets
2. ❌ Running `dotnet publish` which did NOT copy the file to the publish output
3. ❌ Deploying the publish output (without `appsettings.Production.json`) to production

**This has been FIXED** in the updated `.github/workflows/ci-server.yml` file.

## Changes Made

### 1. Enhanced Diagnostics Endpoint
Updated `/api/v2/diagnostics` to show detailed configuration sources:
- Shows which configuration sources are being checked (config files vs environment variables)
- Displays whether values are SET or NOT_SET (without exposing actual secrets)
- Shows the length of configured values to verify they're not empty

### 2. Fixed GitHub Actions Workflow
Updated `.github/workflows/ci-server.yml` to:
- Explicitly copy `appsettings.Production.json` to the publish output directory
- Verify the file exists in the publish output before deployment
- Fail the build if the file is missing

## Root Cause Analysis (for reference)

The server is missing the database connection string and SMTP configuration. Based on the code analysis:

### Configuration Loading Order
The server checks for configuration in this order (first non-empty wins):

1. **Database Connection String** (`Database_production` in Production):
   - `config.GetConnectionString("Database_production")`
   - `config["ConnectionStrings:Database_production"]`
   - `config["Database:Database_production"]`
   - `config["Database_production"]`
   - `Environment.GetEnvironmentVariable("Database_production")`

2. **SMTP Configuration**:
   - `config["Email:SmtpHost"]` or `Environment.GetEnvironmentVariable("SMTP_HOST")`
   - `config["Email:SmtpUsername"]` or `Environment.GetEnvironmentVariable("SMTP_USERNAME")`
   - `config["Email:SmtpPassword"]` or `Environment.GetEnvironmentVariable("SMTP_PASSWORD")`
   - `config["Email:SmtpPort"]` (default: 587)

## Diagnostic Steps

### Step 1: Check Enhanced Diagnostics
I've updated the `/api/v2/diagnostics` endpoint to show detailed configuration sources.

**Action**: Call the diagnostics endpoint again and check the new `configurationSources` section:
```bash
curl https://api.mongoose.gg/api/v2/diagnostics
```

Look for the `configurationSources` object which will show:
- Which configuration sources are being checked
- Whether values are SET or NOT_SET (without exposing actual values)
- The length of configured values (to verify they're not empty)

### Step 2: Verify appsettings.Production.json Deployment

**Check if the file exists on your production server:**
```bash
# SSH into your production server
ls -la /path/to/your/app/appsettings.Production.json
```

**Verify the file contains the correct structure:**
```bash
# Check file exists and has content (without exposing secrets)
cat /path/to/your/app/appsettings.Production.json | jq 'keys'
```

Expected keys: `Logging`, `AllowedHosts`, `ConnectionStrings`, `Riot`, `Auth`, `Jobs`, `Security`, `Email`

### Step 3: Verify GitHub Secrets

Ensure these secrets are configured in your GitHub repository:
- `DB_CONNECTIONSTRING` - MySQL connection string
- `SMTP_HOST` - SMTP server hostname
- `SMTP_USERNAME` - SMTP username
- `SMTP_PASSWORD` - SMTP password
- `SMTP_FROM_EMAIL` - From email address
- `ENCRYPTION_SECRET` - Email encryption key
- `RIOT_API_KEY` - Riot API key

### Step 4: Check Deployment Process

The GitHub Actions workflow (`.github/workflows/ci-server.yml`) should:
1. Generate `appsettings.Production.json` with secrets (line 109-149)
2. Publish the application (line 160+)
3. Deploy to production server

**Verify the file is included in the publish output:**
```bash
# Check if appsettings.Production.json is in the publish directory
ls -la server/bin/Release/net*/publish/appsettings.Production.json
```

## Solutions

### Solution 1: Ensure appsettings.Production.json is Deployed
The most likely issue is that `appsettings.Production.json` is not being copied to the production server.

**Fix**: Update your deployment script to ensure this file is copied:
```bash
# In your deployment script, ensure you copy the file
cp appsettings.Production.json /path/to/production/app/
```

### Solution 2: Use Environment Variables Instead
If you prefer environment variables over config files:

**Set these environment variables on your production server:**
```bash
export ASPNETCORE_ENVIRONMENT=Production
export Database_production="Server=your-host;Port=3306;Database=your-db;User Id=your-user;Password=your-pass;SslMode=Preferred;"
export SMTP_HOST="smtp.your-provider.com"
export SMTP_USERNAME="your-smtp-username"
export SMTP_PASSWORD="your-smtp-password"
export SMTP_FROM_EMAIL="noreply@mongoose.gg"
export ENCRYPTION_SECRET="your-encryption-secret"
export RIOT_API_KEY="your-riot-api-key"
```

### Solution 3: Verify File Permissions
Ensure the application can read the configuration file:
```bash
chmod 644 /path/to/production/app/appsettings.Production.json
```

## Next Steps

### Immediate Actions Required

1. **Commit and push the changes**:
   ```bash
   git add .github/workflows/ci-server.yml
   git add server/Application/Endpoints/Diagnostics/DiagnosticsEndpoint.cs
   git commit -m "Fix: Ensure appsettings.Production.json is deployed to production"
   git push origin main
   ```

2. **Wait for GitHub Actions to complete**:
   - The workflow will automatically trigger on push to `main`
   - Monitor the workflow at: https://github.com/YOUR_USERNAME/YOUR_REPO/actions
   - Verify the "Copy appsettings.Production.json to publish output" step succeeds
   - Verify the "Verify publish output" step confirms the file is present

3. **Verify the deployment**:
   - After deployment completes, call the diagnostics endpoint:
     ```bash
     curl https://api.mongoose.gg/api/v2/diagnostics
     ```
   - Check that all configuration flags are now `true`:
     - `apiKeyConfigured`: true
     - `databaseConfigured`: true
     - `smtpConfigured`: true
     - `emailEncryptionKeyConfigured`: true
     - `allConfigured`: true

4. **Review the enhanced diagnostics**:
   - Check the new `configurationSources` section in the diagnostics output
   - Verify all values show as "SET (X chars)" instead of "NOT_SET"

### Verification Checklist

After deployment, verify:
- [ ] GitHub Actions workflow completed successfully
- [ ] "Copy appsettings.Production.json" step succeeded
- [ ] "Verify publish output" step confirmed file presence
- [ ] `/api/v2/diagnostics` shows `allConfigured: true`
- [ ] Database connection is working (test by accessing any endpoint that queries the database)
- [ ] SMTP is configured (test by triggering an email verification)

## Testing

After fixing the configuration:
1. Restart your production server
2. Call `/api/v2/diagnostics` again
3. Verify all configuration flags are `true`:
   - `apiKeyConfigured`: true
   - `databaseConfigured`: true
   - `smtpConfigured`: true
   - `emailEncryptionKeyConfigured`: true
   - `allConfigured`: true

## Security Notes
- Never commit `appsettings.Production.json` to version control
- The GitHub Actions workflow generates this file during CI/CD
- Ensure proper file permissions on production server (644 or 600)
- Use HTTPS for all production endpoints

