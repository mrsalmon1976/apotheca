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
      <div class="stat-card" v-for="stat in stats" :key="stat.label">
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
          <button class="link-btn" @click="$router.push('/projects')">View all</button>
        </div>
        <div class="project-list">
          <div class="project-card" v-for="project in projects" :key="project.id">
            <div class="project-card-top">
              <span class="project-name">{{ project.name }}</span>
              <span class="project-role-chip">{{ project.role }}</span>
            </div>
            <div class="project-meta">
              <span class="meta-item"><i class="pi pi-check-square"></i> {{ project.openTasks }} open tasks</span>
              <span class="meta-item"><i class="pi pi-users"></i> {{ project.members }} members</span>
            </div>
            <div class="project-bar-bg">
              <div class="project-bar-fill" :style="{ width: project.progress + '%' }"></div>
            </div>
          </div>
        </div>
      </section>

      <!-- Right column -->
      <div class="right-column">

        <!-- Recent Notes -->
        <section class="dash-section">
          <div class="section-header">
            <h2 class="section-title">
              <i class="pi pi-file-edit"></i> Recent Notes
            </h2>
            <button class="link-btn" @click="$router.push('/notes')">View all</button>
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

        <!-- Upcoming Tasks -->
        <section class="dash-section">
          <div class="section-header">
            <h2 class="section-title">
              <i class="pi pi-check-square"></i> Upcoming Tasks
            </h2>
            <button class="link-btn" @click="$router.push('/tasks')">View all</button>
          </div>
          <div class="task-list">
            <div class="task-row" v-for="task in upcomingTasks" :key="task.id">
              <span class="task-priority-dot" :style="{ background: priorityColor(task.priority) }"></span>
              <span class="task-row-title">{{ task.title }}</span>
              <span class="task-due" :class="{ overdue: task.overdue }">{{ task.due }}</span>
            </div>
          </div>
        </section>

      </div>
    </div>
    </div><!-- end main-body -->
  </div><!-- end page-layout -->
</template>

<script setup>
import { ref, computed } from 'vue'
import AccountSidebar from '../../components/AccountSidebar.vue'

const sidebarOpen = ref(window.innerWidth >= 768)

const today = computed(() => new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' }))

const stats = [
  { label: 'Projects', value: 3, icon: 'pi-folder', iconColor: '#a855f7', iconBg: 'rgba(168,85,247,0.12)' },
  { label: 'Notes', value: 12, icon: 'pi-file-edit', iconColor: '#ec4899', iconBg: 'rgba(236,72,153,0.12)' },
  { label: 'Open Tasks', value: 5, icon: 'pi-check-square', iconColor: '#c084fc', iconBg: 'rgba(192,132,252,0.12)' },
]

const projects = [
  { id: 1, name: 'My Project', role: 'Owner', openTasks: 5, members: 1, progress: 30 },
  { id: 2, name: 'Apotheca', role: 'Owner', openTasks: 12, members: 2, progress: 55 },
  { id: 3, name: 'Learning', role: 'Viewer', openTasks: 2, members: 4, progress: 80 },
]

const recentNotes = [
  { id: 1, title: 'Project Kickoff', preview: 'Initial planning and requirements...', date: 'Mar 12' },
  { id: 2, title: 'Architecture Notes', preview: 'Thoughts on microservices approach...', date: 'Mar 8' },
  { id: 3, title: 'API Design', preview: 'REST vs GraphQL considerations...', date: 'Mar 5' },
]

const upcomingTasks = [
  { id: 1, title: 'Review pull request', priority: 'high', due: 'Today', overdue: false },
  { id: 2, title: 'Write unit tests', priority: 'medium', due: 'Tomorrow', overdue: false },
  { id: 3, title: 'Update documentation', priority: 'low', due: 'Mar 15', overdue: true },
  { id: 4, title: 'Deploy to staging', priority: 'high', due: 'Mar 24', overdue: false },
]

function priorityColor(priority) {
  return { high: '#ec4899', medium: '#a855f7', low: '#7a7590' }[priority]
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

.right-column {
  display: flex;
  flex-direction: column;
  gap: 1rem;
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
  background: var(--gradient-brand);
  border-radius: 999px;
  transition: width 0.4s ease;
}

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
