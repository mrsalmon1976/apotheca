<template>
  <div class="page-layout">
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <ProjectSidebar :open="sidebarOpen" @close="sidebarOpen = false" />

    <div class="main-body">
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">Notes</h1>
        </div>
        <button class="primary-btn">
          <i class="pi pi-plus"></i> New Note
        </button>
      </div>

      <div class="notes-grid">
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
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import ProjectSidebar from '../../components/ProjectSidebar.vue'

const sidebarOpen = ref(window.innerWidth >= 768)

const notes = [
  { id: 1, title: 'Project Brief',  date: 'Mar 20', preview: 'Objectives and scope for this project...', tags: ['planning'] },
  { id: 2, title: 'Meeting Notes',  date: 'Mar 18', preview: 'Action items from the kick-off call...',   tags: ['meetings'] },
  { id: 3, title: 'Technical Spec', date: 'Mar 15', preview: 'Architecture decisions and API contracts...', tags: ['tech'] },
]
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
