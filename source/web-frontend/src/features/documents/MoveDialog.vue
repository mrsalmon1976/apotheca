<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div v-if="visible" class="dialog-backdrop" @click.self="close">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title">

          <!-- Header -->
          <div class="dialog-header">
            <div class="dialog-header-left">
              <div class="dialog-icon"><i class="pi pi-arrow-right-arrow-left"></i></div>
              <h2 id="dialog-title" class="dialog-title">
                Move {{ item?.isFolder ? 'Folder' : 'Document' }}
              </h2>
            </div>
            <button class="icon-btn" title="Close" @click="close">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Body -->
          <div class="dialog-body">
            <p class="move-hint">
              Choose a destination for <strong>"{{ item?.title }}"</strong>.
            </p>

            <nav class="browse-breadcrumbs">
              <button
                class="crumb-item"
                :class="{ 'crumb-current': breadcrumbs.length === 0 }"
                @click="navigateTo(-1)"
              >
                <i class="pi pi-folder-open"></i> Root
              </button>
              <template v-for="(crumb, index) in breadcrumbs" :key="crumb.id">
                <i class="pi pi-chevron-right crumb-sep"></i>
                <button
                  class="crumb-item"
                  :class="{ 'crumb-current': index === breadcrumbs.length - 1 }"
                  @click="navigateTo(index)"
                >
                  {{ crumb.title }}
                </button>
              </template>
            </nav>

            <div class="folder-browser">
              <div v-if="loading" class="browser-state">
                <i class="pi pi-spin pi-spinner"></i> Loading...
              </div>
              <div v-else-if="loadError" class="browser-state browser-error">
                <i class="pi pi-exclamation-triangle"></i> {{ loadError }}
              </div>
              <template v-else>
                <div v-if="visibleFolders.length === 0" class="browser-state browser-empty">
                  No subfolders here.
                </div>
                <button
                  v-for="folder in visibleFolders"
                  :key="folder.id"
                  class="browser-item"
                  @click="openFolder(folder)"
                >
                  <i class="pi pi-folder folder-icon"></i>
                  <span class="browser-item-title">{{ folder.title }}</span>
                  <i class="pi pi-chevron-right item-chevron"></i>
                </button>
              </template>
            </div>

            <div v-if="moveError" class="save-error">
              <i class="pi pi-exclamation-triangle"></i>
              <span>{{ moveError }}</span>
            </div>
          </div>

          <!-- Footer -->
          <div class="dialog-footer">
            <button class="cancel-btn" :disabled="moving" @click="close">Cancel</button>
            <button class="save-btn" :disabled="moving || alreadyHere" @click="confirmMove">
              <i :class="moving ? 'pi pi-spin pi-spinner' : 'pi pi-arrow-right'"></i>
              {{ moving ? 'Moving...' : alreadyHere ? 'Already Here' : 'Move Here' }}
            </button>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useDocumentFolders } from '../../composables/useDocumentFolders'

const props = defineProps({
  visible:         { type: Boolean, required: true },
  projectId:       { type: String,  required: true },
  item:            { type: Object,  default: null }, // { id, title, isFolder }
  currentParentId: { type: String,  default: null },
})

const emit = defineEmits(['close', 'moved'])

const { getDocuments, moveDocument } = useDocumentFolders()

const browseFolderId = ref(null)
const breadcrumbs     = ref([])   // [{ id, title }, ...]
const folders         = ref([])
const loading         = ref(false)
const loadError       = ref(null)

const moving    = ref(false)
const moveError = ref(null)

const visibleFolders = computed(() =>
  folders.value.filter(f => f.id !== props.item?.id)
)

const alreadyHere = computed(() => browseFolderId.value === (props.currentParentId ?? null))

watch(() => props.visible, (val) => {
  if (val) {
    browseFolderId.value = null
    breadcrumbs.value     = []
    moveError.value       = null
    moving.value           = false
    loadFolders(null)
  }
})

async function loadFolders(parentId) {
  loading.value   = true
  loadError.value = null
  try {
    const response = await getDocuments(props.projectId, parentId)
    if (response.ok) {
      const items = await response.json()
      folders.value = items.filter(d => d.isFolder)
    } else {
      loadError.value = `Failed to load folders (${response.status}).`
    }
  } catch {
    loadError.value = 'Could not connect to the server.'
  } finally {
    loading.value = false
  }
}

function openFolder(folder) {
  breadcrumbs.value.push({ id: folder.id, title: folder.title })
  browseFolderId.value = folder.id
  loadFolders(folder.id)
}

function navigateTo(index) {
  if (index === -1) {
    breadcrumbs.value     = []
    browseFolderId.value  = null
    loadFolders(null)
  } else {
    breadcrumbs.value    = breadcrumbs.value.slice(0, index + 1)
    browseFolderId.value = breadcrumbs.value.at(-1).id
    loadFolders(browseFolderId.value)
  }
}

function onKeyDown(e) {
  if (e.key === 'Escape' && props.visible) close()
}

onMounted(() => window.addEventListener('keydown', onKeyDown))
onUnmounted(() => window.removeEventListener('keydown', onKeyDown))

function close() {
  if (!moving.value) emit('close')
}

