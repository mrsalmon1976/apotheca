<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div v-if="visible" class="dialog-backdrop" @click.self="close">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title">

          <!-- Header -->
          <div class="dialog-header">
            <div class="dialog-header-left">
              <div class="dialog-icon"><i class="pi pi-folder-plus"></i></div>
              <h2 id="dialog-title" class="dialog-title">New Folder</h2>
            </div>
            <button class="icon-btn" title="Close" @click="close">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Body -->
          <div class="dialog-body">
            <div class="field" :class="{ 'has-error': fieldError }">
              <label class="field-label" for="folder-name">Folder Name <span class="required">*</span></label>
              <input
                id="folder-name"
                ref="nameInput"
                v-model="name"
                class="field-input"
                type="text"
                placeholder="e.g. Meeting Notes"
                maxlength="100"
                @keydown.enter="save"
              />
              <span v-if="fieldError" class="field-error-msg">{{ fieldError }}</span>
            </div>

            <!-- Labels -->
            <div class="field">
              <label class="field-label">Labels</label>
              <div class="label-input-box" :class="{ focused: labelInputFocused }" @click="focusLabelInput">
                <span
                  v-for="(label, i) in selectedLabels"
                  :key="label"
                  class="label-chip"
                >
                  {{ label }}
                  <button class="chip-remove" tabindex="-1" @click.stop="removeLabel(i)">
                    <i class="pi pi-times"></i>
                  </button>
                </span>
                <input
                  ref="labelInput"
                  v-model="labelQuery"
                  class="label-text-input"
                  type="text"
                  placeholder="Type to search labels…"
                  autocomplete="off"
                  @focus="labelInputFocused = true"
                  @blur="onLabelBlur"
                  @keydown="onLabelKeydown"
                />
              </div>

              <!-- Suggestions dropdown -->
              <div v-if="suggestions.length > 0" class="suggestions">
                <button
                  v-for="(s, i) in suggestions"
                  :key="s.id"
                  class="suggestion-item"
                  :class="{ highlighted: i === highlightedIndex }"
                  @mousedown.prevent="selectSuggestion(s.labelText)"
                >
                  {{ s.labelText }}
                </button>
              </div>
            </div>

            <div v-if="saveError" class="save-error">
              <i class="pi pi-exclamation-triangle"></i>
              <span>{{ saveError }}</span>
            </div>
          </div>

          <!-- Footer -->
          <div class="dialog-footer">
            <button class="cancel-btn" :disabled="saving" @click="close">Cancel</button>
            <button class="save-btn" :disabled="saving" @click="save">
              <i :class="saving ? 'pi pi-spin pi-spinner' : 'pi pi-check'"></i>
              {{ saving ? 'Creating...' : 'Create Folder' }}
            </button>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, nextTick, watch, onMounted, onUnmounted } from 'vue'
import { useNoteFolders } from '../../composables/useNoteFolders'

const props = defineProps({
  visible:   { type: Boolean, required: true },
  projectId: { type: String, required: true },
  parentId:  { type: String, default: null },
})

const emit = defineEmits(['close', 'saved'])

const { createFolder, searchLabels } = useNoteFolders()

// Folder name
const nameInput  = ref(null)
const name       = ref('')
const fieldError = ref(null)
const saveError  = ref(null)
const saving     = ref(false)
const MIN_LENGTH = 3

// Labels
const labelInput       = ref(null)
const labelQuery       = ref('')
const selectedLabels   = ref([])
const suggestions      = ref([])
const highlightedIndex = ref(-1)
const labelInputFocused = ref(false)

let debounceTimer = null

// Reset on open
watch(() => props.visible, (val) => {
  if (val) {
    name.value           = ''
    labelQuery.value     = ''
    selectedLabels.value = []
    suggestions.value    = []
    highlightedIndex.value = -1
    fieldError.value     = null
    saveError.value      = null
    saving.value         = false
    nextTick(() => nameInput.value?.focus())
  }
})

// Debounced label search
watch(labelQuery, (val) => {
  clearTimeout(debounceTimer)
  highlightedIndex.value = -1

  if (val.trim().length < 1) {
    suggestions.value = []
    return
  }

  debounceTimer = setTimeout(() => fetchSuggestions(val.trim()), 300)
})

async function fetchSuggestions(query) {
  try {
    const res = await searchLabels(props.projectId, query)
    if (!res.ok) { suggestions.value = []; return }
    const all = await res.json()
    // Exclude labels already selected
    suggestions.value = all.filter(s => !selectedLabels.value.includes(s.labelText))
  } catch {
    suggestions.value = []
  }
}

function commitLabel(text) {
  const trimmed = text.trim()
  if (trimmed && !selectedLabels.value.includes(trimmed)) {
    selectedLabels.value.push(trimmed)
  }
  labelQuery.value   = ''
  suggestions.value  = []
  highlightedIndex.value = -1
}

