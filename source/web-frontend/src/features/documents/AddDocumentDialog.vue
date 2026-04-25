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
            <div class="upload-zone">
              <i class="pi pi-cloud-upload upload-icon"></i>
              <template v-if="file">
                <p class="upload-filename">{{ file.name }}</p>
                <p class="upload-filesize">{{ formatSize(file.size) }}</p>
              </template>
              <template v-else>
                <p class="upload-title">Drop a file here</p>
                <p class="upload-hint">or <span class="browse-link">browse files</span></p>
              </template>
            </div>

            <div class="coming-soon-notice">
              <i class="pi pi-info-circle"></i>
              <span>File upload is coming soon. Document content will be available once storage is set up.</span>
            </div>
          </div>

          <!-- Footer -->
          <div class="dialog-footer">
            <button class="cancel-btn" @click="close">Cancel</button>
            <button class="save-btn" disabled>
              <i class="pi pi-upload"></i>
              Upload
            </button>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { onMounted, onUnmounted } from 'vue'

const props = defineProps({
  visible: { type: Boolean, required: true },
  file:    { type: Object, default: null },
})

const emit = defineEmits(['close'])

function close() { emit('close') }

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
  transition: border-color 0.2s;
}

.upload-icon { font-size: 2.25rem; color: var(--text-dim); }

.upload-title { font-size: 0.95rem; font-weight: 600; color: var(--text-secondary); margin: 0; }

.upload-hint { font-size: 0.85rem; color: var(--text-dim); margin: 0; }

.browse-link { color: var(--color-purple); }

.upload-filename { font-size: 0.95rem; font-weight: 600; color: var(--text-primary); margin: 0; word-break: break-all; }

.upload-filesize { font-size: 0.8rem; color: var(--text-dim); margin: 0; }

.coming-soon-notice {
  display: flex;
  align-items: flex-start;
  gap: 0.6rem;
  background: rgba(168, 85, 247, 0.07);
  border: 1px solid rgba(168, 85, 247, 0.2);
  border-radius: 8px;
  padding: 0.75rem 1rem;
  color: var(--color-purple-light);
  font-size: 0.85rem;
  line-height: 1.5;
}
.coming-soon-notice .pi { margin-top: 2px; flex-shrink: 0; }

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
.cancel-btn:hover { border-color: var(--border-purple); color: var(--text-primary); }

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
  cursor: not-allowed;
  opacity: 0.45;
  box-shadow: none;
}

.dialog-enter-active, .dialog-leave-active { transition: opacity 0.2s ease; }
.dialog-enter-active .dialog, .dialog-leave-active .dialog { transition: transform 0.2s ease, opacity 0.2s ease; }
.dialog-enter-from, .dialog-leave-to { opacity: 0; }
.dialog-enter-from .dialog, .dialog-leave-to .dialog { transform: translateY(-12px) scale(0.97); opacity: 0; }
</style>
