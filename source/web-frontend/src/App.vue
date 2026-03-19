<template>
  <div class="app-dark app-root">
    <nav class="top-bar">
      <div class="logo-area">
<span class="logo-text"><span class="logo-at">@</span>potheca</span>
      </div>
      <div class="nav-tabs">
        <button
          class="nav-tab"
          :class="{ active: $route.path === '/notes' }"
          @click="$router.push('/notes')"
        >
          <i class="pi pi-file-edit"></i>
          Notes
        </button>
        <button
          class="nav-tab"
          :class="{ active: $route.path === '/tasks' }"
          @click="$router.push('/tasks')"
        >
          <i class="pi pi-check-square"></i>
          Tasks
        </button>
      </div>
    </nav>
    <main class="page-content">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { RouterView } from 'vue-router'
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
}

.logo-at {
  font-style: italic;
  font-weight: 900;
}

.nav-tabs {
  display: flex;
  gap: 0.25rem;
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

.page-content {
  flex: 1;
  display: flex;
  overflow: hidden;
}
</style>
