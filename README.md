# League of Legends improvement tracker

> "The only LoL improvement tracker built for duos and teams, powered by AI coaching that turns your stats into actionable goals you can actually achieve."

This project helps players (solo, duo, and full teams) understand their performance with rich match analytics, timeline-derived metrics, and AI goal recommendations.

![League of Legends solo dashboard screenshot](image.png)

## 1st time install

#### Git
##### Mac:
use homebrew if on mac: 
```
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```
```
brew install git
```

##### Windows:
Download git installer

##### Linux Fedora:
```
sudo dnf install git-all
```

##### Set git user (from terminal)
```
git config --global user.name anon

git config --global user.email anon.anonsen@pm.me
```
#### Node.js
Download and install node js installer https://nodejs.org/en/download

#### Install Visual Studio code
https://code.visualstudio.com/download

##### VS Code Extensions
Git Graph

Markdown Preview

Vue

Github actions

C# Dev Kit

## In Visual Studio:
Git clone https://github.com/mongoose84/AgileAstronaut.com.git

The file structure contains a server and a client part.

## .NET SDK
##### Windows:
Download .NET 9.0 SDK installer

##### Linux Fedora:
```
sudo dnf install dotnet-sdk-9.0
```
#### Client part
##### Install development server
from root
```
cd client
```
```
npm install
```
```
npm i -D vitest @vue/test-utils axios-mock-adapter;
```
##### Run dev
```
npm run dev
```
##### Unit test
```
npm run test:unit // Run all test suites once

npm run test:unit:watch // Run all test suites but watch for changes and rerun tests when they change.

npm run test:unit:coverage // Run all tests once and show test coverage
```

#### Client v2 (standalone app)
**Client v2 is a completely separate Vue 3 + Vite application, independent of the legacy client.**

- Location: `client_v2/` (separate directory)
- Decision: Build a fresh Vue 3 + Vite app to avoid disrupting the legacy client while we develop the v2 experience.
- Style: Start with the Vercel developer aesthetic (dark, sharp, neon-tinged) but keep theme tokens configurable for future restyles.
- Rollout: Develop locally until the solo dashboard is ready, then ship v2; legacy client continues running independently.
- The v2 app will have its own `package.json`, `node_modules`, and complete build setup.

##### Install and run Client v2
from root
```
cd client_v2
```
```
npm install
```
```
npm run dev
```

#### Server part
from root
```
cd server
cd RiotProxy
```

##### Riot API Key
Get your API key from the [Riot Developer Portal](https://developer.riotgames.com/).

**Option 1: .NET user-secrets (recommended for local development)**
```bash
# from server/ directory
dotnet user-secrets init  # only needed once
dotnet user-secrets set "Riot:ApiKey" "RGAPI-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

**Option 2: Environment variable**
```bash
# Linux/Mac (add to ~/.bashrc or ~/.zshrc for persistence)
export RIOT_API_KEY="RGAPI-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"

# Windows PowerShell (current session)
$env:RIOT_API_KEY = "RGAPI-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"

# Windows PowerShell (permanent - user level)
[Environment]::SetEnvironmentVariable("RIOT_API_KEY", "RGAPI-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", "User")

# Windows CMD (current session)
set RIOT_API_KEY=RGAPI-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

##### Database connection string (local)
Set via user-secrets:
```


# user-secrets (from server/)
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:DatabaseV2" "Server=...;Password=...;"
```

##### Build and run

build and run the application
```
dotnet build
dotnet run

dotnet publish -c Release -r win-x86 --self-contained true 
```
This will create all the files needed in the folder /bin/Release/publish