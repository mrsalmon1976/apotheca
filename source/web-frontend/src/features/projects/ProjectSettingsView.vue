<template>
  <div class="page-layout">
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <ProjectSidebar :open="sidebarOpen" @close="sidebarOpen = false" />

    <div class="main-body">
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">Project Settings</h1>
        </div>
      </div>

      <div class="settings-container">
        <Tabs value="details" @update:value="onTabChange">
          <TabList>
            <Tab value="details">Details</Tab>
            <Tab value="activity">Activity</Tab>
          </TabList>
          <TabPanels>
            <TabPanel value="details">
              <div class="tab-content">
                <div class="field-block">
                  <label class="field-label" for="project-name">Project Name</label>
                  <InputText
                    id="project-name"
                    v-model="nameInput"
                    class="name-input"
                    placeholder="Project name"
                    :maxlength="100"
                  />
                </div>

                <div class="field-block">
                  <label class="field-label" for="project-summary">Summary</label>
                  <Textarea
                    id="project-summary"
                    v-model="summaryInput"
                    class="summary-input"
                    placeholder="Provide a brief description of this project…"
                    :rows="4"
                    auto-resize
                  />
                </div>

                <button
                  class="save-btn"
                  :disabled="!hasChanges || saving"
                  @click="save"
                >
                  <i v-if="saving" class="pi pi-spin pi-spinner"></i>
                  <span>{{ saving ? 'Saving…' : 'Save' }}</span>
                </button>
              </div>
            </TabPanel>

            <TabPanel value="activity">
              <div class="tab-content activity-tab">
                <div v-if="activityLoading" class="activity-loading">
                  <i class="pi pi-spin pi-spinner"></i> Loading…
                </div>
                <div v-else-if="activityError" class="activity-error">
                  {{ activityError }}
                </div>
                <div v-else-if="activityEntries.length === 0" class="activity-empty">
                  No activity yet.
                </div>
                <table v-else class="activity-table">
                  <thead>
                    <tr>
                      <th>Ref</th>
                      <th>Type</th>
                      <th>Message</th>
                      <th>User</th>
                      <th>Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="entry in activityEntries" :key="entry.id">
                      <td>
                        <router-link :to="refLink(entry)" class="ref-link">
                          {{ entry.refId.slice(0, 5) }}…
                        </router-link>
                      </td>
                      <td>
                        <span :class="['ref-type-badge', entry.refType.toLowerCase()]">
                          {{ entry.refType }}
                        </span>
                      </td>
                      <td class="message-cell">{{ entry.logMessage }}</td>
                      <td>{{ entry.username }}</td>
                      <td class="date-cell">{{ formatDate(entry.createdAt) }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Tabs from 'primevue/tabs'
import TabList from 'primevue/tablist'
import Tab from 'primevue/tab'
import TabPanels from 'primevue/tabpanels'
import TabPanel from 'primevue/tabpanel'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import ProjectSidebar from '../../components/ProjectSidebar.vue'
import { useProjects } from '../../composables/useProjects'
import { useAuth } from '../../composables/useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

const route = useRoute()
const toast = useToast()
const { projects, saveProject } = useProjects()
const { user } = useAuth()

const sidebarOpen = ref(window.innerWidth >= 768)
const projectId = computed(() => route.params.id)
const currentProject = computed(() => projects.value.find(p => p.id === projectId.value))

// --- Details tab ---
const nameInput = ref('')
const summaryInput = ref('')
const saving = ref(false)

watch(currentProject, (p) => {
  if (p) {
    nameInput.value = p.name
    summaryInput.value = p.summary ?? ''
  }
}, { immediate: true })

const hasChanges = computed(() => {
  const p = currentProject.value
  if (!p) return false
  return nameInput.value.trim() !== '' && (
    nameInput.value.trim() !== p.name ||
    (summaryInput.value.trim() || null) !== (p.summary ?? null)
  )
})

async function save() {
  if (!hasChanges.value) return
  saving.value = true
  const success = await saveProject(
    projectId.value,
    nameInput.value.trim(),
    summaryInput.value.trim() || null,
  )
  if (success) {
    toast.add({ severity: 'success', summary: 'Project saved', life: 3000 })
  }
  saving.value = false
}

// --- Activity tab ---
const activityEntries = ref([])
const activityLoading = ref(false)
const activityLoaded = ref(false)
const activityError = ref(null)

async function loadActivity() {
  if (activityLoaded.value) return
  activityLoading.value = true
  activityError.value = null
  try {
    const token = await user.value.getIdToken()
    const response = await fetch(`${API_URL}/projects/${projectId.value}/activity`, {
      headers: { Authorization: `Bearer ${token}` },
    })
    if (response.ok) {
      activityEntries.value = await response.json()
      activityLoaded.value = true
    } else {
      activityError.value = `Failed to load activity (${response.status}).`
    }
  } catch {
    activityError.value = 'Could not connect to the server.'
  } finally {
    activityLoading.value = false
  }
}

function onTabChange(tab) {
  if (tab === 'activity') loadActivity()
}

function refLink(entry) {
  if (entry.refType === 'NOTE') return `/project/${projectId.value}/notes/${entry.refId}`
  return `/project/${entry.refId}`
}

function formatDate(iso) {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
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

.settings-container {
  max-width: 900px;
}

.tab-content {
  padding: 1.5rem 0 0;
}

.field-block {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1.25rem;
}

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--text-muted);
}

