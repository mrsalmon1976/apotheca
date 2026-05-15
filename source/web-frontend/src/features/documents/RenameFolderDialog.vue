<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div v-if="visible" class="dialog-backdrop" @click.self="close">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title">

          <div class="dialog-header">
            <div class="dialog-header-left">
              <div class="dialog-icon"><i class="pi pi-pencil"></i></div>
              <h2 id="dialog-title" class="dialog-title">Rename Folder</h2>
            </div>
            <button class="icon-btn" title="Close" @click="close">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <div class="dialog-body">
            <div class="field" :class="{ 'has-error': fieldError }">
              <label class="field-label" for="folder-name">Folder Name <span class="required">*</span></label>
              <input
                id="folder-name"
                ref="nameInput"
                v-model="name"
                class="field-input"
                type="text"
                maxlength="100"
                @keydown.enter="save"
              />
              <span v-if="fieldError" class="field-error-msg">{{ fieldError }}</span>
            </div>

            <div v-if="saveError" class="save-error">
              <i class="pi pi-exclamation-triangle"></i>
              <span>{{ saveError }}</span>
            </div>
          </div>

          <div class="dialog-footer">
            <button class="cancel-btn" :disabled="saving" @click="close">Cancel</button>
            <button class="save-btn" :disabled="saving" @click="save">
              <i :class="saving ? 'pi pi-spin pi-spinner' : 'pi pi-check'"></i>
              {{ saving ? 'Saving...' : 'Rename' }}
            </button>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, nextTick, watch, onMounted, onUnmounted } from 'vue'
import { useDocumentFolders } from '../../composables/useDocumentFolders'

const props = defineProps({
  visible:      { type: Boolean, required: true },
  projectId:    { type: String,  required: true },
  folderId:     { type: String,  default: null },
  currentTitle: { type: String,  default: '' },
})

const emit = defineEmits(['close', 'renamed'])

const { renameFolder } = useDocumentFolders()

const nameInput  = ref(null)
const name       = ref('')
const fieldError = ref(null)
const saveError  = ref(null)
const saving     = ref(false)
const MIN_LENGTH = 3

watch(() => props.visible, (val) => {
  if (val) {
    name.value       = props.currentTitle
    fieldError.value = null
    saveError.value  = null
    saving.value     = false
    nextTick(() => {
      nameInput.value?.focus()
      nameInput.value?.select()
    })
  }
})

function onKeyDown(e) {
  if (e.key === 'Escape' && props.visible) close()
}

onMounted(() => window.addEventListener('keydown', onKeyDown))
onUnmounted(() => window.removeEventListener('keydown', onKeyDown))

function close() {
  if (!saving.value) emit('close')
}

function validate() {
  const trimmed = name.value.trim()
  if (!trimmed) {
    fieldError.value = 'Folder name is required.'
    return null
  }
  if (trimmed.length < MIN_LENGTH) {
    fieldError.value = `Folder name must be at least ${MIN_LENGTH} characters.`
    return null
  }
  fieldError.value = null
  return trimmed
}

async function save() {
  const title = validate()
  if (!title) {
    nameInput.value?.focus()
    return
  }

  saving.value    = true
  saveError.value = null

  try {
    const response = await renameFolder(props.projectId, props.folderId, title)

    if (response.ok) {
      emit('renamed', { id: props.folderId, title })
      emit('close')
    } else if (response.status === 400) {
      const body = await response.json()
      saveError.value = body.errors?.join(' ') ?? 'Invalid request.'
    } else if (response.status === 403) {
      saveError.value = 'You do not have permission to rename folders in this project.'
    } else if (response.status === 404) {
      saveError.value = 'This folder no longer exists.'
    } else {
      saveError.value = `Unexpected error (${response.status}). Please try again.`
    }
  } catch {
    saveError.value = 'Could not connect to the server. Please try again.'
  } finally {
    saving.value = false
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
  max-width: 400px;
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

.dialog-body { padding: 1.5rem; display: flex; flex-direction: column; gap: 1.25rem; }

.field { display: flex; flex-direction: column; gap: 0.4rem; }

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.required { color: var(--color-pink); margin-left: 2px; }

.field-input {
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-primary);
  font-size: 0.9rem;
  font-family: inherit;
  padding: 0.6rem 0.85rem;
  width: 100%;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
  box-sizing: border-box;
}
.field-input:focus { border-color: var(--color-purple); box-shadow: 0 0 0 3px rgba(168, 85, 247, 0.15); }
.has-error .field-input { border-color: var(--color-pink); }
.has-error .field-input:focus { box-shadow: 0 0 0 3px rgba(236, 72, 153, 0.15); }

.field-error-msg { font-size: 0.78rem; color: var(--color-pink); }

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
