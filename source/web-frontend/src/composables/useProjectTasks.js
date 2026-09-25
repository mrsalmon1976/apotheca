import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useAuth } from './useAuth'
import { PRIORITY_RANK } from '../constants/taskPriorities'

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

  async function saveTask(projectId, taskData) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/tasks`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(taskData),
    })
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

  // Mirrors the API's ORDER BY (GetProjectTasksRepository): date ascending with undated
  // last, then priority descending. Array.sort is stable, so ties keep server order.
  function sortTasks() {
    const dayOf = t => t.dueAt?.split('T')[0] ?? null
    tasks.value.sort((a, b) => {
      const da = dayOf(a), db = dayOf(b)
      if (da !== db) {
        if (da === null) return 1
        if (db === null) return -1
        return da < db ? -1 : 1
      }
      return (PRIORITY_RANK[b.priority] ?? 0) - (PRIORITY_RANK[a.priority] ?? 0)
    })
  }

  // The save endpoint is a full upsert, so resend every editable field unchanged
  // alongside the new date. `date` is a local-midnight Date (or null to clear);
  // it's stored the same way NewTaskDialog stores it — the calendar day at UTC midnight.
  async function setTaskDueDate(projectId, task, date) {
    const dueAt = date
      ? new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())).toISOString()
      : null
    try {
      const response = await saveTask(projectId, {
        id:           task.id,
        parentTaskId: task.parentTaskId,
        title:        task.title,
        notes:        task.notes,
        assignedTo:   task.assignedTo,
        priority:     task.priority,
        dueAt,
      })
      if (response.ok) {
        const existing = tasks.value.find(t => t.id === task.id)
        if (existing) {
          existing.dueAt = dueAt
          sortTasks()
        }
        return true
      }
      toast.add({ severity: 'error', summary: 'Failed to update date', detail: `Server error (${response.status})`, life: 10000 })
    } catch {
      toast.add({ severity: 'error', summary: 'Failed to update date', detail: 'Could not connect to the server.', life: 10000 })
    }
    return false
  }

  return { tasks, loading, error, loadTasks, saveTask, completeTask, setTaskDueDate }
}
