<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div v-if="visible" class="dialog-backdrop" @click.self="close">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title">

          <!-- Header -->
          <div class="dialog-header">
            <div class="dialog-header-left">
              <div class="dialog-icon"><i class="pi pi-upload"></i></div>
              <h2 id="dialog-title" class="dialog-title">Upload Document</h2>
            </div>
            <button class="icon-btn" title="Close" @click="close">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Body -->
          <div class="dialog-body">
            <input
              ref="fileInput"
              type="file"
              class="hidden-input"
              @change="onFileInputChange"
            />

            <div
              class="upload-zone"
              :class="{ 'drag-active': isDragActive, 'has-file': selectedFile }"
              @click="fileInput.click()"
              @dragover.prevent="isDragActive = true"
              @dragleave="isDragActive = false"
              @drop.prevent="onDrop"
            >
              <i class="pi pi-cloud-upload upload-icon"></i>
              <template v-if="selectedFile">
                <p class="upload-filename">{{ selectedFile.name }}</p>
                <p class="upload-filesize">{{ formatSize(selectedFile.size) }}</p>
                <p class="change-hint">Click to change file</p>
              </template>
              <template v-else>
                <p class="upload-title">Drop a file here</p>
                <p class="upload-hint">or <span class="browse-link">browse files</span></p>
              </template>
            </div>

            <input
              v-model="title"
              type="text"
              class="name-input"
              placeholder="Name (defaults to file name after upload)"
            />

            <div v-if="uploadError" class="error-notice">
              <i class="pi pi-exclamation-circle"></i>
              <span>{{ uploadError }}</span>
            </div>
          </div>

          <!-- Footer -->
          <div class="dialog-footer">
            <button class="cancel-btn" :disabled="uploading" @click="close">Cancel</button>
            <button
              class="save-btn"
              :disabled="!selectedFile || !title.trim() || uploading"
              :class="{ 'is-loading': uploading }"
              @click="upload"
            >
              <i :class="uploading ? 'pi pi-spin pi-spinner' : 'pi pi-upload'"></i>
              {{ uploading ? 'Uploading…' : 'Upload' }}
            </button>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, watch, onMounted, onUnmounted } from 'vue'
import { useDocumentFolders } from '../../composables/useDocumentFolders'

const props = defineProps({
  visible:   { type: Boolean, required: true },
  file:      { type: Object, default: null },
  projectId: { type: String, required: true },
  parentId:  { type: String, default: null },
})

const emit = defineEmits(['close', 'uploaded'])

const { uploadDocument } = useDocumentFolders()

const fileInput    = ref(null)
const selectedFile = ref(null)
const title        = ref('')
const isDragActive = ref(false)
const uploading    = ref(false)
const uploadError  = ref(null)

watch(() => props.file, (f) => { if (f) setFile(f) })
watch(() => props.visible, (v) => { if (!v) reset() })

function reset() {
  selectedFile.value = null
  title.value        = ''
  uploadError.value  = null
  uploading.value    = false
  isDragActive.value = false
}

function setFile(f) {
  selectedFile.value = f
  uploadError.value  = null
  if (!title.value.trim()) {
    title.value = f.name.replace(/\.[^.]+$/, '')
  }
}

function close() {
  if (!uploading.value) emit('close')
}

function onFileInputChange(e) {
  const f = e.target.files?.[0]
  if (f) setFile(f)
  e.target.value = ''
}

function onDrop(e) {
  isDragActive.value = false
  const f = e.dataTransfer?.files?.[0]
  if (f) setFile(f)
}

async function upload() {
  if (!selectedFile.value || !title.value.trim() || uploading.value) return
  uploading.value   = true
  uploadError.value = null
  try {
    const response = await uploadDocument(props.projectId, selectedFile.value, props.parentId, title.value.trim())
    if (response.ok) {
      const data = await response.json()
      emit('uploaded', data.id)
      emit('close')
    } else if (response.status === 413) {
      uploadError.value = 'File is too large. Maximum size is 50 MB.'
    } else {
      uploadError.value = `Upload failed (${response.status}). Please try again.`
    }
  } catch {
    uploadError.value = 'Could not connect to the server. Please try again.'
  } finally {
    uploading.value = false
  }
}

function formatSize(bytes) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function onKeyDown(e) {
  if (e.key === 'Escape' && props.visible) close()
}

onMounted(() => window.addEventListener('keydown', onKeyDown))
onUnmounted(() => window.removeEventListener('keydown', onKeyDown))
</script>

<style scoped>
.hidden-input { display: none; }

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
  max-width: 420px;
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

.upload-zone {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  border: 2px dashed var(--border-color);
  border-radius: 12px;
  padding: 2.5rem 1.5rem;
  text-align: center;
  background: rgba(168, 85, 247, 0.03);
  cursor: pointer;
  transition: border-color 0.2s, background 0.2s;
}
.upload-zone:hover,
.upload-zone.drag-active { border-color: var(--color-purple); background: rgba(168, 85, 247, 0.07); }
.upload-zone.has-file { border-color: rgba(168, 85, 247, 0.5); background: rgba(168, 85, 247, 0.05); }

.upload-icon { font-size: 2.25rem; color: var(--text-dim); }

.upload-title { font-size: 0.95rem; font-weight: 600; color: var(--text-secondary); margin: 0; }

.upload-hint { font-size: 0.85rem; color: var(--text-dim); margin: 0; }

.browse-link { color: var(--color-purple); }

.upload-filename { font-size: 0.95rem; font-weight: 600; color: var(--text-primary); margin: 0; word-break: break-all; }

.upload-filesize { font-size: 0.8rem; color: var(--text-dim); margin: 0; }

.change-hint { font-size: 0.75rem; color: var(--text-dim); margin: 0; font-style: italic; }

.name-input {
  width: 100%;
  padding: 0.55rem 0.85rem;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-primary);
  font-size: 0.875rem;
  outline: none;
  transition: border-color 0.2s;
  box-sizing: border-box;
}
.name-input::placeholder { color: var(--text-dim); }
.name-input:focus { border-color: var(--color-purple); }

.error-notice {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  background: rgba(236, 72, 153, 0.08);
  border: 1px solid rgba(236, 72, 153, 0.25);
  border-radius: 8px;
  padding: 0.75rem 1rem;
  color: var(--color-pink-light);
  font-size: 0.85rem;
  line-height: 1.5;
}
.error-notice .pi { margin-top: 2px; flex-shrink: 0; }

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
  box-shadow: 0 0 16px var(--glow-purple);
  transition: opacity 0.2s, box-shadow 0.2s;
}
.save-btn:hover:not(:disabled) { opacity: 0.9; box-shadow: 0 0 24px var(--glow-purple); }
.save-btn:disabled { opacity: 0.45; cursor: not-allowed; box-shadow: none; }

.dialog-enter-active, .dialog-leave-active { transition: opacity 0.2s ease; }
.dialog-enter-active .dialog, .dialog-leave-active .dialog { transition: transform 0.2s ease, opacity 0.2s ease; }
.dialog-enter-from, .dialog-leave-to { opacity: 0; }
.dialog-enter-from .dialog, .dialog-leave-to .dialog { transform: translateY(-12px) scale(0.97); opacity: 0; }
</style>
