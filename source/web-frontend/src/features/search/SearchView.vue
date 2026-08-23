<template>
  <div class="page-layout">
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <AccountSidebar :open="sidebarOpen" @close="sidebarOpen = false" />

    <div class="main-body">

      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">Search</h1>
        </div>
      </div>

      <div class="search-controls">
        <div class="search-input-wrap">
          <i class="pi pi-search search-icon"></i>
          <input
            v-model="query"
            class="search-input"
            placeholder="Type to search…"
            @input="onQueryInput"
            autocomplete="off"
          />
          <button v-if="query" class="clear-btn" @click="clearSearch">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <div class="filter-row">
          <div class="filter-group">
            <span class="filter-label">Search in</span>
            <MultiSelect
              v-model="selectedTypes"
              :options="typeOptions"
              option-label="label"
              option-value="value"
              placeholder="All types"
              class="filter-select"
              @change="runSearch"
            />
          </div>
          <div class="filter-group">
            <span class="filter-label">Match on</span>
            <MultiSelect
              v-model="selectedFields"
              :options="fieldOptions"
              option-label="label"
              option-value="value"
              placeholder="All fields"
              class="filter-select"
              @change="runSearch"
            />
          </div>
        </div>
      </div>

      <div v-if="loading" class="search-state">
        <i class="pi pi-spin pi-spinner"></i> Searching…
      </div>

      <div v-else-if="searched && results.length === 0" class="search-state muted">
        No results found.
      </div>

      <template v-else-if="results.length > 0">
        <div class="results-count">
          {{ results.length }}{{ results.length === 50 ? '+' : '' }}
          result{{ results.length !== 1 ? 's' : '' }}
        </div>

        <div class="result-list">
          <div
            v-for="result in results"
            :key="result.referenceId + result.referenceType"
            class="result-card"
            :class="{ clickable: !!result.projectId }"
            @click="navigateTo(result)"
          >
            <div class="result-top">
              <span class="type-badge" :class="result.referenceType.toLowerCase()">
                <i :class="typeIcon(result.referenceType)"></i>
                {{ typeLabel(result.referenceType) }}
              </span>
              <span class="result-title">{{ result.title }}</span>
            </div>
            <div v-if="result.snippet" class="result-snippet" v-html="result.snippet"></div>
          </div>
        </div>
      </template>

    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import MultiSelect from 'primevue/multiselect'
import AccountSidebar from '../../components/AccountSidebar.vue'
import { useAuth } from '../../composables/useAuth'
import { useWorkspaces } from '../../composables/useWorkspaces'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

const { user }  = useAuth()
const { currentWorkspace } = useWorkspaces()
const router    = useRouter()

const sidebarOpen   = ref(window.innerWidth >= 768)
const query         = ref('')
const selectedTypes  = ref(['note', 'task', 'document', 'mindmap'])
const selectedFields = ref(['title', 'body'])
const results       = ref([])
const loading       = ref(false)
const searched      = ref(false)
let debounceTimer   = null

const typeOptions  = [
  { label: 'Notes',     value: 'note'     },
  { label: 'Tasks',     value: 'task'     },
  { label: 'Documents', value: 'document' },
  { label: 'Mindmaps',  value: 'mindmap'  },
]
const fieldOptions = [
  { label: 'Title', value: 'title' },
  { label: 'Body',  value: 'body'  },
]

function typeLabel(t) {
  switch (t.toLowerCase()) {
    case 'note':     return 'Note'
    case 'task':     return 'Task'
    case 'document': return 'Document'
    case 'mindmap':  return 'Mindmap'
    default:         return t
  }
}
function typeIcon(t) {
  switch (t.toLowerCase()) {
    case 'note':     return 'pi pi-file-edit'
    case 'task':     return 'pi pi-check-square'
    case 'document': return 'pi pi-file'
    case 'mindmap':  return 'pi pi-sitemap'
    default:         return 'pi pi-circle'
  }
}

function navigateTo(result) {
  if (!result.projectId || !currentWorkspace.value) return
  const base = `/workspace/${currentWorkspace.value.id}/project/${result.projectId}`
  const type = result.referenceType.toLowerCase()
  if (type === 'note') {
    router.push(`${base}/notes/${result.referenceId}`)
  } else if (type === 'document') {
    router.push(`${base}/documents/${result.referenceId}`)
  } else if (type === 'mindmap') {
    router.push(`${base}/mindmaps/${result.referenceId}`)
  }
}

function onQueryInput() {
  clearTimeout(debounceTimer)
  if (!query.value || query.value.trim().length < 2) {
    results.value  = []
    searched.value = false
    return
  }
  debounceTimer = setTimeout(runSearch, 300)
}

