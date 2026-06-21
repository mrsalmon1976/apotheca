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
          <div class="title-wrap">
            <input
              ref="titleInput"
              v-model="editingTitle"
              class="title-input"
              type="text"
              maxlength="200"
              :placeholder="note ? '' : 'Note'"
              :disabled="!note || titleSaving"
              @keydown.enter.prevent="titleInput?.blur()"
              @keydown.escape.prevent="revertTitle"
              @blur="onTitleBlur"
            />
            <span v-if="titleSaving" class="title-status saving">
              <i class="pi pi-spin pi-spinner"></i>
            </span>
            <span v-else-if="titleSaveError" class="title-status error" :title="titleSaveError">
              <i class="pi pi-exclamation-triangle"></i>
            </span>
            <span v-else-if="titleSaved" class="title-status saved">
              <i class="pi pi-check"></i>
            </span>
          </div>
        </div>
      </div>

      <!-- Breadcrumbs + labels row -->
      <div v-if="!loadError" class="breadcrumbs-row">
        <nav class="breadcrumbs">
          <button class="breadcrumb-item" @click="router.push(`/project/${projectId}/notes`)">
            Notes
          </button>
          <template v-for="(crumb, index) in folderCrumbs" :key="crumb.id">
            <i class="pi pi-chevron-right breadcrumb-sep"></i>
            <button
              class="breadcrumb-item"
              @click="router.push(`/project/${projectId}/notes/f/${folderCrumbs.slice(0, index + 1).map(c => c.id).join('/')}`)"
            >
              {{ crumb.title }}
            </button>
          </template>
          <template v-if="note">
            <i class="pi pi-chevron-right breadcrumb-sep"></i>
            <span class="breadcrumb-item breadcrumb-current">{{ note.title }}</span>
          </template>
        </nav>
        <div v-if="note" class="labels-inline">
          <div
            class="label-input-box"
            :class="{ focused: labelInputFocused }"
            @click="focusLabelInput"
          >
            <span v-for="(label, i) in selectedLabels" :key="label" class="label-chip">
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
              placeholder="Add a label…"
              autocomplete="off"
              @focus="labelInputFocused = true"
              @blur="onLabelBlur"
              @keydown="onLabelKeydown"
            />
            <span v-if="labelsSaving" class="title-status saving">
              <i class="pi pi-spin pi-spinner"></i>
            </span>
            <span v-else-if="labelsSaveError" class="title-status error" :title="labelsSaveError">
              <i class="pi pi-exclamation-triangle"></i>
            </span>
            <span v-else-if="labelsSaved" class="title-status saved">
              <i class="pi pi-check"></i>
            </span>
          </div>
          <div v-if="suggestions.length > 0" class="suggestions">
            <button
              v-for="(s, i) in suggestions"
              :key="s.id"
              class="suggestion-item"
              :class="{ highlighted: i === highlightedIndex }"
              @mousedown.prevent="selectSuggestion(s.labelText)"
            >
              {{ s.labelText }}
            </button>
          </div>
        </div>
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

      <!-- Note content -->
      <template v-else-if="note">

        <!-- Recycle bin banner -->
        <div v-if="note.deletedAt" class="recycle-banner">
          <i class="pi pi-trash recycle-banner-icon"></i>
          <div class="recycle-banner-body">
            <span class="recycle-banner-title">This note is in the recycle bin.</span>
            <span class="recycle-banner-detail">
              It will be permanently deleted on <strong>{{ permanentDeletionDate }}</strong>.
            </span>
          </div>
        </div>

        <!-- Body editor -->
        <div class="body-section">
          <div class="body-header">
            <span class="section-label">Content</span>
            <span v-if="bodySaving" class="save-status saving">
              <i class="pi pi-spin pi-spinner"></i> Saving…
            </span>
            <span v-else-if="bodySaveError" class="save-status error">
              <i class="pi pi-exclamation-triangle"></i> {{ bodySaveError }}
            </span>
            <span v-else-if="bodySaved" class="save-status saved">
              <i class="pi pi-check"></i> Saved
            </span>
          </div>
          <div class="editor-container">
            <div ref="wysiwygEl" class="wysiwyg-pane"></div>
            <textarea
              ref="markdownPaneEl"
              v-model="bodyMarkdown"
              class="markdown-pane"
              spellcheck="false"
              @input="onMarkdownPaneInput"
            ></textarea>
          </div>
        </div>

      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Crepe } from '@milkdown/crepe'
