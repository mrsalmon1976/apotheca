<template>
  <aside class="sidebar" :class="{ open: open }">
    <div class="sidebar-header">
      <a class="sidebar-title" href="/dashboard" :title="workspaceName ?? undefined" @click.prevent="goToWorkspace"><span v-if="workspaceName">{{ workspaceName }}</span></a>
      <div class="sidebar-project">
        <svg class="sidebar-project-arrow" viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M3 1v6a5 5 0 0 0 5 5h6" />
          <path d="M11 9l3 3-3 3" />
        </svg>
        <a
          class="sidebar-project-name"
          :href="mainNav[0].to.value"
          :title="projectName ?? undefined"
          @click.prevent="router.push(mainNav[0].to.value); closeSidebarOnMobile()"
        >{{ projectName }}</a>
      </div>
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

      <div class="nav-group-label" style="margin-top:1rem">Content</div>
      <a
        class="sidebar-item"
        :class="{ active: $route.path === `/workspace/${workspaceId}/project/${projectId}/notes` }"
        :href="`/workspace/${workspaceId}/project/${projectId}/notes`"
        @click.prevent="router.push(`/workspace/${workspaceId}/project/${projectId}/notes`); closeSidebarOnMobile()"
      >
        <i class="pi pi-file-edit"></i>
        <span>Notes</span>
      </a>
      <a
        class="sidebar-item"
        :class="{ active: $route.path.startsWith(`/workspace/${workspaceId}/project/${projectId}/documents`) }"
        :href="`/workspace/${workspaceId}/project/${projectId}/documents`"
        @click.prevent="router.push(`/workspace/${workspaceId}/project/${projectId}/documents`); closeSidebarOnMobile()"
      >
        <i class="pi pi-folder-open"></i>
        <span>Documents</span>
      </a>
      <a
        class="sidebar-item"
        :class="{ active: $route.path.startsWith(`/workspace/${workspaceId}/project/${projectId}/mindmaps`) }"
        :href="`/workspace/${workspaceId}/project/${projectId}/mindmaps`"
        @click.prevent="router.push(`/workspace/${workspaceId}/project/${projectId}/mindmaps`); closeSidebarOnMobile()"
      >
        <i class="pi pi-sitemap"></i>
        <span>Mindmaps</span>
      </a>

      <div class="nav-group-label" style="margin-top:1rem">Tasks</div>
      <a
        v-for="tf in taskFilters"
        :key="tf.filter"
        class="sidebar-item"
        :class="{ active: $route.path === `/workspace/${workspaceId}/project/${projectId}/tasks/${tf.filter}` }"
        :href="`/workspace/${workspaceId}/project/${projectId}/tasks/${tf.filter}`"
        @click.prevent="router.push(`/workspace/${workspaceId}/project/${projectId}/tasks/${tf.filter}`); closeSidebarOnMobile()"
      >
        <i :class="`pi ${tf.icon}`"></i>
        <span>{{ tf.label }}</span>
      </a>
    </nav>

    <div class="sidebar-version">v{{ appVersion }}</div>
  </aside>
</template>

<script setup>
import { computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProjects } from '../composables/useProjects'
import { useWorkspaces } from '../composables/useWorkspaces'

const appVersion = import.meta.env.VITE_APP_VERSION

const props = defineProps({
  open: { type: Boolean, required: true },
})

const emit = defineEmits(['close'])

const route = useRoute()
const router = useRouter()
const { projects, loadProjects } = useProjects()
const projectId   = computed(() => route.params.id)
const workspaceId = computed(() => route.params.workspaceId)
const projectName = computed(() => projects.value.find(p => p.id === projectId.value)?.name ?? null)
const { workspaces, currentWorkspace, switchWorkspace } = useWorkspaces()
const workspaceName = computed(() => workspaces.value.find(w => w.id === workspaceId.value)?.name ?? null)

// The Dashboard shows the current workspace, so switch to this project's workspace first if needed
async function goToWorkspace() {
  closeSidebarOnMobile()
  if (workspaceId.value && workspaceId.value !== currentWorkspace.value?.id) {
    if (!await switchWorkspace(workspaceId.value)) return
  }
  router.push('/dashboard')
}

// The project list is shared app-wide but only fetched on demand — make sure it's
// loaded when landing directly on a project page (deep link, restored location, etc.)
// rather than relying on the Dashboard having populated it first.
watch(
  [workspaceId, projectId],
  ([wsId, pId]) => {
    if (wsId && pId && !projects.value.some(p => p.id === pId)) {
      loadProjects(wsId)
    }
  },
  { immediate: true }
)

function closeSidebarOnMobile() {
  if (window.innerWidth < 768) emit('close')
}

const mainNav = [
  { id: 'overview', label: 'Overview', icon: 'pi-home',           disabled: false, to: computed(() => `/workspace/${workspaceId.value}/project/${projectId.value}`) },
  { id: 'kanban',   label: 'Kanban',   icon: 'pi-objects-column', disabled: true,  to: computed(() => `/workspace/${workspaceId.value}/project/${projectId.value}/kanban`) },
  { id: 'backlog',  label: 'Backlog',  icon: 'pi-inbox',          disabled: true,  to: computed(() => `/workspace/${workspaceId.value}/project/${projectId.value}/backlog`) },
  { id: 'reports',  label: 'Reports',  icon: 'pi-chart-bar',      disabled: true,  to: computed(() => `/workspace/${workspaceId.value}/project/${projectId.value}/reports`) },
  { id: 'settings', label: 'Settings', icon: 'pi-cog',            disabled: false, to: computed(() => `/workspace/${workspaceId.value}/project/${projectId.value}/settings`) },
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
  flex-direction: column;
  padding: 0.25rem 1rem 0.75rem;
  margin: 0 0.5rem 0.75rem;
  border-bottom: 1px solid var(--border-color);
  font-size: 0.9rem;
  font-weight: 800;
  letter-spacing: 0.02em;
}
.sidebar-title {
  min-width: 0;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  color: var(--color-pink);
  text-decoration: none;
  transition: opacity 0.15s;
}
.sidebar-title:hover { opacity: 0.75; }
.sidebar-title > span {
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}
/* Firefox draws no ellipsis on background-clip: text, so fall back to a solid colour there */
@supports (-moz-appearance: none) {
  .sidebar-title { color: var(--color-purple); }
  .sidebar-title > span { background: none; color: inherit; }
}

.sidebar-project {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  margin-top: 0.3rem;
  padding-left: 0.1rem;
}
.sidebar-project-arrow {
  flex-shrink: 0;
  color: var(--color-purple);
}
.sidebar-project-name {
  min-width: 0;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
  color: var(--text-primary);
  text-decoration: none;
  transition: color 0.15s;
}
.sidebar-project-name:hover { color: var(--color-pink); }

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
