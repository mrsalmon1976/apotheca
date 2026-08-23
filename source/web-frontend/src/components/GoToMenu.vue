<template>
  <div class="go-to-menu" ref="containerRef">
    <button class="go-to-btn" :class="{ active: open }" @click="toggleOpen">
      <i class="pi pi-compass"></i>
      <span>Go to...</span>
      <i class="pi pi-chevron-down go-to-caret" :class="{ open }"></i>
    </button>

    <Transition name="go-to-drop">
      <div v-if="open" class="go-to-dropdown">
        <div v-if="workspacesLoading" class="go-to-empty">Loading…</div>
        <div v-else-if="workspaces.length === 0" class="go-to-empty">No workspaces yet.</div>

        <div v-else class="go-to-list">
          <div v-for="workspace in workspaces" :key="workspace.id" class="go-to-group">
            <div
              class="go-to-row workspace-row"
              :class="{ current: workspace.id === currentWorkspace?.id }"
              @click="goToWorkspace(workspace)"
            >
              <button
                class="go-to-expand-btn"
                :title="isExpanded(workspace.id) ? 'Collapse' : 'Expand'"
                @click.stop="toggleExpand(workspace.id)"
              >
                <i class="pi" :class="isExpanded(workspace.id) ? 'pi-chevron-down' : 'pi-chevron-right'"></i>
              </button>
              <i class="pi pi-building go-to-icon"></i>
              <span class="go-to-label">{{ workspace.name }}</span>
            </div>

            <div v-if="isExpanded(workspace.id)" class="go-to-projects">
              <div v-if="projectsLoading[workspace.id]" class="go-to-empty nested">Loading…</div>
              <div v-else-if="(projectsByWorkspace[workspace.id] ?? []).length === 0" class="go-to-empty nested">
                No projects yet.
              </div>
              <div
                v-else
                v-for="project in projectsByWorkspace[workspace.id]"
                :key="project.id"
                class="go-to-row project-row"
                @click="goToProject(workspace, project)"
              >
                <i class="pi pi-folder go-to-icon"></i>
                <span class="go-to-label">{{ project.name }}</span>
              </div>
            </div>
          </div>
        </div>

        <div class="go-to-footer">
          <button class="go-to-new-workspace-btn" @click="openCreateDialog">
            <i class="pi pi-plus"></i> New Workspace
          </button>
        </div>
      </div>
    </Transition>

    <CreateWorkspaceDialog
      :visible="showCreateDialog"
      @close="showCreateDialog = false"
    />
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { useAuth } from '../composables/useAuth'
import { useWorkspaces } from '../composables/useWorkspaces'
import { useRouter } from 'vue-router'
import CreateWorkspaceDialog from '../features/workspaces/CreateWorkspaceDialog.vue'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

const { user } = useAuth()
const { workspaces, loading: workspacesLoading, currentWorkspace, loadWorkspaces, switchWorkspace } = useWorkspaces()
const router = useRouter()

const containerRef = ref(null)
const open = ref(false)
const expanded = ref(new Set())
const projectsByWorkspace = reactive({})
const projectsLoading = reactive({})
const showCreateDialog = ref(false)

function openCreateDialog() {
  close()
  showCreateDialog.value = true
}

function isExpanded(workspaceId) {
  return expanded.value.has(workspaceId)
}

function toggleOpen() {
  open.value = !open.value
}

function close() {
  open.value = false
}

function toggleExpand(workspaceId) {
  const next = new Set(expanded.value)
  if (next.has(workspaceId)) {
    next.delete(workspaceId)
  } else {
    next.add(workspaceId)
    loadWorkspaceProjects(workspaceId)
  }
  expanded.value = next
}

async function loadWorkspaceProjects(workspaceId) {
  if (!user.value || projectsByWorkspace[workspaceId] || projectsLoading[workspaceId]) return

  projectsLoading[workspaceId] = true
  try {
    const token = await user.value.getIdToken()
    const url = new URL(`${API_URL}/users/me/projects`)
    url.searchParams.set('workspaceId', workspaceId)
    const response = await fetch(url.toString(), {
      headers: { Authorization: `Bearer ${token}` },
    })
    projectsByWorkspace[workspaceId] = response.ok ? await response.json() : []
  } catch {
    projectsByWorkspace[workspaceId] = []
  } finally {
    projectsLoading[workspaceId] = false
  }
}