import { replaceAll } from '@milkdown/utils'
import '@milkdown/crepe/theme/common/style.css'
import '@milkdown/crepe/theme/classic.css'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import { useNoteFolders } from '../../composables/useNoteFolders'

const route  = useRoute()
const router = useRouter()

const projectId   = computed(() => route.params.id)
const noteId      = computed(() => route.params.noteId)
const sidebarOpen = ref(window.innerWidth >= 768)

const { getNote, saveNote, searchLabels, uploadNoteAttachment } = useNoteFolders()

const permanentDeletionDate = computed(() => {
  if (!note.value?.deletedAt) return ''
  const d = new Date(note.value.deletedAt)
  d.setDate(d.getDate() + 30)
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' })
})

// Note load state
const note         = ref(null)
const folderCrumbs = ref([])
const loading      = ref(false)
const loadError    = ref(null)

// Title editing state
const titleInput      = ref(null)
const editingTitle    = ref('')
const titleSaving     = ref(false)
const titleSaved      = ref(false)
const titleSaveError  = ref(null)
let   titleSavedTimer = null

// Label editor state
const labelInput        = ref(null)
const labelQuery        = ref('')
const selectedLabels    = ref([])
const suggestions       = ref([])
const highlightedIndex  = ref(-1)
const labelInputFocused = ref(false)

// Label save state
const labelsSaving   = ref(false)
const labelsSaved    = ref(false)
const labelsSaveError = ref(null)

// Body editor state
const wysiwygEl        = ref(null)
const markdownPaneEl   = ref(null)
let   crepeInstance    = null
const bodyMarkdown     = ref('')
const bodySaving       = ref(false)
const bodySaved        = ref(false)
const bodySaveError    = ref(null)
let   bodySavedTimer   = null
let   bodyDebounce     = null
let   markdownApplyDebounce = null

let debounceTimer  = null
let savedTimer     = null

// ── Note loading ────────────────────────────────────────────────────────────

async function buildFolderCrumbs(parentNoteId) {
  const chain = []
  let currentId = parentNoteId
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
  loading.value   = true
  loadError.value = null
  try {
    const response = await getNote(projectId.value, noteId.value)
    if (response.ok) {
      note.value           = await response.json()
      editingTitle.value   = note.value.title
      selectedLabels.value = [...(note.value.labels ?? [])]
      if (note.value.parentNoteId) {
        folderCrumbs.value = await buildFolderCrumbs(note.value.parentNoteId)
      }
    } else if (response.status === 404) {
      loadError.value = 'Note not found.'
    } else {
      loadError.value = `Failed to load note (${response.status}).`
    }
  } catch {
    loadError.value = 'Could not connect to the server.'
  } finally {
    loading.value = false
  }

  await nextTick()
  if (note.value && wysiwygEl.value) {
    crepeInstance = new Crepe({
      root: wysiwygEl.value,
      defaultValue: note.value.body ?? '',
      featureConfigs: {
        [Crepe.Feature.ImageBlock]: {
          onUpload: async (file) => {
            try {
              return await uploadNoteAttachment(projectId.value, noteId.value, file)
            } catch (err) {
              console.error('Image upload failed:', err)
              throw err
            }
          },
        },
      },
    })
    await crepeInstance.create()
    bodyMarkdown.value = crepeInstance.getMarkdown()

    crepeInstance.on((listener) => {
      listener.markdownUpdated((ctx, markdown) => {
        if (document.activeElement !== markdownPaneEl.value) {
          bodyMarkdown.value = markdown
        }
        clearTimeout(bodyDebounce)
        bodyDebounce = setTimeout(persistBody, 1000)
      })
    })
  }
  if (note.value && route.query.new === 'true') {
    setTimeout(() => {
      titleInput.value?.focus()
      titleInput.value?.select()
    }, 0)
  }
})

