import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

export function useNoteFolders() {
  const { user } = useAuth()

  async function createFolder(projectId, title) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/notes/folders`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ title }),
    })
  }

  return { createFolder }
}
