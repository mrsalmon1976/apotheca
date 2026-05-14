# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with this repository. See @docs/architecture.md for full architectural detail, deployment instructions, and local dev setup.

## Overview

Apotheca is a full-stack web application. Frontend: Vue 3 + PrimeVue SPA (`source/web-frontend/`). Backend: .NET 10 Web API (`source/web-api/`) on Google Cloud Run. Database: Neon (managed PostgreSQL).

## Commands

### Backend (`source/web-api/`)

```bash
dotnet build    # Build
dotnet run      # Run on https://localhost:6060
dotnet watch    # Run with hot reload
```

### Frontend (`source/web-frontend/`)

```bash
npm install     # Install dependencies
npm run dev     # Dev server on http://localhost:5173
npm run build   # Production build
```

### Local services

```bash
docker compose up -d   # PostgreSQL, pgAdmin, Pub/Sub emulator, GCS emulator
```

### Migrations

```bash
dotnet run --project source/web-api/Apotheca.Migrations -- "Host=localhost;Port=5432;Database=apotheca;Username=apotheca;Password=apotheca"
```

## Architecture

Detailed documentation: [Architecture](./docs/architecture.md)

### Backend (`source/web-api/`)

**Vertical slice architecture** — each feature is self-contained under `Features/DomainArea/FeatureName/` with its own controller and repository. No shared service layer.

Key files:
- `Program.cs` — DI registration, Firebase JWT auth, CORS, GCS client
- `Features/AuthenticatedBaseController.cs` — base for all authenticated endpoints; exposes `GetFirebaseUid()`
- `Apotheca.Data/` — `IDbContextFactory` / `IDbContext` (Dapper over Npgsql)

All authenticated controllers inherit `AuthenticatedBaseController` (carries `[Authorize]` + `[ApiController]`). Firebase JWT Bearer is configured for `securetoken.google.com/{projectId}` with `MapInboundClaims = false`.

Data access: inject `IDbContextFactory`, call `CreateAsync()` to get an `IDbContext`, run Dapper queries. All `TIMESTAMPTZ` values are UTC. Methods on `IDbContext` are alphabetically ordered — follow this when adding new ones.

Pub/Sub event handlers live under `Events/DomainArea/EventName/` (e.g. `Events/Documents/DocumentDeleted/`). Each folder contains:
- `{EventName}Event.cs` — the event payload class
- `{EventName}EventHandler.cs` — the controller that receives the Pub/Sub push (`[Authorize(Policy = "PubSubPush")]`, inherits `ControllerBase`)
- `{EventName}Repository.cs` — data access for the handler (if needed)

### Frontend (`source/web-frontend/`)

Vue 3 + Vite + PrimeVue (Aura preset). Dark theme toggled via `.app-dark` on `<html>` (`useTheme.js`); brand colors are CSS custom properties in `src/assets/main.css`.

Layouts:
- `PublicLayout.vue` — unauthenticated pages
- `AppLayout.vue` — authenticated pages; Dashboard nav tab, project jump dropdown (`ProjectMenu`), header search, theme toggle

Sidebars (rendered inside views, not the layout):
- `ProjectSidebar.vue` — project-scoped nav (Overview, Workspace, Tasks sections)
- `AccountSidebar.vue` — account nav for Dashboard (My Account, Tasks sections)

Router (`src/router/index.js`):
- Public: `/home`, `/features`, `/about`, `/auth/login`, `/logging-in`
- Authenticated (`requiresAuth: true`):
  - `/dashboard` — DashboardView
  - `/search` — SearchView
  - `/tasks/:filter` — TasksView (global, no project context)
  - `/project/:id` — ProjectView
  - `/project/:id/notes[/f/:folders*]`, `/project/:id/notes/:noteId`
  - `/project/:id/documents[/f/:folders*]`, `/project/:id/documents/:documentId`
  - `/project/:id/tasks/:filter`
  - `/project/:id/settings`

Key composables:
- `useAuth.js` — Firebase auth singleton (Google, Microsoft, email/password)
- `useProjects.js` — loads the user's projects from the API
- `useTheme.js` — dark/light theme toggle, persisted to `localStorage`
- `useProjectTasks.js` — task loading and completion for a project
- `useNoteFolders.js` — note folder and note CRUD
- `useDocumentFolders.js` — document folder CRUD and file upload

## Color Palette

Defined in `src/assets/main.css` as CSS custom properties.

| Role | Hex |
|---|---|
| Background (primary) | `#0a0a0f` |
| Background (nav/sidebar) | `#0d0d14` |
| Background (card) | `#111118` |
| Brand purple | `#a855f7` |
| Brand purple (light) | `#c084fc` |
| Brand pink | `#ec4899` |
| Brand pink (light) | `#f472b6` |
| Text (primary) | `#f1f0f5` |
| Text (secondary) | `#b8b4c8` |
| Text (muted) | `#7a7590` |
| Text (dim) | `#524e65` |
| Brand gradient | `#a855f7` → `#ec4899` (135°) |

## Local Configuration

| Setting | Value |
|---|---|
| API | `https://localhost:6060` |
| Frontend | `http://localhost:5173` |
| PostgreSQL | `Host=localhost;Port=5432;Database=apotheca;Username=apotheca;Password=apotheca` |
| PGAdmin | `Host=apotheca-postgres;Port=5432;Database=apotheca;Username=apotheca;Password=apotheca` |
| GCS emulator | `http://localhost:4443` |
| Pub/Sub emulator | `http://localhost:8085` |
