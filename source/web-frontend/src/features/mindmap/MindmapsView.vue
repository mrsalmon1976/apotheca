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
          <h1 class="content-title">Mindmaps</h1>
        </div>
        <button class="primary-btn" @click="createAndOpen">
          <i class="pi pi-plus"></i> New Mindmap
        </button>
      </div>

      <table v-if="mindmaps.length" class="doc-table">
        <thead>
          <tr>
            <th class="col-name">Title</th>
            <th class="col-date">Updated</th>
            <th class="col-actions"></th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="item in mindmaps"
            :key="item.id"
            class="doc-row"
            @click="router.push(`/project/${projectId}/mindmaps/${item.id}`)"
          >
            <td class="col-name">
              <i class="pi pi-sitemap file-icon"></i>
              <span class="row-title">{{ item.root.header || 'Untitled' }}</span>
            </td>
            <td class="col-date">{{ formatDate(item.updatedAt) }}</td>
            <td class="col-actions">
              <button class="row-action-btn row-delete-btn" title="Delete mindmap" @click.stop="promptDelete(item)">
                <i class="pi pi-trash"></i>
              </button>
            </td>
          </tr>
        </tbody>
      </table>

      <div v-else class="empty-state">
        <i class="pi pi-sitemap empty-icon"></i>
        <p>No mindmaps yet.</p>
        <p class="empty-hint">Click <strong>New Mindmap</strong> to get started.</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import { useMindmaps } from '../../composables/useMindmaps'

const route = useRoute()
const router = useRouter()
const projectId = computed(() => route.params.id)
const sidebarOpen = ref(window.innerWidth >= 768)

const { mindmaps, createMindmap, deleteMindmap } = useMindmaps(projectId.value)

function createAndOpen() {
  const mindmap = createMindmap()
  router.push(`/project/${projectId.value}/mindmaps/${mindmap.id}`)
}

function promptDelete(item) {
  if (confirm(`Delete "${item.root.header || 'Untitled'}"? This cannot be undone.`)) {
    deleteMindmap(item.id)
  }
}

function formatDate(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
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
  margin-bottom: 1.25rem;
}

.content-header-left { display: flex; align-items: center; gap: 0.75rem; }

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

.content-title { font-size: 1.4rem; font-weight: 700; color: var(--text-primary); margin: 0; }

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

.empty-state { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 3rem 0; color: var(--text-dim); }
.empty-icon { font-size: 2rem; }
.empty-hint { font-size: 0.8rem; color: var(--text-dim); margin-top: 0.25rem; }

/* Table */
.doc-table {
  width: 100%;
  border-collapse: collapse;
  margin-bottom: 1.5rem;
  font-size: 0.875rem;
}

.doc-table thead th {
  text-align: left;
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--text-dim);
  font-weight: 600;
  padding: 0.4rem 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.doc-row {
  cursor: pointer;
  border-bottom: 1px solid var(--border-color);
  transition: background 0.15s;
}
.doc-row:hover { background: var(--bg-card); }

.doc-row td {
  padding: 0.6rem 0.75rem;
  color: var(--text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 0;
}

.col-name { width: 60%; }
.col-date { width: 30%; }
.col-actions { width: 10%; text-align: right; white-space: nowrap; }

.row-title {
  font-weight: 600;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.file-icon { color: var(--color-purple); margin-right: 0.6rem; }

.row-action-btn {
  background: transparent;
  border: none;
  color: var(--text-dim);
  cursor: pointer;
  font-size: 0.75rem;
  padding: 0.2rem 0.3rem;
  border-radius: 4px;
  line-height: 1;
  opacity: 0;
  transition: opacity 0.15s, color 0.15s, background 0.15s;
}
.doc-row:hover .row-action-btn { opacity: 1; }
.row-action-btn:hover { color: var(--text-secondary); background: var(--bg-active); }
.row-delete-btn:hover { color: var(--color-pink-light); background: rgba(236, 72, 153, 0.12); }

.sidebar-backdrop { display: none; }

@media (max-width: 767px) {
  .sidebar-backdrop { display: block; position: fixed; inset: 0; top: 60px; background: rgba(0, 0, 0, 0.6); z-index: 99; }
  .main-body { padding: 1rem; }
}
</style>
