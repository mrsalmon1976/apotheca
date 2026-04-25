<template>
  <aside class="sidebar" :class="{ open: open }">
    <div class="sidebar-header">
      <span>Project<template v-if="projectName">: {{ projectName }}</template></span>
    </div>

    <nav class="sidebar-nav">
      <component
        :is="item.disabled ? 'span' : 'a'"
        v-for="item in mainNav"
        :key="item.id"
        class="sidebar-item"
        :class="{ active: $route.path === item.to.value, disabled: item.disabled }"
        :href="item.disabled ? undefined : item.to.value"
        @click.prevent="!item.disabled && (router.push(item.to.value), closeSidebarOnMobile())"
      >
        <i :class="`pi ${item.icon}`"></i>
        <span>{{ item.label }}</span>
        <span v-if="item.disabled" class="coming-soon">Soon</span>
      </component>

      <div class="nav-group-label" style="margin-top:1rem">Workspace</div>
      <a
        class="sidebar-item"
        :class="{ active: $route.path === `/project/${projectId}/notes` }"
        :href="`/project/${projectId}/notes`"
        @click.prevent="router.push(`/project/${projectId}/notes`); closeSidebarOnMobile()"
      >
        <i class="pi pi-file-edit"></i>
        <span>Notes</span>
      </a>
      <a
        class="sidebar-item"
        :class="{ active: $route.path.startsWith(`/project/${projectId}/documents`) }"
        :href="`/project/${projectId}/documents`"
        @click.prevent="router.push(`/project/${projectId}/documents`); closeSidebarOnMobile()"
      >
        <i class="pi pi-folder-open"></i>
        <span>Documents</span>
      </a>

      <div class="nav-group-label" style="margin-top:1rem">Tasks</div>
      <a
        v-for="tf in taskFilters"
        :key="tf.filter"
        class="sidebar-item"
        :class="{ active: $route.path === `/project/${projectId}/tasks/${tf.filter}` }"
        :href="`/project/${projectId}/tasks/${tf.filter}`"
        @click.prevent="router.push(`/project/${projectId}/tasks/${tf.filter}`); closeSidebarOnMobile()"
      >
        <i :class="`pi ${tf.icon}`"></i>
        <span>{{ tf.label }}</span>
      </a>
    </nav>

    <div class="sidebar-version">v{{ appVersion }}</div>
  </aside>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProjects } from '../composables/useProjects'

const appVersion = import.meta.env.VITE_APP_VERSION

const props = defineProps({
  open: { type: Boolean, required: true },
})

const emit = defineEmits([])

const route = useRoute()
const router = useRouter()
const { projects } = useProjects()
const projectId   = computed(() => route.params.id)
const projectName = computed(() => projects.value.find(p => p.id === projectId.value)?.name ?? null)

function closeSidebarOnMobile() {
  if (window.innerWidth < 768) emit('close')
}

const mainNav = [
  { id: 'overview', label: 'Overview', icon: 'pi-home',           disabled: false, to: computed(() => `/project/${projectId.value}`) },
  { id: 'kanban',   label: 'Kanban',   icon: 'pi-objects-column', disabled: true,  to: computed(() => `/project/${projectId.value}/kanban`) },
  { id: 'backlog',  label: 'Backlog',  icon: 'pi-inbox',          disabled: true,  to: computed(() => `/project/${projectId.value}/backlog`) },
  { id: 'reports',  label: 'Reports',  icon: 'pi-chart-bar',      disabled: true,  to: computed(() => `/project/${projectId.value}/reports`) },
  { id: 'settings', label: 'Settings', icon: 'pi-cog',            disabled: false, to: computed(() => `/project/${projectId.value}/settings`) },
]

const taskFilters = [
  { filter: 'today',    label: 'Today',     icon: 'pi-sun' },
  { filter: 'upcoming', label: 'Upcoming',  icon: 'pi-calendar' },
  { filter: 'all',      label: 'All Tasks', icon: 'pi-list' },
]
</script>

<style scoped>
.sidebar {
  width: 240px;
  min-width: 240px;
  background: var(--bg-sidebar);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
  padding: 1rem 0;
  transition: transform 0.25s ease;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.25rem 1rem 0.75rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-muted);
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.sidebar-nav { padding: 0 0.5rem; }

.nav-group-label {
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--text-dim);
  padding: 0 0.5rem 0.4rem;
}

.sidebar-item {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  width: 100%;
  padding: 0.5rem 0.75rem;
  background: transparent;
  border: none;
  border-radius: 8px;
  color: var(--text-secondary);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.15s;
  text-align: left;
  text-decoration: none;
}
.sidebar-item:hover:not(.disabled) { background: var(--bg-hover); color: var(--text-primary); }
.sidebar-item.active {
  background: var(--bg-active);
  color: var(--color-pink);
}
.sidebar-item.active i { color: var(--color-purple); }

.sidebar-item.disabled {
  cursor: default;
  opacity: 0.45;
}

.coming-soon {
  margin-left: auto;
  font-size: 0.6rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--color-purple);
  background: var(--bg-badge);
  border: 1px solid var(--border-purple);
  padding: 0.1rem 0.4rem;
  border-radius: 999px;
}

.sidebar-version {
  margin-top: auto;
  padding: 0.5rem 0.75rem 0.25rem;
  font-size: 0.7rem;
  color: var(--text-dim);
  letter-spacing: 0.05em;
  text-align: right;
}

@media (max-width: 767px) {
  .sidebar {
    position: fixed;
    top: 60px;
    left: 0;
    bottom: 0;
    z-index: 100;
    transform: translateX(-100%);
    width: 280px;
    min-width: 0;
  }
  .sidebar.open { transform: translateX(0); }
}

@media (min-width: 768px) {
  .sidebar { transform: translateX(0); }
  .sidebar:not(.open) {
    width: 0;
    min-width: 0;
    padding: 0;
    overflow: hidden;
    border-right: none;
  }
}
</style>
