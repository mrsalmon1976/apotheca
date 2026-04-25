import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

export function useDocumentFolders() {
  const { user } = useAuth()

  async function getDocuments(projectId, parentId = null) {
    const token = await user.value.getIdToken()
    const url = parentId
      ? `${API_URL}/projects/${projectId}/documents?parentId=${encodeURIComponent(parentId)}`
      : `${API_URL}/projects/${projectId}/documents`
    return fetch(url, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function createFolder(projectId, title, parentId = null, labels = []) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/folders`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ title, parentDocumentId: parentId, labels }),
    })
  }

  async function getDocument(projectId, documentId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function saveDocument(projectId, documentId, data) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    })
  }

  async function createDocument(projectId, parentDocumentId = null) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ parentDocumentId }),
    })
  }

  async function deleteDocument(projectId, documentId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function restoreDocument(projectId, documentId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}/restore`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function searchLabels(projectId, query) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/labels?q=${encodeURIComponent(query)}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  return { getDocument, getDocuments, createFolder, createDocument, saveDocument, deleteDocument, restoreDocument, searchLabels }
}
