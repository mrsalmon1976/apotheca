import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

export function useUserTasks() {
  const { user } = useAuth()
  const toast = useToast()
  const tasks = ref([])
  const loading = ref(false)
  const error = ref(null)

  async function loadTasks(filter) {
    if (!user.value) return

    loading.value = true
    error.value = null
    tasks.value = []
    try {
      const token = await user.value.getIdToken()
      const url = new URL(`${API_URL}/users/me/tasks`)
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
      } else {
        toast.add({ severity: 'error', summary: 'Failed to load tasks', detail: `Server error (${response.status})`, life: 10000 })
      }
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to load tasks', detail: 'Could not connect to the server.', life: 10000 })
    } finally {
      loading.value = false
    }
  }

  async function completeTask(projectId, taskId) {
    const token = await user.value.getIdToken()
    const response = await fetch(`${API_URL}/projects/${projectId}/tasks/${taskId}/complete`, {
      method: 'PATCH',
      headers: { Authorization: `Bearer ${token}` },
    })
    if (response.ok) {
      const completed = tasks.value.find(t => t.id === taskId)
      tasks.value = tasks.value.filter(t => t.id !== taskId)
      toast.add({ severity: 'success', summary: 'Task completed', detail: completed?.title, life: 4000 })
    } else {
      toast.add({ severity: 'error', summary: 'Failed to complete task', detail: `Server error (${response.status})`, life: 10000 })
    }
  }

  return { tasks, loading, error, loadTasks, completeTask }
}
