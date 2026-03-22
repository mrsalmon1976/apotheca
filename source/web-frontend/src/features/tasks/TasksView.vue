<template>
  <div class="page-layout">
    <!-- Mobile backdrop -->
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <ProjectSidebar :open="sidebarOpen" />

    <NewTaskDialog
      :visible="showNewTaskDialog"
      :project-id="projectId"
      :task="selectedTask"
      @close="showNewTaskDialog = false; selectedTask = null"
      @saved="onTaskSaved"
    />

    <!-- Main Content -->
    <div class="main-body">
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">{{ currentViewName }}</h1>
        </div>
        <button class="primary-btn" @click="showNewTaskDialog = true">
          <i class="pi pi-plus"></i> New Task
        </button>
      </div>

      <div v-if="loading" class="loading-state">
        <i class="pi pi-spin pi-spinner"></i> Loading tasks...
      </div>

      <div v-else-if="error" class="error-state">
        <i class="pi pi-lock error-state-icon"></i>
        <p class="error-state-title">{{ error.title }}</p>
        <p class="error-state-message">{{ error.message }}</p>
      </div>

      <div v-else-if="tasks.length === 0" class="empty-state">
        <i class="pi pi-sun empty-state-icon"></i>
        <p class="empty-state-title">You're all caught up!</p>
        <p class="empty-state-message">There are no tasks open for you on this project. Have a great day!</p>
      </div>

      <div v-else class="task-list">
        <div
          v-for="task in tasks"
          :key="task.id"
          class="task-item"
        >
          <div class="task-check">
            <i class="pi pi-circle"></i>
          </div>
          <div class="task-info">
            <span v-if="task.dueAt" class="task-due" :class="{ overdue: isOverdue(task.dueAt) }">
              <i class="pi pi-calendar"></i> {{ formatDate(task.dueAt) }}
            </span>
            <span class="task-title" @click="openTask(task)">{{ task.title }}</span>
          </div>
          <div class="task-meta">
            <span v-if="task.priority !== 'NONE'" class="priority-badge" :class="task.priority.toLowerCase()">
              {{ task.priority.toLowerCase() }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import NewTaskDialog from './NewTaskDialog.vue'
import { useProjectTasks } from '../../composables/useProjectTasks'

const route = useRoute()
const { tasks, loading, error, loadTasks } = useProjectTasks()

const showNewTaskDialog = ref(false)
const selectedTask = ref(null)

function openTask(task) {
  selectedTask.value = task
  showNewTaskDialog.value = true
}

function onTaskSaved() {
  loadTasks(projectId.value, activeFilter.value)
}

const projectId = computed(() => route.params.id)
const activeFilter = computed(() => route.params.filter ?? 'all')
const sidebarOpen = ref(window.innerWidth >= 768)

const views = [
  { id: 'today',    name: 'Today' },
  { id: 'upcoming', name: 'Upcoming' },
  { id: 'all',      name: 'All Tasks' },
]

const currentViewName = computed(() => views.find(v => v.id === activeFilter.value)?.name ?? 'Tasks')

function formatDate(value) {
  if (!value) return ''
  return new Date(value).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

function isOverdue(value) {
  if (!value) return false
  const due = new Date(value)
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return due < today
}

watch([projectId, activeFilter], ([pid, filter]) => {
  loadTasks(pid, filter)
}, { immediate: true })
</script>

<style scoped>
.page-layout {
  display: flex;
  flex: 1;
  overflow: hidden;
  height: calc(100vh - 60px);
}

/* ── Main Content ── */
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

.loading-state {
  color: var(--text-muted);
  font-size: 0.9rem;
  padding: 2rem 0;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
  gap: 0.75rem;
}

.error-state-icon {
  font-size: 3rem;
  color: var(--color-pink);
  margin-bottom: 0.5rem;
}

.error-state-title {
  font-size: 1.2rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.error-state-message {
  font-size: 0.9rem;
  color: var(--text-muted);
  margin: 0;
  max-width: 360px;
  line-height: 1.6;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  text-align: center;
  gap: 0.75rem;
}

.empty-state-icon {
  font-size: 3rem;
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  margin-bottom: 0.5rem;
}

.empty-state-title {
  font-size: 1.2rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.empty-state-message {
  font-size: 0.9rem;
  color: var(--text-muted);
  margin: 0;
  max-width: 360px;
  line-height: 1.6;
}

.task-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.task-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 0.75rem 1rem;
  transition: all 0.2s;
}
.task-item:hover {
  border-color: var(--color-purple);
  box-shadow: 0 0 12px var(--glow-purple);
}
.task-item.completed { opacity: 0.45; }
.task-item.completed .task-title { text-decoration: line-through; }

.task-check {
  background: transparent;
  border: none;
  cursor: pointer;
  font-size: 1.1rem;
  padding: 0;
  color: var(--color-purple);
  transition: color 0.2s;
  flex-shrink: 0;
}
.task-check:hover { color: var(--color-pink); }

.task-info {
  flex: 1;
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;
}

.task-title {
  font-size: 0.9rem;
  font-weight: 400;
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  cursor: pointer;
  transition: color 0.2s;
}
.task-title:hover { color: var(--color-purple-light); }

.task-due {
  font-size: 0.75rem;
  color: var(--text-dim);
  display: flex;
  align-items: center;
  gap: 0.3rem;
  white-space: nowrap;
  flex-shrink: 0;
  padding-right: 0.75rem;
  border-right: 1px solid var(--border-color);
}

.task-due.overdue {
  color: var(--color-pink);
  border-right-color: rgba(236, 72, 153, 0.25);
}

.task-meta {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.priority-badge {
  font-size: 0.7rem;
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  font-weight: 600;
  text-transform: capitalize;
}
.priority-badge.low    { background: rgba(139, 92, 246, 0.1);  color: #8b5cf6; border: 1px solid rgba(139, 92, 246, 0.25); }
.priority-badge.medium { background: rgba(168, 85, 247, 0.15); color: #a855f7; border: 1px solid rgba(168, 85, 247, 0.3); }
.priority-badge.high   { background: rgba(236, 72, 153, 0.15); color: #ec4899; border: 1px solid rgba(236, 72, 153, 0.3); }
.priority-badge.urgent { background: rgba(239, 68,  68, 0.15); color: #f87171; border: 1px solid rgba(239, 68, 68, 0.35); }

.task-project {
  font-size: 0.75rem;
  color: var(--text-dim);
}

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
</style>
