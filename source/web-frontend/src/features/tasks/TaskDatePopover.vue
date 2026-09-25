<template>
  <Popover ref="popover" :pt="{ root: { class: 'task-date-popover' } }">
    <div class="date-popover">
      <div class="date-popover-header">
        <span class="date-popover-label">Date</span>
        <span class="date-popover-current">{{ currentLabel }}</span>
      </div>

      <div class="quick-actions">
        <button
          v-for="action in quickActions"
          :key="action.id"
          class="quick-action"
          :class="action.id"
          type="button"
          :title="action.title"
          @click="pick(action.date())"
        >
          <i :class="action.icon"></i>
          <span class="quick-action-label">{{ action.label }}</span>
        </button>
      </div>

      <DatePicker
        :model-value="selectedDate"
        inline
        class="date-popover-calendar"
        :pt="{ panel: { class: 'task-date-panel' } }"
        @update:model-value="pick"
      />
    </div>
  </Popover>
</template>

<script setup>
import { ref, computed } from 'vue'
import Popover from 'primevue/popover'
import DatePicker from 'primevue/datepicker'

const emit = defineEmits(['pick'])

const popover = ref(null)
const task = ref(null)

function startOfToday() {
  const d = new Date()
  d.setHours(0, 0, 0, 0)
  return d
}

function addDays(date, days) {
  const d = new Date(date)
  d.setDate(d.getDate() + days)
  return d
}

// "Next week" follows Todoist: the coming Monday (a full week away if today is Monday).
function nextMonday() {
  const today = startOfToday()
  const daysUntil = ((8 - today.getDay()) % 7) || 7
  return addDays(today, daysUntil)
}

const quickActions = computed(() => [
  { id: 'tomorrow',  label: 'Tomorrow',  icon: 'pi pi-sun',         date: () => addDays(startOfToday(), 1) },
  { id: 'next-week', label: 'Next week', icon: 'pi pi-arrow-right', date: nextMonday },
  { id: 'no-date',   label: 'No date',   icon: 'pi pi-ban',         date: () => null },
].map(a => {
  const d = a.date()
  return { ...a, title: d ? `${a.label} (${formatLong(d)})` : a.label }
}))

// Task dueAt comes from the API as an ISO timestamp whose date part is the chosen day
// (see NewTaskDialog.save()); only the date part is meaningful.
const selectedDate = computed(() => {
  const value = task.value?.dueAt
  if (!value) return null
  const [year, month, day] = value.split('T')[0].split('-').map(Number)
  return new Date(year, month - 1, day)
})

const currentLabel = computed(() => selectedDate.value ? formatLong(selectedDate.value) : 'No date')

function formatLong(date) {
  return date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' })
}

function pick(date) {
  const t = task.value
  popover.value?.hide()
  if (t) emit('pick', t, date)
}

function toggle(event, forTask) {
  // Re-opening for a different task should move the popover rather than close it.
  // currentTarget is only set during dispatch, so capture it before deferring.
  if (task.value && task.value.id !== forTask.id) {
    const target = event.currentTarget
    popover.value.hide()
    task.value = forTask
    requestAnimationFrame(() => popover.value.show(event, target))
    return
  }
  task.value = forTask
  popover.value.toggle(event)
}

defineExpose({ toggle })
</script>

<style scoped>
.date-popover {
  display: flex;
  flex-direction: column;
  width: 18.5rem;
  max-width: calc(100vw - 2rem);
}

.date-popover-header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.85rem 1rem 0.5rem;
}

.date-popover-label {
  font-size: 0.9rem;
  font-weight: 700;
  color: var(--text-primary);
}

.date-popover-current {
  font-size: 0.78rem;
  color: var(--color-purple-light);
  white-space: nowrap;
}

.quick-actions {
  display: flex;
  gap: 0.25rem;
  padding: 0 0.6rem 0.6rem;
  border-bottom: 1px solid var(--border-color);
}

.quick-action {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.3rem;
  padding: 0.5rem 0.25rem;
  background: transparent;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.15s;
}
.quick-action:hover { background: var(--bg-hover); }
.quick-action .pi { font-size: 1.15rem; }
.quick-action.tomorrow  .pi { color: #f59e0b; }
.quick-action.next-week .pi { color: var(--color-purple); }
.quick-action.no-date   .pi { color: var(--text-muted); }

.quick-action-label {
  font-size: 0.72rem;
  color: var(--text-secondary);
  white-space: nowrap;
}

/* The inline panel reuses the global .task-date-panel theme (main.css) but sits inside
   the popover's own chrome, so drop its standalone border/shadow. */
:deep(.date-popover-calendar .task-date-panel) {
  border: none !important;
  box-shadow: none !important;
  border-radius: 0 !important;
  background: transparent !important;
}
</style>
