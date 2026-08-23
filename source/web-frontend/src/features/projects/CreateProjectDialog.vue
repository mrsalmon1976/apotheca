<template>
  <Teleport to="body">
    <Transition name="dialog">
      <div v-if="visible" class="dialog-backdrop" @click.self="close">
        <div class="dialog" role="dialog" aria-modal="true" aria-labelledby="dialog-title">

          <!-- Header -->
          <div class="dialog-header">
            <div class="dialog-header-left">
              <div class="dialog-icon"><i class="pi pi-plus-circle"></i></div>
              <h2 id="dialog-title" class="dialog-title">New Project</h2>
            </div>
            <button class="icon-btn" title="Close" @click="close">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Body -->
          <div class="dialog-body">

            <div class="field" :class="{ 'has-error': fieldError.name }">
              <label class="field-label">Name <span class="required">*</span></label>
              <input
                ref="nameInput"
                v-model="form.name"
                class="field-input"
                type="text"
                placeholder="Project name"
                @keydown.enter="save"
              />
              <span v-if="fieldError.name" class="field-error-msg">{{ fieldError.name }}</span>
            </div>

            <div class="field">
              <label class="field-label">Summary</label>
              <textarea
                v-model="form.summary"
                class="field-input field-textarea"
                placeholder="Provide a brief description of this project…"
                rows="3"
              ></textarea>
            </div>

            <div v-if="otherMembers.length > 0" class="field">
              <label class="field-label">Members</label>
              <div class="member-list">
                <div v-for="member in otherMembers" :key="member.userId" class="member-row">
                  <label class="member-check">
                    <input type="checkbox" v-model="selectedUserIds" :value="member.userId" />
                    <span>{{ member.displayName }}</span>
                  </label>
                  <select
                    v-model="memberRoles[member.userId]"
                    class="member-role-select"
                    :disabled="!selectedUserIds.includes(member.userId)"
                  >
                    <option value="ADMIN">Admin</option>
                    <option value="CONTRIBUTOR">Contributor</option>
                    <option value="VIEWER">Viewer</option>
                  </select>
                </div>
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
              {{ saving ? 'Creating...' : 'Create Project' }}
            </button>
          </div>

        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref, reactive, nextTick, watch, onMounted, onUnmounted, computed } from 'vue'
import { useProjects } from '../../composables/useProjects'
import { useWorkspaceMembers } from '../../composables/useWorkspaceMembers'
import { useAuth } from '../../composables/useAuth'

const props = defineProps({
  visible:     { type: Boolean, required: true },
  workspaceId: { type: String, default: null },
})

const emit = defineEmits(['close', 'saved'])

const { createProject } = useProjects()
const { members, loadMembers } = useWorkspaceMembers()
const { user } = useAuth()

const nameInput = ref(null)
const saving = ref(false)
const saveError = ref(null)

const form = reactive({ name: '', summary: '' })
const fieldError = reactive({ name: null })
const selectedUserIds = ref([])
const memberRoles = reactive({})

const otherMembers = computed(() => members.value.filter(m => m.email !== user.value?.email))

watch(() => props.visible, (val) => {
  if (val) {
    resetForm()
    if (props.workspaceId) loadMembers(props.workspaceId)
    nextTick(() => nameInput.value?.focus())
  }
})

function onKeyDown(e) {
  if (e.key === 'Escape' && props.visible) close()
}

onMounted(() => window.addEventListener('keydown', onKeyDown))
onUnmounted(() => window.removeEventListener('keydown', onKeyDown))

function resetForm() {
  form.name = ''
  form.summary = ''
  fieldError.name = null
  saveError.value = null
  saving.value = false
  selectedUserIds.value = []
  for (const key of Object.keys(memberRoles)) delete memberRoles[key]
}

function close() {
  if (!saving.value) emit('close')
}

async function save() {
  fieldError.name = null
  saveError.value = null

  if (!form.name.trim()) {
    fieldError.name = 'Name is required.'
    nameInput.value?.focus()
    return
  }

  if (!props.workspaceId) {
    saveError.value = 'No workspace selected.'
    return
  }

  saving.value = true
  try {
    const members = selectedUserIds.value.map(userId => ({
      userId,
      projectRole: memberRoles[userId] ?? 'CONTRIBUTOR',
    }))

    const created = await createProject(props.workspaceId, form.name.trim(), form.summary.trim() || null, members)

    if (created) {
      emit('saved')
      emit('close')
    } else {
      saveError.value = 'Could not create the project. Please try again.'
    }
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
  max-width: 520px;
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
.icon-btn:hover {
  color: var(--text-primary);
  background: var(--bg-hover);
}

.dialog-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  max-height: 60vh;
  overflow-y: auto;
}

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
  min-height: 70px;
  line-height: 1.5;
}

.has-error .field-input {
  border-color: var(--color-pink);
}

.field-error-msg {
  font-size: 0.78rem;
  color: var(--color-pink);
}

.member-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  max-height: 180px;
  overflow-y: auto;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 0.5rem 0.75rem;
}

.member-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.member-check {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: var(--text-primary);
  cursor: pointer;
}

.member-role-select {
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  color: var(--text-secondary);
  font-size: 0.8rem;
  padding: 0.25rem 0.5rem;
}
.member-role-select:disabled { opacity: 0.4; }

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
