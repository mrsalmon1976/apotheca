<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div v-if="visible" class="dialog-backdrop" @click.self="close">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title">

          <!-- Header -->
          <div class="dialog-header">
            <div class="dialog-header-left">
              <div class="dialog-icon"><i :class="task ? 'pi pi-pencil' : 'pi pi-plus-circle'"></i></div>
              <h2 id="dialog-title" class="dialog-title">{{ task ? 'Edit Task' : 'New Task' }}</h2>
            </div>
            <button class="icon-btn" title="Close" @click="close">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Body -->
          <div class="dialog-body">

            <!-- Title -->
            <div class="field" :class="{ 'has-error': fieldError.title }">
              <label class="field-label">Title <span class="required">*</span></label>
              <input
                ref="titleInput"
                v-model="form.title"
                class="field-input"
                type="text"
                placeholder="What needs to be done?"
                @keydown.enter="save"
              />
              <span v-if="fieldError.title" class="field-error-msg">{{ fieldError.title }}</span>
            </div>

            <!-- Priority -->
            <div class="field">
              <label class="field-label">Priority</label>
              <div class="priority-group">
                <button
                  v-for="p in priorities"
                  :key="p.value"
                  class="priority-btn"
                  :class="[p.cls, { active: form.priority === p.value }]"
                  type="button"
                  @click="form.priority = p.value"
                >
                  {{ p.label }}
                </button>
              </div>
            </div>

            <!-- Due Date -->
            <div class="field">
              <label class="field-label">Due Date</label>
              <input v-model="form.dueAt" class="field-input field-date" type="date" />
            </div>

            <!-- Notes -->
            <div class="field">
              <label class="field-label">Notes</label>
              <textarea
                v-model="form.notes"
                class="field-input field-textarea"
                placeholder="Add any additional notes..."
                rows="3"
              ></textarea>
            </div>

            <!-- API error -->
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
              {{ saving ? 'Saving...' : 'Save Task' }}
            </button>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, reactive, nextTick, watch, onMounted, onUnmounted } from 'vue'
import { useProjectTasks } from '../../composables/useProjectTasks'

const props = defineProps({
  visible:   { type: Boolean, required: true },
  projectId: { type: String, required: true },
  task:      { type: Object, default: null },
})

const emit = defineEmits(['close', 'saved'])

const { saveTask } = useProjectTasks()

const titleInput = ref(null)
const saving = ref(false)
const saveError = ref(null)

const form = reactive({
  title: '',
  priority: 'NONE',
  dueAt: '',
  notes: '',
})

const fieldError = reactive({ title: null })

const priorities = [
  { value: 'NONE',   label: 'None',   cls: 'p-none'   },
  { value: 'LOW',    label: 'Low',    cls: 'p-low'    },
  { value: 'MEDIUM', label: 'Medium', cls: 'p-medium' },
  { value: 'HIGH',   label: 'High',   cls: 'p-high'   },
  { value: 'URGENT', label: 'Urgent', cls: 'p-urgent' },
]

watch(() => props.visible, (val) => {
  if (val) {
    resetForm()
    nextTick(() => titleInput.value?.focus())
  }
})

function onKeyDown(e) {
  if (e.key === 'Escape' && props.visible) close()
}

onMounted(() => window.addEventListener('keydown', onKeyDown))
onUnmounted(() => window.removeEventListener('keydown', onKeyDown))

function resetForm() {
  const t = props.task
  form.title    = t?.title    ?? ''
  form.priority = t?.priority ?? 'NONE'
  form.dueAt    = t?.dueAt    ? t.dueAt.split('T')[0] : ''
  form.notes    = t?.notes    ?? ''
  fieldError.title = null
  saveError.value  = null
  saving.value     = false
}

function close() {
  if (!saving.value) emit('close')
}

