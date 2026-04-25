<template>
  <div class="page-layout">
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />
    <ProjectSidebar :open="sidebarOpen" />

    <div class="main-body">

      <!-- Loading -->
      <div v-if="loading" class="loading-state">
        <i class="pi pi-spin pi-spinner"></i>
        <span>Loading...</span>
      </div>

      <!-- Error -->
      <div v-else-if="loadError" class="load-error">
        <i class="pi pi-exclamation-triangle"></i>
        <span>{{ loadError }}</span>
      </div>

      <template v-else-if="document">

        <!-- Top bar -->
        <div class="top-bar">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <input
            ref="titleInput"
            v-model="titleDraft"
            class="title-input"
            type="text"
            placeholder="Document title"
            maxlength="200"
            @blur="saveTitle"
            @keydown.enter="titleInput?.blur()"
          />
          <div class="save-status">
            <i v-if="saving" class="pi pi-spin pi-spinner save-icon saving"></i>
            <i v-else-if="saveError" class="pi pi-exclamation-circle save-icon error" :title="saveError"></i>
            <i v-else-if="savedRecently" class="pi pi-check-circle save-icon saved"></i>
          </div>
        </div>

        <!-- Breadcrumb -->
        <nav v-if="breadcrumbs.length > 0" class="breadcrumbs">
          <button class="breadcrumb-item" @click="router.push(`/project/${projectId}/documents`)">Documents</button>
          <template v-for="(crumb, index) in breadcrumbs" :key="crumb.id">
            <i class="pi pi-chevron-right breadcrumb-sep"></i>
            <button
              class="breadcrumb-item"
              @click="router.push(`/project/${projectId}/documents?folderId=${crumb.id}`)"
            >
              {{ crumb.title }}
            </button>
          </template>
        </nav>
        <nav v-else class="breadcrumbs">
          <button class="breadcrumb-item" @click="router.push(`/project/${projectId}/documents`)">
            <i class="pi pi-arrow-left"></i> Documents
          </button>
        </nav>

        <!-- Deleted banner -->
        <div v-if="document.deletedAt" class="deleted-banner">
          <div class="deleted-banner-left">
            <i class="pi pi-trash"></i>
            <div>
              <strong>This document is in the recycle bin.</strong>
              <span> It will be permanently deleted on {{ permanentDeleteDate }}.</span>
            </div>
          </div>
          <button class="restore-btn" :disabled="restoring" @click="onRestore">
            <i :class="restoring ? 'pi pi-spin pi-spinner' : 'pi pi-undo'"></i>
            {{ restoring ? 'Restoring...' : 'Restore' }}
          </button>
        </div>

        <!-- Labels -->
        <div class="section">
          <div class="section-label">Labels</div>
          <div class="label-input-box" :class="{ focused: labelInputFocused }" @click="focusLabelInput">
            <span
              v-for="(label, i) in selectedLabels"
              :key="label"
              class="label-chip"
            >
              {{ label }}
              <button class="chip-remove" tabindex="-1" @click.stop="removeLabel(i)">
                <i class="pi pi-times"></i>
              </button>
            </span>
            <input
              ref="labelInput"
              v-model="labelQuery"
              class="label-text-input"
              type="text"
              placeholder="Add labels…"
              autocomplete="off"
              @focus="labelInputFocused = true"
              @blur="onLabelBlur"
              @keydown="onLabelKeydown"
            />
          </div>
          <div v-if="labelSuggestions.length > 0" class="suggestions">
            <button
              v-for="(s, i) in labelSuggestions"
              :key="s.id"
              class="suggestion-item"
              :class="{ highlighted: i === highlightedIndex }"
              @mousedown.prevent="selectLabelSuggestion(s.labelText)"
            >
              {{ s.labelText }}
            </button>
          </div>
        </div>

        <!-- File info -->
        <div class="section">
          <div class="section-label">File</div>
          <div v-if="document.fileName" class="file-info">
            <i :class="`pi ${fileIcon(document.fileExtension)} file-icon-lg`"></i>
            <div class="file-details">
              <span class="file-name">{{ document.fileName }}</span>
              <span v-if="document.fileLength" class="file-size">{{ formatFileSize(document.fileLength) }}</span>
            </div>
          </div>
          <div v-else class="file-placeholder">
            <i class="pi pi-cloud-upload placeholder-icon"></i>
            <p>File upload coming soon.</p>
          </div>
        </div>

      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import { useDocumentFolders } from '../../composables/useDocumentFolders'

