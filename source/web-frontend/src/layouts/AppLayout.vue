<template>
  <div class="app-root">
    <nav class="top-bar">
      <div class="logo-area">
        <span class="logo-text" @click="$router.push('/home')"><span class="logo-at">@</span>potheca</span>
      </div>
      <div class="nav-tabs">
        <GoToMenu />
        <HeaderSearch />
      </div>
      <div class="nav-user">
        <button class="theme-toggle" :title="isDark ? 'Switch to light mode' : 'Switch to dark mode'" @click="toggleTheme">
          <i :class="isDark ? 'pi pi-sun' : 'pi pi-moon'"></i>
        </button>
        <div class="user-menu" ref="userMenuRef">
          <button class="user-menu-btn" :title="`Logged in as: ${user?.displayName || user?.email}`" @click="userMenuOpen = !userMenuOpen">
            <i class="pi pi-user"></i>
            <i class="pi pi-chevron-down user-menu-caret"></i>
          </button>
          <Transition name="user-menu-drop">
            <div v-if="userMenuOpen" class="user-menu-dropdown">
              <div class="user-menu-info">
                <span class="user-menu-name">{{ user?.displayName || 'Account' }}</span>
                <span class="user-menu-email">{{ user?.email }}</span>
              </div>
              <button class="user-menu-item" @click="handleLogout">
                <i class="pi pi-sign-out"></i>
                Logout
              </button>
            </div>
          </Transition>
        </div>
      </div>
    </nav>
    <main class="page-content">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { RouterView, useRouter } from 'vue-router'
import { useAuth } from '../composables/useAuth'
import { useTheme } from '../composables/useTheme'
import { useWorkspaces } from '../composables/useWorkspaces'
import GoToMenu from '../components/GoToMenu.vue'
import HeaderSearch from '../components/HeaderSearch.vue'

const { user, logout } = useAuth()
const { isDark, toggleTheme } = useTheme()
const { loadWorkspaces } = useWorkspaces()
const router = useRouter()

const userMenuOpen = ref(false)
const userMenuRef = ref(null)

function handleDocumentClick(e) {
  if (!userMenuRef.value?.contains(e.target)) userMenuOpen.value = false
}
onMounted(() => {
  loadWorkspaces()
  document.addEventListener('click', handleDocumentClick)
})
onUnmounted(() => document.removeEventListener('click', handleDocumentClick))

async function handleLogout() {
  userMenuOpen.value = false
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
  padding-right: 1.5rem;
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

.user-menu {
  position: relative;
}

.user-menu-btn {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.7rem;
  background: transparent;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-muted);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.user-menu-btn:hover {
  border-color: var(--border-purple);
  color: var(--text-primary);
  background: var(--bg-hover);
}

.user-menu-caret {
  font-size: 0.65rem;
}

.user-menu-dropdown {
  position: absolute;
  top: calc(100% + 8px);
  right: 0;
  min-width: 220px;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
  overflow: hidden;
  z-index: 1000;
}

.user-menu-info {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  padding: 0.75rem 0.9rem;
  border-bottom: 1px solid var(--border-color);
}

.user-menu-name {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
}

.user-menu-email {
  font-size: 0.78rem;
  color: var(--text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.user-menu-item {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  width: 100%;
  padding: 0.6rem 0.9rem;
  background: transparent;
  border: none;
  color: var(--text-secondary);
  font-size: 0.85rem;
  cursor: pointer;
  transition: background 0.15s, color 0.15s;
}
.user-menu-item:hover {
  background: var(--bg-hover);
  color: var(--text-primary);
}

.user-menu-drop-enter-active,
.user-menu-drop-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.user-menu-drop-enter-from,
.user-menu-drop-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}

.page-content {
  flex: 1;
  display: flex;
  overflow: hidden;
}
</style>
