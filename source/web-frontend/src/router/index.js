import { createRouter, createWebHistory } from 'vue-router'
import PublicLayout from '../layouts/PublicLayout.vue'
import AppLayout from '../layouts/AppLayout.vue'
import HomeView from '../features/home/HomeView.vue'
import AboutView from '../features/about/AboutView.vue'
import FeaturesView from '../features/features/FeaturesView.vue'
import NotesView from '../features/notes/NotesView.vue'
import TasksView from '../features/tasks/TasksView.vue'

const routes = [
  {
    path: '/',
    redirect: '/home'
  },
  {
    path: '/',
    component: PublicLayout,
    children: [
      { path: 'home', component: HomeView },
      { path: 'features', component: FeaturesView },
      { path: 'about', component: AboutView }
    ]
  },
  {
    path: '/',
    component: AppLayout,
    children: [
      { path: 'notes', component: NotesView },
      { path: 'tasks', component: TasksView }
    ]
  }
]

export default createRouter({
  history: createWebHistory(),
  routes
})