const route = useRoute()
const router = useRouter()
const projectId  = computed(() => route.params.id)
const documentId = computed(() => route.params.documentId)

const sidebarOpen = ref(window.innerWidth >= 768)

const { getDocument, saveDocument, restoreDocument, searchLabels } = useDocumentFolders()

const document     = ref(null)
const loading      = ref(false)
const loadError    = ref(null)
const breadcrumbs  = ref([])

// Title
const titleInput   = ref(null)
const titleDraft   = ref('')
const saving       = ref(false)
const saveError    = ref(null)
const savedRecently = ref(false)
let savedTimer = null

// Labels
const labelInput        = ref(null)
const labelQuery        = ref('')
const selectedLabels    = ref([])
const labelSuggestions  = ref([])
const highlightedIndex  = ref(-1)
const labelInputFocused = ref(false)
let labelDebounce = null

// Restore
const restoring = ref(false)

const permanentDeleteDate = computed(() => {
  if (!document.value?.deletedAt) return ''
  const d = new Date(document.value.deletedAt)
  d.setDate(d.getDate() + 30)
  return d.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
})

async function load() {
  loading.value   = true
  loadError.value = null
  try {
    const res = await getDocument(projectId.value, documentId.value)
    if (!res.ok) { loadError.value = `Failed to load document (${res.status}).`; return }
    document.value    = await res.json()
    titleDraft.value  = document.value.title
    selectedLabels.value = [...(document.value.labels ?? [])]
    if (document.value.parentDocumentId) {
      breadcrumbs.value = await buildBreadcrumbs(document.value.parentDocumentId)
    }
    if (route.query.new === 'true') nextTick(() => titleInput.value?.select())
  } catch {
    loadError.value = 'Could not connect to the server.'
  } finally {
    loading.value = false
  }
}

async function buildBreadcrumbs(folderId) {
  const chain = []
  let currentId = folderId
  while (currentId) {
    const res = await getDocument(projectId.value, currentId)
    if (!res.ok) break
    const folder = await res.json()
    chain.unshift({ id: folder.id, title: folder.title })
    currentId = folder.parentDocumentId ?? null
  }
  return chain
}

async function saveTitle() {
  const trimmed = titleDraft.value.trim()
  if (!trimmed || trimmed === document.value?.title) return
  await persist({ title: trimmed })
}

async function saveLabels() {
  await persist({ labels: selectedLabels.value })
}

async function persist(data) {
  saving.value    = true
  saveError.value = null
  clearTimeout(savedTimer)
  try {
    const res = await saveDocument(projectId.value, documentId.value, data)
    if (res.ok) {
      if (data.title) document.value = { ...document.value, title: data.title }
      savedRecently.value = true
      savedTimer = setTimeout(() => { savedRecently.value = false }, 2000)
    } else {
      const body = await res.json().catch(() => null)
      saveError.value = body?.errors?.join(' ') ?? `Save failed (${res.status}).`
    }
  } catch {
    saveError.value = 'Could not connect to the server.'
  } finally {
    saving.value = false
  }
}

// Labels
watch(labelQuery, (val) => {
  clearTimeout(labelDebounce)
  highlightedIndex.value = -1
  if (val.trim().length < 1) { labelSuggestions.value = []; return }
  labelDebounce = setTimeout(() => fetchLabelSuggestions(val.trim()), 300)
})

