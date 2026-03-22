<template>
  <div class="project-menu">
    <Select
      v-model="selectedProject"
      :options="projects"
      option-label="name"
      :placeholder="projectsLoading ? 'Loading...' : 'Jump to project...'"
      :disabled="projectsLoading"
      class="project-select"
      :class="{ active: selectedProject }"
      @update:modelValue="navigateToProject"
    >
      <template #value="{ value, placeholder }">
        <div class="select-value">
          <i class="pi pi-folder"></i>
          <span>{{ value?.name ?? placeholder }}</span>
        </div>
      </template>
      <template #option="{ option }">
        <div class="select-option-item">
          <i class="pi pi-folder"></i>
          <span>{{ option.name }}</span>
        </div>
      </template>
    </Select>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import Select from 'primevue/select'
import { useProjects } from '../composables/useProjects'

const { projects, loading: projectsLoading, loadProjects } = useProjects()
const router = useRouter()
const route = useRoute()

const selectedProject = ref(null)
const isProjectRoute = computed(() => route.path.startsWith('/project/'))

watch(isProjectRoute, (onProject) => {
  if (!onProject) selectedProject.value = null
})

watch(
  [() => route.params.id, projects],
  ([id]) => {
    if (id) selectedProject.value = projects.value.find(p => p.id === id) ?? null
  },
  { immediate: true }
)

onMounted(loadProjects)

function navigateToProject(project) {
  if (project) router.push(`/project/${project.id}`)
}
</script>

<style scoped>
.project-menu {
  display: contents;
}

:deep(.project-select) {
  height: 36px;
  min-width: 180px;
  background: var(--bg-input);
  border: 1px solid var(--border-purple);
  border-radius: 8px;
  transition: border-color 0.2s, box-shadow 0.2s;
}

:deep(.project-select:hover) {
  border-color: var(--border-purple);
}

:deep(.project-select.p-focus) {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 1px var(--color-purple);
  outline: none;
}

:deep(.project-select .p-select-label) {
  font-size: 0.875rem;
  color: var(--text-secondary);
  padding: 0 0.75rem;
  line-height: 34px;
}

:deep(.project-select .p-select-label.p-placeholder) {
  color: var(--text-muted);
}

:deep(.project-select .p-select-dropdown) {
  color: var(--text-muted);
  width: 2rem;
}

:deep(.project-select.active) {
  background: var(--bg-active);
  border-color: transparent;
  box-shadow: 0 0 12px var(--glow-pink);
}

:deep(.project-select.active .p-select-label) {
  color: var(--color-pink);
}

:deep(.project-select.active .p-select-dropdown) {
  color: var(--color-pink);
}

.project-select.active .select-value .pi {
  color: var(--color-pink);
}

.select-value,
.select-option-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.select-value .pi,
.select-option-item .pi {
  color: var(--color-purple);
  font-size: 0.85rem;
}
</style>
