// src/stores/auth.ts
import { reactive, computed } from 'vue'

type User = { id: number; username: string } | null

const state = reactive({
  accessToken: '' as string,       // 只放在記憶體
  user: null as User,
})

export function useAuth() {
  const isAuthed = computed(() => !!state.accessToken && !!state.user)

  function setAuth(token: string, user: User) {
    state.accessToken = token
    state.user = user
  }
  function clearAuth() {
    state.accessToken = ''
    state.user = null
  }

  return { state, isAuthed, setAuth, clearAuth }
}