async function fetchLabelSuggestions(query) {
  try {
    const res = await searchLabels(projectId.value, query)
    if (!res.ok) { labelSuggestions.value = []; return }
    const all = await res.json()
    labelSuggestions.value = all.filter(s => !selectedLabels.value.includes(s.labelText))
  } catch {
    labelSuggestions.value = []
  }
}

function commitLabel(text) {
  const t = text.trim()
  if (t && !selectedLabels.value.includes(t)) {
    selectedLabels.value.push(t)
    saveLabels()
  }
  labelQuery.value       = ''
  labelSuggestions.value = []
  highlightedIndex.value = -1
}

function selectLabelSuggestion(text) {
  commitLabel(text)
  nextTick(() => labelInput.value?.focus())
}

function removeLabel(index) {
  selectedLabels.value.splice(index, 1)
  saveLabels()
}

function focusLabelInput() { labelInput.value?.focus() }

function onLabelBlur() {
  setTimeout(() => {
    labelInputFocused.value = false
    labelSuggestions.value  = []
    if (labelQuery.value.trim()) commitLabel(labelQuery.value)
  }, 150)
}

function onLabelKeydown(e) {
  if (e.key === 'Enter' || e.key === ',') {
    e.preventDefault()
    if (highlightedIndex.value >= 0 && labelSuggestions.value[highlightedIndex.value]) {
      selectLabelSuggestion(labelSuggestions.value[highlightedIndex.value].labelText)
    } else {
      commitLabel(labelQuery.value)
    }
  } else if (e.key === 'ArrowDown') {
    e.preventDefault()
    highlightedIndex.value = Math.min(highlightedIndex.value + 1, labelSuggestions.value.length - 1)
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    highlightedIndex.value = Math.max(highlightedIndex.value - 1, 0)
  } else if (e.key === 'Backspace' && labelQuery.value === '') {
    selectedLabels.value.pop()
    saveLabels()
  } else if (e.key === 'Escape') {
    labelSuggestions.value = []
    highlightedIndex.value = -1
  }
}

async function onRestore() {
  restoring.value = true
  try {
    const res = await restoreDocument(projectId.value, documentId.value)
    if (res.ok) await load()
  } finally {
    restoring.value = false
  }
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

function formatFileSize(bytes) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  if (bytes < 1024 * 1024 * 1024) return `${(bytes / 1024 / 1024).toFixed(1)} MB`
  return `${(bytes / 1024 / 1024 / 1024).toFixed(1)} GB`
}

onMounted(load)
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

/* Top bar */
.top-bar {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
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
  flex-shrink: 0;
}
.hamburger-btn:hover { color: var(--color-purple); }

.title-input {
  flex: 1;
  background: transparent;
  border: none;
  border-bottom: 1px solid transparent;
  color: var(--text-primary);
  font-size: 1.4rem;
  font-weight: 700;
  font-family: inherit;
  outline: none;
  padding: 0.2rem 0;
  transition: border-color 0.2s;
  min-width: 0;
}
.title-input::placeholder { color: var(--text-dim); }
.title-input:focus { border-bottom-color: var(--color-purple); }

.save-status { display: flex; align-items: center; width: 1.25rem; flex-shrink: 0; }
.save-icon { font-size: 0.95rem; }
.save-icon.saving { color: var(--text-muted); }
.save-icon.error { color: var(--color-pink); }
.save-icon.saved { color: #4ade80; }

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
  display: flex;
  align-items: center;
  gap: 0.3rem;
}
.breadcrumb-item:hover { background: var(--bg-active); }

.breadcrumb-sep { color: var(--text-dim); font-size: 0.65rem; }

