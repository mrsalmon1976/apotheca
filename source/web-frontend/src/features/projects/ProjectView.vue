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
          <h1 class="content-title">Overview</h1>
        </div>
      </div>

      <!-- Stats -->
      <div class="stats-grid">
        <div v-for="stat in stats" :key="stat.label" class="stat-card">
          <div class="stat-icon"><i :class="`pi ${stat.icon}`"></i></div>
          <div class="stat-body">
            <span class="stat-value">{{ stat.value }}</span>
            <span class="stat-label">{{ stat.label }}</span>
          </div>
        </div>
      </div>

      <!-- Quick links -->
      <div class="section-title">Quick Links</div>
      <div class="quick-links">
        <button v-for="link in quickLinks" :key="link.label" class="quick-link-card" @click="router.push(link.to)">
          <i :class="`pi ${link.icon}`"></i>
          <span>{{ link.label }}</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useAuth } from '../../composables/useAuth'
import ProjectSidebar from '../../components/ProjectSidebar.vue'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const { user } = useAuth()
const projectId = computed(() => route.params.id)
const sidebarOpen = ref(window.innerWidth >= 768)

const openTaskCount = ref('—')
const noteCount     = ref('—')
const documentCount = ref('—')

const stats = computed(() => [
  { label: 'Your open tasks', value: openTaskCount.value, icon: 'pi-check-square' },
  { label: 'Notes',      value: noteCount.value,     icon: 'pi-file-edit' },
  { label: 'Documents',  value: documentCount.value, icon: 'pi-folder-open' },
])

async function loadOverview() {
  if (!user.value) return
  try {
    const token    = await user.value.getIdToken()
    const response = await fetch(`${API_URL}/projects/${projectId.value}/overview`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    if (response.ok) {
      const data      = await response.json()
      openTaskCount.value = data.openTaskCount
      noteCount.value     = data.noteCount
      documentCount.value = data.documentCount
    } else if (response.status === 403) {
      toast.add({ severity: 'error', summary: 'Access denied', detail: 'You do not have permission to view this project.', life: 10000 })
    } else {
      toast.add({ severity: 'error', summary: 'Failed to load overview', detail: `Server error (${response.status})`, life: 10000 })
    }
  } catch {
    toast.add({ severity: 'error', summary: 'Failed to load overview', detail: 'Could not connect to the server.', life: 10000 })
  }
}

onMounted(loadOverview)

const quickLinks = computed(() => [
  { label: 'Notes',      icon: 'pi-file-edit',     to: `/project/${projectId.value}/notes` },
  { label: 'Documents',  icon: 'pi-folder-open',   to: `/project/${projectId.value}/documents` },
  { label: 'All Tasks',  icon: 'pi-list',           to: `/project/${projectId.value}/tasks/all` },
  { label: 'Kanban',     icon: 'pi-objects-column', to: `/project/${projectId.value}/kanban` },
  { label: 'Backlog',    icon: 'pi-inbox',          to: `/project/${projectId.value}/backlog` },
  { label: 'Reports',    icon: 'pi-chart-bar',      to: `/project/${projectId.value}/reports` },
])
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
  margin-bottom: 1.5rem;
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

/* ── Stats ── */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
  gap: 1rem;
  margin-bottom: 2rem;
}

.stat-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  padding: 1.25rem 1.5rem;
  display: flex;
  align-items: center;
  gap: 1rem;
  transition: border-color 0.2s;
}
.stat-card:hover { border-color: var(--border-purple); }

.stat-icon {
  font-size: 1.4rem;
  color: var(--color-purple);
  flex-shrink: 0;
}

.stat-body {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.stat-value {
  font-size: 1.6rem;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1;
}

.stat-label {
  font-size: 0.75rem;
  color: var(--text-muted);
}

/* ── Quick links ── */
.section-title {
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--text-dim);
  margin-bottom: 0.75rem;
}

.quick-links {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}

.quick-link-card {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.6rem 1.1rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-secondary);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s;
}
.quick-link-card:hover {
  border-color: var(--color-purple);
  color: var(--text-primary);
  box-shadow: 0 0 10px var(--glow-purple);
}
.quick-link-card .pi {
  color: var(--color-purple);
  font-size: 0.9rem;
}

/* ── Mobile ── */
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
