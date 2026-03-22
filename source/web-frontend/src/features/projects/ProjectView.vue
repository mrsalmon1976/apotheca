<template>
  <div class="page-layout">
    <!-- Mobile backdrop -->
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <!-- Left Sidebar -->
    <aside class="sidebar" :class="{ open: sidebarOpen }">
      <div class="sidebar-header">
        <span>Project</span>
        <button class="icon-btn" title="Close menu" @click="sidebarOpen = false">
          <i class="pi pi-times"></i>
        </button>
      </div>

      <nav class="sidebar-nav">
        <div class="nav-group-label">Views</div>
        <button
          v-for="section in sections"
          :key="section.id"
          class="sidebar-item"
          :class="{ active: activeSection === section.id }"
          @click="activeSection = section.id; closeSidebarOnMobile()"
        >
          <i :class="`pi ${section.icon}`"></i>
          <span>{{ section.label }}</span>
        </button>

        <div class="nav-group-label" style="margin-top:1rem">Members</div>
        <button
          v-for="member in members"
          :key="member.id"
          class="sidebar-item"
        >
          <i class="pi pi-user"></i>
          <span>{{ member.name }}</span>
          <span class="role-chip">{{ member.role }}</span>
        </button>
      </nav>
    </aside>

    <!-- Main Content -->
    <div class="main-body">
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">{{ currentSectionLabel }}</h1>
        </div>
        <button class="primary-btn">
          <i class="pi pi-plus"></i> New
        </button>
      </div>

      <!-- Notes section -->
      <div v-if="activeSection === 'notes'" class="notes-grid">
        <div v-for="note in notes" :key="note.id" class="note-card">
          <div class="note-card-header">
            <span class="note-title">{{ note.title }}</span>
            <span class="note-date">{{ note.date }}</span>
          </div>
          <p class="note-preview">{{ note.preview }}</p>
          <div class="note-tags">
            <span v-for="tag in note.tags" :key="tag" class="tag-chip">{{ tag }}</span>
          </div>
        </div>
      </div>

      <!-- Tasks section -->
      <div v-else-if="activeSection === 'tasks'" class="task-list">
        <div v-for="task in tasks" :key="task.id" class="task-item">
          <i class="pi pi-check-circle task-check" :class="{ done: task.done }"></i>
          <span class="task-label" :class="{ done: task.done }">{{ task.label }}</span>
          <span class="task-priority" :class="task.priority">{{ task.priority }}</span>
        </div>
      </div>

      <!-- Activity section -->
      <div v-else-if="activeSection === 'activity'" class="activity-list">
        <div v-for="event in activity" :key="event.id" class="activity-item">
          <div class="activity-dot"></div>
          <div class="activity-body">
            <span class="activity-text">{{ event.text }}</span>
            <span class="activity-time">{{ event.time }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const projectId = computed(() => route.params.id)

const sidebarOpen = ref(window.innerWidth >= 768)
const activeSection = ref('notes')

function closeSidebarOnMobile() {
  if (window.innerWidth < 768) sidebarOpen.value = false
}

const sections = [
  { id: 'notes',    label: 'Notes',    icon: 'pi-file-edit' },
  { id: 'tasks',    label: 'Tasks',    icon: 'pi-check-square' },
  { id: 'activity', label: 'Activity', icon: 'pi-history' },
]

const currentSectionLabel = computed(
  () => sections.find(s => s.id === activeSection.value)?.label ?? ''
)

const members = [
  { id: 1, name: 'You', role: 'Owner' },
]

const notes = [
  { id: 1, title: 'Project Brief',      date: 'Mar 20', preview: 'Objectives and scope for this project...', tags: ['planning'] },
  { id: 2, title: 'Meeting Notes',      date: 'Mar 18', preview: 'Action items from the kick-off call...', tags: ['meetings'] },
  { id: 3, title: 'Technical Spec',     date: 'Mar 15', preview: 'Architecture decisions and API contracts...', tags: ['tech'] },
]

const tasks = [
  { id: 1, label: 'Define project scope',   done: true,  priority: 'high' },
  { id: 2, label: 'Set up repository',      done: true,  priority: 'high' },
  { id: 3, label: 'Write technical spec',   done: false, priority: 'high' },
  { id: 4, label: 'Design data model',      done: false, priority: 'medium' },
  { id: 5, label: 'Implement auth flow',    done: false, priority: 'medium' },
  { id: 6, label: 'Write unit tests',       done: false, priority: 'low' },
]

const activity = [
  { id: 1, text: 'Project created',         time: 'Mar 20' },
  { id: 2, text: 'Technical spec added',    time: 'Mar 18' },
  { id: 3, text: 'Task "Define scope" completed', time: 'Mar 17' },
]
</script>

<style scoped>
.page-layout {
  display: flex;
  flex: 1;
  overflow: hidden;
  height: calc(100vh - 60px);
}

/* ── Sidebar ── */
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
.sidebar-item.active i { color: var(--color-purple); }

.role-chip {
  margin-left: auto;
  font-size: 0.7rem;
  color: var(--color-purple);
  background: var(--bg-badge);
  border: 1px solid var(--border-purple);
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
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

/* ── Notes grid ── */
.notes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1rem;
}

.note-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 1rem 1.25rem;
  cursor: pointer;
  transition: all 0.2s;
}
.note-card:hover {
  border-color: var(--color-purple);
  box-shadow: 0 0 16px var(--glow-purple);
  transform: translateY(-2px);
}

.note-card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 0.5rem;
}

.note-title {
  font-weight: 600;
  font-size: 0.95rem;
  color: var(--text-primary);
}

.note-date {
  font-size: 0.75rem;
  color: var(--text-dim);
  white-space: nowrap;
}

.note-preview {
  font-size: 0.8rem;
  color: var(--text-secondary);
  line-height: 1.5;
  margin: 0 0 0.75rem;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.note-tags {
  display: flex;
  gap: 0.4rem;
  flex-wrap: wrap;
}

.tag-chip {
  font-size: 0.7rem;
  padding: 0.15rem 0.6rem;
  border-radius: 999px;
  background: var(--bg-badge);
  color: var(--color-purple);
  border: 1px solid var(--border-purple);
  font-weight: 500;
}

/* ── Tasks ── */
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
}

.task-check {
  font-size: 1.1rem;
  color: var(--text-dim);
}
.task-check.done { color: var(--color-purple); }

.task-label {
  flex: 1;
  font-size: 0.875rem;
  color: var(--text-primary);
}
.task-label.done {
  text-decoration: line-through;
  color: var(--text-dim);
}

.task-priority {
  font-size: 0.7rem;
  font-weight: 600;
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  text-transform: capitalize;
}
.task-priority.high   { background: rgba(236,72,153,0.15); color: var(--color-pink); }
.task-priority.medium { background: rgba(168,85,247,0.15); color: var(--color-purple); }
.task-priority.low    { background: var(--bg-badge);       color: var(--text-muted); }

/* ── Activity ── */
.activity-list {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.activity-item {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  padding: 0.75rem 0;
  border-bottom: 1px solid var(--border-color);
}
.activity-item:last-child { border-bottom: none; }

.activity-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--color-purple);
  margin-top: 0.35rem;
  flex-shrink: 0;
}

.activity-body {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.activity-text {
  font-size: 0.875rem;
  color: var(--text-primary);
}

.activity-time {
  font-size: 0.75rem;
  color: var(--text-dim);
}

/* ── Mobile ── */
.sidebar-backdrop { display: none; }

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