onUnmounted(async () => {
  clearTimeout(debounceTimer)
  clearTimeout(savedTimer)
  clearTimeout(titleSavedTimer)
  clearTimeout(bodyDebounce)
  clearTimeout(bodySavedTimer)
  clearTimeout(markdownApplyDebounce)
  await crepeInstance?.destroy()
  crepeInstance = null
})

// ── Title editing ────────────────────────────────────────────────────────────

function revertTitle() {
  editingTitle.value   = note.value.title
  titleSaveError.value = null
  titleInput.value?.blur()
}

async function onTitleBlur() {
  const trimmed = editingTitle.value.trim()

  if (trimmed === note.value.title) return

  if (!trimmed || trimmed.length < 3) {
    editingTitle.value = note.value.title
    return
  }

  titleSaving.value    = true
  titleSaved.value     = false
  titleSaveError.value = null
  clearTimeout(titleSavedTimer)

  try {
    const response = await saveNote(projectId.value, noteId.value, { title: trimmed })
    if (response.ok) {
      note.value         = { ...note.value, title: trimmed }
      editingTitle.value = trimmed
      titleSaved.value   = true
      titleSavedTimer    = setTimeout(() => { titleSaved.value = false }, 2000)
    } else {
      titleSaveError.value = `Failed to save (${response.status}).`
      editingTitle.value   = note.value.title
    }
  } catch {
    titleSaveError.value = 'Could not connect to the server.'
    editingTitle.value   = note.value.title
  } finally {
    titleSaving.value = false
  }
}

// ── Label save ───────────────────────────────────────────────────────────────

async function persistLabels() {
  labelsSaving.value   = true
  labelsSaved.value    = false
  labelsSaveError.value = null
  clearTimeout(savedTimer)
  try {
    const response = await saveNote(projectId.value, noteId.value, { labels: selectedLabels.value })
    if (response.ok) {
      labelsSaved.value = true
      savedTimer = setTimeout(() => { labelsSaved.value = false }, 2000)
    } else {
      labelsSaveError.value = `Failed to save labels (${response.status}).`
    }
  } catch {
    labelsSaveError.value = 'Could not connect to the server.'
  } finally {
    labelsSaving.value = false
  }
}

// ── Body save ────────────────────────────────────────────────────────────────

async function persistBody() {
  if (!crepeInstance) return
  bodySaving.value    = true
  bodySaved.value     = false
  bodySaveError.value = null
  clearTimeout(bodySavedTimer)
  try {
    const response = await saveNote(projectId.value, noteId.value, { body: bodyMarkdown.value })
    if (response.ok) {
      bodySaved.value  = true
      bodySavedTimer   = setTimeout(() => { bodySaved.value = false }, 2000)
    } else {
      bodySaveError.value = `Failed to save (${response.status}).`
    }
  } catch {
    bodySaveError.value = 'Could not connect to the server.'
  } finally {
    bodySaving.value = false
  }
}

function onMarkdownPaneInput() {
  clearTimeout(markdownApplyDebounce)
  markdownApplyDebounce = setTimeout(() => {
    crepeInstance?.editor.action(replaceAll(bodyMarkdown.value))
  }, 350)
  clearTimeout(bodyDebounce)
  bodyDebounce = setTimeout(persistBody, 1000)
}

// ── Label input interactions ─────────────────────────────────────────────────

function focusLabelInput() {
  labelInput.value?.focus()
}