/* Deleted banner */
.deleted-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  background: rgba(236, 72, 153, 0.08);
  border: 1px solid rgba(236, 72, 153, 0.3);
  border-radius: 10px;
  padding: 0.85rem 1.25rem;
  margin-bottom: 1.25rem;
  font-size: 0.875rem;
  color: var(--color-pink-light);
}

.deleted-banner-left { display: flex; align-items: flex-start; gap: 0.65rem; }
.deleted-banner-left .pi { font-size: 1rem; margin-top: 1px; flex-shrink: 0; }

.restore-btn {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.4rem 0.9rem;
  background: transparent;
  border: 1px solid rgba(236, 72, 153, 0.5);
  border-radius: 7px;
  color: var(--color-pink-light);
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
  flex-shrink: 0;
}
.restore-btn:hover:not(:disabled) { background: rgba(236, 72, 153, 0.12); }
.restore-btn:disabled { opacity: 0.5; cursor: not-allowed; }

/* Sections */
.section { margin-bottom: 1.5rem; position: relative; }

.section-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.06em;
  margin-bottom: 0.5rem;
}

/* Labels */
.label-input-box {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 0.45rem 0.75rem;
  cursor: text;
  min-height: 2.5rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.label-input-box.focused { border-color: var(--color-purple); box-shadow: 0 0 0 3px rgba(168, 85, 247, 0.12); }

.label-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.75rem;
  padding: 0.15rem 0.4rem 0.15rem 0.6rem;
  border-radius: 999px;
  background: rgba(168, 85, 247, 0.15);
  color: var(--color-purple);
  border: 1px solid rgba(168, 85, 247, 0.4);
  font-weight: 500;
  white-space: nowrap;
}
.chip-remove { background: none; border: none; color: var(--color-purple); cursor: pointer; padding: 0; line-height: 1; opacity: 0.7; display: flex; align-items: center; }
.chip-remove .pi { font-size: 0.7rem; }
.chip-remove:hover { opacity: 1; }

.label-text-input { flex: 1; min-width: 8rem; background: none; border: none; outline: none; color: var(--text-primary); font-size: 0.875rem; font-family: inherit; padding: 0.15rem 0; }
.label-text-input::placeholder { color: var(--text-dim); }

.suggestions {
  position: absolute;
  left: 0;
  right: 0;
  top: 100%;
  margin-top: 2px;
  background: var(--bg-card);
  border: 1px solid var(--border-purple);
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
  overflow: hidden;
  z-index: 10;
}
.suggestion-item { display: block; width: 100%; text-align: left; background: none; border: none; color: var(--text-primary); font-size: 0.875rem; font-family: inherit; padding: 0.55rem 0.85rem; cursor: pointer; transition: background 0.15s; }
.suggestion-item:hover, .suggestion-item.highlighted { background: var(--bg-active); color: var(--color-purple); }

/* File info */
.file-info {
  display: flex;
  align-items: center;
  gap: 1rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 1rem 1.25rem;
}

.file-icon-lg { font-size: 1.75rem; color: var(--text-muted); flex-shrink: 0; }

.file-details { display: flex; flex-direction: column; gap: 0.2rem; min-width: 0; }
.file-name { font-size: 0.9rem; font-weight: 500; color: var(--text-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.file-size { font-size: 0.78rem; color: var(--text-muted); }

.file-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 2rem;
  background: var(--bg-card);
  border: 1px dashed var(--border-color);
  border-radius: 10px;
  color: var(--text-dim);
  font-size: 0.875rem;
}
.placeholder-icon { font-size: 2rem; opacity: 0.5; }

/* States */
.loading-state { display: flex; align-items: center; gap: 0.6rem; color: var(--text-muted); font-size: 0.875rem; padding: 1rem 0; }

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

.sidebar-backdrop { display: none; }

@media (max-width: 767px) {
  .sidebar-backdrop { display: block; position: fixed; inset: 0; top: 60px; background: rgba(0, 0, 0, 0.6); z-index: 99; }
  .main-body { padding: 1rem; }
}
</style>
