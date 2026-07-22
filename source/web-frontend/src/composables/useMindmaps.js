import { useAuth } from './useAuth'

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

export function buildMindmapTree(flatNodes) {
  const byId = new Map()
  for (const n of flatNodes) {
    byId.set(n.id, { id: n.id, header: n.header, body: n.body ?? '', collapsed: n.collapsed, children: [] })
  }

  let root = null
  for (const n of flatNodes) {
    const node = byId.get(n.id)
    if (n.parentNodeId) {
      byId.get(n.parentNodeId)?.children.push(node)
    } else {
      root = node
    }
  }
  return root
}

export function useMindmaps() {
  const { user } = useAuth()

  async function getMindmaps(projectId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/mindmaps`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function getMindmap(projectId, mindmapId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/mindmaps/${mindmapId}`, {
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function createMindmap(projectId, name) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/mindmaps`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ name }),
    })
  }

  async function renameMindmap(projectId, mindmapId, name) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/mindmaps/${mindmapId}`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ name }),
    })
  }

  async function deleteMindmap(projectId, mindmapId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/mindmaps/${mindmapId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  async function createMindmapNode(projectId, mindmapId, parentNodeId, header = '', body = '') {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/mindmaps/${mindmapId}/nodes`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ parentNodeId, header, body }),
    })
  }

  async function saveMindmapNode(projectId, mindmapId, nodeId, data) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/mindmaps/${mindmapId}/nodes/${nodeId}`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    })
  }

  async function deleteMindmapNode(projectId, mindmapId, nodeId) {
    const token = await user.value.getIdToken()
    return fetch(`${API_URL}/projects/${projectId}/mindmaps/${mindmapId}/nodes/${nodeId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` },
    })
  }

  return {
    getMindmaps,
    getMindmap,
    createMindmap,
    renameMindmap,
    deleteMindmap,
    createMindmapNode,
    saveMindmapNode,
    deleteMindmapNode,
  }
}
