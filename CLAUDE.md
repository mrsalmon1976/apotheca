# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Apotheca is a full-stack web application. The active frontend is a Vue 3 + PrimeVue SPA (`source/web-frontend/`).
The backend is a .NET 10 Web API (`source/web-api/`) deployed as a **Google Cloud Run** service (`apotheca-api`). The database is **Neon** (managed PostgreSQL). Deployments are automated via `deployment/deploy.ps1`.

## Commands

### Backend (.NET 10 — `source/web-api/`)

```bash
dotnet build          # Build the project
dotnet run            # Run on https://localhost:6060
dotnet watch          # Run with hot reload
```

### Frontend (Vue 3 — `source/web-frontend/`)

```bash
npm install        # Install dependencies
npm run dev        # Dev server (Vite, default port 5173)
npm run build      # Production build
npm run preview    # Preview production build
```

### Database

```bash
docker compose up -d		# Start up the database and pgAdmin (http://localhost:5050)
```

### Deployment (`deployment/`)

#### Prerequisites (one-time setup)

**Google Cloud SDK** — required for API deployment:
```powershell
winget install -e --id Google.CloudSDK
gcloud auth login
```

**Firebase CLI** — required for frontend deployment. Install and authenticate once:
```powershell
npm install -g firebase-tools
firebase login
```

#### Running a deployment

```powershell
.\deployment\deploy.ps1
```

The script prompts for each phase independently so steps can be skipped:

| Phase | What it does |
|---|---|
| Migrations | Runs `Apotheca.Migrations` against Neon via `ConnectionString` env var |
| API | Cloud Build compiles and pushes a Docker image to Artifact Registry, then deploys to Cloud Run |
| Frontend | Writes `.env.production` from secrets, runs `npm run build`, deploys to Firebase Hosting, deletes `.env.production` |

Before first run, copy `secrets/deploy_secrets.template.json` to `secrets/deploy_secrets.json` (gitignored) and fill in all values.

#### Post-deployment checklist (one-time per new domain)

When the frontend URL changes, update these before redeploying:
- **`secrets/deploy_secrets.json`** → `FrontendUrl` — controls the API's CORS allowed origin
- **Firebase Console** → Authentication → Settings → Authorized domains → add the new domain
- **Azure Portal** → App registrations → your app → Authentication → Redirect URIs → add `https://<domain>/__/auth/handler`

## Architecture

- See @docs/architecture.md for more architectural detail.

### Backend (`source/web-api/`)

.NET 10 Web API using **vertical slice architecture** — features are self-contained under `Features/DomainArea/<FeatureName>/` with their own controller and models, rather than a layered Controllers/Services/Models split.

- **`Features/Ping/PingController.cs`** — `GET /api/ping` returns status and UTC timestamp
- **`Program.cs`** — Minimal host setup: controllers, Firebase JWT Bearer auth middleware, CORS

#### Authentication (backend)

All API endpoints requiring auth inherit from `Features/AuthenticatedBaseController.cs`, which carries `[Authorize]` and `[ApiController]` and exposes `GetFirebaseUid()` (reads the `sub` claim).

JWT Bearer middleware is configured in `Program.cs` for Firebase OIDC (`securetoken.google.com/{projectId}`), with `MapInboundClaims = false` so claim names are preserved as-is (e.g. `sub` rather than the Microsoft URI equivalent).

#### Data access

All database access uses **Dapper** via `IDbContext`/`IDbContextFactory` in `Apotheca.Data`. `DbContextFactory` uses `NpgsqlDataSourceBuilder` to create a `NpgsqlDataSource` (singleton), which enforces UTC timestamp handling — all `TIMESTAMPTZ` columns are read/written as UTC. See @docs/architecture.md for query patterns.

### Frontend (`source/web-frontend/`)

Vue 3 SPA built with Vite. PrimeVue (Aura preset) provides the component library, styled with a custom dark theme (black background, purple/pink brand colors via CSS custom properties in `src/assets/main.css`). Dark mode is activated via the `.app-dark` class on the root element.

#### Layouts

- **`src/layouts/PublicLayout.vue`** — Nav bar for unauthenticated pages (Home, Features, About, Login). Shows a Dashboard button and Logout when logged in.
- **`src/layouts/AppLayout.vue`** — Nav bar for authenticated pages. Includes Dashboard/Notes/Tasks nav tabs, a project jump dropdown (loaded from API), and a Logout button with the username as a tooltip.

#### Router (`src/router/index.js`)

- `/` → redirects to `/home`
- Public (PublicLayout): `/home`, `/features`, `/about`, `/auth/login`, `/logging-in`
- Authenticated (AppLayout, `requiresAuth: true`): `/dashboard`, `/notes`, `/tasks`, `/project/:id`

Post-login redirect goes to `/dashboard`.

#### Key views

- **`src/features/dashboard/DashboardView.vue`** — Default post-login page; stat cards and activity overview
- **`src/features/notes/NotesView.vue`** — Left sidebar (folders, tags) + right notes card grid
- **`src/features/tasks/TasksView.vue`** — Left sidebar (view filters, projects) + right task list
- **`src/features/projects/ProjectView.vue`** — Per-project page; sidebar with sections (Notes, Tasks, Activity) and members; URL includes project ID (`/project/:id`)

#### Composables

- **`src/composables/useAuth.js`** — Firebase auth singleton; supports Google, Microsoft, email/password. Errors shown via PrimeVue Toast.
- **`src/composables/useProjects.js`** — Fetches the user's projects from the API; surfaces load errors via PrimeVue Toast.

### Authentication (frontend)

Firebase Authentication handles all auth. The Firebase client is initialised in `src/firebase.js` and all auth logic lives in `src/composables/useAuth.js`.

Supported providers:
- **Google** — OAuth via `GoogleAuthProvider`
- **Microsoft** — OAuth via `OAuthProvider('microsoft.com')`
- **Email/Password** — `signInWithEmailAndPassword` / `createUserWithEmailAndPassword`, with `sendPasswordResetEmail` for password reset

Auth state is tracked via `onAuthStateChanged` as a module-level singleton so it is shared across all callers of `useAuth()`. Errors are surfaced to the user via PrimeVue Toast with Firebase error codes mapped to friendly messages in `EMAIL_ERRORS`.

## Color Palette

Defined as CSS custom properties in `source/web-frontend/src/assets/main.css`.

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

## Key Configuration

| Setting | Value |
|---|---|
| Frontend dev port | `5173` (Vite) |
| API URL (local) | `https://localhost:6060` |
| API URL (production) | `https://apotheca-api-fmoznjmzma-nw.a.run.app` |
| API ping (production) | `https://apotheca-api-fmoznjmzma-nw.a.run.app/ping` |
| Frontend URL (production) | `https://apotheca-490805.web.app` |
| Firebase project | `apotheca-490805` |
| PostgreSQL | `Host=localhost;Port=5432;Database=apotheca;Username=apotheca;Password=apotheca` |
