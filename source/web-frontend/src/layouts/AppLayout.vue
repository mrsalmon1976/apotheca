<template>
  <div class="app-root">
    <nav class="top-bar">
      <div class="logo-area">
        <span class="logo-text" @click="$router.push('/home')"><span class="logo-at">@</span>potheca</span>
      </div>
      <div class="nav-tabs">
        <button
          class="nav-tab"
          :class="{ active: $route.path === '/dashboard' }"
          @click="$router.push('/dashboard')"
        >
          <i class="pi pi-th-large"></i>
          Dashboard
        </button>
<ProjectMenu />
        <HeaderSearch />
      </div>
      <div class="nav-user">
        <button class="theme-toggle" :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'" @click="toggleTheme">
          <i :class="isDark ? 'pi pi-sun' : 'pi pi-moon'"></i>
        </button>
        <button class="logout-btn" :title="`Logged in as: ${user?.displayName || user?.email}`" @click="handleLogout">
          <i class="pi pi-sign-out"></i>
          Logout
        </button>
      </div>
    </nav>
    <main class="page-content">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { RouterView, useRouter } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import { useTheme } from '../composables/useTheme'
import ProjectMenu from '../components/ProjectMenu.vue'
import HeaderSearch from '../components/HeaderSearch.vue'

const { user, logout } = useAuth()
const { isDark, toggleTheme } = useTheme()
const router = useRouter()

async function handleLogout() {
  await logout()
  router.push('/home')
}
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

.nav-user {
  display: flex;
  align-items: center;
  gap: 0.75rem;
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

.logout-btn {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.9rem;
  background: transparent;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-muted);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.logout-btn:hover {
  border-color: var(--border-purple);
  color: var(--text-primary);
  background: var(--bg-hover);
}


.page-content {
  flex: 1;
  display: flex;
  overflow: hidden;
}
</style>
