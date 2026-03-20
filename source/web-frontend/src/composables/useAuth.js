import { ref } from 'vue'
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
  async function loginWithGoogle() {
    const provider = new GoogleAuthProvider()
    await signInWithPopup(auth, provider)
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
