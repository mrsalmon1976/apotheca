<template>
  <div class="page-layout">
    <NewFolderDialog
      :visible="showNewFolderDialog"
      :project-id="projectId"
      :parent-id="currentFolderId"
      @close="showNewFolderDialog = false"
      @saved="onFolderSaved"
    />

    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <ProjectSidebar :open="sidebarOpen" />

    <div class="main-body">
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">Notes</h1>
        </div>
        <div class="header-actions">
          <button class="secondary-btn" @click="showNewFolderDialog = true">
            <i class="pi pi-folder-plus"></i> New Folder
          </button>
          <button class="primary-btn" :disabled="creatingNote" @click="onCreateNote">
            <i :class="creatingNote ? 'pi pi-spin pi-spinner' : 'pi pi-plus'"></i> New Note
          </button>
        </div>
      </div>

      <!-- Breadcrumb -->
      <nav v-if="breadcrumbs.length > 0" class="breadcrumbs">
        <button class="breadcrumb-item" @click="navigateTo(-1)">Notes</button>
        <template v-for="(crumb, index) in breadcrumbs" :key="crumb.id">
          <i class="pi pi-chevron-right breadcrumb-sep"></i>
          <button
            class="breadcrumb-item"
            :class="{ 'breadcrumb-current': index === breadcrumbs.length - 1 }"
            @click="navigateTo(index)"
          >
            {{ crumb.title }}
          </button>
        </template>
      </nav>

      <!-- Create note error -->
      <div v-if="createNoteError" class="load-error">
        <i class="pi pi-exclamation-triangle"></i>
        <span>{{ createNoteError }}</span>
      </div>

      <!-- Load error -->
      <div v-if="loadError" class="load-error">
        <i class="pi pi-exclamation-triangle"></i>
        <span>{{ loadError }}</span>
      </div>

      <!-- Loading -->
      <div v-else-if="loading" class="loading-state">
        <i class="pi pi-spin pi-spinner"></i>
        <span>Loading...</span>
      </div>

      <!-- API notes/folders -->
      <template v-else>
        <div v-if="apiNotes.length > 0" class="notes-grid">
          <!-- Folders -->
          <button
            v-for="item in apiFolders"
            :key="item.id"
            class="note-card folder-card"
            @click="openFolder(item)"
          >
            <div class="note-card-header">
              <span class="note-title"><i class="pi pi-folder folder-icon"></i> {{ item.title }}</span>
            </div>
            <div v-if="item.labels?.length > 0" class="note-labels">
              <span v-for="label in item.labels" :key="label" class="label-chip">{{ label }}</span>
            </div>
            <p v-else class="note-preview folder-hint">Click to browse contents</p>
          </button>

          <!-- Notes (non-folders) -->
          <button
            v-for="item in apiNoteItems"
            :key="item.id"
            class="note-card"
            @click="router.push(`/project/${projectId}/notes/${item.id}`)"
          >
            <div class="note-card-header">
              <span class="note-title">{{ item.title }}</span>
              <span class="note-date">{{ formatDate(item.updatedAt) }}</span>
            </div>
          </button>
        </div>

        <div v-else-if="!loading" class="empty-state">
          <i class="pi pi-folder-open empty-icon"></i>
          <p>{{ currentFolderId ? 'This folder is empty.' : 'No notes yet.' }}</p>
        </div>
      </template>

      <!-- Sample notes (kept for reference) -->
      <div v-if="breadcrumbs.length === 0" class="section-divider">
        <span class="section-label">Sample layout</span>
      </div>
      <div v-if="breadcrumbs.length === 0" class="notes-grid">
        <div v-for="note in sampleNotes" :key="note.id" class="note-card">
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
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import NewFolderDialog from './NewFolderDialog.vue'
import { useNoteFolders } from '../../composables/useNoteFolders'

const route = useRoute()
const router = useRouter()
const projectId = computed(() => route.params.id)
const sidebarOpen = ref(window.innerWidth >= 768)
const showNewFolderDialog = ref(false)

const { getNote, getNotes, createNote } = useNoteFolders()

const creatingNote    = ref(false)
const createNoteError = ref(null)

const currentFolderId = ref(null)
const breadcrumbs     = ref([])   // [{ id, title }, ...]
const apiNotes        = ref([])
const loading         = ref(false)
const loadError       = ref(null)

const apiFolders   = computed(() => apiNotes.value.filter(n => n.isFolder))
const apiNoteItems = computed(() => apiNotes.value.filter(n => !n.isFolder))

async function loadNotes(parentId = null) {
  loading.value   = true
  loadError.value = null
  try {
    const response = await getNotes(projectId.value, parentId)
    if (response.ok) {
      apiNotes.value = await response.json()
    } else {
      loadError.value = `Failed to load notes (${response.status}).`
    }
  } catch {
    loadError.value = 'Could not connect to the server.'
  } finally {
    loading.value = false
  }
}

