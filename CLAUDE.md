# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Apotheca is a full-stack web application. The active frontend is a Vue 3 + PrimeVue SPA (`src/frontend/`). The backend is a .NET 6 ASP.NET Core API with MongoDB (`src/web-api/`). `src/web-frontend/` is deprecated and should be ignored.

## Commands

### Frontend (Vue 3 — `src/frontend/`)

```bash
npm install        # Install dependencies
npm run dev        # Dev server (Vite, default port 5173)
npm run build      # Production build
npm run preview    # Preview production build
```

### Backend (.NET — `src/web-api/`)

```bash
dotnet build                                      # Build solution
dotnet test                                       # Run all tests
dotnet test --filter "Name=MyTest"                # Run a single test
dotnet run --project Apotheca.Web.Api             # Run the API (HTTPS on port 6060)
```

## Architecture

### Frontend (`src/frontend/`)

Vue 3 SPA built with Vite. PrimeVue (Aura preset) provides the component library, styled with a custom dark theme (black background, purple/pink brand colors via CSS custom properties in `src/assets/main.css`). Dark mode is activated via the `.app-dark` class on the root element.

- **`src/App.vue`** — Root layout: top nav bar (logo + Notes/Tasks tabs) and `<RouterView>`
- **`src/router/index.js`** — `/` redirects to `/notes`; routes for `/notes` and `/tasks`
- **`src/views/NotesView.vue`** — Left sidebar (folders, tags) + right notes card grid
- **`src/views/TasksView.vue`** — Left sidebar (view filters, projects) + right task list
- **`src/assets/main.css`** — Global CSS custom properties for all colors, backgrounds, glows, and gradients

### Backend (`src/web-api/`)

Layered architecture:

- **`Apotheca.Web.Api`** — ASP.NET Core controllers, DI wiring (`ServiceCollectionExtensions.cs`), JWT/Auth0 setup, CORS, Swagger
- **`Apotheca.BLL.Commands`** — Command pattern for business operations
- **`Apotheca.BLL.Repositories`** — Repository pattern over MongoDB (`UserRepository`, `WorkspaceRepository`)
- **`Apotheca.Db`** — `MongoDbContext`, `MongoDbMigrator`; DB models inherit from `DbModel` with BSON attributes

Each layer has a corresponding `.Tests` project using xUnit.

Data flow: Controller → View Service → Command → Repository → MongoDbContext

## Key Configuration

| Setting | Value |
|---|---|
| Frontend dev port | `5173` (Vite) |
| API URL | `https://localhost:6060` |
| Auth0 domain | `apotheca-dev.eu.auth0.com` |
| Auth0 audience | `https://apotheca-dev.com/api` |
| MongoDB | `mongodb://localhost` |
