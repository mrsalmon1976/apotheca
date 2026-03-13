<template>
  <div class="page-layout">
    <!-- Left Sidebar -->
    <aside class="sidebar">
      <div class="sidebar-header">
        <span>Task Lists</span>
        <button class="icon-btn" title="New list">
          <i class="pi pi-plus"></i>
        </button>
      </div>
      <div class="sidebar-search">
        <i class="pi pi-search search-icon"></i>
        <input class="search-input" placeholder="Search tasks..." />
      </div>
      <nav class="sidebar-nav">
        <div class="nav-group-label">Views</div>
        <button
          v-for="view in views"
          :key="view.id"
          class="sidebar-item"
          :class="{ active: activeView === view.id }"
          @click="activeView = view.id"
        >
          <i :class="`pi ${view.icon}`"></i>
          <span>{{ view.name }}</span>
          <span v-if="view.count" class="item-count">{{ view.count }}</span>
        </button>
        <div class="nav-group-label" style="margin-top:1rem">Projects</div>
        <button
          v-for="project in projects"
          :key="project.id"
          class="sidebar-item"
          :class="{ active: activeProject === project.id }"
          @click="activeProject = project.id"
        >
          <span class="project-dot" :style="{ background: project.color }"></span>
          <span>{{ project.name }}</span>
        </button>
      </nav>
    </aside>

    <!-- Main Content -->
    <div class="main-body">
      <div class="content-header">
        <h1 class="content-title">{{ currentViewName }}</h1>
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

const activeView = ref('today')
const activeProject = ref(null)

const views = [
  { id: 'today', name: 'Today', icon: 'pi-sun', count: 4 },
  { id: 'upcoming', name: 'Upcoming', icon: 'pi-calendar', count: 9 },
  { id: 'all', name: 'All Tasks', icon: 'pi-list', count: 15 },
  { id: 'completed', name: 'Completed', icon: 'pi-check-circle', count: null },
]

const projects = [
  { id: 'apotheca', name: 'Apotheca', color: '#a855f7' },
  { id: 'personal', name: 'Personal', color: '#ec4899' },
  { id: 'learning', name: 'Learning', color: '#8b5cf6' },
]

const tasks = ref([
  { id: 1, title: 'Design new dashboard layout', done: false, due: 'Today', priority: 'high', project: 'Apotheca' },
  { id: 2, title: 'Set up MongoDB indexes', done: true, due: 'Yesterday', priority: 'medium', project: 'Apotheca' },
  { id: 3, title: 'Write unit tests for UserRepository', done: false, due: 'Mar 15', priority: 'medium', project: 'Apotheca' },
  { id: 4, title: 'Read Clean Architecture book', done: false, due: 'Mar 20', priority: 'low', project: 'Learning' },
  { id: 5, title: 'Weekly grocery shopping', done: false, due: 'Today', priority: 'low', project: 'Personal' },
  { id: 6, title: 'Review pull request #42', done: true, due: 'Mar 11', priority: 'high', project: 'Apotheca' },
  { id: 7, title: 'Update API documentation', done: false, due: 'Mar 16', priority: 'medium', project: 'Apotheca' },
])

const currentViewName = computed(() => {
  return views.find(v => v.id === activeView.value)?.name || 'Tasks'
})
</script>

<style scoped>
.page-layout {
  display: flex;
  flex: 1;
  overflow: hidden;
  height: calc(100vh - 60px);
}

.sidebar {
  width: 240px;
  min-width: 240px;
  background: var(--bg-sidebar);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
  padding: 1rem 0;
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

.icon-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  padding: 0.25rem;
  border-radius: 4px;
  transition: color 0.2s;
}
.icon-btn:hover { color: var(--color-purple); }

.sidebar-search {
  position: relative;
  padding: 0 0.75rem 0.75rem;
}
.search-icon {
  position: absolute;
  left: 1.25rem;
  top: 50%;
  transform: translateY(-60%);
  font-size: 0.75rem;
  color: var(--text-muted);
}
.search-input {
  width: 100%;
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 0.4rem 0.75rem 0.4rem 2rem;
  color: var(--text-primary);
  font-size: 0.8rem;
  outline: none;
  box-sizing: border-box;
  transition: border-color 0.2s;
}
.search-input:focus { border-color: var(--color-purple); }

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
}
.sidebar-item:hover { background: var(--bg-hover); color: var(--text-primary); }
.sidebar-item.active {
  background: var(--bg-active);
  color: var(--color-pink);
}

.item-count {
  margin-left: auto;
  font-size: 0.75rem;
  color: var(--text-dim);
  background: var(--bg-badge);
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
}

.project-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
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
.priority-badge.high { background: rgba(236, 72, 153, 0.15); color: #ec4899; border: 1px solid rgba(236, 72, 153, 0.3); }
.priority-badge.medium { background: rgba(168, 85, 247, 0.15); color: #a855f7; border: 1px solid rgba(168, 85, 247, 0.3); }
.priority-badge.low { background: rgba(139, 92, 246, 0.1); color: #8b5cf6; border: 1px solid rgba(139, 92, 246, 0.25); }

.task-project {
  font-size: 0.75rem;
  color: var(--text-dim);
}
</style>