function commitLabel(text) {
  const trimmed = text.trim()
  if (trimmed && !selectedLabels.value.includes(trimmed)) {
    selectedLabels.value.push(trimmed)
    persistLabels()
  }
  labelQuery.value      = ''
  suggestions.value     = []
  highlightedIndex.value = -1
}

function removeLabel(index) {
  selectedLabels.value.splice(index, 1)
  persistLabels()
}

function selectSuggestion(text) {
  commitLabel(text)
  labelInput.value?.focus()
}

function onLabelBlur() {
  setTimeout(() => {
    labelInputFocused.value = false
    suggestions.value = []
    if (labelQuery.value.trim()) commitLabel(labelQuery.value)
  }, 150)
}

function onLabelKeydown(e) {
  if (e.key === 'Enter' || e.key === ',') {
    e.preventDefault()
    if (highlightedIndex.value >= 0 && suggestions.value[highlightedIndex.value]) {
      selectSuggestion(suggestions.value[highlightedIndex.value].labelText)
    } else {
      commitLabel(labelQuery.value)
    }
  } else if (e.key === 'ArrowDown') {
    e.preventDefault()
    highlightedIndex.value = Math.min(highlightedIndex.value + 1, suggestions.value.length - 1)
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    highlightedIndex.value = Math.max(highlightedIndex.value - 1, 0)
  } else if (e.key === 'Backspace' && labelQuery.value === '') {
    if (selectedLabels.value.length > 0) {
      selectedLabels.value.pop()
      persistLabels()
    }
  } else if (e.key === 'Escape') {
    suggestions.value      = []
    highlightedIndex.value = -1
  }
}

// ── Label autocomplete ────────────────────────────────────────────────────────

watch(labelQuery, (val) => {
  clearTimeout(debounceTimer)
  highlightedIndex.value = -1
  if (val.trim().length < 1) { suggestions.value = []; return }
  debounceTimer = setTimeout(() => fetchSuggestions(val.trim()), 300)
})

