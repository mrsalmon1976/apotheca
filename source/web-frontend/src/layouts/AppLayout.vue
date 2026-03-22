<template>
  <div class="app-dark app-root">
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
        <Select
          v-model="selectedProject"
          :options="projects"
          option-label="name"
          :placeholder="projectsLoading ? 'Loading...' : 'Jump to project...'"
          :disabled="projectsLoading"
          class="project-select"
          :class="{ active: selectedProject }"
          @update:modelValue="navigateToProject"
        >
          <template #value="{ value, placeholder }">
            <div class="select-value">
              <i class="pi pi-folder"></i>
              <span>{{ value?.name ?? placeholder }}</span>
            </div>
          </template>
          <template #option="{ option }">
            <div class="select-option-item">
              <i class="pi pi-folder"></i>
              <span>{{ option.name }}</span>
            </div>
          </template>
        </Select>
      </div>
      <div class="nav-user">
        <span class="user-name">{{ user?.displayName || user?.email }}</span>
        <button class="logout-btn" @click="handleLogout">
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
import { ref, computed, watch, onMounted } from 'vue'
import { RouterView, useRouter, useRoute } from 'vue-router'
import Select from 'primevue/select'
import { useAuth } from '../composables/useAuth'
import { useProjects } from '../composables/useProjects'

const { user, logout } = useAuth()
const { projects, loading: projectsLoading, loadProjects } = useProjects()
const router = useRouter()
const route = useRoute()

const selectedProject = ref(null)
const isProjectRoute = computed(() => route.path.startsWith('/project/'))

watch(isProjectRoute, (onProject) => {
  if (!onProject) selectedProject.value = null
})

onMounted(loadProjects)

function navigateToProject(project) {
  if (project) router.push(`/project/${project.id}`)
}

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

.user-name {
  font-size: 0.875rem;
  color: var(--text-secondary);
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
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

/* ── PrimeVue Select overrides ── */
:deep(.project-select) {
  height: 36px;
  min-width: 180px;
  background: var(--bg-input);
  border: 1px solid var(--border-purple);
  border-radius: 8px;
  transition: border-color 0.2s, box-shadow 0.2s;
}

:deep(.project-select:hover) {
  border-color: var(--border-purple);
}

:deep(.project-select.p-focus) {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 1px var(--color-purple);
  outline: none;
}

:deep(.project-select .p-select-label) {
  font-size: 0.875rem;
  color: var(--text-secondary);
  padding: 0 0.75rem;
  line-height: 34px;
}

:deep(.project-select .p-select-label.p-placeholder) {
  color: var(--text-muted);
}

:deep(.project-select .p-select-dropdown) {
  color: var(--text-muted);
  width: 2rem;
}

:deep(.project-select.active) {
  background: var(--bg-active);
  border-color: transparent;
  box-shadow: 0 0 12px var(--glow-pink);
}

:deep(.project-select.active .p-select-label) {
  color: var(--color-pink);
}

:deep(.project-select.active .p-select-dropdown) {
  color: var(--color-pink);
}

.project-select.active .select-value .pi {
  color: var(--color-pink);
}


.select-value,
.select-option-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.select-value .pi,
.select-option-item .pi {
  color: var(--color-purple);
  font-size: 0.85rem;
}

.page-content {
  flex: 1;
  display: flex;
  overflow: hidden;
}
</style>
