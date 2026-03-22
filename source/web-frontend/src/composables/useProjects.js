import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

export function useProjects() {
  const { user } = useAuth()
  const toast = useToast()
  const projects = ref([])
  const loading = ref(false)

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

  return { projects, loading, loadProjects }
}
