<template>
  <div class="login-page">
    <div class="login-card">
      <div class="login-header">
        <span class="logo-text"><span class="logo-at">@</span>potheca</span>
        <p class="login-subtitle">Sign in to your account</p>
      </div>

      <div class="login-buttons">
        <button class="provider-btn provider-btn--google" @click="handleGoogle" :disabled="busy">
          <svg class="provider-icon" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
            <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
            <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05"/>
            <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
          </svg>
          Continue with Google
        </button>

        <button class="provider-btn provider-btn--microsoft" @click="handleMicrosoft" :disabled="busy">
          <svg class="provider-icon" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M11.4 2H2v9.4h9.4V2z" fill="#F25022"/>
            <path d="M22 2h-9.4v9.4H22V2z" fill="#7FBA00"/>
            <path d="M11.4 12.6H2V22h9.4v-9.4z" fill="#00A4EF"/>
            <path d="M22 12.6h-9.4V22H22v-9.4z" fill="#FFB900"/>
          </svg>
          Continue with Microsoft
        </button>
      </div>

      <div class="divider"><span>or</span></div>

      <form v-if="!isForgotPassword" class="email-form" @submit.prevent="handleEmail">
        <input
          v-model="email"
          type="email"
          placeholder="Email address"
          class="email-input"
          autocomplete="email"
          :disabled="busy"
          required
        />
        <input
          v-model="password"
          type="password"
          placeholder="Password"
          class="email-input"
          :autocomplete="isRegistering ? 'new-password' : 'current-password'"
          :disabled="busy"
          required
        />
        <button
          v-if="!isRegistering"
          type="button"
          class="forgot-link"
          @click="isForgotPassword = true"
          :disabled="busy"
        >Forgot password?</button>
        <button type="submit" class="provider-btn email-submit-btn" :disabled="busy">
          {{ isRegistering ? 'Create account' : 'Sign in with Email' }}
        </button>
      </form>

      <form v-else class="email-form" @submit.prevent="handleForgotPassword">
        <p class="forgot-instructions">Enter your email and we'll send you a reset link.</p>
        <input
          v-model="email"
          type="email"
          placeholder="Email address"
          class="email-input"
          autocomplete="email"
          :disabled="busy"
          required
        />
        <button type="submit" class="provider-btn email-submit-btn" :disabled="busy">
          Send reset email
        </button>
        <button type="button" class="toggle-link back-link" @click="isForgotPassword = false" :disabled="busy">
          ← Back to sign in
        </button>
      </form>

      <p v-if="!isForgotPassword" class="toggle-mode">
        {{ isRegistering ? 'Already have an account?' : "Don't have an account?" }}
        <button class="toggle-link" @click="isRegistering = !isRegistering" :disabled="busy">
          {{ isRegistering ? 'Sign in' : 'Create one' }}
        </button>
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../../composables/useAuth'

const { loginWithGoogle, loginWithMicrosoft, loginWithEmail, registerWithEmail, sendPasswordReset } = useAuth()
const router = useRouter()
const busy = ref(false)
const email = ref('')
const password = ref('')
const isRegistering = ref(false)
const isForgotPassword = ref(false)

async function handleGoogle() {
  busy.value = true
  try {
    await loginWithGoogle()
    router.push('/notes')
  } catch {
    // error already shown via toast
  } finally {
    busy.value = false
  }
}

async function handleMicrosoft() {
  busy.value = true
  try {
    await loginWithMicrosoft()
    router.push('/notes')
  } catch {
    // error already shown via toast
  } finally {
    busy.value = false
  }
}

async function handleEmail() {
  busy.value = true
  try {
    if (isRegistering.value) {
      await registerWithEmail(email.value, password.value)
    } else {
      await loginWithEmail(email.value, password.value)
    }
    router.push('/notes')
  } catch {
    // error already shown via toast
  } finally {
    busy.value = false
  }
}

async function handleForgotPassword() {
  busy.value = true
  try {
    await sendPasswordReset(email.value)
    isForgotPassword.value = false
  } catch {
    // error already shown via toast
  } finally {
    busy.value = false
  }
}
</script>

<style scoped>
.login-page {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
}

.login-card {
  width: 100%;
  max-width: 520px;
  background: var(--bg-card);
  border: 1px solid var(--border-purple);
  border-radius: 20px;
  padding: 3.5rem 3rem;
  display: flex;
  flex-direction: column;
  gap: 2.5rem;
  box-shadow: 0 0 40px var(--glow-purple), 0 0 0 1px var(--border-purple);
}

.login-header {
  text-align: center;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.logo-text {
  font-size: 2.2rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  background: var(--gradient-brand);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.logo-at {
  font-style: italic;
  font-weight: 900;
}

.login-subtitle {
  color: var(--text-secondary);
  font-size: 0.95rem;
  margin: 0;
}

.login-buttons {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.provider-btn {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  padding: 0.75rem 1.25rem;
  border-radius: 10px;
  font-size: 0.95rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  letter-spacing: 0.02em;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
}

.provider-btn:hover:not(:disabled) {
  background: var(--bg-hover);
  border-color: var(--border-purple);
}

.provider-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.provider-icon {
  width: 20px;
  height: 20px;
  flex-shrink: 0;
}

.divider {
  display: flex;
  align-items: center;
  gap: 1rem;
  color: var(--text-muted);
  font-size: 0.85rem;
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: var(--border-color);
}

.email-form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.email-input {
  width: 100%;
  padding: 0.75rem 1.25rem;
  border-radius: 10px;
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 0.95rem;
  outline: none;
  transition: border-color 0.2s ease;
  box-sizing: border-box;
}

.email-input::placeholder {
  color: var(--text-muted);
}

.email-input:focus {
  border-color: var(--border-purple);
}

.email-input:disabled {
  opacity: 0.5;
}

.email-submit-btn {
  justify-content: center;
  margin-top: 0.25rem;
}

.toggle-mode {
  text-align: center;
  color: var(--text-secondary);
  font-size: 0.875rem;
  margin: 0;
}

.toggle-link {
  background: none;
  border: none;
  padding: 0;
  color: var(--color-purple, #a855f7);
  font-size: 0.875rem;
  cursor: pointer;
  text-decoration: underline;
  text-underline-offset: 2px;
}

.toggle-link:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.forgot-link {
  background: none;
  border: none;
  padding: 0;
  color: var(--text-muted);
  font-size: 0.8rem;
  cursor: pointer;
  text-align: right;
  align-self: flex-end;
}

.forgot-link:hover {
  color: var(--text-secondary);
}

.forgot-link:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.forgot-instructions {
  color: var(--text-secondary);
  font-size: 0.875rem;
  margin: 0;
}

.back-link {
  text-align: center;
  text-decoration: none;
  color: var(--text-muted);
  font-size: 0.875rem;
}

.back-link:hover {
  color: var(--text-secondary);
}
</style>
