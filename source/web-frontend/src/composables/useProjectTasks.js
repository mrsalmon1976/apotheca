import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

export function useProjectTasks() {
  const { user } = useAuth()
  const toast = useToast()
  const tasks = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function loadTasks(projectId, filter) {
    if (!user.value) return

    loading.value = true
    error.value = null
    tasks.value = []
    try {
      const token = await user.value.getIdToken()
      const url = new URL(`${API_URL}/projects/${projectId}/tasks`)
      if (filter === 'today' || filter === 'upcoming') {
        url.searchParams.set('filter', filter)
      }
      const response = await fetch(url.toString(), {
        headers: { Authorization: `Bearer ${token}` },
      })
      if (response.ok) {
        tasks.value = await response.json()
      } else if (response.status === 401) {
        error.value = { title: 'Session expired', message: 'Please log in again to view tasks.' }
      } else if (response.status === 403) {
        error.value = { title: 'Access denied', message: 'You do not have permission to view tasks for this project.' }
      } else {
        toast.add({ severity: 'error', summary: 'Failed to load tasks', detail: `Server error (${response.status})`, life: 10000 })
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to load tasks', detail: 'Could not connect to the server.', life: 10000 })
    } finally {
      loading.value = false
    }
  }

  return { tasks, loading, error, loadTasks }
}
