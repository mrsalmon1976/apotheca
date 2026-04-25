<template>
  <div class="app-root">
    <nav class="top-bar">
      <div class="logo-area">
        <span class="logo-text" @click="$router.push('/home')"><span class="logo-at">@</span>potheca</span>
      </div>
      <div class="nav-tabs">
        <button
          class="nav-tab"
          :class="{ active: $route.path === '/home' }"
          @click="$router.push('/home')"
        >
          <i class="pi pi-home"></i>
          Home
        </button>
        <button
          class="nav-tab"
          :class="{ active: $route.path === '/features' }"
          @click="$router.push('/features')"
        >
          <i class="pi pi-star"></i>
          Features
        </button>
        <button
          class="nav-tab"
          :class="{ active: $route.path === '/about' }"
          @click="$router.push('/about')"
        >
          <i class="pi pi-info-circle"></i>
          About
        </button>
      </div>
      <div class="nav-actions">
        <button class="theme-toggle" :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'" @click="toggleTheme">
          <i :class="isDark ? 'pi pi-sun' : 'pi pi-moon'"></i>
        </button>
        <template v-if="user">
          <button class="action-btn action-btn--ghost" :title="`Logged in as: ${user.displayName || user.email}`" @click="logout">Logout</button>
          <button class="action-btn action-btn--primary" @click="$router.push('/dashboard')">Dashboard</button>
        </template>
        <template v-else>
          <button class="action-btn action-btn--primary" @click="$router.push('/auth/login')">Sign In</button>
        </template>
      </div>
    </nav>
    <main class="page-content">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { RouterView } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import { useTheme } from '../composables/useTheme'

const { user, logout } = useAuth()
const { isDark, toggleTheme } = useTheme()
</script>

<style scoped>
.app-root {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--bg-primary);
}

.top-bar {
  display: flex;
  align-items: center;
  padding: 0 1.5rem;
  height: 60px;
  background: var(--bg-nav);
  border-bottom: 1px solid var(--border-color);
  flex-shrink: 0;
}

.logo-area {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  margin-right: 2.5rem;
}

.logo-text {
  font-size: 1.2rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  cursor: pointer;
}

.logo-at {
  font-style: italic;
  font-weight: 900;
}

.nav-tabs {
  display: flex;
  gap: 0.25rem;
  flex: 1;
}

.nav-tab {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1.25rem;
  background: transparent;
  border: none;
  border-radius: 8px;
  color: var(--text-muted);
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  letter-spacing: 0.02em;
}

.nav-tab:hover {
  background: var(--bg-hover);
  color: var(--text-primary);
}

.nav-tab.active {
  background: var(--bg-active);
  color: var(--color-pink);
  box-shadow: 0 0 12px var(--glow-pink);
}

.nav-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.theme-toggle {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  background: transparent;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-muted);
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.theme-toggle:hover {
  border-color: var(--border-purple);
  color: var(--color-purple);
  background: var(--bg-hover);
}

.action-btn {
  padding: 0.4rem 1.1rem;
  border-radius: 8px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  letter-spacing: 0.02em;
}

.action-btn--ghost {
  background: transparent;
  border: 1px solid var(--border-purple);
  color: var(--text-secondary);
}

.action-btn--ghost:hover {
  background: var(--bg-hover);
  color: var(--text-primary);
}

.action-btn--primary {
  background: var(--gradient-brand);
  border: none;
  color: #fff;
  box-shadow: 0 0 12px var(--glow-purple);
}

.action-btn--primary:hover {
  box-shadow: 0 0 20px var(--glow-purple);
  opacity: 0.9;
}

.page-content {
  flex: 1;
  display: flex;
  overflow: hidden;
}
</style>
