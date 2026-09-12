const STORAGE_KEY = 'apotheca:lastLocation'

export function saveLastLocation(route) {
  const entry = { path: route.fullPath, workspaceId: route.params.workspaceId ?? null }
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(entry))
  } catch {
    // localStorage unavailable (private browsing, etc.) — ignore
  }
}

export function getLastLocation() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}