async function confirmMove() {
  if (!props.item || alreadyHere.value) return

  moving.value    = true
  moveError.value = null

  try {
    const response = await moveDocument(props.projectId, props.item.id, browseFolderId.value)

    if (response.ok) {
      emit('moved', { id: props.item.id, targetFolderId: browseFolderId.value })
      emit('close')
    } else if (response.status === 400) {
      const body = await response.json()
      moveError.value = body.error ?? 'Invalid move.'
    } else if (response.status === 403) {
      moveError.value = 'You do not have permission to move items in this project.'
    } else if (response.status === 404) {
      moveError.value = 'This item or destination folder no longer exists.'
    } else {
      moveError.value = `Unexpected error (${response.status}). Please try again.`
    }
  } catch {
    moveError.value = 'Could not connect to the server. Please try again.'
  } finally {
    moving.value = false
  }
}
</script>

<style scoped>
.dialog-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.65);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 500;
  padding: 1rem;
}

.dialog {
  background: var(--bg-card);
  border: 1px solid var(--border-purple);
  border-radius: 16px;
  box-shadow: 0 0 48px var(--glow-purple), 0 24px 64px rgba(0, 0, 0, 0.7);
  width: 100%;
  max-width: 460px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid var(--border-color);
}

.dialog-header-left { display: flex; align-items: center; gap: 0.75rem; }

.dialog-icon {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: var(--gradient-brand);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1rem;
  color: white;
  flex-shrink: 0;
  box-shadow: 0 0 14px var(--glow-purple);
}

.dialog-title { font-size: 1.1rem; font-weight: 700; color: var(--text-primary); margin: 0; }

.icon-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 1rem;
  padding: 0.35rem;
  border-radius: 6px;
  line-height: 1;
  transition: color 0.2s, background 0.2s;
}
.icon-btn:hover { color: var(--text-primary); background: var(--bg-hover); }

.dialog-body { padding: 1.5rem; display: flex; flex-direction: column; gap: 1rem; }

.move-hint { font-size: 0.9rem; color: var(--text-secondary); margin: 0; line-height: 1.5; }
.move-hint strong { color: var(--text-primary); }

.browse-breadcrumbs {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  flex-wrap: wrap;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.crumb-item {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  background: transparent;
  border: none;
  color: var(--color-purple);
  font-size: 0.85rem;
  font-weight: 500;
  cursor: pointer;
  padding: 0.2rem 0.4rem;
  border-radius: 4px;
  transition: background 0.15s, color 0.15s;
}
.crumb-item:hover { background: var(--bg-active); }
.crumb-current { color: var(--text-primary); cursor: default; font-weight: 600; }
.crumb-current:hover { background: transparent; }

.crumb-sep { color: var(--text-dim); font-size: 0.6rem; }

.folder-browser {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  max-height: 260px;
  min-height: 100px;
  overflow-y: auto;
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 0.4rem;
  background: var(--bg-input);
}

.browser-state {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  color: var(--text-dim);
  font-size: 0.85rem;
  padding: 1.5rem 0;
}
.browser-error { color: var(--color-pink-light); }

.browser-item {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  width: 100%;
  background: transparent;
  border: none;
  border-radius: 8px;
  color: var(--text-primary);
  font-size: 0.875rem;
  text-align: left;
  padding: 0.55rem 0.65rem;
  cursor: pointer;
  transition: background 0.15s;
}
.browser-item:hover { background: var(--bg-hover); }

.browser-item-title { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

.folder-icon { color: var(--color-purple); font-size: 0.9rem; }
.item-chevron { color: var(--text-dim); font-size: 0.7rem; flex-shrink: 0; }

.save-error {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  background: rgba(236, 72, 153, 0.08);
  border: 1px solid rgba(236, 72, 153, 0.25);
  border-radius: 8px;
  padding: 0.65rem 0.9rem;
  color: var(--color-pink-light);
  font-size: 0.85rem;
  line-height: 1.5;
}
.save-error .pi { margin-top: 2px; flex-shrink: 0; }

.dialog-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 0.75rem;
  padding: 1rem 1.5rem;
  border-top: 1px solid var(--border-color);
}

.cancel-btn {
  padding: 0.5rem 1.1rem;
  background: transparent;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-muted);
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}
.cancel-btn:hover:not(:disabled) { border-color: var(--border-purple); color: var(--text-primary); }
.cancel-btn:disabled { opacity: 0.5; cursor: not-allowed; }

.save-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1.25rem;
  background: var(--gradient-brand);
  border: none;
  border-radius: 8px;
  color: white;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s, box-shadow 0.2s;
  box-shadow: 0 0 14px var(--glow-purple);
}
.save-btn:hover:not(:disabled) { opacity: 0.9; box-shadow: 0 0 22px var(--glow-purple); }
.save-btn:disabled { opacity: 0.55; cursor: not-allowed; box-shadow: none; }

.dialog-enter-active,
.dialog-leave-active { transition: opacity 0.2s ease; }
.dialog-enter-active .dialog,
.dialog-leave-active .dialog { transition: transform 0.2s ease, opacity 0.2s ease; }
.dialog-enter-from,
.dialog-leave-to { opacity: 0; }
.dialog-enter-from .dialog,
.dialog-leave-to .dialog { transform: translateY(-12px) scale(0.97); opacity: 0; }
</style>
