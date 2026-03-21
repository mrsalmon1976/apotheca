<template>
  <div class="logging-in-page">
    <div class="logging-in-card">
      <span class="logo-text"><span class="logo-at">@</span>potheca</span>

      <div v-if="error" class="error-state">
        <p class="error-message">{{ error }}</p>
        <button class="back-btn" @click="router.replace('/auth/login')">Back to sign in</button>
      </div>

      <div v-else class="loading-state">
        <div class="spinner" />
        <p class="loading-text">Verifying your account…</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../../composables/useAuth'

const { user, loading } = useAuth()
const router = useRouter()
const error = ref(null)
const verifying = ref(false)

const API_URL = import.meta.env.VITE_API_URL ?? 'https://localhost:6060'

watch(
  [loading, user],
  async ([isLoading, currentUser]) => {
    if (isLoading || verifying.value) return
    verifying.value = true

    if (!currentUser) {
      router.replace('/auth/login')
      return
    }

    try {
      const idToken = await currentUser.getIdToken()
      const response = await fetch(`${API_URL}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ idToken }),
      })

      if (!response.ok) {
        const data = await response.json().catch(() => ({}))
        throw new Error(data.error ?? 'Account verification failed. Please try again.')
      }

      router.replace('/notes')
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Something went wrong. Please try again.'
      verifying.value = false
    }
  },
  { immediate: true }
)
</script>

<style scoped>
.logging-in-page {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
}

.logging-in-card {
  width: 100%;
  max-width: 420px;
  background: var(--bg-card);
  border: 1px solid var(--border-purple);
  border-radius: 20px;
  padding: 3.5rem 3rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2.5rem;
  box-shadow: 0 0 40px var(--glow-purple), 0 0 0 1px var(--border-purple);
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

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.25rem;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 3px solid var(--border-color);
  border-top-color: var(--color-purple, #a855f7);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.loading-text {
  color: var(--text-secondary);
  font-size: 0.95rem;
  margin: 0;
}

.error-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.25rem;
  width: 100%;
}

.error-message {
  color: #f87171;
  font-size: 0.9rem;
  text-align: center;
  margin: 0;
  padding: 0.75rem 1rem;
  background: rgba(248, 113, 113, 0.08);
  border: 1px solid rgba(248, 113, 113, 0.25);
  border-radius: 10px;
  width: 100%;
  box-sizing: border-box;
}

.back-btn {
  background: none;
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 0.65rem 1.5rem;
  color: var(--text-secondary);
  font-size: 0.875rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.back-btn:hover {
  border-color: var(--border-purple);
  color: var(--text-primary);
}
</style>
