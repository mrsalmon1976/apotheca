<template>
  <div class="page-layout">
    <!-- Mobile backdrop -->
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <ProjectSidebar :open="sidebarOpen" />

    <!-- Main Content -->
    <div class="main-body">
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">{{ currentViewName }}</h1>
        </div>
        <button class="primary-btn">
          <i class="pi pi-plus"></i> New Task
        </button>
      </div>

      <div class="task-list">
        <div
          v-for="task in tasks"
          :key="task.id"
          class="task-item"
          :class="{ completed: task.done }"
        >
          <button class="task-check" @click="task.done = !task.done">
            <i :class="task.done ? 'pi pi-check-circle' : 'pi pi-circle'"></i>
          </button>
          <div class="task-info">
            <span class="task-title">{{ task.title }}</span>
            <span v-if="task.due" class="task-due">
              <i class="pi pi-calendar"></i> {{ task.due }}
            </span>
          </div>
          <div class="task-meta">
            <span class="priority-badge" :class="task.priority">{{ task.priority }}</span>
            <span class="task-project">{{ task.project }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'

const route = useRoute()

const activeFilter = computed(() => route.params.filter ?? 'all')
const sidebarOpen = ref(window.innerWidth >= 768)

const views = [
  { id: 'today',     name: 'Today' },
  { id: 'upcoming',  name: 'Upcoming' },
  { id: 'all',       name: 'All Tasks' },
  { id: 'completed', name: 'Completed' },
]

const tasks = ref([
  { id: 1, title: 'Design new dashboard layout',          done: false, due: 'Today',     priority: 'high',   project: 'Apotheca' },
  { id: 2, title: 'Set up MongoDB indexes',               done: true,  due: 'Yesterday', priority: 'medium', project: 'Apotheca' },
  { id: 3, title: 'Write unit tests for UserRepository',  done: false, due: 'Mar 15',    priority: 'medium', project: 'Apotheca' },
  { id: 4, title: 'Read Clean Architecture book',         done: false, due: 'Mar 20',    priority: 'low',    project: 'Learning' },
  { id: 5, title: 'Weekly grocery shopping',              done: false, due: 'Today',     priority: 'low',    project: 'Personal' },
  { id: 6, title: 'Review pull request #42',              done: true,  due: 'Mar 11',    priority: 'high',   project: 'Apotheca' },
  { id: 7, title: 'Update API documentation',             done: false, due: 'Mar 16',    priority: 'medium', project: 'Apotheca' },
])

const currentViewName = computed(() => views.find(v => v.id === activeFilter.value)?.name ?? 'Tasks')
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
  flex-direction: column;
  gap: 0.2rem;
}

.task-title {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--text-primary);
}

.task-due {
  font-size: 0.75rem;
  color: var(--text-dim);
  display: flex;
  align-items: center;
  gap: 0.3rem;
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
.priority-badge.high   { background: rgba(236, 72, 153, 0.15); color: #ec4899; border: 1px solid rgba(236, 72, 153, 0.3); }
.priority-badge.medium { background: rgba(168, 85, 247, 0.15); color: #a855f7; border: 1px solid rgba(168, 85, 247, 0.3); }
.priority-badge.low    { background: rgba(139, 92, 246, 0.1);  color: #8b5cf6; border: 1px solid rgba(139, 92, 246, 0.25); }

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
