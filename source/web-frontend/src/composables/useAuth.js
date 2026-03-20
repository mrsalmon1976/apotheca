import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import {
  GoogleAuthProvider,
  OAuthProvider,
  signInWithPopup,
  signOut,
  onAuthStateChanged,
} from 'firebase/auth'
import { auth } from '../firebase'

const user = ref(null)
const loading = ref(true)

onAuthStateChanged(auth, (u) => {
  user.value = u
  loading.value = false
})

export function useAuth() {
  const toast = useToast()

  async function loginWithGoogle() {
    const provider = new GoogleAuthProvider()
    provider.addScope('email')
    provider.addScope('profile')
    await signInWithPopup(auth, provider)
      .catch((error) => {
        console.error("Firebase Auth Error:", error.code, error.message)
        toast.add({ severity: 'error', summary: 'Sign-in failed', detail: error.message, life: 15000 })
      })
  }

  async function loginWithMicrosoft() {
    const provider = new OAuthProvider('microsoft.com')
    await signInWithPopup(auth, provider)
  }

  async function logout() {
    await signOut(auth)
  }

  return { user, loading, loginWithGoogle, loginWithMicrosoft, logout }
}