async function save() {
  fieldError.title = null
  saveError.value  = null

  if (!form.title.trim()) {
    fieldError.title = 'Title is required.'
    titleInput.value?.focus()
    return
  }

  saving.value = true
  try {
    const payload = {
      id:       props.task?.id ?? null,
      title:    form.title.trim(),
      priority: form.priority,
      notes:    form.notes.trim() || null,
      dueAt:    form.dueAt ? new Date(form.dueAt).toISOString() : null,
    }

    const response = await saveTask(props.projectId, payload)

    if (response.ok) {
      emit('saved')
      emit('close')
    } else if (response.status === 400) {
      const body = await response.json()
      saveError.value = body.errors?.join(' ') ?? 'Invalid request.'
    } else if (response.status === 403) {
      saveError.value = 'You do not have permission to add tasks to this project.'
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
/* ── Backdrop ── */
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

/* ── Dialog ── */
.dialog {
  background: var(--bg-card);
  border: 1px solid var(--border-purple);
  border-radius: 16px;
  box-shadow: 0 0 48px var(--glow-purple), 0 24px 64px rgba(0, 0, 0, 0.7);
  width: 100%;
  max-width: 520px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* ── Header ── */
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
.icon-btn:hover {
  color: var(--text-primary);
  background: var(--bg-hover);
}

/* ── Body ── */
.dialog-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

/* ── Fields ── */
.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.required {
  color: var(--color-pink);
  margin-left: 2px;
}

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

.field-textarea {
  resize: vertical;
  min-height: 80px;
  line-height: 1.5;
}

.field-date {
  color-scheme: dark;
}

.has-error .field-input {
  border-color: var(--color-pink);
}
.has-error .field-input:focus {
  box-shadow: 0 0 0 3px rgba(236, 72, 153, 0.15);
}

.field-error-msg {
  font-size: 0.78rem;
  color: var(--color-pink);
}

/* ── Priority toggle group ── */
.priority-group {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.priority-btn {
  padding: 0.35rem 0.85rem;
  border-radius: 999px;
  border: 1px solid transparent;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  background: var(--bg-input);
  color: var(--text-muted);
}
.priority-btn:hover { color: var(--text-primary); }

.priority-btn.p-none.active    { background: rgba(122,117,144,0.2);  color: #b8b4c8; border-color: rgba(122,117,144,0.4); }
.priority-btn.p-low.active     { background: rgba(139,92,246,0.15);  color: #8b5cf6; border-color: rgba(139,92,246,0.4); }
.priority-btn.p-medium.active  { background: rgba(168,85,247,0.15);  color: #a855f7; border-color: rgba(168,85,247,0.45); }
.priority-btn.p-high.active    { background: rgba(236,72,153,0.15);  color: #ec4899; border-color: rgba(236,72,153,0.4); }
.priority-btn.p-urgent.active  { background: rgba(239,68,68,0.15);   color: #f87171; border-color: rgba(239,68,68,0.4); }

.priority-btn.p-none:hover     { color: #b8b4c8; }
.priority-btn.p-low:hover      { color: #8b5cf6; }
.priority-btn.p-medium:hover   { color: #a855f7; }
.priority-btn.p-high:hover     { color: #ec4899; }
.priority-btn.p-urgent:hover   { color: #f87171; }

/* ── Save error ── */
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

/* ── Footer ── */
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
.cancel-btn:hover:not(:disabled) {
  border-color: var(--border-purple);
  color: var(--text-primary);
}
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

/* ── Transition ── */
.dialog-enter-active,
.dialog-leave-active {
  transition: opacity 0.2s ease;
}
.dialog-enter-active .dialog,
.dialog-leave-active .dialog {
  transition: transform 0.2s ease, opacity 0.2s ease;
}
.dialog-enter-from,
.dialog-leave-to {
  opacity: 0;
}
.dialog-enter-from .dialog,
.dialog-leave-to .dialog {
  transform: translateY(-12px) scale(0.97);
  opacity: 0;
}
</style>
