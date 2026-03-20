import { createRouter, createWebHistory } from 'vue-router'
import { auth } from '../firebase'
import PublicLayout from '../layouts/PublicLayout.vue'
import AppLayout from '../layouts/AppLayout.vue'
import HomeView from '../features/home/HomeView.vue'
import AboutView from '../features/about/AboutView.vue'
import FeaturesView from '../features/features/FeaturesView.vue'
import NotesView from '../features/notes/NotesView.vue'
import TasksView from '../features/tasks/TasksView.vue'
import LoginView from '../features/auth/LoginView.vue'

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
      { path: 'login', component: LoginView },
    ]
  },
  {
    path: '/',
    component: AppLayout,
    meta: { requiresAuth: true },
    children: [
      { path: 'notes', component: NotesView },
      { path: 'tasks', component: TasksView }
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
      next('/login')
    }
  })
})

export default router
