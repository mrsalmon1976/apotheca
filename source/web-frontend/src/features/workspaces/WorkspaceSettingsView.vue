<template>
  <div class="page-layout">
    <div v-if="sidebarOpen" class="sidebar-backdrop" @click="sidebarOpen = false" />

    <AccountSidebar :open="sidebarOpen" @close="sidebarOpen = false" />

    <div class="main-body">
      <div class="content-header">
        <div class="content-header-left">
          <button class="hamburger-btn" title="Toggle menu" @click="sidebarOpen = !sidebarOpen">
            <i class="pi pi-bars"></i>
          </button>
          <h1 class="content-title">Workspace Settings</h1>
        </div>
      </div>

      <div class="settings-container">
        <Tabs value="details" @update:value="onTabChange">
          <TabList>
            <Tab value="details">Details</Tab>
            <Tab value="people">People</Tab>
          </TabList>
          <TabPanels>
            <TabPanel value="details">
              <div class="tab-content">
                <div class="field-block">
                  <label class="field-label" for="workspace-name">Workspace Name</label>
                  <InputText
                    id="workspace-name"
                    v-model="nameInput"
                    class="name-input"
                    placeholder="Workspace name"
                    :maxlength="100"
                    :disabled="!isWorkspaceAdmin"
                  />
                </div>

                <button
                  v-if="isWorkspaceAdmin"
                  class="save-btn"
                  :disabled="!hasChanges || saving"
                  @click="save"
                >
                  <i v-if="saving" class="pi pi-spin pi-spinner"></i>
                  <span>{{ saving ? 'Saving…' : 'Save' }}</span>
                </button>
              </div>
            </TabPanel>

            <TabPanel value="people">
              <div class="tab-content activity-tab">
                <div v-if="isWorkspaceAdmin" class="add-member-row">
                  <input
                    v-model="newMemberEmail"
                    class="email-input"
                    type="email"
                    placeholder="Email address"
                    @keydown.enter="addMember"
                  />
                  <select v-model="newMemberRole" class="member-role-select-inline">
                    <option value="VIEWER">Viewer</option>
                    <option value="ADMIN">Admin</option>
                  </select>
                  <button class="restore-btn" :disabled="!newMemberEmail.trim() || addingMember" @click="addMember">
                    <i :class="addingMember ? 'pi pi-spin pi-spinner' : 'pi pi-plus'"></i> Add
                  </button>
                </div>
                <p v-if="addMemberError" class="modal-error">
                  <i class="pi pi-exclamation-triangle"></i> {{ addMemberError }}
                </p>

                <div v-if="membersLoading" class="activity-loading">
                  <i class="pi pi-spin pi-spinner"></i> Loading…
                </div>
                <div v-else-if="members.length === 0" class="activity-empty">
                  No members yet.
                </div>
                <table v-else class="activity-table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Role</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="member in members" :key="member.userId">
                      <td class="message-cell">{{ member.displayName }}</td>
                      <td>{{ member.email }}</td>
                      <td>
                        <select
                          v-if="isWorkspaceAdmin"
                          :value="member.workspaceRole"
                          class="member-role-select-inline"
                          @change="changeMemberRole(member.userId, $event.target.value)"
                        >
                          <option value="ADMIN">Admin</option>
                          <option value="VIEWER">Viewer</option>
                        </select>
                        <span v-else>{{ formatRole(member.workspaceRole) }}</span>
                      </td>
                      <td class="action-cell">
                        <button v-if="isWorkspaceAdmin" class="restore-btn" @click="removeMember(member.userId)">
                          <i class="pi pi-times"></i> Remove
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import Tabs from 'primevue/tabs'
import TabList from 'primevue/tablist'
import Tab from 'primevue/tab'
import TabPanels from 'primevue/tabpanels'
import TabPanel from 'primevue/tabpanel'
import InputText from 'primevue/inputtext'
import AccountSidebar from '../../components/AccountSidebar.vue'
import { useWorkspaces } from '../../composables/useWorkspaces'
import { useWorkspaceMembers } from '../../composables/useWorkspaceMembers'

const route = useRoute()
const toast = useToast()
const { workspaces, saveWorkspace } = useWorkspaces()
const { members, loading: membersLoading, loadMembers, addMember: addWorkspaceMember, saveMemberRole, removeMember: removeWorkspaceMember } = useWorkspaceMembers()

const sidebarOpen = ref(window.innerWidth >= 768)
const workspaceId = computed(() => route.params.workspaceId)
const currentWorkspaceEntry = computed(() => workspaces.value.find(w => w.id === workspaceId.value))
const isWorkspaceAdmin = computed(() => currentWorkspaceEntry.value?.workspaceRole === 'ADMIN')

// --- Details tab ---
const nameInput = ref('')
const saving = ref(false)

watch(currentWorkspaceEntry, (w) => {
  if (w) nameInput.value = w.name
}, { immediate: true })

const hasChanges = computed(() => {
  const w = currentWorkspaceEntry.value
  if (!w) return false
  return nameInput.value.trim() !== '' && nameInput.value.trim() !== w.name
})

async function save() {
  if (!hasChanges.value) return
  saving.value = true
  const success = await saveWorkspace(workspaceId.value, nameInput.value.trim())
  if (success) {
    toast.add({ severity: 'success', summary: 'Workspace saved', life: 3000 })
  }
  saving.value = false
}

// --- People tab ---
const peopleLoaded = ref(false)
const newMemberEmail = ref('')
const newMemberRole = ref('VIEWER')
const addingMember = ref(false)
const addMemberError = ref(null)

function onTabChange(tab) {
  if (tab === 'people') loadPeople()
}

