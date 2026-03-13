# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Apotheca is a full-stack web application with a .NET 6 backend API and Vue.js 2 frontend, using MongoDB for persistence and Auth0 for authentication.

## Commands

### Backend (.NET — `src/web-api/`)

```bash
dotnet build                 # Build solution
dotnet test                  # Run all tests
dotnet test --filter "Name=MyTest"  # Run a single test
dotnet run --project Apotheca.Web.Api  # Run the API (HTTPS on port 6060)
```

### Frontend (Vue.js — `src/web-frontend/`)

```bash
npm install        # Install dependencies
npm run serve      # Dev server on port 4040
npm run build      # Production build
npm run test:unit  # Run Jest unit tests
npm run lint       # ESLint
```

## Architecture

### Backend (`src/web-api/`)

Layered architecture with the following projects:

- **`Apotheca.Web.Api`** — ASP.NET Core controllers (`UserController`, `UserWorkspaceController`, `VersionController`), DI wiring (`ServiceCollectionExtensions.cs`), JWT/Auth0 setup, CORS for `localhost:4040`, Swagger
- **`Apotheca.BLL.Commands`** — Command pattern for business operations (e.g. `CreateUserCommand`, `CreateWorkspaceCommand`)
- **`Apotheca.BLL.Repositories`** — Repository pattern abstracting MongoDB access (`UserRepository`, `WorkspaceRepository`)
- **`Apotheca.Db`** — MongoDB context (`MongoDbContext`) and migration logic (`MongoDbMigrator`); DB models live here with BSON attributes, all inheriting from `DbModel`

Each layer has a corresponding `.Tests` project using xUnit.

Data flow: Controller → View Service → Command → Repository → MongoDbContext

### Frontend (`src/web-frontend/src/`)

Vue.js 2 SPA with:
- **`auth/`** — Auth0 integration via `@auth0/auth0-spa-js`; auth guard for protected routes
- **`services/`** — Axios-based API clients with bearer token injection
- **`router/`** — Vue Router (public: Home, About, Callback; protected: Dashboard, Profile)
- **`plugins/`** — Vuetify 2 (Material Design)

## Key Configuration

| Setting | Value |
|---|---|
| API URL | `https://localhost:6060` |
| Frontend dev port | `4040` |
| Auth0 domain | `apotheca-dev.eu.auth0.com` |
| Auth0 audience | `https://apotheca-dev.com/api` |
| MongoDB | `mongodb://localhost` |

Frontend environment is configured via `src/web-frontend/.env` and `auth_config.json`.

## Docker

Both services have Dockerfiles with multi-stage builds. The frontend uses `http-server` in production (Node Alpine image).
