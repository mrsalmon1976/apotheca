<template>
  <div class="page-layout">
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <ProjectSidebar :open="sidebarOpen" />

    <div class="main-body">
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">Overview</h1>
        </div>
      </div>

      <!-- Stats -->
      <div class="stats-grid">
        <div v-for="stat in stats" :key="stat.label" class="stat-card">
          <div class="stat-icon"><i :class="`pi ${stat.icon}`"></i></div>
          <div class="stat-body">
            <span class="stat-value">{{ stat.value }}</span>
            <span class="stat-label">{{ stat.label }}</span>
          </div>
        </div>
      </div>

      <!-- Quick links -->
      <div class="section-label">Quick Links</div>
      <div class="quick-links">
        <button v-for="link in quickLinks" :key="link.label" class="quick-link-card" @click="router.push(link.to)">
          <i :class="`pi ${link.icon}`"></i>
          <span>{{ link.label }}</span>
        </button>
      </div>

      <!-- Content grid -->
      <div class="content-grid">

        <!-- Recent Notes -->
        <section class="dash-section">
          <div class="section-header">
            <h2 class="section-title">
              <i class="pi pi-file-edit"></i> Recent Notes
            </h2>
            <button class="link-btn" @click="router.push(`/project/${projectId}/notes`)">View all</button>
          </div>
          <div class="note-list">
            <div class="note-row" v-for="note in recentNotes" :key="note.id">
              <div class="note-row-body">
                <span class="note-row-title">{{ note.title }}</span>
                <span class="note-row-preview">{{ note.preview }}</span>
              </div>
              <span class="note-row-date">{{ note.date }}</span>
            </div>
          </div>
        </section>

        <!-- Open Tasks -->
        <section class="dash-section">
          <div class="section-header">
            <h2 class="section-title">
              <i class="pi pi-check-square"></i> Open Tasks
            </h2>
            <button class="link-btn" @click="router.push(`/project/${projectId}/tasks/all`)">View all</button>
          </div>
          <div v-if="tasksLoading" class="task-list-empty">Loading…</div>
          <div v-else-if="tasks.length === 0" class="task-list-empty">No open tasks.</div>
          <div v-else class="task-list">
            <div
              class="task-row"
              v-for="task in tasks"
              :key="task.id"
              @click="router.push(`/project/${projectId}/tasks/all`)"
            >
              <span class="task-priority-dot" :style="{ background: priorityColor(task.priority) }"></span>
              <span class="task-row-title">{{ task.title }}</span>
              <span v-if="task.assignedToDisplayName" class="task-assignee">{{ task.assignedToDisplayName }}</span>
              <span class="task-due" :class="{ overdue: isOverdue(task.dueAt) }">{{ formatDueDate(task.dueAt) }}</span>
            </div>
          </div>
        </section>

      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useAuth } from '../../composables/useAuth'
import ProjectSidebar from '../../components/ProjectSidebar.vue'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const { user } = useAuth()
const projectId = computed(() => route.params.id)
const sidebarOpen = ref(window.innerWidth >= 768)

const openTaskCount = ref('—')
const noteCount     = ref('—')
const documentCount = ref('—')

const stats = computed(() => [
  { label: 'Your open tasks', value: openTaskCount.value, icon: 'pi-check-square' },
  { label: 'Notes',      value: noteCount.value,     icon: 'pi-file-edit' },
  { label: 'Documents',  value: documentCount.value, icon: 'pi-folder-open' },
])

