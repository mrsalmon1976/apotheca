<template>
  <li class="mindmap-node">
    <div class="node-card">
      <button
        v-if="node.children.length"
        class="collapse-btn"
        :title="node.collapsed ? 'Expand' : 'Collapse'"
        @click="node.collapsed = !node.collapsed"
      >
        <i :class="node.collapsed ? 'pi pi-chevron-right' : 'pi pi-chevron-down'"></i>
      </button>

      <div class="node-content">
        <input
          v-model="node.header"
          class="node-header-input"
          type="text"
          placeholder="Untitled"
          @keydown.enter.prevent="$event.target.blur()"
        />

        <ul v-if="!editingBody" class="node-body-list" @click="startEditingBody">
          <li v-for="(line, i) in bodyLines" :key="i">{{ line }}</li>
          <li v-if="bodyLines.length === 0" class="body-placeholder">Click to add notes…</li>
        </ul>
        <textarea
          v-else
          ref="bodyTextarea"
          v-model="draftBody"
          class="node-body-textarea"
          rows="1"
          @blur="commitBody"
          @keydown.escape="cancelEditingBody"
        />
      </div>

      <div class="node-actions">
        <button class="node-action-btn" title="Add child" @click="addChild">
          <i class="pi pi-plus"></i>
        </button>
        <button
          v-if="!isRoot"
          class="node-action-btn node-delete-btn"
          title="Delete node"
          @click="$emit('delete-self')"
        >
          <i class="pi pi-trash"></i>
        </button>
      </div>
    </div>

    <ul v-if="node.children.length && !node.collapsed" class="mindmap-children">
      <MindmapNode
        v-for="child in node.children"
        :key="child.id"
        :node="child"
        @delete-self="removeChild(child.id)"
      />
    </ul>
  </li>
</template>

<script setup>
import { ref, computed, nextTick } from 'vue'
import { makeNode } from '../../composables/useMindmaps'

const props = defineProps({
  node: { type: Object, required: true },
  isRoot: { type: Boolean, default: false }
})
defineEmits(['delete-self'])

const bodyLines = computed(() =>
  props.node.body.split('\n').filter(line => line.trim() !== '')
)

function addChild() {
  props.node.children.push(makeNode('New Node', ''))
  props.node.collapsed = false
}

function removeChild(id) {
  const index = props.node.children.findIndex(child => child.id === id)
  if (index !== -1) props.node.children.splice(index, 1)
}

// ── Body editing (bullet list <-> textarea) ──────────────────────────────────
const editingBody = ref(false)
const draftBody = ref('')
const bodyTextarea = ref(null)

async function startEditingBody() {
  draftBody.value = props.node.body
  editingBody.value = true
  await nextTick()
  bodyTextarea.value?.focus()
}

function commitBody() {
  props.node.body = draftBody.value
  editingBody.value = false
}

function cancelEditingBody() {
  editingBody.value = false
  bodyTextarea.value?.blur()
}
</script>

<style scoped>
.mindmap-node {
  display: flex;
  flex-direction: column;
  align-items: center;
  list-style: none;
  position: relative;
  padding: 2rem 0.75rem 0 0.75rem;
}

/* left/right halves of the horizontal bar connecting this node to its siblings */
.mindmap-node::before,
.mindmap-node::after {
  content: '';
  position: absolute;
  top: 0;
  width: 50%;
  height: 2rem;
}
.mindmap-node::before {
  right: 50%;
  border-top: 1px solid var(--border-purple);
}
.mindmap-node::after {
  left: 50%;
  border-top: 1px solid var(--border-purple);
  border-left: 1px solid var(--border-purple);
}

.mindmap-node:first-child::before { border: none; }
.mindmap-node:last-child::after { border: none; }
.mindmap-node:last-child::before { border-right: 1px solid var(--border-purple); }

.mindmap-node:only-child { padding-top: 0; }
.mindmap-node:only-child::before,
.mindmap-node:only-child::after {
  display: none;
}

.node-card {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 0.75rem 0.85rem;
  min-width: 220px;
  max-width: 320px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.15);
  transition: border-color 0.2s;
}
.node-card:hover {
  border-color: var(--border-purple);
}

.collapse-btn {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  padding: 0.15rem;
  border-radius: 4px;
  font-size: 0.85rem;
  flex-shrink: 0;
  margin-top: 0.15rem;
  transition: color 0.15s, background 0.15s;
}
.collapse-btn:hover {
  color: var(--color-purple);
  background: var(--bg-hover);
}

.node-content {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.node-header-input {
  background: transparent;
  border: 1px solid transparent;
  border-radius: 6px;
  color: var(--text-primary);
  font-size: 0.95rem;
  font-weight: 600;
  padding: 0.2rem 0.35rem;
  width: 100%;
  min-width: 0;
  transition: border-color 0.2s;
}
.node-header-input:hover { border-color: var(--border-color); }
.node-header-input:focus { border-color: var(--color-purple); }

.node-body-list {
  list-style: disc;
  margin: 0;
  padding: 0.2rem 0.35rem 0.2rem 1.1rem;
  border: 1px solid transparent;
  border-radius: 6px;
  cursor: text;
  color: var(--text-secondary);
  font-size: 0.85rem;
  transition: border-color 0.2s;
}
.node-body-list:hover { border-color: var(--border-color); }

.body-placeholder {
  color: var(--text-dim);
  font-style: italic;
  list-style: none;
  margin-left: -1.1rem;
}

.node-body-textarea {
  background: transparent;
  border: 1px solid var(--color-purple);
  border-radius: 6px;
  color: var(--text-secondary);
  font-size: 0.85rem;
  font-family: inherit;
  padding: 0.2rem 0.35rem;
  width: 100%;
  min-height: 3.5rem;
  resize: vertical;
}

.node-actions {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  opacity: 0;
  transition: opacity 0.15s;
  flex-shrink: 0;
}
.node-card:hover .node-actions { opacity: 1; }

.node-action-btn {
  background: transparent;
  border: none;
  color: var(--text-dim);
  cursor: pointer;
  padding: 0.25rem;
  border-radius: 4px;
  font-size: 0.8rem;
  transition: color 0.15s, background 0.15s;
}
.node-action-btn:hover { color: var(--text-secondary); background: var(--bg-active); }
.node-delete-btn:hover { color: var(--color-pink-light); background: rgba(236, 72, 153, 0.12); }

.mindmap-children {
  display: flex;
  align-items: flex-start;
  justify-content: center;
  list-style: none;
  margin: 0;
  padding-top: 2rem;
  position: relative;
}

.mindmap-children::before {
  content: '';
  position: absolute;
  top: 0;
  left: 50%;
  width: 0;
  height: 2rem;
  border-left: 1px solid var(--border-purple);
  transform: translateX(-50%);
}
</style>