async function loadPeople(force = false) {
  if (peopleLoaded.value && !force) return
  await loadMembers(workspaceId.value)
  peopleLoaded.value = true
}

async function addMember() {
  if (!newMemberEmail.value.trim()) return
  addingMember.value = true
  addMemberError.value = null
  try {
    const response = await addWorkspaceMember(workspaceId.value, newMemberEmail.value.trim(), newMemberRole.value)
    if (response.ok) {
      newMemberEmail.value = ''
      newMemberRole.value = 'VIEWER'
    } else if (response.status === 400 || response.status === 409) {
      const body = await response.json()
      addMemberError.value = body.error ?? 'Could not add that member.'
    } else {
      addMemberError.value = `Unexpected error (${response.status}). Please try again.`
    }
  } finally {
    addingMember.value = false
  }
}

function changeMemberRole(userId, role) {
  saveMemberRole(workspaceId.value, userId, role)
}

function removeMember(userId) {
  removeWorkspaceMember(workspaceId.value, userId)
}

function formatRole(role) {
  if (!role) return ''
  return role.charAt(0) + role.slice(1).toLowerCase()
}
</script>

<style scoped>
.page-layout {
  display: flex;
  flex: 1;
  overflow: hidden;
  height: calc(100vh - 60px);
}

.main-body {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem 2rem;
  background: var(--bg-primary);
}

.content-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
}

.content-header-left {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.hamburger-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 1.1rem;
  padding: 0.25rem;
  border-radius: 6px;
  transition: color 0.2s;
  display: flex;
  align-items: center;
}
.hamburger-btn:hover { color: var(--color-purple); }

.content-title {
  font-size: 1.4rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.settings-container {
  max-width: 900px;
}

.tab-content {
  padding: 1.5rem 0 0;
}

.field-block {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1.25rem;
}

.field-label {
  font-size: 0.8rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--text-muted);
}

.name-input { width: 100%; }

:deep(.name-input.p-inputtext) {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  border-radius: 8px;
  padding: 0.5rem 0.875rem;
  font-size: 0.9rem;
  width: 100%;
  transition: border-color 0.2s, box-shadow 0.2s;
}
:deep(.name-input.p-inputtext:focus) {
  border-color: var(--color-purple);
  box-shadow: 0 0 0 1px var(--color-purple);
  outline: none;
}

.save-btn {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 1.25rem;
  background: var(--gradient-brand);
  border: none;
  border-radius: 8px;
  color: white;
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s, box-shadow 0.2s;
  box-shadow: 0 0 12px var(--glow-purple);
  white-space: nowrap;
}
.save-btn:hover:not(:disabled) { opacity: 0.9; box-shadow: 0 0 20px var(--glow-purple); }
.save-btn:disabled { opacity: 0.4; cursor: default; box-shadow: none; }

.activity-tab { padding-top: 1.25rem; }

.add-member-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.email-input {
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-primary);
  font-size: 0.875rem;
  padding: 0.5rem 0.75rem;
  min-width: 240px;
  outline: none;
  transition: border-color 0.2s;
}
.email-input:focus { border-color: var(--color-purple); }

.member-role-select-inline {
  background: var(--bg-input);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  color: var(--text-secondary);
  font-size: 0.8rem;
  padding: 0.4rem 0.5rem;
}

.activity-loading,
.activity-error,
.activity-empty {
  color: var(--text-muted);
  font-size: 0.875rem;
  padding: 1rem 0;
}

.activity-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;
}

.activity-table th {
  text-align: left;
  font-size: 0.7rem;
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--text-muted);
  padding: 0.4rem 0.75rem;
  border-bottom: 1px solid var(--border-color);
}

.activity-table td {
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid color-mix(in srgb, var(--border-color) 50%, transparent);
  color: var(--text-secondary);
  vertical-align: middle;
}

.activity-table tbody tr:hover td {
  background: var(--bg-card);
}

.message-cell { color: var(--text-primary); }

.action-cell {
  white-space: nowrap;
  text-align: right;
}

.restore-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.3rem 0.75rem;
  background: transparent;
  border: 1px solid var(--border-purple);
  border-radius: 6px;
  color: var(--color-purple);
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}
.restore-btn:hover:not(:disabled) { background: var(--bg-active); box-shadow: 0 0 8px var(--glow-purple); }
.restore-btn:disabled { opacity: 0.4; cursor: not-allowed; }

.modal-error {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 0.5rem;
  padding: 0.5rem 0.75rem;
  background: rgba(236, 72, 153, 0.08);
  border: 1px solid rgba(236, 72, 153, 0.25);
  border-radius: 8px;
  color: var(--color-pink-light);
  font-size: 0.83rem;
}

/* PrimeVue Tabs theming */
:deep(.p-tabs) { background: transparent; }
:deep(.p-tablist) { background: transparent; border-bottom: 1px solid var(--border-color); }
:deep(.p-tab) {
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 0.875rem;
  font-weight: 500;
  padding: 0.6rem 1rem;
  cursor: pointer;
  transition: color 0.2s;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
}
:deep(.p-tab:hover) { color: var(--text-primary); }
:deep(.p-tab[data-p-active="true"]) {
  color: var(--color-pink);
  border-bottom-color: var(--color-purple);
}
:deep(.p-tabpanels) { background: transparent; padding: 0; }
:deep(.p-tabpanel) { padding: 0; }

.sidebar-backdrop { display: none; }

@media (max-width: 767px) {
  .sidebar-backdrop {
    display: block;
    position: fixed;
    inset: 0;
    top: 60px;
    background: rgba(0, 0, 0, 0.6);
    z-index: 99;
  }
  .main-body { padding: 1rem; }
  .save-btn { justify-content: center; }
}
</style>
