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
          <h1 class="content-title">Mindmap</h1>
        </div>
      </div>

      <nav class="breadcrumbs">
        <button class="breadcrumb-item" @click="router.push(`/project/${projectId}/mindmaps`)">
          Mindmaps
        </button>
        <template v-if="root">
          <i class="pi pi-chevron-right breadcrumb-sep"></i>
          <span class="breadcrumb-item breadcrumb-current">{{ root.header || 'Untitled' }}</span>
        </template>
      </nav>

      <div v-if="notFound" class="load-error">
        <i class="pi pi-exclamation-triangle"></i>
        <span>This mindmap no longer exists.</span>
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
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import MindmapNode from './MindmapNode.vue'
import { useMindmapEditor } from '../../composables/useMindmaps'

const route = useRoute()
const router = useRouter()
const projectId = computed(() => route.params.id)
const mindmapId = computed(() => route.params.mindmapId)
const sidebarOpen = ref(window.innerWidth >= 768)

const { root, notFound } = useMindmapEditor(projectId.value, mindmapId.value)
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
