import { ref } from 'vue'
import { useToast } from 'primevue/usetoast'
import {
  GoogleAuthProvider,
  OAuthProvider,
  signInWithPopup,
  signInWithEmailAndPassword,
  createUserWithEmailAndPassword,
  sendPasswordResetEmail,
  signOut,
  onAuthStateChanged,
} from 'firebase/auth'
import { auth } from '../firebase'

function firebaseMessage(error) {
  return (EMAIL_ERRORS[error.code] ?? error.message).replace(/^Firebase:\s*/i, '')
}

const EMAIL_ERRORS = {
  'auth/user-not-found': 'No account found with this email.',
  'auth/wrong-password': 'Incorrect password.',
  'auth/invalid-credential': 'Incorrect email or password.',
  'auth/email-already-in-use': 'An account with this email already exists.',
  'auth/invalid-email': 'Invalid email address.',
  'auth/weak-password': 'Password must be at least 6 characters.',
}

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
        toast.add({ severity: 'error', summary: 'Sign-in failed', detail: firebaseMessage(error), life: 15000 })
      })
  }

  async function loginWithMicrosoft() {
    const provider = new OAuthProvider('microsoft.com')
    await signInWithPopup(auth, provider)
  }

  async function loginWithEmail(email, password) {
    await signInWithEmailAndPassword(auth, email, password)
      .catch((error) => {
        const message = firebaseMessage(error)
        console.error("Firebase Auth Error:", error.code, error.message)
        toast.add({ severity: 'error', summary: 'Sign-in failed', detail: message, life: 15000 })
        throw error
      })
  }

  async function registerWithEmail(email, password) {
    await createUserWithEmailAndPassword(auth, email, password)
      .catch((error) => {
        const message = firebaseMessage(error)
        console.error("Firebase Auth Error:", error.code, error.message)
        toast.add({ severity: 'error', summary: 'Registration failed', detail: message, life: 15000 })
        throw error
      })
  }

  async function sendPasswordReset(email) {
    await sendPasswordResetEmail(auth, email)
      .then(() => {
        toast.add({ severity: 'success', summary: 'Email sent', detail: `A password reset link has been sent to ${email}`, life: 15000 })
      })
      .catch((error) => {
        const message = firebaseMessage(error)
        console.error("Firebase Auth Error:", error.code, error.message)
        toast.add({ severity: 'error', summary: 'Reset failed', detail: message, life: 15000 })
        throw error
      })
  }

  async function logout() {
    await signOut(auth)
  }

  return { user, loading, loginWithGoogle, loginWithMicrosoft, loginWithEmail, registerWithEmail, sendPasswordReset, logout }
}
