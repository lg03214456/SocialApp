

// src/stores/auth.ts
import { reactive, computed } from 'vue'
type User = { id: number; username: string; userId?: string; email?: string } | null

const state = reactive({
  accessToken: '' as string,
  user: null as User,
})
export function useAuth() {
  const isAuthed = computed(() => !!state.accessToken && !!state.user)
  const setAuth = (token: string, user: User) => { state.accessToken = token; state.user = user }
  const clearAuth = () => { state.accessToken = ''; state.user = null }
  return { state, isAuthed, setAuth, clearAuth }
}
