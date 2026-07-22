<template>
  <div class="header-search" ref="containerRef">
    <div class="hs-input-wrap">
      <i class="pi pi-search hs-icon"></i>
      <input
        v-model="query"
        class="hs-input"
        placeholder="Search…"
        autocomplete="off"
        @input="onInput"
        @focus="onFocus"
        @keydown.escape="close"
      />
      <i v-if="loading" class="pi pi-spin pi-spinner hs-spinner"></i>
      <button v-else-if="query" class="hs-clear" @mousedown.prevent @click="clear">
        <i class="pi pi-times"></i>
      </button>
    </div>

    <Transition name="hs-drop">
      <div v-if="open" class="hs-dropdown">

        <template v-if="results.length > 0">
          <div
            v-for="result in results"
            :key="result.referenceId + result.referenceType"
            class="hs-item"
            @mousedown.prevent
            @click="navigate(result)"
          >
            <span class="hs-badge" :class="result.referenceType.toLowerCase()">
              <i :class="typeIcon(result.referenceType)"></i>
              {{ typeLabel(result.referenceType) }}
            </span>
            <span class="hs-title">{{ result.title }}</span>
          </div>
        </template>

        <div v-else-if="searched && !loading" class="hs-empty">
          No results found.
        </div>

        <div class="hs-footer">
          <RouterLink to="/search" class="hs-advanced" @click="close">
            <i class="pi pi-search"></i>
            Advanced search
          </RouterLink>
        </div>

      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { useAuth } from '../composables/useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

const { user }    = useAuth()
const route       = useRoute()
const router      = useRouter()

const containerRef = ref(null)
const query        = ref('')
const results      = ref([])
const loading      = ref(false)
const searched     = ref(false)
const open         = ref(false)
let   debounceTimer = null

// Close when the user navigates away
watch(() => route.path, () => { open.value = false })

function handleDocumentClick(e) {
  if (!containerRef.value?.contains(e.target)) open.value = false
}
onMounted(() => document.addEventListener('click', handleDocumentClick))
onUnmounted(() => {
  document.removeEventListener('click', handleDocumentClick)
  clearTimeout(debounceTimer)
})

function onFocus() {
  if (query.value.trim().length >= 2) open.value = true
}

function onInput() {
  clearTimeout(debounceTimer)
  if (!query.value || query.value.trim().length < 2) {
    results.value  = []
    searched.value = false
    open.value     = false
    return
  }
  open.value    = true
  debounceTimer = setTimeout(runSearch, 300)
}

async function runSearch() {
  if (!user.value || !query.value || query.value.trim().length < 2) return

  loading.value  = true
  searched.value = false
  try {
    const token = await user.value.getIdToken()
    const url   = new URL(`${API_URL}/search`)
    url.searchParams.set('q', query.value.trim())

    const response = await fetch(url.toString(), {
      headers: { Authorization: `Bearer ${token}` },
    })
    results.value = response.ok ? (await response.json()).slice(0, 10) : []
  } catch {
    results.value = []
  } finally {
    loading.value  = false
    searched.value = true
  }
}

function navigate(result) {
  if (!result.projectId) return
  const type = result.referenceType.toLowerCase()
  if (type === 'note') {
    router.push(`/project/${result.projectId}/notes/${result.referenceId}`)
  } else if (type === 'task') {
    router.push(`/project/${result.projectId}/tasks/all`)
  } else if (type === 'document') {
    router.push(`/project/${result.projectId}/documents/${result.referenceId}`)
  } else if (type === 'mindmap') {
    router.push(`/project/${result.projectId}/mindmaps/${result.referenceId}`)
  }
  close()
}

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

function clear() {
  query.value    = ''
  results.value  = []
  searched.value = false
  open.value     = false
  clearTimeout(debounceTimer)
}

function close() {
  open.value = false
}
</script>

<style scoped>
.header-search {
  position: relative;
  flex: 1;
  min-width: 0;
  margin-left: 20px;
}

/* ── Input ── */
.hs-input-wrap {
  position: relative;
  display: flex;
  align-items: center;
}

.hs-icon {
  position: absolute;
  left: 0.75rem;
  color: var(--text-muted);
  font-size: 0.85rem;
  pointer-events: none;
  z-index: 1;
}

.hs-input {
  width: 100%;
  padding: 0.45rem 2.25rem;
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-primary);
  font-size: 0.875rem;
  font-family: inherit;
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.hs-input:focus {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 3px var(--glow-purple);
}

.hs-input::placeholder {
  color: var(--text-dim);
}

.hs-spinner {
  position: absolute;
  right: 0.75rem;
  color: var(--text-muted);
  font-size: 0.8rem;
}

.hs-clear {
  position: absolute;
  right: 0.5rem;
  background: none;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  padding: 0.2rem;
  display: flex;
  align-items: center;
  border-radius: 4px;
  font-size: 0.75rem;
  transition: color 0.15s;
}
.hs-clear:hover { color: var(--text-primary); }

/* ── Dropdown ── */
.hs-dropdown {
  position: absolute;
  top: calc(100% + 6px);
  left: 0;
  right: 0;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
  z-index: 1000;
  overflow: hidden;
}

.hs-item {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.55rem 0.85rem;
  cursor: pointer;
  transition: background 0.1s;
}
.hs-item:hover {
  background: var(--bg-hover);
}

.hs-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.15rem 0.45rem;
  border-radius: 4px;
  font-size: 0.65rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  white-space: nowrap;
  flex-shrink: 0;
}
.hs-badge.note {
  background: rgba(168, 85, 247, 0.12);
  color: var(--color-purple-light);
  border: 1px solid rgba(168, 85, 247, 0.25);
}
.hs-badge.task {
  background: rgba(236, 72, 153, 0.12);
  color: var(--color-pink-light);
  border: 1px solid rgba(236, 72, 153, 0.25);
}
.hs-badge.document {
  background: rgba(56, 189, 248, 0.12);
  color: #7dd3fc;
  border: 1px solid rgba(56, 189, 248, 0.25);
}
.hs-badge.mindmap {
  background: rgba(52, 211, 153, 0.12);
  color: #6ee7b7;
  border: 1px solid rgba(52, 211, 153, 0.25);
}

.hs-title {
  font-size: 0.85rem;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.hs-empty {
  padding: 0.75rem 0.85rem;
  font-size: 0.82rem;
  color: var(--text-dim);
}

/* ── Footer ── */
.hs-footer {
  border-top: 1px solid var(--border-color);
  padding: 0.45rem 0.85rem;
}

.hs-advanced {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.78rem;
  color: var(--text-muted);
  text-decoration: none;
  transition: color 0.15s;
}
.hs-advanced:hover {
  color: var(--color-purple-light);
}
.hs-advanced .pi {
  font-size: 0.75rem;
}

/* ── Transition ── */
.hs-drop-enter-active,
.hs-drop-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.hs-drop-enter-from,
.hs-drop-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