function openFolder(folder) {
  breadcrumbs.value.push({ id: folder.id, title: folder.title })
  currentFolderId.value = folder.id
  loadNotes(folder.id)
}

function navigateTo(index) {
  if (index === -1) {
    // Back to root
    breadcrumbs.value     = []
    currentFolderId.value = null
    loadNotes(null)
  } else {
    breadcrumbs.value     = breadcrumbs.value.slice(0, index + 1)
    currentFolderId.value = breadcrumbs.value[index].id
    loadNotes(currentFolderId.value)
  }
}

function onFolderSaved(folder) {
  loadNotes(currentFolderId.value)
}

async function onCreateNote() {
  creatingNote.value    = true
  createNoteError.value = null
  try {
    const response = await createNote(projectId.value, currentFolderId.value)
    if (response.ok) {
      const { id } = await response.json()
      router.push(`/project/${projectId.value}/notes/${id}`)
    } else {
      createNoteError.value = `Failed to create note (${response.status}).`
    }
  } catch {
    createNoteError.value = 'Could not connect to the server.'
  } finally {
    creatingNote.value = false
  }
}

function formatDate(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

async function buildBreadcrumbsFromFolder(folderId) {
  const chain = []
  let currentId = folderId
  while (currentId) {
    const response = await getNote(projectId.value, currentId)
    if (!response.ok) break
    const folder = await response.json()
    chain.unshift({ id: folder.id, title: folder.title })
    currentId = folder.parentNoteId ?? null
  }
  return chain
}

onMounted(async () => {
  const folderId = route.query.folderId ?? null
  if (folderId) {
    breadcrumbs.value     = await buildBreadcrumbsFromFolder(folderId)
    currentFolderId.value = folderId
  }
  loadNotes(folderId)
})

const sampleNotes = [
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
  margin-bottom: 1.25rem;
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

.header-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.secondary-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1.25rem;
  background: transparent;
  border: 1px solid var(--border-purple);
  border-radius: 8px;
  color: var(--color-purple);
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}
.secondary-btn:hover { background: var(--bg-active); box-shadow: 0 0 12px var(--glow-purple); }

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

/* Breadcrumbs */
.breadcrumbs {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  margin-bottom: 1.25rem;
  flex-wrap: wrap;
}

.breadcrumb-item {
  background: transparent;
  border: none;
  color: var(--color-purple);
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  padding: 0.2rem 0.3rem;
  border-radius: 4px;
  transition: background 0.15s, color 0.15s;
}
.breadcrumb-item:hover { background: var(--bg-active); }
.breadcrumb-current { color: var(--text-primary); cursor: default; }
.breadcrumb-current:hover { background: transparent; }

.breadcrumb-sep {
  color: var(--text-dim);
  font-size: 0.65rem;
}

/* States */
.load-error {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  background: rgba(236, 72, 153, 0.08);
  border: 1px solid rgba(236, 72, 153, 0.25);
  border-radius: 8px;
  padding: 0.75rem 1rem;
  color: var(--color-pink-light);
  font-size: 0.875rem;
  margin-bottom: 1rem;
}

.loading-state {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  color: var(--text-muted);
  font-size: 0.875rem;
  padding: 1rem 0;
}

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 3rem 0;
  color: var(--text-dim);
}
.empty-icon { font-size: 2rem; }

/* Notes grid */
.notes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.note-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 1rem 1.25rem;
  cursor: pointer;
  transition: all 0.2s;
  text-align: left;
  width: 100%;
}
.note-card:hover {
  border-color: var(--color-purple);
  box-shadow: 0 0 16px var(--glow-purple);
  transform: translateY(-2px);
}

.folder-card {
  background: rgba(168, 85, 247, 0.07);
  border-color: rgba(168, 85, 247, 0.35);
}
.folder-card:hover {
  background: rgba(168, 85, 247, 0.13);
  border-color: var(--color-purple);
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

.folder-icon {
  color: var(--color-purple);
  margin-right: 0.4rem;
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

.folder-hint {
  color: var(--text-dim);
  font-style: italic;
}

.note-labels {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  margin-top: 0.4rem;
}

.label-chip {
  font-size: 0.7rem;
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  background: rgba(168, 85, 247, 0.12);
  color: var(--color-purple);
  border: 1px solid rgba(168, 85, 247, 0.35);
  font-weight: 500;
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

/* Section divider for sample notes */
.section-divider {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin: 0.5rem 0 1rem;
}
.section-divider::before,
.section-divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: var(--border-color);
}
.section-label {
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--text-dim);
  white-space: nowrap;
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
