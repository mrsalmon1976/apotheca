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
        <Tabs value="details">
          <TabList>
            <Tab value="details">Details</Tab>
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

const route = useRoute()
const toast = useToast()
const { projects, saveProject } = useProjects()

const sidebarOpen = ref(window.innerWidth >= 768)
const projectId = computed(() => route.params.id)
const currentProject = computed(() => projects.value.find(p => p.id === projectId.value))

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
  max-width: 600px;
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
