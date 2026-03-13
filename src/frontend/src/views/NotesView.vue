<template>
  <div class="page-layout">
    <!-- Left Sidebar -->
    <aside class="sidebar">
      <div class="sidebar-header">
        <span>My Notes</span>
        <button class="icon-btn" title="New note">
          <i class="pi pi-plus"></i>
        </button>
      </div>
      <div class="sidebar-search">
        <i class="pi pi-search search-icon"></i>
        <input class="search-input" placeholder="Search notes..." />
      </div>
      <nav class="sidebar-nav">
        <div class="nav-group-label">Folders</div>
        <button
          v-for="folder in folders"
          :key="folder.id"
          class="sidebar-item"
          :class="{ active: activeFolder === folder.id }"
          @click="activeFolder = folder.id"
        >
          <i :class="`pi ${folder.icon}`"></i>
          <span>{{ folder.name }}</span>
          <span class="item-count">{{ folder.count }}</span>
        </button>
        <div class="nav-group-label" style="margin-top:1rem">Tags</div>
        <button
          v-for="tag in tags"
          :key="tag.id"
          class="sidebar-item"
          :class="{ active: activeTag === tag.id }"
          @click="activeTag = tag.id"
        >
          <span class="tag-dot" :style="{ background: tag.color }"></span>
          <span>{{ tag.label }}</span>
        </button>
      </nav>
    </aside>

    <!-- Main Content -->
    <div class="main-body">
      <div class="content-header">
        <h1 class="content-title">{{ currentFolderName }}</h1>
        <button class="primary-btn">
          <i class="pi pi-plus"></i> New Note
        </button>
      </div>
      <div class="notes-grid">
        <div
          v-for="note in notes"
          :key="note.id"
          class="note-card"
        >
          <div class="note-card-header">
            <span class="note-title">{{ note.title }}</span>
            <span class="note-date">{{ note.date }}</span>
          </div>
          <p class="note-preview">{{ note.preview }}</p>
          <div class="note-tags">
            <span
              v-for="tag in note.tags"
              :key="tag"
              class="tag-chip"
            >{{ tag }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const activeFolder = ref('all')
const activeTag = ref(null)

const folders = [
  { id: 'all', name: 'All Notes', icon: 'pi-inbox', count: 12 },
  { id: 'personal', name: 'Personal', icon: 'pi-user', count: 4 },
  { id: 'work', name: 'Work', icon: 'pi-briefcase', count: 6 },
  { id: 'archive', name: 'Archive', icon: 'pi-box', count: 2 },
]

const tags = [
  { id: 'ideas', label: 'Ideas', color: '#a855f7' },
  { id: 'research', label: 'Research', color: '#ec4899' },
  { id: 'todo', label: 'To Do', color: '#8b5cf6' },
]

const notes = [
  { id: 1, title: 'Project Kickoff', date: 'Mar 12', preview: 'Initial planning and requirements for the new product launch...', tags: ['work', 'ideas'] },
  { id: 2, title: 'Reading List', date: 'Mar 10', preview: 'Books and articles to read this quarter. Focus on distributed systems...', tags: ['personal', 'research'] },
  { id: 3, title: 'Architecture Notes', date: 'Mar 8', preview: 'Thoughts on the new microservices approach and event sourcing patterns...', tags: ['work'] },
  { id: 4, title: 'Weekly Review', date: 'Mar 6', preview: 'Summary of the week: completed API integration, started on the frontend...', tags: ['personal'] },
  { id: 5, title: 'API Design', date: 'Mar 5', preview: 'REST vs GraphQL considerations. Authentication patterns using JWT...', tags: ['work', 'research'] },
  { id: 6, title: 'Ideas Dump', date: 'Mar 3', preview: 'Random ideas and shower thoughts. New feature concepts for Q2...', tags: ['ideas'] },
]

const currentFolderName = ref('All Notes')
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
.sidebar-item.active i { color: var(--color-purple); }

.item-count {
  margin-left: auto;
  font-size: 0.75rem;
  color: var(--text-dim);
  background: var(--bg-badge);
  padding: 0.1rem 0.45rem;
  border-radius: 999px;
}

.tag-dot {
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
</style>
