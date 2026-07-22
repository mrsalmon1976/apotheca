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
          <input
            v-if="!loadError"
            v-model="mindmapName"
            class="mindmap-name-input"
            type="text"
            placeholder="Untitled Mindmap"
            @keydown.enter.prevent="$event.target.blur()"
            @blur="saveMindmapName"
          />
          <h1 v-else class="content-title">Mindmap</h1>
        </div>
      </div>

      <nav class="breadcrumbs">
        <button class="breadcrumb-item" @click="router.push(`/project/${projectId}/mindmaps`)">
          Mindmaps
        </button>
        <template v-if="root">
          <i class="pi pi-chevron-right breadcrumb-sep"></i>
          <span class="breadcrumb-item breadcrumb-current">{{ mindmapName || 'Untitled' }}</span>
        </template>
      </nav>

      <div v-if="loadError" class="load-error">
        <i class="pi pi-exclamation-triangle"></i>
        <span>{{ loadError }}</span>
      </div>

      <div v-else-if="loading" class="loading-state">
        <i class="pi pi-spin pi-spinner"></i>
        <span>Loading...</span>
      </div>

      <div v-else class="mindmap-canvas">
        <ul class="mindmap-tree">
          <MindmapNode :node="root" :is-root="true" />
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, provide } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import MindmapNode from './MindmapNode.vue'
import { useMindmaps, buildMindmapTree } from '../../composables/useMindmaps'

const route = useRoute()
const router = useRouter()
const projectId = computed(() => route.params.id)
const mindmapId = computed(() => route.params.mindmapId)
const sidebarOpen = ref(window.innerWidth >= 768)

provide('projectId', projectId)
provide('mindmapId', mindmapId)

const { getMindmap, renameMindmap } = useMindmaps()

const root         = ref(null)
const mindmapName  = ref('')
const loading      = ref(true)
const loadError    = ref(null)

async function loadMindmap() {
  loading.value   = true
  loadError.value = null
  try {
    const response = await getMindmap(projectId.value, mindmapId.value)
    if (response.ok) {
      const data = await response.json()
      mindmapName.value = data.name
      root.value = buildMindmapTree(data.nodes)
    } else if (response.status === 404) {
      loadError.value = 'This mindmap no longer exists.'
    } else {
      loadError.value = `Failed to load mindmap (${response.status}).`
    }
  } catch {
    loadError.value = 'Could not connect to the server.'
  } finally {
    loading.value = false
  }
}

loadMindmap()

async function saveMindmapName() {
  await renameMindmap(projectId.value, mindmapId.value, mindmapName.value.trim() || 'Untitled Mindmap')
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
  overflow: auto;
  padding: 1.5rem 2rem;
  background: var(--bg-primary);
}

.content-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
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

.mindmap-name-input {
  background: transparent;
  border: 1px solid transparent;
  border-radius: 6px;
  color: var(--text-primary);
  font-size: 1.4rem;
  font-weight: 700;
  padding: 0.15rem 0.4rem;
  min-width: 0;
  transition: border-color 0.2s;
}
.mindmap-name-input:hover { border-color: var(--border-color); }
.mindmap-name-input:focus { border-color: var(--color-purple); }

/* Breadcrumbs */
.breadcrumbs { display: flex; align-items: center; gap: 0.25rem; margin-bottom: 1.5rem; flex-wrap: wrap; }

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

.mindmap-canvas {
  width: max-content;
  min-width: 100%;
  padding: 0 2rem 2rem 2rem;
}

.mindmap-tree {
  display: flex;
  justify-content: center;
  list-style: none;
  margin: 0;
  padding: 0;
}

.sidebar-backdrop { display: none; }

@media (max-width: 767px) {
  .sidebar-backdrop { display: block; position: fixed; inset: 0; top: 60px; background: rgba(0, 0, 0, 0.6); z-index: 99; }
  .main-body { padding: 1rem; }
}
</style>