async function fetchSuggestions(query) {
  try {
    const res = await searchLabels(projectId.value, query)
    if (!res.ok) { suggestions.value = []; return }
    const all = await res.json()
    suggestions.value = all.filter(s => !selectedLabels.value.includes(s.labelText))
  } catch {
    suggestions.value = []
  }
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

.title-wrap {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  min-width: 0;
}

.title-input {
  background: transparent;
  border: 1px solid transparent;
  border-radius: 6px;
  color: var(--text-primary);
  font-size: 1.4rem;
  font-weight: 700;
  font-family: inherit;
  outline: none;
  padding: 0.1rem 0.4rem;
  min-width: 0;
  flex: 1;
  transition: border-color 0.2s;
}
.title-input:hover:not(:disabled) { border-color: var(--border-color); }
.title-input:focus                 { border-color: var(--color-purple); }
.title-input:disabled              { opacity: 0.6; cursor: default; }

.title-status {
  font-size: 0.9rem;
  display: flex;
  align-items: center;
  flex-shrink: 0;
}
.title-status.saving { color: var(--text-muted); }
.title-status.saved  { color: #4ade80; }
.title-status.error  { color: var(--color-pink-light); cursor: default; }

/* Breadcrumbs + labels row */
.breadcrumbs-row {
  display: flex;
  align-items: center;
  gap: 80px;
  margin-bottom: 1.25rem;
}

.breadcrumbs {
  display: flex;
  align-items: center;
  gap: 0.25rem;
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

/* Recycle bin banner */
.recycle-banner {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  background: rgba(236, 72, 153, 0.07);
  border: 1px solid rgba(236, 72, 153, 0.3);
  border-radius: 10px;
  padding: 0.85rem 1.1rem;
  margin-bottom: 1.5rem;
}

.recycle-banner-icon {
  color: var(--color-pink);
  font-size: 1rem;
  margin-top: 0.15rem;
  flex-shrink: 0;
}

.recycle-banner-body {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.recycle-banner-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-pink-light);
}

.recycle-banner-detail {
  font-size: 0.825rem;
  color: var(--text-secondary);
}

/* Labels inline */
.labels-inline {
  position: relative;
  flex: 1;
}

.section-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.save-status {
  font-size: 0.75rem;
  display: flex;
  align-items: center;
  gap: 0.3rem;
}
.save-status.saving { color: var(--text-muted); }
.save-status.saved  { color: #4ade80; }
.save-status.error  { color: var(--color-pink-light); }

.label-input-box {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem;
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 0.25rem 0.6rem;
  cursor: text;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.label-input-box.focused {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 3px rgba(168, 85, 247, 0.15);
}

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

.chip-remove {
  background: none;
  border: none;
  color: var(--color-purple);
  cursor: pointer;
  padding: 0;
  line-height: 1;
  opacity: 0.7;
  display: flex;
  align-items: center;
}
.chip-remove .pi { font-size: 0.7rem; }
.chip-remove:hover { opacity: 1; }

.label-text-input {
  flex: 1;
  min-width: 8rem;
  background: none;
  border: none;
  outline: none;
  color: var(--text-primary);
  font-size: 0.875rem;
  font-family: inherit;
  padding: 0.15rem 0;
}
.label-text-input::placeholder { color: var(--text-dim); }

.suggestions {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  margin-top: 2px;
  background: var(--bg-card);
  border: 1px solid var(--border-purple);
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
  overflow: hidden;
  z-index: 10;
}

.suggestion-item {
  display: block;
  width: 100%;
  text-align: left;
  background: none;
  border: none;
  color: var(--text-primary);
  font-size: 0.875rem;
  font-family: inherit;
  padding: 0.55rem 0.85rem;
  cursor: pointer;
  transition: background 0.15s;
}
.suggestion-item:hover,
.suggestion-item.highlighted {
  background: var(--bg-active);
  color: var(--color-purple);
}

/* Body editor */
.body-section {
  position: relative;
}

.body-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.4rem;
}

.editor-container {
  display: flex;
  align-items: stretch;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  overflow: hidden;
  transition: border-color 0.2s;
}
.editor-container:focus-within {
  border-color: var(--color-purple);
}

.wysiwyg-pane {
  flex: 1;
  min-width: 0;
  min-height: 400px;
  max-height: 70vh;
  overflow-y: auto;
  padding: 0.75rem 1rem;
  border-right: 1px solid var(--border-color);
}

.markdown-pane {
  flex: 1;
  min-width: 0;
  min-height: 400px;
  max-height: 70vh;
  background: var(--bg-card);
  color: var(--text-secondary);
  border: none;
  outline: none;
  resize: none;
  padding: 0.75rem 1rem;
  font-family: 'Fira Code', Menlo, Monaco, 'Courier New', Courier, monospace;
  font-size: 0.85rem;
  line-height: 1.6;
}

/* Map Crepe's theme vars onto Apotheca's palette (already light/dark aware) */
:deep(.milkdown) {
  --crepe-color-background: var(--bg-card);
  --crepe-color-on-background: var(--text-primary);
  --crepe-color-surface: var(--bg-card);
  --crepe-color-surface-low: var(--bg-input);
  --crepe-color-on-surface: var(--text-primary);
  --crepe-color-on-surface-variant: var(--text-secondary);
  --crepe-color-outline: var(--border-color);
  --crepe-color-primary: var(--color-purple);
  --crepe-color-secondary: var(--color-pink);
  --crepe-color-on-secondary: var(--text-primary);
  --crepe-color-hover: var(--bg-hover);
  --crepe-color-selected: var(--bg-active);
  --crepe-color-inline-area: var(--bg-input);
  --crepe-font-default: inherit;
  background: var(--bg-card);
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
  .editor-container { flex-direction: column; }
  .wysiwyg-pane {
    border-right: none;
    border-bottom: 1px solid var(--border-color);
    max-height: none;
  }
  .markdown-pane { max-height: none; }
}
</style>
