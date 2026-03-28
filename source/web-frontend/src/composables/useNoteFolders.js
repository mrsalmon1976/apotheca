import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

export function useNoteFolders() {
  const { user } = useAuth()

  async function getNotes(projectId, parentId = null) {
    const token = await user.value.getIdToken()
    const url = parentId
      ? `${API_URL}/projects/${projectId}/notes?parentId=${encodeURIComponent(parentId)}`
      : `${API_URL}/projects/${projectId}/notes`
    return fetch(url, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function createFolder(projectId, title, parentId = null) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/notes/folders`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ title, parentNoteId: parentId }),
    })
  }

  return { getNotes, createFolder }
}
