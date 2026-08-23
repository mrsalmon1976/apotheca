import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

export function useWorkspaceMembers() {
  const { user } = useAuth()
  const toast = useToast()

  const members = ref([])
  const loading = ref(false)

  async function loadMembers(workspaceId) {
    if (!user.value || !workspaceId) return

    loading.value = true
    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/workspaces/${workspaceId}/users`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      if (response.ok) {
        members.value = await response.json()
      } else {
        toast.add({ severity: 'error', summary: 'Failed to load members', detail: `Server error (${response.status})`, life: 10000 })
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to load members', detail: 'Could not connect to the server.', life: 10000 })
    } finally {
      loading.value = false
    }
  }

  async function addMember(workspaceId, email, workspaceRole) {
    if (!user.value) return { ok: false }

    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/workspaces/${workspaceId}/users`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, workspaceRole }),
      })
      if (response.ok) {
        await loadMembers(workspaceId)
      }
      return response
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to add member', detail: 'Could not connect to the server.', life: 10000 })
      return { ok: false }
    }
  }

  async function saveMemberRole(workspaceId, userId, workspaceRole) {
    if (!user.value) return false

    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/workspaces/${workspaceId}/users/${userId}`, {
        method: 'PATCH',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ workspaceRole }),
      })
      if (response.ok) {
        const item = members.value.find(m => m.userId === userId)
        if (item) item.workspaceRole = workspaceRole
        return true
      } else {
        toast.add({ severity: 'error', summary: 'Failed to update role', detail: `Server error (${response.status})`, life: 10000 })
        return false
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to update role', detail: 'Could not connect to the server.', life: 10000 })
      return false
    }
  }

  async function removeMember(workspaceId, userId) {
    if (!user.value) return false

    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/workspaces/${workspaceId}/users/${userId}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` },
      })
      if (response.ok) {
        members.value = members.value.filter(m => m.userId !== userId)
        return true
      } else {
        toast.add({ severity: 'error', summary: 'Failed to remove member', detail: `Server error (${response.status})`, life: 10000 })
        return false
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to remove member', detail: 'Could not connect to the server.', life: 10000 })
      return false
    }
  }

  return { members, loading, loadMembers, addMember, saveMemberRole, removeMember }
}