.name-input,
.summary-input {
  width: 100%;
}

:deep(.name-input.p-inputtext),
:deep(.summary-input.p-textarea) {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  border-radius: 8px;
  padding: 0.5rem 0.875rem;
  font-size: 0.9rem;
  width: 100%;
  transition: border-color 0.2s, box-shadow 0.2s;
}
:deep(.name-input.p-inputtext:focus),
:deep(.summary-input.p-textarea:focus) {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 1px var(--color-purple);
  outline: none;
}

.save-btn {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 1.25rem;
  background: var(--gradient-brand);
  border: none;
  border-radius: 8px;
  color: white;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s, box-shadow 0.2s;
  box-shadow: 0 0 12px var(--glow-purple);
  white-space: nowrap;
}
.save-btn:hover:not(:disabled) { opacity: 0.9; box-shadow: 0 0 20px var(--glow-purple); }
.save-btn:disabled { opacity: 0.4; cursor: default; box-shadow: none; }

/* Activity tab */
.activity-tab {
  padding-top: 1.25rem;
}

.activity-loading,
.activity-error,
.activity-empty {
  color: var(--text-muted);
  font-size: 0.875rem;
  padding: 1rem 0;
}

.activity-error { color: #f87171; }

.activity-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;
}

.activity-table th {
  text-align: left;
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--text-muted);
  padding: 0.4rem 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.activity-table td {
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid color-mix(in srgb, var(--border-color) 50%, transparent);
  color: var(--text-secondary);
  vertical-align: middle;
}

.activity-table tbody tr:hover td {
  background: var(--bg-card);
}

.ref-link {
  font-family: monospace;
  font-size: 0.8rem;
  color: var(--color-purple);
  text-decoration: none;
  letter-spacing: 0.03em;
}
.ref-link:hover { color: var(--color-pink); text-decoration: underline; }

.ref-type-badge {
  display: inline-block;
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  padding: 0.15rem 0.45rem;
  border-radius: 4px;
  text-transform: uppercase;
}
.ref-type-badge.note    { background: color-mix(in srgb, var(--color-purple) 15%, transparent); color: var(--color-purple-light); }
.ref-type-badge.project { background: color-mix(in srgb, var(--color-pink) 15%, transparent);   color: var(--color-pink-light); }

.message-cell { color: var(--text-primary); }

.date-cell {
  white-space: nowrap;
  color: var(--text-muted);
  font-size: 0.8rem;
}

/* PrimeVue Tabs theming */
:deep(.p-tabs) {
  background: transparent;
}

:deep(.p-tablist) {
  background: transparent;
  border-bottom: 1px solid var(--border-color);
}

:deep(.p-tab) {
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 0.875rem;
  font-weight: 500;
  padding: 0.6rem 1rem;
  cursor: pointer;
  transition: color 0.2s;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
}
:deep(.p-tab:hover) { color: var(--text-primary); }
:deep(.p-tab[data-p-active="true"]) {
  color: var(--color-pink);
  border-bottom-color: var(--color-purple);
}

:deep(.p-tabpanels) {
  background: transparent;
  padding: 0;
}

:deep(.p-tabpanel) {
  padding: 0;
}

/* Mobile */
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
  .save-btn { justify-content: center; }
}
</style>
