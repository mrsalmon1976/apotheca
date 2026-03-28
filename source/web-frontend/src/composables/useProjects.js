import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

// Module-level shared state so all callers see the same list
const projects = ref([])
const loading = ref(false)

export function useProjects() {
  const { user } = useAuth()
  const toast = useToast()

  async function loadProjects() {
    if (!user.value) return

    loading.value = true
    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/projects`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      if (response.ok) {
        projects.value = await response.json()
      } else {
        toast.add({ severity: 'error', summary: 'Failed to load projects', detail: `Server error (${response.status})`, life: 10000 })
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to load projects', detail: 'Could not connect to the server.', life: 10000 })
    } finally {
      loading.value = false
    }
  }

  async function saveProject(projectId, name, summary) {
    if (!user.value) return false

    try {
      const token = await user.value.getIdToken()
      const response = await fetch(`${API_URL}/projects/${projectId}`, {
        method: 'PATCH',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ name, summary }),
      })
      if (response.ok) {
        const item = projects.value.find(p => p.id === projectId)
        if (item) {
          item.name = name
          item.summary = summary ?? null
        }
        return true
      } else {
        toast.add({ severity: 'error', summary: 'Failed to save project', detail: `Server error (${response.status})`, life: 10000 })
        return false
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to save project', detail: 'Could not connect to the server.', life: 10000 })
      return false
    }
  }

  return { projects, loading, loadProjects, saveProject }
}