async function goToWorkspace(workspace) {
  close()
  if (workspace.id !== currentWorkspace.value?.id) {
    await switchWorkspace(workspace.id)
  }
  router.push('/dashboard')
}

async function goToProject(workspace, project) {
  close()
  if (workspace.id !== currentWorkspace.value?.id) {
    await switchWorkspace(workspace.id)
  }
  router.push(`/workspace/${workspace.id}/project/${project.id}`)
}

function handleDocumentClick(e) {
  if (!containerRef.value?.contains(e.target)) close()
}

onMounted(() => {
  loadWorkspaces()
  document.addEventListener('click', handleDocumentClick)
})
onUnmounted(() => document.removeEventListener('click', handleDocumentClick))
</script>

<style scoped>
.go-to-menu {
  position: relative;
}

.go-to-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  height: 36px;
  padding: 0 0.85rem;
  background: var(--bg-input);
  border: 1px solid var(--border-purple);
  border-radius: 8px;
  color: var(--text-secondary);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.go-to-btn:hover {
  border-color: var(--color-purple);
  color: var(--text-primary);
}

.go-to-btn.active {
  background: var(--bg-active);
  border-color: transparent;
  box-shadow: 0 0 12px var(--glow-pink);
  color: var(--color-pink);
}

.go-to-btn .pi-compass {
  color: var(--color-purple);
  font-size: 0.85rem;
}

.go-to-btn.active .pi-compass {
  color: var(--color-pink);
}

.go-to-caret {
  font-size: 0.65rem;
  color: var(--text-muted);
  transition: transform 0.15s ease;
}

.go-to-caret.open {
  transform: rotate(180deg);
}

.go-to-dropdown {
  position: absolute;
  top: calc(100% + 8px);
  left: 0;
  min-width: 260px;
  max-width: 320px;
  max-height: 420px;
  overflow-y: auto;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
  z-index: 1000;
  padding: 0.35rem;
}

.go-to-empty {
  padding: 0.65rem 0.85rem;
  font-size: 0.82rem;
  color: var(--text-dim);
}

.go-to-empty.nested {
  padding-left: 2.5rem;
}

.go-to-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 0.6rem;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.1s, color 0.1s;
}

.go-to-row:hover {
  background: var(--bg-hover);
}

.workspace-row.current {
  color: var(--color-pink);
}

.workspace-row.current .go-to-icon {
  color: var(--color-pink);
}

.go-to-expand-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 0.65rem;
  cursor: pointer;
  border-radius: 4px;
  transition: color 0.1s, background 0.1s;
}

.go-to-expand-btn:hover {
  color: var(--text-primary);
  background: var(--bg-active);
}

.go-to-icon {
  color: var(--color-purple);
  font-size: 0.85rem;
  flex-shrink: 0;
}

.go-to-label {
  font-size: 0.85rem;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.go-to-projects {
  padding-left: 1.6rem;
}

.project-row .go-to-icon {
  font-size: 0.78rem;
}

.project-row .go-to-label {
  font-size: 0.82rem;
  color: var(--text-secondary);
}

.go-to-footer {
  border-top: 1px solid var(--border-color);
  margin-top: 0.35rem;
  padding: 0.4rem 0.35rem 0.05rem;
}

.go-to-new-workspace-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  width: 100%;
  padding: 0.5rem 0.6rem;
  background: transparent;
  border: none;
  border-radius: 6px;
  color: var(--color-purple);
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  transition: background 0.15s;
}
.go-to-new-workspace-btn:hover {
  background: var(--bg-hover);
  color: var(--color-pink);
}

/* ── Transition ── */
.go-to-drop-enter-active,
.go-to-drop-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.go-to-drop-enter-from,
.go-to-drop-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
