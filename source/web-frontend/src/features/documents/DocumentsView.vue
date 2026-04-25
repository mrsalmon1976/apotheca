<template>
  <div class="page-layout">
    <NewFolderDialog
      :visible="showNewFolderDialog"
      :project-id="projectId"
      :parent-id="currentFolderId"
      @close="showNewFolderDialog = false"
      @saved="onFolderSaved"
    />

    <AddDocumentDialog
      :visible="showAddDocumentDialog"
      :file="droppedFile"
      :project-id="projectId"
      :parent-id="currentFolderId"
      @close="showAddDocumentDialog = false; droppedFile = null"
      @uploaded="onDocumentUploaded"
    />

    <DeleteConfirmDialog
      :visible="showDeleteDialog"
      :item-title="deleteTarget?.title ?? ''"
      :is-folder="deleteTarget?.isFolder ?? false"
      @close="showDeleteDialog = false"
      @confirm="onDeleteConfirm"
    />

    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <ProjectSidebar :open="sidebarOpen" />

    <div
      class="main-body"
      :class="{ 'drag-over': isDragging }"
      @dragover.prevent="isDragging = true"
      @dragleave="isDragging = false"
      @drop.prevent="onDrop"
    >
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">Documents</h1>
        </div>
        <div class="header-actions">
          <button class="secondary-btn" @click="showNewFolderDialog = true">
            <i class="pi pi-folder-plus"></i> New Folder
          </button>
          <button class="primary-btn" @click="showAddDocumentDialog = true">
            <i class="pi pi-upload"></i> Add Document
          </button>
        </div>
      </div>

      <!-- Breadcrumb -->
      <nav v-if="breadcrumbs.length > 0" class="breadcrumbs">
        <button class="breadcrumb-item" @click="navigateTo(-1)">Documents</button>
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

      <!-- Drag overlay hint -->
      <div v-if="isDragging" class="drag-hint">
        <i class="pi pi-upload"></i>
        <span>Drop files to add documents</span>
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

      <template v-if="!loading && !isDragging">

        <!-- Folders -->
        <template v-if="apiFolders.length > 0">
          <div class="section-divider">
            <span class="section-label">Folders</span>
          </div>

          <div class="docs-grid">
            <div
              v-for="item in apiFolders"
              :key="item.id"
              class="doc-card folder-card"
              @click="openFolder(item)"
            >
              <div class="doc-card-header">
                <span class="doc-title"><i class="pi pi-folder folder-icon"></i> {{ item.title }}</span>
                <button
                  class="card-delete-btn"
                  title="Delete folder"
                  @click.stop="promptDelete(item)"
                >
                  <i class="pi pi-trash"></i>
                </button>
              </div>
              <div v-if="item.labels?.length > 0" class="doc-labels">
                <span v-for="label in item.labels" :key="label" class="label-chip">{{ label }}</span>
              </div>
              <p v-else class="doc-preview folder-hint">Click to browse contents</p>
            </div>
          </div>
        </template>

        <!-- Documents section -->
        <div class="section-divider">
          <span class="section-label">Documents</span>
        </div>

        <div v-if="apiDocumentItems.length > 0" class="docs-grid">
          <div
            v-for="item in apiDocumentItems"
            :key="item.id"
            class="doc-card"
            @click="router.push(`/project/${projectId}/documents/${item.id}`)"
          >
            <div class="doc-card-header">
              <div class="doc-title-row">
                <i :class="`pi ${fileIcon(item.fileExtension)} file-icon`"></i>
                <span class="doc-title">{{ item.title }}</span>
              </div>
              <div class="doc-card-header-right">
                <span class="doc-date">{{ formatDate(item.updatedAt) }}</span>
                <button
                  class="card-delete-btn"
                  title="Delete document"
                  @click.stop="promptDelete(item)"
                >
                  <i class="pi pi-trash"></i>
                </button>
              </div>
            </div>
            <p v-if="item.fileName" class="doc-preview">{{ item.fileName }}</p>
            <div v-if="item.labels?.length > 0" class="doc-labels">
              <span v-for="label in item.labels" :key="label" class="label-chip">{{ label }}</span>
            </div>
          </div>
        </div>

        <div v-else class="empty-state">
          <i class="pi pi-file empty-icon"></i>
          <p>{{ currentFolderId ? 'No documents in this folder.' : 'No documents yet.' }}</p>
          <p class="empty-hint">Click <strong>Add Document</strong> or drag a file here to get started.</p>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import NewFolderDialog from './NewFolderDialog.vue'