async function runSearch() {
  if (!query.value || query.value.trim().length < 2) return
  if (!user.value) return

  const types  = selectedTypes.value.length  > 0 ? selectedTypes.value.join(',')  : 'note,task,document,mindmap'
  const fields = selectedFields.value.length > 0 ? selectedFields.value.join(',') : 'title,body'

  loading.value  = true
  searched.value = false
  try {
    const token = await user.value.getIdToken()
    const url   = new URL(`${API_URL}/search`)
    url.searchParams.set('q',      query.value.trim())
    url.searchParams.set('types',  types)
    url.searchParams.set('fields', fields)

    const response = await fetch(url.toString(), {
      headers: { Authorization: `Bearer ${token}` },
    })

    results.value = response.ok ? await response.json() : []
  } catch {
    results.value = []
  } finally {
    loading.value  = false
    searched.value = true
  }
}

function clearSearch() {
  query.value    = ''
  results.value  = []
  searched.value = false
  clearTimeout(debounceTimer)
}
</script>

<style scoped>
/* ── Page layout (sidebar + body) ── */
.page-layout {
  display: flex;
  flex: 1;
  overflow: hidden;
  height: calc(100vh - 60px);
}

.sidebar-backdrop { display: none; }

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
  margin-bottom: 1.75rem;
}

.content-header-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.content-title {
  font-size: 1.4rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
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

/* ── Controls ── */
.search-controls {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-bottom: 1.75rem;
}

.search-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.search-icon {
  position: absolute;
  left: 1rem;
  color: var(--text-muted);
  font-size: 0.95rem;
  pointer-events: none;
  z-index: 1;
}

.search-input {
  width: 100%;
  padding: 0.65rem 2.75rem;
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-primary);
  font-size: 0.95rem;
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
  font-family: inherit;
}

.search-input:focus {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 3px var(--glow-purple);
}

.search-input::placeholder {
  color: var(--text-dim);
}

.clear-btn {
  position: absolute;
  right: 0.75rem;
  background: none;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  padding: 0.25rem;
  display: flex;
  align-items: center;
  border-radius: 4px;
  transition: color 0.15s;
  font-size: 0.8rem;
}
.clear-btn:hover { color: var(--text-primary); }

/* ── Filters ── */
.filter-row {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  align-items: center;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.filter-label {
  font-size: 0.8rem;
  color: var(--text-muted);
  white-space: nowrap;
}

.filter-select {
  min-width: 155px;
}

/* ── States ── */
.search-state {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: var(--text-muted);
  font-size: 0.9rem;
  padding: 2.5rem 0;
}
.search-state.muted {
  color: var(--text-dim);
  justify-content: center;
}

/* ── Results ── */
.results-count {
  font-size: 0.75rem;
  color: var(--text-dim);
  text-transform: uppercase;
  letter-spacing: 0.06em;
  margin-bottom: 0.75rem;
}

.result-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.result-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 0.85rem 1.25rem;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.result-card.clickable {
  cursor: pointer;
}
.result-card.clickable:hover {
  border-color: var(--color-purple);
  box-shadow: 0 0 12px var(--glow-purple);
}

.result-top {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.4rem;
}

.type-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.18rem 0.55rem;
  border-radius: 4px;
  font-size: 0.7rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  white-space: nowrap;
  flex-shrink: 0;
}
.type-badge.note {
  background: rgba(168, 85, 247, 0.12);
  color: var(--color-purple-light);
  border: 1px solid rgba(168, 85, 247, 0.25);
}
.type-badge.task {
  background: rgba(236, 72, 153, 0.12);
  color: var(--color-pink-light);
  border: 1px solid rgba(236, 72, 153, 0.25);
}
.type-badge.document {
  background: rgba(56, 189, 248, 0.12);
  color: #7dd3fc;
  border: 1px solid rgba(56, 189, 248, 0.25);
}
.type-badge.mindmap {
  background: rgba(52, 211, 153, 0.12);
  color: #6ee7b7;
  border: 1px solid rgba(52, 211, 153, 0.25);
}

.result-title {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.result-snippet {
  font-size: 0.82rem;
  color: var(--text-secondary);
  line-height: 1.55;
}

.result-snippet :deep(b) {
  color: var(--color-purple-light);
  font-weight: 600;
}

/* ── Mobile ── */
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

  .filter-row { gap: 0.5rem; }

  .filter-group { flex-direction: column; align-items: flex-start; }

  .filter-select { min-width: 100%; }
}
</style>
