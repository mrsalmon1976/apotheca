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
          <h1 class="content-title">{{ note?.title ?? 'Note' }}</h1>
        </div>
      </div>

      <!-- Breadcrumbs -->
      <nav v-if="!loadError" class="breadcrumbs">
        <button class="breadcrumb-item" @click="router.push(`/project/${projectId}/notes`)">
          Notes
        </button>
        <template v-for="crumb in folderCrumbs" :key="crumb.id">
          <i class="pi pi-chevron-right breadcrumb-sep"></i>
          <button
            class="breadcrumb-item"
            @click="router.push(`/project/${projectId}/notes?folderId=${crumb.id}`)"
          >
            {{ crumb.title }}
          </button>
        </template>
        <template v-if="note">
          <i class="pi pi-chevron-right breadcrumb-sep"></i>
          <span class="breadcrumb-item breadcrumb-current">{{ note.title }}</span>
        </template>
      </nav>

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

        <!-- Labels -->
        <div class="labels-section">
          <div class="labels-header">
            <span class="section-label">Labels</span>
            <span v-if="labelsSaving" class="save-status saving">
              <i class="pi pi-spin pi-spinner"></i> Saving…
            </span>
            <span v-else-if="labelsSaveError" class="save-status error">
              <i class="pi pi-exclamation-triangle"></i> {{ labelsSaveError }}
            </span>
            <span v-else-if="labelsSaved" class="save-status saved">
              <i class="pi pi-check"></i> Saved
            </span>
          </div>

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
          </div>

          <!-- Suggestions dropdown -->
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

        <!-- Placeholder body -->
        <div class="note-placeholder">
          <i class="pi pi-file-edit placeholder-icon"></i>
          <p class="placeholder-title">Note editor coming soon</p>
          <p class="placeholder-subtitle">Note ID: <code>{{ noteId }}</code></p>
        </div>

      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import { useNoteFolders } from '../../composables/useNoteFolders'

const route  = useRoute()
const router = useRouter()

const projectId   = computed(() => route.params.id)
const noteId      = computed(() => route.params.noteId)
const sidebarOpen = ref(window.innerWidth >= 768)

const { getNote, saveNote, searchLabels } = useNoteFolders()

// Note load state
const note         = ref(null)
const folderCrumbs = ref([])
const loading      = ref(false)
const loadError    = ref(null)

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
      note.value = await response.json()
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
})

onUnmounted(() => {
  clearTimeout(debounceTimer)
  clearTimeout(savedTimer)
})

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

.content-title {
  font-size: 1.4rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

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

/* Labels section */
.labels-section {
  margin-bottom: 1.5rem;
  position: relative;
}

.labels-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.4rem;
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
  padding: 0.45rem 0.75rem;
  cursor: text;
  min-height: 2.5rem;
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

/* Placeholder */
.note-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 4rem 0;
  color: var(--text-dim);
}

.placeholder-icon {
  font-size: 3rem;
  color: var(--color-purple);
  opacity: 0.4;
}

.placeholder-title {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--text-secondary);
  margin: 0;
}

.placeholder-subtitle {
  font-size: 0.875rem;
  color: var(--text-muted);
  margin: 0;
}

.placeholder-subtitle code {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 4px;
  padding: 0.1rem 0.4rem;
  font-family: monospace;
  color: var(--color-purple-light);
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
