# Apotheca Project Instructions

This file defines the architecture, conventions, and workflows for the Apotheca project. Adhere to these instructions for all development tasks.

## Overview

Apotheca is a full-stack web application consisting of a .NET 10 Web API backend and a Vue 3 SPA frontend.

- **Backend:** .NET 10 Web API (`source/web-api/`) hosted on Google Cloud Run.
- **Frontend:** Vue 3 + Vite + PrimeVue SPA (`source/web-frontend/`).
- **Database:** Neon (managed PostgreSQL).
- **Infrastrucutre:** Docker Compose for local services (PostgreSQL, GCS, Pub/Sub).

## Backend Conventions (`source/web-api/`)

### Architecture: Vertical Slice
The backend follows a **Vertical Slice Architecture**. Each feature is self-contained under `Features/DomainArea/FeatureName/`.
- Each slice should contain its own Controller, Repository, and Request/Response models.
- Avoid shared service layers; logic should be contained within the slice or domain-specific utilities.

### Controllers & Security
- All authenticated controllers MUST inherit from `AuthenticatedBaseController`.
- `AuthenticatedBaseController` provides `[Authorize]` and `[ApiController]` attributes and exposes `GetFirebaseUid()`.
- Firebase JWT Bearer is used for authentication.

### Data Access (Dapper)
- Use `IDbContextFactory` to create `IDbContext` instances.
- Use Dapper for all SQL queries.
- **Ordering:** Methods in `IDbContext` and its implementations MUST be ordered alphabetically.
- **Timestamps:** All `TIMESTAMPTZ` values MUST be UTC.

### Event Handling (Pub/Sub)
Pub/Sub event handlers are located under `Events/DomainArea/EventName/`.
- `{EventName}Event.cs`: Event payload.
- `{EventName}EventHandler.cs`: Controller receiving Pub/Sub pushes (uses `[Authorize(Policy = "PubSubPush")]`).
- `{EventName}Repository.cs`: Data access for the handler.

## Frontend Conventions (`source/web-frontend/`)

### Technology Stack
- Vue 3 (Composition API) + Vite.
- PrimeVue (Aura preset) for UI components.
- Dark theme support via `.app-dark` on `<html>`.

### Layouts & Sidebars
- `PublicLayout.vue`: Unauthenticated pages.
- `AppLayout.vue`: Authenticated pages (Dashboard, Nav, Project Jump).
- Sidebars (`ProjectSidebar.vue`, `AccountSidebar.vue`) are rendered inside specific views.

### State & Logic
- Use **Composables** (`src/composables/`) for shared logic and state management (e.g., `useAuth.js`, `useProjects.js`).
- Router (`src/router/index.js`) handles navigation and authentication guards (`requiresAuth: true`).

## Development Workflow

### Commands

**Backend:**
- `dotnet build`: Build the solution.
- `dotnet run`: Run the API on `https://localhost:6060`.
- `dotnet watch`: Run with hot reload.

**Frontend:**
- `npm install`: Install dependencies.
- `npm run dev`: Start dev server on `http://localhost:5173`.
- `npm run build`: Production build.

**Local Services:**
- `docker compose up -d`: Start PostgreSQL, pgAdmin, Pub/Sub emulator, and GCS emulator.

**Migrations:**
- Run the migration project: `dotnet run --project source/web-api/Apotheca.Migrations -- "Connection_String"`.

## Project Structure Highlights

- `docs/`: Architectural details, data models, and setup guides.
- `images/`: Brand assets and logos.
- `source/web-api/Apotheca.Data/`: Core data access layer.
- `source/web-frontend/src/features/`: Domain-specific frontend modules.
