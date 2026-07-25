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

    <RenameFolderDialog
      :visible="showRenameDialog"
      :project-id="projectId"
      :folder-id="renameTarget?.id ?? null"
      :current-title="renameTarget?.title ?? ''"
      @close="showRenameDialog = false"
      @renamed="onFolderRenamed"
    />

    <DeleteConfirmDialog
      :visible="showDeleteDialog"
      :item-title="deleteTarget?.title ?? ''"
      :is-folder="deleteTarget?.isFolder ?? false"
      @close="showDeleteDialog = false"
      @confirm="onDeleteConfirm"
    />

    <MoveDialog
      :visible="showMoveDialog"
      :project-id="projectId"
      :item="moveTarget"
      :current-parent-id="currentFolderId"
      @close="showMoveDialog = false"
      @moved="onMoved"
    />

    <Menu ref="actionsMenu" :model="menuItems" :popup="true" />

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
      <nav class="breadcrumbs">
        <span v-if="breadcrumbs.length === 0" class="breadcrumb-item breadcrumb-current">Root</span>
        <button v-else class="breadcrumb-item" @click="navigateTo(-1)">Documents</button>
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

          <table class="doc-table">
            <thead>
              <tr>
                <th class="col-name">Name</th>
                <th class="col-actions"></th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in apiFolders"
                :key="item.id"
                class="doc-row folder-row"
                @click="openFolder(item)"
              >
                <td class="col-name">
                  <i class="pi pi-folder folder-icon"></i>
                  <span class="row-title">{{ item.title }}</span>
                </td>
                <td class="col-actions">
                  <button class="row-action-btn" title="Actions" @click.stop="openActionsMenu($event, item)">
                    <i class="pi pi-ellipsis-v"></i>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </template>

        <!-- Documents -->
        <div class="section-divider">
          <span class="section-label">Documents</span>
        </div>

        <template v-if="apiDocumentItems.length > 0">
          <table class="doc-table">
            <thead>
              <tr>
                <th class="col-name">Title</th>
                <th class="col-filename">File Name</th>
                <th class="col-size">Size</th>
                <th class="col-date">Updated</th>
                <th class="col-actions"></th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in apiDocumentItems"
                :key="item.id"
                class="doc-row"
                @click="router.push(`/project/${projectId}/documents/${item.id}`)"
              >
                <td class="col-name">
                  <i :class="`pi ${fileIcon(item.fileExtension)} file-icon`"></i>
                  <span class="row-title">{{ item.title }}</span>
                </td>
                <td class="col-filename">{{ item.fileName ?? '—' }}</td>
                <td class="col-size">{{ formatSize(item.fileLength) }}</td>
                <td class="col-date">{{ formatDate(item.updatedAt) }}</td>
                <td class="col-actions">
                  <button class="row-action-btn" title="Actions" @click.stop="openActionsMenu($event, item)">
                    <i class="pi pi-ellipsis-v"></i>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </template>

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
import { ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Menu from 'primevue/menu'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import NewFolderDialog from './NewFolderDialog.vue'
import RenameFolderDialog from './RenameFolderDialog.vue'
import AddDocumentDialog from './AddDocumentDialog.vue'
import DeleteConfirmDialog from './DeleteConfirmDialog.vue'
import MoveDialog from './MoveDialog.vue'
import { useDocumentFolders } from '../../composables/useDocumentFolders'

const route = useRoute()
const router = useRouter()
const projectId = computed(() => route.params.id)
const sidebarOpen = ref(window.innerWidth >= 768)
const showNewFolderDialog    = ref(false)
const showAddDocumentDialog  = ref(false)
const droppedFile            = ref(null)

const { getDocument, getDocuments, deleteDocument, downloadDocument } = useDocumentFolders()

const isDragging = ref(false)

const showRenameDialog = ref(false)
const renameTarget     = ref(null)

const showDeleteDialog = ref(false)
const deleteTarget     = ref(null)

const showMoveDialog = ref(false)
const moveTarget     = ref(null)   // { id, title, isFolder }

const actionsMenu = ref(null)
const menuTarget   = ref(null)
const menuItems    = computed(() => {
  if (!menuTarget.value) return []
  const items = []
  if (menuTarget.value.isFolder) {
    items.push({ label: 'Rename', icon: 'pi pi-pencil', command: () => promptRename(menuTarget.value) })
  } else {
    items.push({ label: 'Download', icon: 'pi pi-download', command: () => downloadItem(menuTarget.value) })
  }
  items.push({ label: 'Move', icon: 'pi pi-arrow-right-arrow-left', command: () => promptMove(menuTarget.value) })
  items.push({ label: 'Delete', icon: 'pi pi-trash', class: 'danger-item', command: () => promptDelete(menuTarget.value) })
  return items
})

const folderIds = computed(() => {
  const f = route.params.folders
  if (!f) return []
  if (Array.isArray(f)) return f.filter(Boolean)
  return f ? [f] : []
})

const currentFolderId = computed(() => folderIds.value.at(-1) ?? null)
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
  const path = [...folderIds.value, folder.id].join('/')
  router.push(`/project/${projectId.value}/documents/f/${path}`)
}

