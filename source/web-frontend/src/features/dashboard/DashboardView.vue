<template>
  <div class="page-layout">
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <AccountSidebar :open="sidebarOpen" @close="sidebarOpen = false" />

    <div class="main-body">
    <div class="content-header">
      <div class="content-header-left">
        <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
          <i class="pi pi-bars"></i>
        </button>
        <div class="dashboard-header">
          <h1 class="dashboard-title">Dashboard</h1>
          <span class="dashboard-date">{{ today }}</span>
        </div>
      </div>
    </div>

    <!-- Quick stats -->
    <div class="stat-row">
      <div
        class="stat-card"
        v-for="stat in stats"
        :key="stat.label"
        :class="{ clickable: !!stat.link }"
        @click="stat.link && $router.push(stat.link)"
      >
        <div class="stat-icon" :style="{ background: stat.iconBg }">
          <i :class="`pi ${stat.icon}`" :style="{ color: stat.iconColor }"></i>
        </div>
        <div class="stat-body">
          <span class="stat-value">{{ stat.value }}</span>
          <span class="stat-label">{{ stat.label }}</span>
        </div>
      </div>
    </div>

    <!-- Main grid -->
    <div class="dashboard-grid">

      <!-- Recent Projects -->
      <section class="dash-section">
        <div class="section-header">
          <h2 class="section-title">
            <i class="pi pi-folder"></i> Projects
          </h2>
          <button class="link-btn" :disabled="!currentWorkspace" @click="showCreateProjectDialog = true">+ New Project</button>
        </div>
        <div v-if="projectsLoading" class="project-list-empty">Loading…</div>
        <div v-else-if="projects.length === 0" class="project-list-empty">No projects yet.</div>
        <div v-else class="project-list">
          <div
            class="project-card"
            v-for="project in projects"
            :key="project.id"
            @click="$router.push(`/workspace/${project.workspaceId}/project/${project.id}`)"
          >
            <div class="project-card-top">
              <span class="project-name">{{ project.name }}</span>
              <span class="project-role-chip">{{ formatRole(project.projectRole) }}</span>
            </div>
            <div class="project-meta">
              <span class="meta-item"><i class="pi pi-check-square"></i> {{ project.openTaskCount }} open tasks</span>
              <span class="meta-item"><i class="pi pi-users"></i> {{ project.memberCount }} members</span>
            </div>
            <div class="project-bar-bg">
              <div class="project-bar-fill"></div>
            </div>
          </div>
        </div>
      </section>

      <!-- Upcoming Tasks -->
      <section class="dash-section">
        <div class="section-header">
          <h2 class="section-title">
            <i class="pi pi-check-square"></i> Upcoming Tasks
          </h2>
          <button class="link-btn" @click="$router.push('/tasks/upcoming')">View all</button>
        </div>
        <div v-if="tasksLoading" class="task-list-empty">Loading…</div>
        <div v-else-if="upcomingTasks.length === 0" class="task-list-empty">No upcoming tasks.</div>
        <div v-else class="task-list">
          <div
            class="task-row"
            v-for="task in upcomingTasks"
            :key="task.id"
            @click="currentWorkspace && $router.push(`/workspace/${currentWorkspace.id}/project/${task.projectId}/tasks/upcoming`)"
          >
            <span class="task-priority-dot" :style="{ background: priorityColor(task.priority) }"></span>
            <span class="task-row-title">{{ task.title }}</span>
            <span class="task-project-chip">{{ task.projectName }}</span>
            <span class="task-due" :class="{ overdue: isOverdue(task.dueAt) }">{{ formatDueDate(task.dueAt) }}</span>
          </div>
        </div>
      </section>
    </div>
    </div><!-- end main-body -->

    <CreateProjectDialog
      :visible="showCreateProjectDialog"
      :workspace-id="currentWorkspace?.id"
      @close="showCreateProjectDialog = false"
      @saved="loadProjects(currentWorkspace.id)"
    />
  </div><!-- end page-layout -->
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useToast } from 'primevue/usetoast'
import AccountSidebar from '../../components/AccountSidebar.vue'
import CreateProjectDialog from '../projects/CreateProjectDialog.vue'
import { useProjects } from '../../composables/useProjects'
import { useAuth } from '../../composables/useAuth'
import { useWorkspaces } from '../../composables/useWorkspaces'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

const sidebarOpen = ref(window.innerWidth >= 768)
const toast = useToast()
const { user } = useAuth()
const { currentWorkspace } = useWorkspaces()

