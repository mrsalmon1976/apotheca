import { ref } from 'vue'

const STORAGE_KEY = 'apotheca-theme'
const isDark = ref(localStorage.getItem(STORAGE_KEY) !== 'light')

function applyTheme(dark) {
  document.documentElement.classList.toggle('app-dark', dark)
}

applyTheme(isDark.value)

export function useTheme() {
  function toggleTheme() {
    isDark.value = !isDark.value
    applyTheme(isDark.value)
    localStorage.setItem(STORAGE_KEY, isDark.value ? 'dark' : 'light')
  }
  return { isDark, toggleTheme }
}
