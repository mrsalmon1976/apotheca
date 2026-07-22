import { ref, watch } from 'vue'

function storageKey(projectId) {
  return `apotheca-mindmaps-${projectId}`
}

export function makeNode(header = 'New Node', body = '') {
  return {
    id: crypto.randomUUID(),
    header,
    body,
    collapsed: false,
    children: []
  }
}

function loadAll(projectId) {
  try {
    const raw = localStorage.getItem(storageKey(projectId))
    const parsed = raw ? JSON.parse(raw) : []
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function saveAll(projectId, mindmaps) {
  localStorage.setItem(storageKey(projectId), JSON.stringify(mindmaps))
}

export function useMindmaps(projectId) {
  const mindmaps = ref(loadAll(projectId))

  function createMindmap() {
    const mindmap = {
      id: crypto.randomUUID(),
      root: makeNode('New Mindmap', ''),
      updatedAt: new Date().toISOString()
    }
    mindmaps.value.push(mindmap)
    saveAll(projectId, mindmaps.value)
    return mindmap
  }

  function deleteMindmap(id) {
    mindmaps.value = mindmaps.value.filter(m => m.id !== id)
    saveAll(projectId, mindmaps.value)
  }

  return { mindmaps, createMindmap, deleteMindmap }
}

export function useMindmapEditor(projectId, mindmapId) {
  const existing = loadAll(projectId).find(m => m.id === mindmapId)

  const root = ref(existing?.root ?? null)
  const notFound = ref(!existing)

  let saveTimer = null
  watch(root, () => {
    if (!root.value) return
    clearTimeout(saveTimer)
    saveTimer = setTimeout(() => {
      const current = loadAll(projectId)
      const idx = current.findIndex(m => m.id === mindmapId)
      const updated = { id: mindmapId, root: root.value, updatedAt: new Date().toISOString() }
      if (idx !== -1) current[idx] = updated
      else current.push(updated)
      saveAll(projectId, current)
    }, 400)
  }, { deep: true })

  return { root, notFound }
}
