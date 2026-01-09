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

#### Server part
from root
```
cd server
cd RiotProxy
```

##### Riot API Key
Add the Riot API key to a file in the RiotProxy folder and name it RiotSecret.txt

##### Database connection string
add a database connection string to the RiotProxy folder and name it DatabaseSecret.txt

it should have this format

```
Server=your.mysql.server;Port=yourPort;Database=Database_name;User Id=User_Id;Password=yourPassword;SslMode=Preferred;
```
##### Build and run

build and run the application on Windows
```
dotnet build
dotnet run
```
build and run the application on Linux Fedora
```
dotnet publish -c Release -r linux-x64 --self-contained false
dotnet bin/Release/net9.0/linux-x64/publish/RiotProxy.dll
```
create publishable build for the hosting server
```
dotnet publish -c Release -r win-x86 --self-contained true 
```
This will create all the files needed in the folder /bin/Release/publish