async function loadOverview() {
  if (!user.value) return
  try {
    const token    = await user.value.getIdToken()
    const response = await fetch(`${API_URL}/projects/${projectId.value}/overview`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    if (response.ok) {
      const data      = await response.json()
      openTaskCount.value = data.openTaskCount
      noteCount.value     = data.noteCount
      documentCount.value = data.documentCount
    } else if (response.status === 403) {
      toast.add({ severity: 'error', summary: 'Access denied', detail: 'You do not have permission to view this project.', life: 10000 })
    } else {
      toast.add({ severity: 'error', summary: 'Failed to load overview', detail: `Server error (${response.status})`, life: 10000 })
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Failed to load overview', detail: 'Could not connect to the server.', life: 10000 })
  }
}

const tasks       = ref([])
const tasksLoading = ref(false)

async function loadTasks() {
  if (!user.value) return
  tasksLoading.value = true
  try {
    const token    = await user.value.getIdToken()
    const response = await fetch(`${API_URL}/projects/${projectId.value}/tasks?limit=25`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    if (response.ok) {
      tasks.value = await response.json()
    } else {
      toast.add({ severity: 'error', summary: 'Failed to load tasks', detail: `Server error (${response.status})`, life: 10000 })
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Failed to load tasks', detail: 'Could not connect to the server.', life: 10000 })
  } finally {
    tasksLoading.value = false
  }
}

onMounted(() => {
  loadOverview()
  loadTasks()
})

const quickLinks = computed(() => [
  { label: 'Notes',      icon: 'pi-file-edit',     to: `/project/${projectId.value}/notes` },
  { label: 'Documents',  icon: 'pi-folder-open',   to: `/project/${projectId.value}/documents` },
  { label: 'All Tasks',  icon: 'pi-list',           to: `/project/${projectId.value}/tasks/all` },
  { label: 'Kanban',     icon: 'pi-objects-column', to: `/project/${projectId.value}/kanban` },
  { label: 'Backlog',    icon: 'pi-inbox',          to: `/project/${projectId.value}/backlog` },
  { label: 'Reports',    icon: 'pi-chart-bar',      to: `/project/${projectId.value}/reports` },
])

const recentNotes = [
  { id: 1, title: 'Project Kickoff', preview: 'Initial planning and requirements...', date: 'Mar 12' },
  { id: 2, title: 'Architecture Notes', preview: 'Thoughts on microservices approach...', date: 'Mar 8' },
  { id: 3, title: 'API Design', preview: 'REST vs GraphQL considerations...', date: 'Mar 5' },
]

const PRIORITY_COLORS = { HIGH: '#ec4899', URGENT: '#f87171', MEDIUM: '#a855f7', LOW: '#7a7590', NONE: '#524e65' }

function priorityColor(priority) {
  return PRIORITY_COLORS[priority?.toUpperCase()] ?? '#524e65'
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
</script>

<style scoped>
.page-layout {
  display: flex;
  flex: 1;
  overflow: hidden;
  height: calc(100vh - 60px);
}

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
  margin-bottom: 1.5rem;
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

.content-title {
  font-size: 1.4rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.primary-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1.25rem;
  background: var(--gradient-brand);
  border: none;
  border-radius: 8px;
  color: white;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s, box-shadow 0.2s;
  box-shadow: 0 0 16px var(--glow-purple);
}
.primary-btn:hover { opacity: 0.9; box-shadow: 0 0 24px var(--glow-purple); }

/* ── Stats ── */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 1.25rem 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  transition: border-color 0.2s;
}
.stat-card:hover { border-color: var(--border-purple); }

.stat-icon {
  font-size: 1.4rem;
  color: var(--color-purple);
  flex-shrink: 0;
}

.stat-body {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.stat-value {
  font-size: 1.6rem;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
}

.stat-label {
  font-size: 0.75rem;
  color: var(--text-muted);
}

/* ── Quick links ── */
.section-label {
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--text-dim);
  margin-bottom: 0.75rem;
}

.quick-links {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.quick-link-card {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.6rem 1.1rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-secondary);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
}
.quick-link-card:hover {
  border-color: var(--color-purple);
  color: var(--text-primary);
  box-shadow: 0 0 10px var(--glow-purple);
}
.quick-link-card .pi {
  color: var(--color-purple);
  font-size: 0.9rem;
}

/* ── Content grid ── */
.content-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-top: 1.5rem;
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

/* ── Notes list ── */
.note-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.note-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.6rem 0.75rem;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.15s;
}
.note-row:hover { background: var(--bg-primary); }

.note-row-body {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
}

.note-row-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.note-row-preview {
  font-size: 0.78rem;
  color: var(--text-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.note-row-date {
  font-size: 0.75rem;
  color: var(--text-dim);
  white-space: nowrap;
  flex-shrink: 0;
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

.task-assignee {
  font-size: 0.72rem;
  color: var(--text-muted);
  white-space: nowrap;
  flex-shrink: 0;
}

.task-due {
  font-size: 0.75rem;
  color: var(--text-muted);
  white-space: nowrap;
}
.task-due.overdue { color: #f87171; }

/* ── Mobile ── */
.sidebar-backdrop { display: none; }

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

@media (max-width: 900px) {
  .content-grid { grid-template-columns: 1fr; }
}
</style>