import AddDocumentDialog from './AddDocumentDialog.vue'
import DeleteConfirmDialog from './DeleteConfirmDialog.vue'
import { useDocumentFolders } from '../../composables/useDocumentFolders'

const route = useRoute()
const router = useRouter()
const projectId = computed(() => route.params.id)
const sidebarOpen = ref(window.innerWidth >= 768)
const showNewFolderDialog    = ref(false)
const showAddDocumentDialog  = ref(false)
const droppedFile            = ref(null)

const { getDocument, getDocuments, deleteDocument } = useDocumentFolders()

const isDragging = ref(false)

const showDeleteDialog = ref(false)
const deleteTarget     = ref(null)

const currentFolderId = ref(null)
const breadcrumbs     = ref([])
const apiDocuments    = ref([])
const loading         = ref(false)
const loadError       = ref(null)

const apiFolders        = computed(() => apiDocuments.value.filter(d => d.isFolder))
const apiDocumentItems  = computed(() => apiDocuments.value.filter(d => !d.isFolder))

async function loadDocuments(parentId = null) {
  loading.value   = true
  loadError.value = null
  try {
    const response = await getDocuments(projectId.value, parentId)
    if (response.ok) {
      apiDocuments.value = await response.json()
    } else {
      loadError.value = `Failed to load documents (${response.status}).`
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
  loadDocuments(folder.id)
}

function navigateTo(index) {
  if (index === -1) {
    breadcrumbs.value     = []
    currentFolderId.value = null
    loadDocuments(null)
  } else {
    breadcrumbs.value     = breadcrumbs.value.slice(0, index + 1)
    currentFolderId.value = breadcrumbs.value[index].id
    loadDocuments(currentFolderId.value)
  }
}

function onFolderSaved() {
  loadDocuments(currentFolderId.value)
}

function onDocumentUploaded() {
  loadDocuments(currentFolderId.value)
}

function onDrop(e) {
  isDragging.value = false
  const files = Array.from(e.dataTransfer?.files ?? [])
  droppedFile.value           = files[0] ?? null
  showAddDocumentDialog.value = true
}

function promptDelete(item) {
  deleteTarget.value     = { id: item.id, title: item.title, isFolder: item.isFolder }
  showDeleteDialog.value = true
}

async function onDeleteConfirm({ setError, done }) {
  try {
    const response = await deleteDocument(projectId.value, deleteTarget.value.id)
    if (response.ok) {
      done()
      loadDocuments(currentFolderId.value)
    } else if (response.status === 403) {
      setError('You do not have permission to delete this item.')
    } else if (response.status === 404) {
      setError('This item no longer exists.')
    } else {
      setError(`Unexpected error (${response.status}). Please try again.`)
    }
  } catch {
    setError('Could not connect to the server. Please try again.')
  }
}

function formatDate(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

function fileIcon(ext) {
  if (!ext) return 'pi-file'
  const e = ext.toLowerCase().replace('.', '')
  if (['pdf'].includes(e)) return 'pi-file-pdf'
  if (['doc', 'docx'].includes(e)) return 'pi-file-word'
  if (['xls', 'xlsx', 'csv'].includes(e)) return 'pi-file-excel'
  if (['png', 'jpg', 'jpeg', 'gif', 'svg', 'webp'].includes(e)) return 'pi-image'
  if (['zip', 'tar', 'gz', 'rar'].includes(e)) return 'pi-box'
  if (['mp4', 'mov', 'avi', 'mkv'].includes(e)) return 'pi-video'
  if (['mp3', 'wav', 'flac'].includes(e)) return 'pi-volume-up'
  return 'pi-file'
}

async function buildBreadcrumbsFromFolder(folderId) {
  const chain = []
  let currentId = folderId
  while (currentId) {
    const response = await getDocument(projectId.value, currentId)
    if (!response.ok) break
    const folder = await response.json()
    chain.unshift({ id: folder.id, title: folder.title })
    currentId = folder.parentDocumentId ?? null
  }
  return chain
}

onMounted(async () => {
  const folderId = route.query.folderId ?? null
  if (folderId) {
    breadcrumbs.value     = await buildBreadcrumbsFromFolder(folderId)
    currentFolderId.value = folderId
  }
  loadDocuments(folderId)
})
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
  position: relative;
  transition: background 0.15s;
}

.main-body.drag-over {
  background: rgba(168, 85, 247, 0.04);
  outline: 2px dashed var(--color-purple);
  outline-offset: -8px;
}

.drag-hint {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  min-height: 200px;
  color: var(--color-purple);
  font-size: 1rem;
  font-weight: 500;
  pointer-events: none;
}
.drag-hint .pi { font-size: 2.5rem; opacity: 0.7; }

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

.header-actions { display: flex; align-items: center; gap: 0.5rem; }

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
.breadcrumbs { display: flex; align-items: center; gap: 0.25rem; margin-bottom: 1.25rem; flex-wrap: wrap; }

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

.breadcrumb-sep { color: var(--text-dim); font-size: 0.65rem; }

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

.loading-state { display: flex; align-items: center; gap: 0.6rem; color: var(--text-muted); font-size: 0.875rem; padding: 1rem 0; }

.empty-state { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 3rem 0; color: var(--text-dim); }
.empty-icon { font-size: 2rem; }
.empty-hint { font-size: 0.8rem; color: var(--text-dim); margin-top: 0.25rem; }

/* Grid */
.docs-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.doc-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 1rem 1.25rem;
  cursor: pointer;
  transition: all 0.2s;
  text-align: left;
  width: 100%;
}
.doc-card:hover { border-color: var(--color-purple); box-shadow: 0 0 16px var(--glow-purple); transform: translateY(-2px); }

.folder-card { background: rgba(168, 85, 247, 0.07); border-color: rgba(168, 85, 247, 0.35); }
.folder-card:hover { background: rgba(168, 85, 247, 0.13); border-color: var(--color-purple); }

.doc-card-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.5rem; }