function selectSuggestion(text) {
  commitLabel(text)
  nextTick(() => labelInput.value?.focus())
}

function removeLabel(index) {
  selectedLabels.value.splice(index, 1)
}

function focusLabelInput() {
  labelInput.value?.focus()
}

function onLabelBlur() {
  // Short delay so mousedown on a suggestion fires first
  setTimeout(() => {
    labelInputFocused.value = false
    suggestions.value = []
    // Commit any unconfirmed text on blur
    if (labelQuery.value.trim()) commitLabel(labelQuery.value)
  }, 150)
}

function onLabelKeydown(e) {
  if (e.key === 'Enter' || e.key === ',') {
    e.preventDefault()
    if (highlightedIndex.value >= 0 && suggestions.value[highlightedIndex.value]) {
      selectSuggestion(suggestions.value[highlightedIndex.value].labelText)
    } else {
      commitLabel(labelQuery.value)
    }
  } else if (e.key === 'ArrowDown') {
    e.preventDefault()
    highlightedIndex.value = Math.min(highlightedIndex.value + 1, suggestions.value.length - 1)
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    highlightedIndex.value = Math.max(highlightedIndex.value - 1, 0)
  } else if (e.key === 'Backspace' && labelQuery.value === '') {
    selectedLabels.value.pop()
  } else if (e.key === 'Escape') {
    suggestions.value = []
    highlightedIndex.value = -1
  }
}

// Global Escape to close dialog
function onKeyDown(e) {
  if (e.key === 'Escape' && props.visible && suggestions.value.length === 0) close()
}

onMounted(() => window.addEventListener('keydown', onKeyDown))
onUnmounted(() => { window.removeEventListener('keydown', onKeyDown); clearTimeout(debounceTimer) })

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
  // Commit any unconfirmed label text before saving
  if (labelQuery.value.trim()) commitLabel(labelQuery.value)

  const title = validate()
  if (!title) {
    nameInput.value?.focus()
    return
  }

  saving.value    = true
  saveError.value = null

  try {
    const response = await createFolder(props.projectId, title, props.parentId, selectedLabels.value)

    if (response.ok) {
      const body = await response.json()
      emit('saved', { id: body.id, title })
      emit('close')
    } else if (response.status === 400) {
      const body = await response.json()
      saveError.value = body.errors?.join(' ') ?? 'Invalid request.'
    } else if (response.status === 403) {
      saveError.value = 'You do not have permission to create folders in this project.'
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
  max-width: 440px;
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

.dialog-header-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

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

.dialog-title {
  font-size: 1.1rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

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

.dialog-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  position: relative;
}

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
.field-input::placeholder { color: var(--text-dim); }
.field-input:focus {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 3px rgba(168, 85, 247, 0.15);
}

.has-error .field-input { border-color: var(--color-pink); }
.has-error .field-input:focus { box-shadow: 0 0 0 3px rgba(236, 72, 153, 0.15); }

.field-error-msg {
  font-size: 0.78rem;
  color: var(--color-pink);
}

/* Label chip-input box */
.label-input-box {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem;
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 0.45rem 0.75rem;
  cursor: text;
  min-height: 2.5rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.label-input-box.focused {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 3px rgba(168, 85, 247, 0.15);
}

.label-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.75rem;
  padding: 0.15rem 0.4rem 0.15rem 0.6rem;
  border-radius: 999px;
  background: rgba(168, 85, 247, 0.15);
  color: var(--color-purple);
  border: 1px solid rgba(168, 85, 247, 0.4);
  font-weight: 500;
  white-space: nowrap;
}

.chip-remove {
  background: none;
  border: none;
  color: var(--color-purple);
  cursor: pointer;
  padding: 0;
  line-height: 1;
  opacity: 0.7;
  display: flex;
  align-items: center;
}
.chip-remove .pi { font-size: 0.7rem; }
.chip-remove:hover { opacity: 1; }

.label-text-input {
  flex: 1;
  min-width: 8rem;
  background: none;
  border: none;
  outline: none;
  color: var(--text-primary);
  font-size: 0.875rem;
  font-family: inherit;
  padding: 0.15rem 0;
}
.label-text-input::placeholder { color: var(--text-dim); }

/* Suggestions dropdown */
.suggestions {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  margin-top: 2px;
  background: var(--bg-card);
  border: 1px solid var(--border-purple);
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
  overflow: hidden;
  z-index: 10;
}

.suggestion-item {
  display: block;
  width: 100%;
  text-align: left;
  background: none;
  border: none;
  color: var(--text-primary);
  font-size: 0.875rem;
  font-family: inherit;
  padding: 0.55rem 0.85rem;
  cursor: pointer;
  transition: background 0.15s;
}
.suggestion-item:hover,
.suggestion-item.highlighted {
  background: var(--bg-active);
  color: var(--color-purple);
}

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