function navigateTo(index) {
  if (index === -1) {
    router.push(`/project/${projectId.value}/documents`)
  } else {
    const path = folderIds.value.slice(0, index + 1).join('/')
    router.push(`/project/${projectId.value}/documents/f/${path}`)
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

async function downloadItem(item) {
  const res = await downloadDocument(projectId.value, item.id)
  if (!res.ok) return
  const blob = await res.blob()
  const url = URL.createObjectURL(blob)
  const a = window.document.createElement('a')
  a.href = url
  a.download = item.fileName ?? item.title
  a.click()
  URL.revokeObjectURL(url)
}

function openActionsMenu(event, item) {
  menuTarget.value = item
  actionsMenu.value.toggle(event)
}

function promptRename(item) {
  renameTarget.value     = { id: item.id, title: item.title }
  showRenameDialog.value = true
}

function onFolderRenamed() {
  loadDocuments(currentFolderId.value)
}

function promptMove(item) {
  moveTarget.value     = { id: item.id, title: item.title, isFolder: item.isFolder }
  showMoveDialog.value = true
}

function onMoved() {
  loadDocuments(currentFolderId.value)
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

function formatSize(bytes) {
  if (bytes == null) return '—'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`
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

async function buildBreadcrumbsFromPath(ids) {
  const crumbs = []
  for (const id of ids) {
    const res = await getDocument(projectId.value, id)
    if (!res.ok) break
    const folder = await res.json()
    crumbs.push({ id: folder.id, title: folder.title })
  }
  return crumbs
}

watch(folderIds, async (ids) => {
  if (ids.length > 0) {
    breadcrumbs.value = await buildBreadcrumbsFromPath(ids)
  } else {
    breadcrumbs.value = []
  }
  loadDocuments(ids.at(-1) ?? null)
}, { immediate: true })
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
.breadcrumb-current { color: var(--text-primary); cursor: default; font-weight: 400; }
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
.folder-row:hover { background: rgba(168, 85, 247, 0.06); }

.doc-row td {
  padding: 0.6rem 0.75rem;
  color: var(--text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 0;
}

.col-name { width: 34%; }
.col-filename { width: 26%; }
.col-size { width: 10%; }
.col-date { width: 14%; }
.col-actions { width: 8%; text-align: right; white-space: nowrap; }

.row-title {
  font-weight: 600;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.folder-icon { color: var(--color-purple); margin-right: 0.6rem; }
.file-icon { color: var(--text-muted); margin-right: 0.6rem; font-size: 0.9rem; }

.row-action-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 0.8rem;
  padding: 0.2rem 0.3rem;
  border-radius: 4px;
  line-height: 1;
  opacity: 0.65;
  transition: opacity 0.15s, color 0.15s, background 0.15s;
}
.doc-row:hover .row-action-btn { opacity: 1; }
.row-action-btn:hover { color: var(--color-purple-light); background: var(--bg-active); }

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
