import { createRouter, createWebHistory } from 'vue-router'
import NotesView from '../views/NotesView.vue'
import TasksView from '../views/TasksView.vue'

const routes = [
  { path: '/', redirect: '/notes' },
  { path: '/notes', component: NotesView },
  { path: '/tasks', component: TasksView }
]

export default createRouter({
  history: createWebHistory(),
  routes
})
