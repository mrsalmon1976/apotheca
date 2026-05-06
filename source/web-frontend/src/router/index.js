import { createRouter, createWebHistory } from 'vue-router'
import { auth } from '../firebase'
import PublicLayout from '../layouts/PublicLayout.vue'
import AppLayout from '../layouts/AppLayout.vue'
import HomeView from '../features/home/HomeView.vue'
import AboutView from '../features/about/AboutView.vue'
import FeaturesView from '../features/features/FeaturesView.vue'
import DashboardView from '../features/dashboard/DashboardView.vue'
import TasksView from '../features/tasks/TasksView.vue'
import ProjectView from '../features/projects/ProjectView.vue'
import NotesView from '../features/notes/NotesView.vue'
import NoteView from '../features/notes/NoteView.vue'
import DocumentsView from '../features/documents/DocumentsView.vue'
import DocumentView from '../features/documents/DocumentView.vue'
import ProjectSettingsView from '../features/projects/ProjectSettingsView.vue'
import SearchView from '../features/search/SearchView.vue'
import LoginView from '../features/auth/LoginView.vue'
import LoggingInView from '../features/auth/LoggingInView.vue'

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
      { path: 'about', component: AboutView },
      { path: 'auth/login', component: LoginView },
      { path: 'logging-in', component: LoggingInView },
    ]
  },
  {
    path: '/',
    component: AppLayout,
    meta: { requiresAuth: true },
    children: [
      { path: 'dashboard', component: DashboardView },
      { path: 'project/:id', component: ProjectView },
      { path: 'project/:id/notes', component: NotesView },
      { path: 'project/:id/notes/f/:folders(.*)*', component: NotesView },
      { path: 'project/:id/notes/:noteId', component: NoteView },
      { path: 'project/:id/documents', component: DocumentsView },
      { path: 'project/:id/documents/f/:folders(.*)*', component: DocumentsView },
      { path: 'project/:id/documents/:documentId', component: DocumentView },
      { path: 'tasks', redirect: '/tasks/all' },
      { path: 'tasks/:filter', component: TasksView },
      { path: 'project/:id/tasks', redirect: to => `/project/${to.params.id}/tasks/all` },
      { path: 'project/:id/tasks/:filter', component: TasksView },
      { path: 'project/:id/settings', component: ProjectSettingsView },
      { path: 'search', component: SearchView }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to, from, next) => {
  if (!to.meta.requiresAuth) return next()

  // Wait for Firebase to resolve the initial auth state
  const unsubscribe = auth.onAuthStateChanged((user) => {
    unsubscribe()
    if (user) {
      next()
    } else {
      next('/auth/login')
    }
  })
})

export default router
