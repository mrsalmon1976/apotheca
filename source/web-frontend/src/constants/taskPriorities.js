// Single source of truth for task priorities. Values match DataConstants.TaskPriority
// in the API. Listed lowest to highest; `rank` drives sorting (higher = more urgent).
export const TASK_PRIORITIES = [
  { value: 'NONE',   label: 'None',   rank: 0, color: '#524e65' },
  { value: 'LOW',    label: 'Low',    rank: 1, color: '#7a7590' },
  { value: 'MEDIUM', label: 'Medium', rank: 2, color: '#a855f7' },
  { value: 'HIGH',   label: 'High',   rank: 3, color: '#ec4899' },
  { value: 'URGENT', label: 'Urgent', rank: 4, color: '#f87171' },
]

export const PRIORITY_RANK = Object.fromEntries(TASK_PRIORITIES.map(p => [p.value, p.rank]))

export const PRIORITY_COLORS = Object.fromEntries(TASK_PRIORITIES.map(p => [p.value, p.color]))