.doc-title-row { display: flex; align-items: center; gap: 0.4rem; min-width: 0; }

.doc-title { font-weight: 600; font-size: 0.95rem; color: var(--text-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.folder-icon { color: var(--color-purple); margin-right: 0.4rem; flex-shrink: 0; }
.file-icon { color: var(--text-muted); flex-shrink: 0; font-size: 0.9rem; }

.doc-card-header-right { display: flex; align-items: center; gap: 0.4rem; flex-shrink: 0; }

.doc-date { font-size: 0.75rem; color: var(--text-dim); white-space: nowrap; }

.card-delete-btn {
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
  flex-shrink: 0;
}
.doc-card:hover .card-delete-btn,
.folder-card:hover .card-delete-btn { opacity: 1; }
.card-delete-btn:hover { color: var(--color-pink-light); background: rgba(236, 72, 153, 0.12); }

.doc-preview {
  font-size: 0.8rem;
  color: var(--text-secondary);
  line-height: 1.5;
  margin: 0 0 0.75rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.folder-hint { color: var(--text-dim); font-style: italic; }

.doc-labels { display: flex; flex-wrap: wrap; gap: 0.35rem; margin-top: 0.4rem; }

.label-chip {
  font-size: 0.7rem;
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  background: rgba(168, 85, 247, 0.12);
  color: var(--color-purple);
  border: 1px solid rgba(168, 85, 247, 0.35);
  font-weight: 500;
}

/* Section divider */
.section-divider { display: flex; align-items: center; gap: 0.75rem; margin: 0.5rem 0 1rem; }
.section-divider::before, .section-divider::after { content: ''; flex: 1; height: 1px; background: var(--border-color); }
.section-label { font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.08em; color: var(--text-dim); white-space: nowrap; }

.sidebar-backdrop { display: none; }

@media (max-width: 767px) {
  .sidebar-backdrop { display: block; position: fixed; inset: 0; top: 60px; background: rgba(0, 0, 0, 0.6); z-index: 99; }
  .main-body { padding: 1rem; }
}
</style>
