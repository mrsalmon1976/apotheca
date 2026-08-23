import { ref, computed } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

// Module-level shared state so all callers see the same list
const workspaces = ref([])
const loading = ref(false)

export function useWorkspaces() {
  const { user } = useAuth()
  const toast = useToast()

  const currentWorkspace = computed(() => workspaces.value.find(w => w.isCurrent) ?? null)

  async function loadWorkspaces() {
    if (!user.value) return

    loading.value = true
    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/users/me/workspaces`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      if (response.ok) {
        workspaces.value = await response.json()
      } else {
        toast.add({ severity: 'error', summary: 'Failed to load workspaces', detail: `Server error (${response.status})`, life: 10000 })
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to load workspaces', detail: 'Could not connect to the server.', life: 10000 })
    } finally {
      loading.value = false
    }
  }

  async function createWorkspace(name) {
    if (!user.value) return null

    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/workspaces`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name }),
      })
      if (response.ok) {
        const created = await response.json()
        workspaces.value = workspaces.value.map(w => ({ ...w, isCurrent: false }))
        workspaces.value.push({
          id: created.id,
          name: created.name,
          workspaceRole: 'ADMIN',
          plan: 'FREE',
          billingStatus: 'ACTIVE',
          memberCount: 1,
          projectCount: 0,
          isCurrent: true,
          createdAt: new Date().toISOString(),
        })
        return created
      } else {
        toast.add({ severity: 'error', summary: 'Failed to create workspace', detail: `Server error (${response.status})`, life: 10000 })
        return null
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to create workspace', detail: 'Could not connect to the server.', life: 10000 })
      return null
    }
  }

  async function saveWorkspace(workspaceId, name) {
    if (!user.value) return false

    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/workspaces/${workspaceId}`, {
        method: 'PATCH',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name }),
      })
      if (response.ok) {
        const item = workspaces.value.find(w => w.id === workspaceId)
        if (item) item.name = name
        return true
      } else {
        toast.add({ severity: 'error', summary: 'Failed to save workspace', detail: `Server error (${response.status})`, life: 10000 })
        return false
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to save workspace', detail: 'Could not connect to the server.', life: 10000 })
      return false
    }
  }

  async function switchWorkspace(workspaceId) {
    if (!user.value) return false

    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/workspaces/${workspaceId}/current`, {
        method: 'PATCH',
        headers: { Authorization: `Bearer ${token}` },
      })
      if (response.ok) {
        workspaces.value = workspaces.value.map(w => ({ ...w, isCurrent: w.id === workspaceId }))
        return true
      } else {
        toast.add({ severity: 'error', summary: 'Failed to switch workspace', detail: `Server error (${response.status})`, life: 10000 })
        return false
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to switch workspace', detail: 'Could not connect to the server.', life: 10000 })
      return false
    }
  }

  return { workspaces, loading, currentWorkspace, loadWorkspaces, createWorkspace, saveWorkspace, switchWorkspace }
}
