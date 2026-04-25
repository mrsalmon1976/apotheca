<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div v-if="visible" class="dialog-backdrop" @click.self="close">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title">

          <!-- Header -->
          <div class="dialog-header">
            <div class="dialog-header-left">
              <div class="dialog-icon"><i class="pi pi-trash"></i></div>
              <h2 id="dialog-title" class="dialog-title">
                {{ isFolder ? 'Delete Folder' : 'Delete Document' }}
              </h2>
            </div>
            <button class="icon-btn" title="Close" @click="close">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Body -->
          <div class="dialog-body">
            <p class="confirm-message">
              Are you sure you want to delete
              <strong>"{{ itemTitle }}"</strong>?
            </p>
            <p v-if="isFolder" class="folder-warning">
              <i class="pi pi-exclamation-triangle"></i>
              All folders and documents within this folder will also be deleted.
            </p>
            <p class="irreversible-note">This item will be available for restore for 30 days, after which it will be permanently deleted.</p>

            <div v-if="deleteError" class="delete-error">
              <i class="pi pi-exclamation-triangle"></i>
              <span>{{ deleteError }}</span>
            </div>
          </div>

          <!-- Footer -->
          <div class="dialog-footer">
            <button class="cancel-btn" :disabled="deleting" @click="close">Cancel</button>
            <button class="delete-btn" :disabled="deleting" @click="confirm">
              <i :class="deleting ? 'pi pi-spin pi-spinner' : 'pi pi-trash'"></i>
              {{ deleting ? 'Deleting...' : 'Delete' }}
            </button>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, watch, onMounted, onUnmounted } from 'vue'

const props = defineProps({
  visible:   { type: Boolean, required: true },
  itemTitle: { type: String, default: '' },
  isFolder:  { type: Boolean, default: false },
})

const emit = defineEmits(['close', 'confirm'])

const deleting    = ref(false)
const deleteError = ref(null)

watch(() => props.visible, (val) => {
  if (val) { deleting.value = false; deleteError.value = null }
})

function onKeyDown(e) { if (e.key === 'Escape' && props.visible) close() }
onMounted(() => window.addEventListener('keydown', onKeyDown))
onUnmounted(() => window.removeEventListener('keydown', onKeyDown))

function close() { if (!deleting.value) emit('close') }

async function confirm() {
  deleting.value    = true
  deleteError.value = null
  emit('confirm', { setError, done })
}

function setError(msg) { deleteError.value = msg; deleting.value = false }
function done() { deleting.value = false; emit('close') }
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
  border: 1px solid rgba(236, 72, 153, 0.4);
  border-radius: 16px;
  box-shadow: 0 0 48px rgba(236, 72, 153, 0.15), 0 24px 64px rgba(0, 0, 0, 0.7);
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
  background: linear-gradient(135deg, #ec4899, #be185d);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1rem;
  color: white;
  flex-shrink: 0;
  box-shadow: 0 0 14px rgba(236, 72, 153, 0.4);
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

.dialog-body { padding: 1.5rem; display: flex; flex-direction: column; gap: 0.75rem; }

.confirm-message { font-size: 0.95rem; color: var(--text-primary); margin: 0; line-height: 1.5; }

.folder-warning {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: var(--color-pink-light);
  background: rgba(236, 72, 153, 0.08);
  border: 1px solid rgba(236, 72, 153, 0.25);
  border-radius: 8px;
  padding: 0.65rem 0.9rem;
  margin: 0;
  line-height: 1.5;
}
.folder-warning .pi { margin-top: 2px; flex-shrink: 0; }

.irreversible-note { font-size: 0.8rem; color: var(--text-muted); margin: 0; }

.delete-error {
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
.delete-error .pi { margin-top: 2px; flex-shrink: 0; }

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

.delete-btn {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1.25rem;
  background: linear-gradient(135deg, #ec4899, #be185d);
  border: none;
  border-radius: 8px;
  color: white;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s, box-shadow 0.2s;
  box-shadow: 0 0 14px rgba(236, 72, 153, 0.35);
}
.delete-btn:hover:not(:disabled) { opacity: 0.9; box-shadow: 0 0 22px rgba(236, 72, 153, 0.5); }
.delete-btn:disabled { opacity: 0.55; cursor: not-allowed; box-shadow: none; }

.dialog-enter-active, .dialog-leave-active { transition: opacity 0.2s ease; }
.dialog-enter-active .dialog, .dialog-leave-active .dialog { transition: transform 0.2s ease, opacity 0.2s ease; }
.dialog-enter-from, .dialog-leave-to { opacity: 0; }
.dialog-enter-from .dialog, .dialog-leave-to .dialog { transform: translateY(-12px) scale(0.97); opacity: 0; }
</style>
