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
        <template v-for="(crumb, index) in folderCrumbs" :key="crumb.id">
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

      <!-- Placeholder content -->
      <div v-else class="note-placeholder">
        <i class="pi pi-file-edit placeholder-icon"></i>
        <p class="placeholder-title">Note editor coming soon</p>
        <p class="placeholder-subtitle">Note ID: <code>{{ noteId }}</code></p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import { useNoteFolders } from '../../composables/useNoteFolders'

const route  = useRoute()
const router = useRouter()

const projectId   = computed(() => route.params.id)
const noteId      = computed(() => route.params.noteId)
const sidebarOpen = ref(window.innerWidth >= 768)

const { getNote } = useNoteFolders()

const note        = ref(null)
const folderCrumbs = ref([])  // ancestor folders: [{ id, title }, ...]
const loading     = ref(false)
const loadError   = ref(null)

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

.note-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  padding: 5rem 0;
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