const today = computed(() => new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' }))

const { projects, loading: projectsLoading, loadProjects } = useProjects()
const showCreateProjectDialog = ref(false)

const upcomingTasks = ref([])
const tasksLoading  = ref(false)

async function loadUpcomingTasks() {
  if (!user.value) return
  tasksLoading.value = true
  try {
    const token    = await user.value.getIdToken()
    const response = await fetch(`${API_URL}/users/me/tasks?filter=overdue-upcoming`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    if (response.ok) {
      upcomingTasks.value = await response.json()
    } else {
      toast.add({ severity: 'error', summary: 'Failed to load tasks', detail: `Server error (${response.status})`, life: 10000 })
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Failed to load tasks', detail: 'Could not connect to the server.', life: 10000 })
  } finally {
    tasksLoading.value = false
  }
}

watch(
  () => currentWorkspace.value?.id,
  (workspaceId) => { if (workspaceId) loadProjects(workspaceId) },
  { immediate: true }
)

onMounted(() => {
  loadUpcomingTasks()
})

function formatRole(role) {
  if (!role) return ''
  return role.charAt(0).toUpperCase() + role.slice(1).toLowerCase()
}

function formatDueDate(dueAt) {
  if (!dueAt) return ''
  const due          = new Date(dueAt)
  const todayStart   = new Date()
  todayStart.setHours(0, 0, 0, 0)
  const tomorrowStart = new Date(todayStart)
  tomorrowStart.setDate(tomorrowStart.getDate() + 1)
  const dueStart = new Date(due)
  dueStart.setHours(0, 0, 0, 0)
  if (dueStart.getTime() === todayStart.getTime())    return 'Today'
  if (dueStart.getTime() === tomorrowStart.getTime()) return 'Tomorrow'
  return due.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

function isOverdue(dueAt) {
  if (!dueAt) return false
  const todayStart = new Date()
  todayStart.setHours(0, 0, 0, 0)
  return new Date(dueAt) < todayStart
}

const stats = [
  { label: 'Projects', value: 3, icon: 'pi-folder', iconColor: '#a855f7', iconBg: 'rgba(168,85,247,0.12)' },
  { label: 'Notes', value: 12, icon: 'pi-file-edit', iconColor: '#ec4899', iconBg: 'rgba(236,72,153,0.12)' },
  { label: 'Open Tasks', value: 5, icon: 'pi-check-square', iconColor: '#c084fc', iconBg: 'rgba(192,132,252,0.12)', link: '/tasks/all' },
]

const PRIORITY_COLORS = { HIGH: '#ec4899', URGENT: '#f87171', MEDIUM: '#a855f7', LOW: '#7a7590', NONE: '#524e65' }

function priorityColor(priority) {
  return PRIORITY_COLORS[priority?.toUpperCase()] ?? '#524e65'
}
</script>

<style scoped>
/* ── Page layout (sidebar + body) ── */
.page-layout {
  display: flex;
  flex: 1;
  overflow: hidden;
  height: calc(100vh - 60px);
}

.sidebar-backdrop { display: none; }

.main-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem 2rem;
  background: var(--bg-primary);
}

.content-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.75rem;
}

.content-header-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.hamburger-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 1.1rem;
  padding: 0.25rem;
  border-radius: 6px;
  transition: color 0.2s;
  display: flex;
  align-items: center;
}
.hamburger-btn:hover { color: var(--color-purple); }

/* ── Header ── */
.dashboard-header {
  display: flex;
  align-items: baseline;
  gap: 1rem;
}

.dashboard-title {
  font-size: 1.4rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.dashboard-date {
  font-size: 0.85rem;
  color: var(--text-muted);
}

@media (max-width: 767px) {
  .sidebar-backdrop {
    display: block;
    position: fixed;
    inset: 0;
    top: 60px;
    background: rgba(0, 0, 0, 0.6);
    z-index: 99;
  }
  .main-body { padding: 1rem; }
}

/* ── Stat row ── */
.stat-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  margin-bottom: 1.75rem;
}

.stat-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 1.1rem 1.25rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  transition: border-color 0.2s;
}
.stat-card:hover { border-color: var(--border-purple); }
.stat-card.clickable { cursor: pointer; }

.stat-icon {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.1rem;
  flex-shrink: 0;
}

.stat-body {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
}

.stat-label {
  font-size: 0.78rem;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.06em;
}

/* ── Main grid ── */
.dashboard-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  align-items: start;
}

/* ── Sections ── */
.dash-section {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 1.25rem;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.section-title {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
}
.section-title .pi { color: var(--color-purple); font-size: 0.85rem; }

.link-btn {
  background: none;
  border: none;
  color: var(--color-purple);
  font-size: 0.8rem;
  cursor: pointer;
  padding: 0;
  transition: color 0.2s;
}
.link-btn:hover { color: var(--color-pink); }

/* ── Projects ── */
.project-list-empty {
  font-size: 0.85rem;
  color: var(--text-muted);
  padding: 0.5rem 0;
}

.project-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.project-card {
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 0.9rem 1rem;
  cursor: pointer;
  transition: border-color 0.2s;
}
.project-card:hover { border-color: var(--border-purple); }

.project-card-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.project-name {
  font-weight: 600;
  font-size: 0.9rem;
  color: var(--text-primary);
}

.project-role-chip {
  font-size: 0.7rem;
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  background: rgba(168,85,247,0.12);
  color: var(--color-purple);
  border: 1px solid var(--border-purple);
  font-weight: 500;
}

.project-meta {
  display: flex;
  gap: 1rem;
  margin-bottom: 0.65rem;
}

.meta-item {
  font-size: 0.78rem;
  color: var(--text-muted);
  display: flex;
  align-items: center;
  gap: 0.3rem;
}
.meta-item .pi { font-size: 0.75rem; }

.project-bar-bg {
  height: 3px;
  background: var(--border-color);
  border-radius: 999px;
  overflow: hidden;
}

.project-bar-fill {
  height: 100%;
  width: 100%;
  background: var(--gradient-brand);
  border-radius: 999px;
}

/* ── Tasks list ── */
.task-list-empty {
  font-size: 0.85rem;
  color: var(--text-muted);
  padding: 0.5rem 0;
}

.task-list {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.task-row {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.55rem 0.75rem;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.15s;
}
.task-row:hover { background: var(--bg-primary); }

.task-priority-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  flex-shrink: 0;
}

.task-row-title {
  font-size: 0.875rem;
  color: var(--text-primary);
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.task-project-chip {
  font-size: 0.68rem;
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
  background: rgba(168,85,247,0.1);
  color: var(--color-purple);
  white-space: nowrap;
  flex-shrink: 0;
}

.task-due {
  font-size: 0.75rem;
  color: var(--text-muted);
  white-space: nowrap;
}
.task-due.overdue { color: #f87171; }

/* ── Responsive ── */
@media (max-width: 900px) {
  .dashboard-grid { grid-template-columns: 1fr; }
  .stat-row { grid-template-columns: 1fr; }
}
</style>
