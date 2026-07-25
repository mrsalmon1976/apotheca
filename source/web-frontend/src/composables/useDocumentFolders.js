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

  async function renameFolder(projectId, folderId, title) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/folders/${folderId}`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ title }),
    })
  }

  async function deleteDocument(projectId, documentId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function moveDocument(projectId, documentId, targetFolderId = null) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}/move`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ targetFolderId }),
    })
  }

  async function downloadDocument(projectId, documentId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}/download`, {
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

  async function uploadDocument(projectId, file, parentId = null, title = null) {
    const token = await user.value.getIdToken()
    const url = parentId
      ? `${API_URL}/projects/${projectId}/documents/upload?parentId=${encodeURIComponent(parentId)}`
      : `${API_URL}/projects/${projectId}/documents/upload`
    const formData = new FormData()
    formData.append('file', file)
    if (title) formData.append('title', title)
    return fetch(url, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: formData,
    })
  }

  async function searchLabels(projectId, query) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/labels?q=${encodeURIComponent(query)}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function getDocumentLinks(projectId, documentId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}/links`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function createDocumentLink(projectId, documentId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}/links`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function deleteDocumentLink(projectId, documentId, linkId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/documents/${documentId}/links/${linkId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  return { getDocument, getDocuments, createFolder, renameFolder, createDocument, saveDocument, deleteDocument, moveDocument, downloadDocument, restoreDocument, uploadDocument, searchLabels, getDocumentLinks, createDocumentLink, deleteDocumentLink }
}
