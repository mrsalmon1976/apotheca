# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Apotheca is a full-stack web application. The active frontend is a Vue 3 + PrimeVue SPA (`source/web-frontend/`).
The backend is a .NET 10 Web API (`source/web-api/`) targeting Google Cloud Run.

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

```base
docker compose up -d		# Start up the database and pgAdmin (http://localhost:5050)
```

## Architecture

- See @docs/architecture.md for more architectural detail.

### Backend (`source/web-api/`)

.NET 10 Web API using **vertical slice architecture** — features are self-contained under `Features/DomainArea/<FeatureName>/` with their own controller and models, rather than a layered Controllers/Services/Models split.

- **`Features/Ping/PingController.cs`** — `GET /api/ping` returns status and UTC timestamp
- **`Program.cs`** — Minimal host setup: controllers only, no OpenAPI

### Frontend (`source/web-frontend/`)

Vue 3 SPA built with Vite. PrimeVue (Aura preset) provides the component library, styled with a custom dark theme (black background, purple/pink brand colors via CSS custom properties in `source/assets/main.css`). Dark mode is activated via the `.app-dark` class on the root element.

- **`source/App.vue`** — Root layout: top nav bar (logo + Notes/Tasks tabs) and `<RouterView>`
- **`source/router/index.js`** — `/` redirects to `/notes`; routes for `/notes` and `/tasks`
- **`source/views/NotesView.vue`** — Left sidebar (folders, tags) + right notes card grid
- **`source/views/TasksView.vue`** — Left sidebar (view filters, projects) + right task list
- **`source/assets/main.css`** — Global CSS custom properties for all colors, backgrounds, glows, and gradients

### Authentication

Firebase Authentication handles all auth. The Firebase client is initialised in `src/firebase.js` and all auth logic lives in `src/composables/useAuth.js`.

Supported providers:
- **Google** — OAuth via `GoogleAuthProvider`
- **Microsoft** — OAuth via `OAuthProvider('microsoft.com')`
- **Email/Password** — `signInWithEmailAndPassword` / `createUserWithEmailAndPassword`, with `sendPasswordResetEmail` for password reset

Auth state is tracked via `onAuthStateChanged` as a module-level singleton so it is shared across all callers of `useAuth()`. Errors are surfaced to the user via PrimeVue Toast with Firebase error codes mapped to friendly messages in `EMAIL_ERRORS`.

## Color Palette

Defined as CSS custom properties in `source/web-frontend/source/assets/main.css`.

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
| API URL | `https://localhost:6060` |
| Firebase project | `apotheca-dev` |
| MongoDB | `mongodb://localhost` |
