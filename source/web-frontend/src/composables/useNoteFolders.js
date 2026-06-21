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

  async function createFolder(projectId, title, parentId = null, labels = []) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/notes/folders`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ title, parentNoteId: parentId, labels }),
    })
  }

  async function getNote(projectId, noteId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/notes/${noteId}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function saveNote(projectId, noteId, data, options = {}) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/notes/${noteId}`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
      ...options,
    })
  }

  async function createNote(projectId, parentNoteId = null) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/notes`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ parentNoteId }),
    })
  }

  async function renameFolder(projectId, folderId, title) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/notes/folders/${folderId}`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ title }),
    })
  }

  async function deleteNote(projectId, noteId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/notes/${noteId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function searchLabels(projectId, query) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/labels?q=${encodeURIComponent(query)}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function uploadNoteAttachment(projectId, noteId, file) {
    const token = await user.value.getIdToken()
    const formData = new FormData()
    formData.append('file', file)
    const res = await fetch(`${API_URL}/projects/${projectId}/notes/${noteId}/attachments`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: formData,
    })
    if (!res.ok) throw new Error(`Attachment upload failed (${res.status})`)
    const data = await res.json()
    return `${API_URL}${data.url}`
  }

  return { getNote, getNotes, createFolder, renameFolder, createNote, saveNote, deleteNote, searchLabels, uploadNoteAttachment }
}
